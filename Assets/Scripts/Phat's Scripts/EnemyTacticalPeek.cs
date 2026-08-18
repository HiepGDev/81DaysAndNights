using UnityEngine;
using UnityEngine.AI;

public class EnemyTacticalPeek : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyShooting shooting;
    private EnemyDetection detection;
    private EnemyBehaviorAgent behaviorAgent;
    
    [SerializeField] private EnemySO enemyData;

    [Header("Peek Settings")]
    [SerializeField] private float peekDistance = 0.7f; 

    private Vector3 originalCoverPos;
    private Vector3 peekPos;
    private bool isCurrentlyPeeking = false;
    private bool hasArrivedAtPeek = false;
    private float nextPeekTime = 0f;
    private float peekEndTime = 0f;
    private Transform currentTarget;

    public bool IsPeeking => isCurrentlyPeeking;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        shooting = GetComponent<EnemyShooting>();
        detection = GetComponent<EnemyDetection>();
        behaviorAgent = GetComponent<EnemyBehaviorAgent>();

        // THE TRIGGER FIX: Allow agent to get closer than the shooting threshold
        if (agent != null) agent.stoppingDistance = 0.1f;

        if (enemyData != null)
        {
            peekDistance = enemyData.peekDistance;
        }
    }

    private void Update()
    {
        if (behaviorAgent == null || !behaviorAgent.IsInCover) 
        {
            if (isCurrentlyPeeking) isCurrentlyPeeking = false;
            // THE LOCK FIX: If we aren't at a wall, the safety MUST be OFF.
            if (shooting != null) shooting.allowFiring = true;
            return;
        }

        if (shooting == null || agent == null) return;

        // THE AMBUSH FIX: Determine if we have a target (Sensor OR Ambush)
        bool hasTarget = (detection != null && detection.IsTargetDetected) || 
                         (behaviorAgent.currentMode == EnemyBehaviorAgent.EnemyMode.Ambush && behaviorAgent.CurrentAmbushTarget != null);

        // Keep target reference to avoid aborting peek while moving behind the wall
        bool hasPhysicalTarget = (currentTarget != null && currentTarget.gameObject.activeInHierarchy);

        // TRIGGER PEEK: Ammo full and ready and we see someone and cooldown has expired
        if (!shooting.IsOutOfAmmo && !shooting.IsReloading && !isCurrentlyPeeking && hasTarget && Time.time >= nextPeekTime)
        {
            // If the Agent is stopped at the hide spot, start the peek
            if (agent.isStopped || agent.remainingDistance < 0.4f)
            {
                StartPeek();
            }
        }

        // TRIGGER STOP: Empty, physical target lost completely, or peek duration expired
        if (isCurrentlyPeeking && (shooting.IsOutOfAmmo || !hasPhysicalTarget || Time.time >= peekEndTime))
        {
            ReturnToCover();
        }

        // 3. SHOOT CONTROL: 
        if (isCurrentlyPeeking)
        {
            if (!hasArrivedAtPeek)
            {
                // THE FIX: Use flat distance (ignore Y) so ground height doesn't break the trigger
                Vector3 flatSelf = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flatTarget = new Vector3(peekPos.x, 0, peekPos.z);
                float distToPeek = Vector3.Distance(flatSelf, flatTarget);

                bool hasLos = (detection != null && detection.IsTargetDetected);

                // Arrive if very close, OR if reasonably close and we already have line of sight to target (e.g. when blocked/crowded)
                if (distToPeek <= 0.25f || (distToPeek <= 0.6f && hasLos))
                {
                    hasArrivedAtPeek = true;
                    Debug.Log($"[Tactical] Arrived at Peek position. Distance left: {distToPeek:F2}m. Has Line-of-Sight: {hasLos}");
                }
            }

            // Check if facing the target before firing to avoid shooting into walls sideways
            bool isFacingTarget = false;
            Transform target = (detection != null && detection.CurrentTarget != null) ? detection.CurrentTarget : 
                              (behaviorAgent != null ? behaviorAgent.CurrentAmbushTarget : null);
            if (target != null)
            {
                Vector3 dirToTarget = (target.position - transform.position);
                dirToTarget.y = 0;
                float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
                isFacingTarget = (angle <= 30f);
            }

            shooting.allowFiring = hasArrivedAtPeek && isFacingTarget;
        }
        else
        {
            hasArrivedAtPeek = false;
            shooting.allowFiring = false;
        }
    }

    private void StartPeek()
    {
        currentTarget = (detection != null && detection.CurrentTarget != null) ? detection.CurrentTarget : 
                        (behaviorAgent != null ? behaviorAgent.CurrentAmbushTarget : null);
        if (currentTarget == null && behaviorAgent != null) currentTarget = behaviorAgent.PlayerTransform; // Fallback to player

        Transform target = currentTarget;

        // Use the master safe position from the behavior agent
        Vector3 safeHome = behaviorAgent.ActiveCover.position;
        Vector3 edgeDirection = behaviorAgent.ActiveCover.lookDirection;
        
        // Diagonal step: move sideways towards corner (edgeDirection) and forward towards player (dirToPlayer)
        Vector3 dirToPlayer = Vector3.forward;
        if (target != null)
        {
            dirToPlayer = (target.position - safeHome).normalized;
            dirToPlayer.y = 0;
            dirToPlayer.Normalize();
        }

        float forwardPush = 0.5f;
        peekPos = safeHome + (edgeDirection * peekDistance) + (dirToPlayer * forwardPush);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(peekPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            Vector3 oldPeek = peekPos;
            peekPos = hit.position;
            isCurrentlyPeeking = true;
            agent.isStopped = false;
            agent.SetDestination(peekPos);

            float duration = (enemyData != null) ? enemyData.peekDuration : 2.5f;
            peekEndTime = Time.time + duration;

            Debug.Log($"[Tactical] StartPeek: Home={safeHome}, TargetDir={dirToPlayer}, RawPeek={oldPeek}, NavPeek={peekPos}, Dist={Vector3.Distance(safeHome, peekPos):F2}m, Duration={duration:F2}s");
        }
        else
        {
            Debug.LogWarning($"[Tactical] StartPeek: Failed to find valid NavMesh position near {peekPos}");
        }
    }

    private void ReturnToCover()
    {
        isCurrentlyPeeking = false;

        float cooldown = (enemyData != null) ? enemyData.peekCooldown : 1.5f;
        nextPeekTime = Time.time + cooldown;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            // Always return to the EXACT master safe spot
            agent.SetDestination(behaviorAgent.ActiveCover.position);
            Debug.Log($"[Tactical] Pulling back to master safe spot. Cooldown: {cooldown}s.");
        }
    }
}
