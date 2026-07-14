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
    [SerializeField] private float peekCooldown = 1.0f;
    [SerializeField] private float peekDuration = 3.0f;

    private Vector3 originalCoverPos;
    private Vector3 peekPos;
    private bool isCurrentlyPeeking = false;
    
    private float peekTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isWaitingOnCooldown = false;

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
            peekCooldown = enemyData.peekCooldown;
            peekDuration = enemyData.peekDuration;
        }
    }

    private void Update()
    {
        if (behaviorAgent == null || !behaviorAgent.IsInCover) 
        {
            if (isCurrentlyPeeking) isCurrentlyPeeking = false;
            isWaitingOnCooldown = false;
            // THE LOCK FIX: If we aren't at a wall, the safety MUST be OFF.
            if (shooting != null) shooting.allowFiring = true;
            return;
        }

        if (shooting == null || agent == null) return;

        // Handle Cooldown Timer
        if (isWaitingOnCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= peekCooldown)
            {
                isWaitingOnCooldown = false;
            }
        }

        // THE AMBUSH FIX: Determine if we have a target (Sensor OR Ambush)
        bool hasTarget = (detection != null && detection.IsTargetDetected) || 
                         (behaviorAgent.currentMode == EnemyBehaviorAgent.EnemyMode.Ambush && behaviorAgent.CurrentAmbushTarget != null);

        // TRIGGER PEEK: Ammo full and ready, we see someone, and not waiting on cooldown
        if (!shooting.IsOutOfAmmo && !shooting.IsReloading && !isCurrentlyPeeking && hasTarget && !isWaitingOnCooldown)
        {
            // If the Agent is stopped at the hide spot, start the peek
            if (agent.isStopped || agent.remainingDistance < 0.4f)
            {
                StartPeek();
            }
        }

        // TRIGGER STOP / SHOOT CONTROL: 
        if (isCurrentlyPeeking)
        {
            // THE FIX: Use flat distance (ignore Y) so ground height doesn't break the trigger
            Vector3 flatSelf = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatTarget = new Vector3(peekPos.x, 0, peekPos.z);
            float distToPeek = Vector3.Distance(flatSelf, flatTarget);

            // Allow firing if we are roughly at the peek spot (Loosened to 0.6m)
            bool atPeekSpot = (distToPeek <= 0.6f);
            shooting.allowFiring = atPeekSpot;

            if (atPeekSpot)
            {
                peekTimer += Time.deltaTime;
            }

            // Return to cover if out of ammo, target lost, or peek duration exceeded
            if (shooting.IsOutOfAmmo || !hasTarget || peekTimer >= peekDuration)
            {
                ReturnToCover();
            }
        }
        else
        {
            // ALWAYS pause while sitting behind the wall
            shooting.allowFiring = false;
        }
    }

    private void StartPeek()
    {
        // Use the master safe position from the behavior agent
        Vector3 safeHome = behaviorAgent.ActiveCover.position;
        Vector3 edgeDirection = behaviorAgent.ActiveCover.lookDirection;
        
        peekPos = safeHome + (edgeDirection * peekDistance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(peekPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            peekPos = hit.position;
            isCurrentlyPeeking = true;
            peekTimer = 0f; // Reset peek duration timer
            agent.isStopped = false;
            agent.SetDestination(peekPos);
            Debug.Log("[Tactical] Peeking out...");
        }
    }

    private void ReturnToCover()
    {
        isCurrentlyPeeking = false;
        isWaitingOnCooldown = true;
        cooldownTimer = 0f; // Reset cooldown wait timer
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            // Always return to the EXACT master safe spot
            agent.SetDestination(behaviorAgent.ActiveCover.position);
            Debug.Log("[Tactical] Pulling back to master safe spot.");
        }
    }
}
