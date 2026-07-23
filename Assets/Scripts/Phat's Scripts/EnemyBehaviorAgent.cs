using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviorAgent : MonoBehaviour
{
    public enum EnemyMode { Wander, Ambush, Sniper }

    private NavMeshAgent agent;
    private EnemyDetection detection;
    private EnemyShooting shooting;
    private EnemyCover cover;
    private EnemyTacticalPeek peek;
    private Animator animator;
    [SerializeField] private EnemySO enemyData;
    private EnemyHealth enemyHealth;
    private bool hasDecidedLowHealthBehavior = false;
    private bool shouldPushAtLowHealth = false;

    [Header("Ambush Settings")]
    public EnemyMode currentMode = EnemyMode.Wander;
    [SerializeField] private float ambushShootRange = 15f; 
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float idleTime = 2f;

    [Header("Squad Spacing")]
    [SerializeField] private float minEngagementDist = 8.0f; 
    [SerializeField] private float rangeSpread = 3.0f;       
    [SerializeField] private float destinationSpread = 2.0f;
    
    private Transform playerTransform;
    private Vector3 spawnPoint;
    private float idleTimer;
    private bool isIdle = false;
    private bool isInCover = false;
    private bool hasSearchedForCover = false;
    private Vector3 currentTarget; 
    private EnemyCover.CoverPoint activeCover;

    // Spacing and Targeting
    private float personalRangeOffset;
    private float personalFlankingAngle;
    private float targetSearchTimer = 0f;
    private float sniperLogTimer = 0f;

    // Squad Distribution
    private static System.Collections.Generic.Dictionary<int, int> targetAttackers = new System.Collections.Generic.Dictionary<int, int>();
    private int myCurrentTargetID = -1;

    public bool IsReadyToShoot { get; private set; }
    public float CurrentEngagementDist { get; private set; } 
    public Transform CurrentAmbushTarget { get; private set; }
    public Transform PlayerTransform => playerTransform;
    public EnemySO EnemyData => enemyData;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        shooting = GetComponent<EnemyShooting>();
        cover = GetComponent<EnemyCover>();
        peek = GetComponent<EnemyTacticalPeek>();
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (enemyData != null)
        {
            currentMode = enemyData.defaultMode;
            ambushShootRange = enemyData.ambushShootRange;
            wanderRadius = enemyData.wanderRadius;
            idleTime = enemyData.idleTime;
            minEngagementDist = enemyData.minEngagementDist;
            rangeSpread = enemyData.rangeSpread;
            destinationSpread = enemyData.destinationSpread;

            if (detection != null)
            {
                detection.DetectionRadius = enemyData.detectionRadius;
            }
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        if (animator != null) {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        agent.updatePosition = false; 
        agent.updateRotation = true;
        agent.speed = 4.0f;
        agent.acceleration = 30f; 
        agent.angularSpeed = 600f; 
        agent.stoppingDistance = 0.5f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Initialize personal spacing
        personalRangeOffset = Random.Range(-rangeSpread, rangeSpread);
        personalFlankingAngle = Random.Range(-destinationSpread * 10f, destinationSpread * 10f);
    }

    private void Start()
    {
        spawnPoint = transform.position;
        if (agent.isOnNavMesh) agent.Warp(transform.position);
        StartIdle();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;
        transform.position = agent.nextPosition;

        IsReadyToShoot = false;

        // 1. RE-FIND PLAYER REFERENCE
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // 2. TACTICAL TARGETING: Hunt closest person (Optimized & Shared)
        bool isAmbushing = (currentMode == EnemyMode.Ambush);
        bool isCombatUnit = (currentMode == EnemyMode.Ambush || currentMode == EnemyMode.Sniper);
        if (isCombatUnit)
        {
            targetSearchTimer -= Time.deltaTime;
            if (targetSearchTimer <= 0 || CurrentAmbushTarget == null || !IsAlive(CurrentAmbushTarget.gameObject))
            {
                CurrentAmbushTarget = GetClosestTarget();
                targetSearchTimer = 0.5f; 
            }
        }

        bool isDetected = (isCombatUnit && CurrentAmbushTarget != null) || (detection != null && detection.IsTargetDetected);
        Transform target = isCombatUnit ? CurrentAmbushTarget : (detection != null ? detection.CurrentTarget : null);

        if (!isDetected)
        {
            hasSearchedForCover = false;
        }

        float distToTarget = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;

        // Sniper periodic status logging
        if (currentMode == EnemyMode.Sniper)
        {
            sniperLogTimer -= Time.deltaTime;
            if (sniperLogTimer <= 0f)
            {
                sniperLogTimer = 2.0f;
                Debug.Log($"[Sniper Log] GameObject: {gameObject.name}, Target: {(target != null ? target.name : "None")}, isDetected: {isDetected}, isInCover: {isInCover}, coverFound: {activeCover.found}, distance: {(target != null ? Vector3.Distance(transform.position, target.position).ToString("F1") : "N/A")}m, agent.isStopped: {agent.isStopped}");
            }
        }

        float maxGunRange = (shooting != null ? shooting.FireDistance : 25f);
        float baseRange = isAmbushing ? ambushShootRange : maxGunRange;
        
        CurrentEngagementDist = Mathf.Clamp(baseRange + personalRangeOffset, minEngagementDist, maxGunRange - 2.0f);
        
        // Hysteresis buffer: extend shooting range slightly if already shooting to prevent boundary flickering
        bool currentlyShooting = (shooting != null && shooting.IsShootingInProgress);
        float rangeThreshold = currentlyShooting ? (CurrentEngagementDist + 2.0f) : CurrentEngagementDist;
        bool inShootingRange = isDetected && distToTarget <= rangeThreshold;

        // 3. ANIMATION CONTROL
        if (animator != null)
        {
            float intentSpeed = agent.isStopped ? 0f : agent.desiredVelocity.magnitude;
            bool isActuallyReloading = (shooting != null && shooting.IsReloading);
            bool isMovingToCover = IsMovingToCover;

            if (isActuallyReloading && !isMovingToCover)
            {
                if (!agent.isStopped) StopAgent();
            }

            if (!isActuallyReloading || isMovingToCover)
            {
                bool shouldBeRunning = intentSpeed > 2.0f;
                if (HasParameter("isRunning", animator))
                    animator.SetBool("isRunning", shouldBeRunning);

                if (HasParameter("Speed", animator))
                {
                    float finalSpeed = shouldBeRunning ? 0.5f : intentSpeed; 
                    animator.SetFloat("Speed", finalSpeed);
                }
            }
            else
            {
                if (HasParameter("isRunning", animator)) animator.SetBool("isRunning", false);
                if (HasParameter("Speed", animator)) animator.SetFloat("Speed", 0f);
            }
        }

        // 4. THE BRAIN (PRIORITY SYSTEM)

        if (shooting != null && shooting.IsOutOfAmmo)
        {
            if (!isInCover) FindAndGoToCover();
            else HandleCoverLogic(inShootingRange);
            return;
        }

        // Reset low health decision when target is lost
        if (!isDetected)
        {
            hasDecidedLowHealthBehavior = false;
        }

        // Low health decision branch
        if (enemyHealth != null && isDetected && target != null && enemyHealth.CurrentHealth <= enemyHealth.MaxHealth * 0.3f)
        {
            if (!hasDecidedLowHealthBehavior)
            {
                hasDecidedLowHealthBehavior = true;
                float pushWeight = (enemyData != null) ? enemyData.pushProbability : 0.5f;
                shouldPushAtLowHealth = (UnityEngine.Random.value <= pushWeight);
            }

            if (!shouldPushAtLowHealth)
            {
                // Run away to cover
                if (!isInCover) FindAndGoToCover();
                else HandleCoverLogic(inShootingRange);
                return;
            }
            else
            {
                // Push/rush the player!
                if (agent.isStopped) agent.isStopped = false;
                agent.SetDestination(target.position);
                FaceTarget();
                if (distToTarget <= maxGunRange)
                {
                    IsReadyToShoot = true;
                }
                return;
            }
        }

        if (currentMode == EnemyMode.Sniper && isDetected && target != null)
        {
            if (!isInCover && !hasSearchedForCover)
            {
                hasSearchedForCover = true;
                TryFindSniperCover(target.position);
            }

            if (isInCover)
            {
                HandleCoverLogic(inShootingRange);
            }
            else
            {
                if (!agent.isStopped) StopAgent();
                FaceTarget();
                IsReadyToShoot = true;
            }
            return;
        }

        if (isInCover)
        {
            HandleCoverLogic(inShootingRange);
            return;
        }

        if (isDetected && target != null)
        {
            if (!inShootingRange)
            {
                if (agent.isStopped) agent.isStopped = false;
                
                // Determine flanking angle dynamically based on evolved weights
                float targetFlankAngle = 0f;
                if (currentMode != EnemyMode.Sniper && enemyData != null)
                {
                    bool shouldFlank = (UnityEngine.Random.value <= enemyData.flankProbability);
                    if (shouldFlank)
                    {
                        // Flank wide: either left 65 degrees or right 65 degrees
                        targetFlankAngle = (UnityEngine.Random.value < 0.5f) ? -65f : 65f;
                    }
                }

                Vector3 dirFromTarget = (transform.position - target.position).normalized;
                if (dirFromTarget == Vector3.zero) dirFromTarget = Vector3.forward;
                Vector3 flankingDir = Quaternion.Euler(0, targetFlankAngle, 0) * dirFromTarget;
                Vector3 tacticalPos = target.position + flankingDir * (CurrentEngagementDist - 1.0f); 

                NavMeshHit hit;
                if (NavMesh.SamplePosition(tacticalPos, out hit, 4.0f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(target.position);
                
                FaceTarget();
            }
            else
            {
                if (!agent.isStopped) StopAgent();
                FaceTarget();
                IsReadyToShoot = true;
            }
            return;
        }

        if (currentMode == EnemyMode.Sniper)
        {
            if (!agent.isStopped) StopAgent();
            return;
        }

        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    public bool IsMovingToCover => isInCover && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
    public bool IsInCover => isInCover;
    public EnemyCover.CoverPoint ActiveCover => activeCover;

    private void FindAndGoToCover()
    {
        bool isAmbushing = (currentMode == EnemyMode.Ambush && CurrentAmbushTarget != null);
        if (cover == null || (detection.CurrentTarget == null && !isAmbushing)) 
        {
            shooting.TriggerReload();
            return;
        }

        Vector3 targetPos = isAmbushing ? CurrentAmbushTarget.position : detection.CurrentTarget.position;
        activeCover = cover.FindNearestCover(targetPos);
        
        if (activeCover.found)
        {
            isInCover = true;
            isIdle = false;
            agent.isStopped = false;
            agent.SetDestination(activeCover.position);
        }
        else
        {
            shooting.TriggerReload();
        }
    }

    private bool TryFindSniperCover(Vector3 targetPos)
    {
        if (cover == null) return false;
        activeCover = cover.FindNearestCover(targetPos);
        
        if (activeCover.found)
        {
            isInCover = true;
            isIdle = false;
            agent.isStopped = false;
            agent.SetDestination(activeCover.position);
            return true;
        }
        return false;
    }

    private void HandleCoverLogic(bool inShootingRange)
    {
        bool isPeeking = (peek != null && peek.IsPeeking);
        float distToCover = Vector3.Distance(transform.position, activeCover.position);

        if (isPeeking || distToCover <= 0.2f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            bool isReloading = (shooting != null && shooting.IsReloading);
            bool isOutOfAmmo = (shooting != null && shooting.IsOutOfAmmo);
            bool shouldCrouch = !isPeeking || isOutOfAmmo;

            if (isPeeking && !isReloading)
            {
                // THE RANGE FIX: If the target moved out of reach while peeking, break cover to chase!
                if (!inShootingRange)
                {
                    ExitCover();
                    return;
                }

                IsReadyToShoot = true;
                FaceTarget();
            }
            else if (shouldCrouch || isReloading)
            {
                StopAgent();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);
            }
            if (!isReloading)
            {
                // Only crossfade to Cover_Crouching if we are NOT out of ammo.
                // If we ARE out of ammo, TriggerReload() will play crouching_reload instead, avoiding double-crossfades.
                if (shouldCrouch && !isOutOfAmmo)
                {
                    if (animator != null && HasParameter("isCovering", animator))
                    {
                        animator.SetBool("isCovering", true);
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Cover_Crouching"))
                            animator.CrossFade("Cover_Crouching", 0.1f);
                    }
                }
                else if (!isOutOfAmmo && !isPeeking)
                {
                    if (!inShootingRange) ExitCover();
                }
            }

            if (shooting != null && isOutOfAmmo && !isReloading)
                shooting.TriggerReload();
        }
    }

    private void ExitCover()
    {
        if (cover != null) cover.ReleaseCover();
        isInCover = false;
        if (animator != null) animator.SetBool("isCovering", false);
        agent.isStopped = false;
        if (activeCover.found)
        {
            Vector3 pushOutDir = (transform.position - activeCover.position).normalized;
            agent.Warp(transform.position + pushOutDir * 0.5f);
        }
    }

    public void FaceTarget()
    {
        bool isAmbushing = (currentMode == EnemyMode.Ambush && CurrentAmbushTarget != null);
        Transform t = isAmbushing ? CurrentAmbushTarget : detection.CurrentTarget;
        if (t != null)
        {
            Vector3 dir = (t.position - transform.position);
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    private bool HasParameter(string n, Animator a)
    {
        foreach (var p in a.parameters) if (p.name == n) return true;
        return false;
    }

    private void HandleWandering()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            StartIdle();
    }

    private void HandleIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0) PickNewWanderPoint();
    }

    private void PickNewWanderPoint()
    {
        if (!agent.isOnNavMesh) return;
        for (int i = 0; i < 10; i++)
        {
            Vector3 rand = spawnPoint + Random.insideUnitSphere * wanderRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(rand, out hit, 5f, NavMesh.AllAreas))
            {
                currentTarget = hit.position;
                isIdle = false;
                agent.isStopped = false;
                agent.SetDestination(currentTarget);
                return;
            }
        }
    }

    private void StartIdle()
    {
        isIdle = true;
        idleTimer = idleTime;
        StopAgent();
    }

    private void StopAgent()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private Transform GetClosestTarget()
    {
        float bestScore = float.MaxValue;
        Transform bestTarget = null;
        string[] tags = (detection != null) ? detection.TargetTags : new string[] { "Player", "Teammate" };

        foreach (string tag in tags)
        {
            GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in potentialTargets)
            {
                if (!IsAlive(obj)) continue;

                float dist = Vector3.Distance(transform.position, obj.transform.position);

                // Limit global targeting by the configured detection radius only for Sniper mode
                if (currentMode == EnemyMode.Sniper)
                {
                    float maxDetectDist = (detection != null) ? detection.DetectionRadius : 15f;
                    if (dist > maxDetectDist) continue;
                }

                int targetID = obj.GetInstanceID();

                int attackers = targetAttackers.ContainsKey(targetID) ? targetAttackers[targetID] : 0;
                float score = dist + (attackers * 10f);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = obj.transform;
                }
            }
        }

        UpdateAttackerCount(bestTarget);
        return bestTarget;
    }

    private void UpdateAttackerCount(Transform newTarget)
    {
        if (myCurrentTargetID != -1 && targetAttackers.ContainsKey(myCurrentTargetID))
            targetAttackers[myCurrentTargetID]--;

        if (newTarget != null)
        {
            myCurrentTargetID = newTarget.gameObject.GetInstanceID();
            if (!targetAttackers.ContainsKey(myCurrentTargetID)) targetAttackers[myCurrentTargetID] = 0;
            targetAttackers[myCurrentTargetID]++;
        }
        else myCurrentTargetID = -1;
    }

    private void OnDisable()
    {
        if (cover != null) cover.ReleaseCover();
        UpdateAttackerCount(null);
    }

    private void OnDestroy() 
    { 
        if (cover != null) cover.ReleaseCover(); 
        UpdateAttackerCount(null); 
    }

    private bool IsAlive(GameObject obj)
    {
        if (obj == null) return false;
        GameObject root = obj.transform.root.gameObject;
        var ph = root.GetComponentInChildren<PlayerHealth>();
        if (ph != null) return true; 
        var th = root.GetComponentInChildren<TeammateHealth>();
        if (th != null) return root.CompareTag("Teammate");
        return true; 
    }
}
