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

    [Header("Aiming Settings")]
    [SerializeField] private float aimingOffsetAngle = 0f;
    
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

    private Transform designatedSniperPoint;
    private float pathUpdateTimer = 0f;
    private const float pathUpdateInterval = 0.5f; // Max 2 path recalculations per second
    private Vector3 lastSetDestination = Vector3.positiveInfinity;
    private float losBlockedTimer = 0f;
    private float flankDecisionTimer = 0f;
    private float currentFlankAngle = 0f;
    private float minRunTimer = 0f;

    public void SetDesignatedSniperPoint(Transform point) 
    { 
        designatedSniperPoint = point; 
        if (point != null)
        {
            Vector3 navPos = point.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(point.position, out hit, 3.0f, NavMesh.AllAreas))
            {
                navPos = hit.position;
            }

            // Waypoints are in the open, not treated as physical cover
            isInCover = false;
            hasSearchedForCover = true; // Prevents the AI from searching for other covers

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                SetAgentDestination(navPos, true);
            }
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        shooting = GetComponent<EnemyShooting>();
        cover = GetComponent<EnemyCover>();
        peek = GetComponent<EnemyTacticalPeek>();
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (enemyData == null && AISyncService.Instance != null && AISyncService.Instance.EnemyConfigs != null)
        {
            EnemyMode defaultSearchMode = EnemyMode.Ambush;
            if (gameObject.name.Contains("Sniper") || (shooting != null && shooting.gameObject.name.Contains("Sniper")))
            {
                defaultSearchMode = EnemyMode.Sniper;
            }

            foreach (var so in AISyncService.Instance.EnemyConfigs)
            {
                if (so != null && so.defaultMode == defaultSearchMode)
                {
                    enemyData = so;
                    break;
                }
            }
        }

        if (enemyData != null)
        {
            // Clone the ScriptableObject so each individual enemy has its own unique runtime stats
            enemyData = Instantiate(enemyData);

            if (AISyncService.Instance != null)
            {
                var configs = AISyncService.Instance.GetConfigsForRole(enemyData.defaultMode.ToString());
                if (configs != null && configs.Length > 0)
                {
                    int randIdx = UnityEngine.Random.Range(0, configs.Length);
                    var chosenConfig = configs[randIdx];

                    enemyData.maxHealth = chosenConfig.base_health;
                    enemyData.minSpread = chosenConfig.min_spread;
                    enemyData.maxSpread = chosenConfig.max_spread;
                    enemyData.pushProbability = chosenConfig.push_probability;
                    enemyData.coverProbability = chosenConfig.cover_probability;
                    enemyData.peekCooldown = chosenConfig.peek_cooldown;
                    enemyData.peekDuration = chosenConfig.peek_duration;
                    enemyData.flankProbability = chosenConfig.flank_probability;
                    enemyData.configId = chosenConfig.id;

                    Debug.Log($"[EnemyBehaviorAgent] Assigned Candidate {chosenConfig.id} to cloned SO of {gameObject.name}. HP={enemyData.maxHealth}, MinSpread={enemyData.minSpread}, PushProb={enemyData.pushProbability}");
                }
            }

            // Sync the cloned SO and properties to other components via reflection to ensure they use individual values
            if (enemyHealth != null)
            {
                typeof(EnemyHealth).GetField("enemyData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyHealth, enemyData);
                typeof(EnemyHealth).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyHealth, enemyData.maxHealth);
            }
            if (shooting != null)
            {
                typeof(EnemyShooting).GetField("enemyData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, enemyData);
                typeof(EnemyShooting).GetField("minSpread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, enemyData.minSpread);
                typeof(EnemyShooting).GetField("maxSpread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, enemyData.maxSpread);
                typeof(EnemyShooting).GetField("currentAmmo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, enemyData.magazineSize);
            }
            if (peek != null)
            {
                typeof(EnemyTacticalPeek).GetField("enemyData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(peek, enemyData);
                typeof(EnemyTacticalPeek).GetField("peekDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(peek, enemyData.peekDistance);
            }

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

        agent.updatePosition = true; 
        agent.updateRotation = false;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        agent.avoidancePriority = UnityEngine.Random.Range(30, 70); // Prevent deadlocks by giving agents different avoidance weights
        agent.radius = 0.55f; // Rebalanced radius to block them from narrow steps while letting them cross the bridge
        agent.speed = 4.0f;
        agent.acceleration = 30f; 
        agent.angularSpeed = 600f; 
        agent.stoppingDistance = 0.5f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Initialize personal spacing (ensure at least +/- 3.5m variation to stagger squad vertically along narrow pathways)
        personalRangeOffset = Random.Range(-Mathf.Max(3.5f, rangeSpread), Mathf.Max(3.5f, rangeSpread));
        // Ensure at least +/- 30 degrees lateral spread to fan them out wide in combat
        personalFlankingAngle = Random.Range(-Mathf.Max(30f, destinationSpread * 10f), Mathf.Max(30f, destinationSpread * 10f));
    }

    private void Start()
    {
        spawnPoint = transform.position;
        if (agent.isOnNavMesh) agent.Warp(transform.position);
        StartIdle();
    }

    private void Update()
    {
        if (pathUpdateTimer > 0f) pathUpdateTimer -= Time.deltaTime;
        if (minRunTimer > 0f) minRunTimer -= Time.deltaTime;

        if (!agent.isOnNavMesh) return;

        // Pathing Safety Check: Ensure the agent has a path if they have a designated waypoint and are in cover mode
        if (isInCover && activeCover.found && !agent.hasPath && !agent.pathPending)
        {
            agent.isStopped = false;
            SetAgentDestination(activeCover.position, true);
        }

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
            // Sticky targeting check: only find a new target if the current one is null, dead, or out of range
            bool needsNewTarget = CurrentAmbushTarget == null || !IsAlive(CurrentAmbushTarget.gameObject);
            if (!needsNewTarget && detection != null)
            {
                float distToCurrent = Vector3.Distance(transform.position, CurrentAmbushTarget.position);
                float maxDetectDist = (currentMode == EnemyMode.Sniper) ? detection.DetectionRadius : 25f; // Ambush hunting range
                if (distToCurrent > maxDetectDist) needsNewTarget = true;
            }

            if (needsNewTarget)
            {
                CurrentAmbushTarget = GetClosestTarget();
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
        float rangeThreshold = currentlyShooting ? (CurrentEngagementDist + 5.0f) : CurrentEngagementDist;
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

                if (shouldBeRunning)
                {
                    if (HasParameter("isCrouching", animator))
                        animator.SetBool("isCrouching", false);
                }

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
            if (designatedSniperPoint != null)
            {
                float distToWaypoint = Vector3.Distance(transform.position, designatedSniperPoint.position);
                if (distToWaypoint > agent.stoppingDistance + 0.2f)
                {
                    if (agent.isStopped) agent.isStopped = false;
                    SetAgentDestination(designatedSniperPoint.position);
                    return;
                }

                if (!agent.isStopped) StopAgent();
                shooting.TriggerReload();
                return;
            }

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
                SetAgentDestination(target.position);
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
                if (designatedSniperPoint != null)
                {
                    float distToWaypoint = Vector3.Distance(transform.position, designatedSniperPoint.position);
                    if (distToWaypoint > agent.stoppingDistance + 0.2f)
                    {
                        if (agent.isStopped) agent.isStopped = false;
                        SetAgentDestination(designatedSniperPoint.position);
                        IsReadyToShoot = false;
                        return;
                    }
                }

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
                
                // If they are far away from combat range, just run straight to the target to prevent unnecessary path recalculation halts
                bool isFarAway = distToTarget > (CurrentEngagementDist + 5.0f);
                if (isFarAway)
                {
                    SetAgentDestination(target.position);
                }
                else
                {
                    // Determine flanking angle periodically based on evolved weights to prevent path flickering
                    flankDecisionTimer -= Time.deltaTime;
                    if (flankDecisionTimer <= 0f)
                    {
                        flankDecisionTimer = 2.0f; // Re-evaluate flanking angle every 2 seconds
                        currentFlankAngle = 0f;
                        if (currentMode != EnemyMode.Sniper && enemyData != null)
                        {
                            bool shouldFlank = (UnityEngine.Random.value <= enemyData.flankProbability);
                            if (shouldFlank)
                            {
                                // Pick a continuous random angle across the entire width (from -65 to 65) to distribute them across the center too
                                currentFlankAngle = UnityEngine.Random.Range(-65f, 65f);
                            }
                        }
                    }

                    Vector3 dirFromTarget = (transform.position - target.position).normalized;
                    if (dirFromTarget == Vector3.zero) dirFromTarget = Vector3.forward;
                    
                    // Apply the personal flanking angle offset to spread the squad laterally in a combat arc
                    float finalFlankAngle = currentFlankAngle + personalFlankingAngle;
                    Vector3 flankingDir = Quaternion.Euler(0, finalFlankAngle, 0) * dirFromTarget;
                    Vector3 tacticalPos = target.position + flankingDir * (CurrentEngagementDist - 1.0f); 

                    NavMeshHit hit;
                    bool validTactical = false;
                    if (NavMesh.SamplePosition(tacticalPos, out hit, 4.0f, NavMesh.AllAreas))
                    {
                        tacticalPos = hit.position;
                        
                        // Virtual Line of Sight check: ensure that if we go to this flanking position, 
                        // we will actually have line of sight to the target (so we don't go down steps/under bridges)
                        Vector3 eyeOrigin = tacticalPos + Vector3.up * 1.5f;
                        Vector3 targetCenter = target.position + Vector3.up * 1.0f;
                        Vector3 toTarget = targetCenter - eyeOrigin;
                        float dist = toTarget.magnitude;
                        
                        if (!Physics.Raycast(eyeOrigin, toTarget.normalized, out RaycastHit rayHit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            validTactical = true;
                        }
                        else
                        {
                            if (rayHit.transform.root == target.root || rayHit.transform.root == transform.root)
                            {
                                validTactical = true;
                            }
                        }
                    }

                    if (validTactical)
                        SetAgentDestination(hit.position);
                    else
                        SetAgentDestination(target.position); // Fallback to direct path along the bridge/road
                }
                
                FaceTarget();
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                    minRunTimer = 1.0f; // Force them to run for at least 1.0s
                }
                IsReadyToShoot = false;
            }
            else
            {
                // In range, but check if we have a clear line of sight to shoot
                bool directSight = HasLineOfSight(target);
                if (directSight)
                {
                    losBlockedTimer = 0f; // Reset when sight is clear
                }
                else
                {
                    losBlockedTimer += Time.deltaTime;
                }

                // Only chase if sight has been blocked continuously for at least 0.5 seconds
                bool treatAsBlocked = (losBlockedTimer >= 0.5f);

                // Only stop and shoot if we have sight AND the minimum run duration has finished
                if (!treatAsBlocked && minRunTimer <= 0f)
                {
                    if (!agent.isStopped) StopAgent();
                    FaceTarget();
                    IsReadyToShoot = true;
                }
                else
                {
                    // Blocked or still in minimum run phase: keep running to destination
                    if (agent.isStopped) 
                    {
                        agent.isStopped = false;
                        minRunTimer = 1.0f; // Force run timer if starting to move
                    }
                    SetAgentDestination(target.position);
                    FaceTarget();
                    IsReadyToShoot = false;
                }
            }
            return;
        }

        if (currentMode == EnemyMode.Sniper)
        {
            if (designatedSniperPoint != null)
            {
                float distToWaypoint = Vector3.Distance(transform.position, designatedSniperPoint.position);
                if (distToWaypoint > agent.stoppingDistance + 0.2f)
                {
                    if (agent.isStopped) agent.isStopped = false;
                    SetAgentDestination(designatedSniperPoint.position);
                    return;
                }
            }

            if (!agent.isStopped) StopAgent();
            return;
        }

        if (currentMode == EnemyMode.Ambush)
        {
            float distToSpawn = Vector3.Distance(transform.position, spawnPoint);
            if (distToSpawn > agent.stoppingDistance + 0.2f)
            {
                if (agent.isStopped) agent.isStopped = false;
                SetAgentDestination(spawnPoint);
            }
            else
            {
                if (!agent.isStopped) StopAgent();
            }
            return;
        }

        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    private void LateUpdate()
    {
        if (!agent.isOnNavMesh) return;

        bool isCombatUnit = (currentMode == EnemyMode.Ambush || currentMode == EnemyMode.Sniper);
        bool isDetected = (isCombatUnit && CurrentAmbushTarget != null) || (detection != null && detection.IsTargetDetected);
        Transform target = isCombatUnit ? CurrentAmbushTarget : (detection != null ? detection.CurrentTarget : null);

        // If the agent is physically moving, face the direction of movement to prevent sideways-running/strafing conflicts
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 moveDir = agent.velocity;
            moveDir.y = 0;
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        // Only face the target when standing still to shoot
        else if (isDetected && target != null)
        {
            FaceTarget();
        }
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
            SetAgentDestination(activeCover.position, true);
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
            SetAgentDestination(activeCover.position, true);
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
                // THE PEEK FIX: Only flag ready to shoot if they have stopped moving at the peek edge!
                if (agent.velocity.sqrMagnitude < 0.1f)
                {
                    IsReadyToShoot = true;
                }
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
            {
                Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, aimingOffsetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
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
                SetAgentDestination(currentTarget, true);
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
            agent.velocity = Vector3.zero;
        }
    }

    private bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;
        Vector3 origin = transform.position + Vector3.up * 1.5f; // Eye level of enemy
        Vector3 targetCenter = target.position + Vector3.up * 1.0f; // Target center
        Vector3 direction = targetCenter - origin;
        float distance = direction.magnitude;

        // Raycast to check for obstacles, ignoring trigger colliders
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root != target.root && hit.transform.root != transform.root)
            {
                return false; // Blocked by physical obstacle
            }
        }
        return true; // Clear line of sight
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

    private void SetAgentDestination(Vector3 targetPos, bool force = false)
    {
        if (!agent.isOnNavMesh) return;

        // If not forced and within rate limit, and destination hasn't shifted significantly, skip to prevent stuttering
        if (!force && pathUpdateTimer > 0f && Vector3.SqrMagnitude(targetPos - lastSetDestination) < 4.0f)
        {
            return;
        }

        pathUpdateTimer = pathUpdateInterval;
        lastSetDestination = targetPos;
        agent.SetDestination(targetPos);
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
