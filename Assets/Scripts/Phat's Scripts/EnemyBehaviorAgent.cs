using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviorAgent : MonoBehaviour
{
    public enum EnemyMode { Wander, Ambush }

    private NavMeshAgent agent;
    private EnemyDetection detection;
    private EnemyShooting shooting;
    private EnemyCover cover;
    private Animator animator;

    [Header("Ambush Settings")]
    public EnemyMode currentMode = EnemyMode.Wander;
    [SerializeField] private float ambushShootRange = 15f; 
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float idleTime = 2f;
    
    private Transform playerTransform;
    private Vector3 spawnPoint;
    private float idleTimer;
    private bool isIdle = false;
    private bool isInCover = false;
    private Vector3 currentTarget; 
    private EnemyCover.CoverPoint activeCover;

    public bool IsReadyToShoot { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        shooting = GetComponent<EnemyShooting>();
        cover = GetComponent<EnemyCover>();
        animator = GetComponentInChildren<Animator>();

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

        // 1. RE-FIND PLAYER
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) 
            {
                playerTransform = p.transform;
            }
            else if (Time.frameCount % 120 == 0)
            {
                Debug.LogError("[BRAIN] CRITICAL: No object with tag 'Player' found!");
            }
        }

        // 2. TACTICAL DETECTION
        bool isAmbushing = (currentMode == EnemyMode.Ambush && playerTransform != null);
        bool isDetected = isAmbushing || (detection != null && detection.IsTargetDetected);
        Transform target = isAmbushing ? playerTransform : (detection != null ? detection.CurrentTarget : null);

        float distToTarget = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
        float engagementDist = isAmbushing ? ambushShootRange : (shooting != null ? shooting.FireDistance : 25f);
        bool inShootingRange = isDetected && distToTarget <= engagementDist;

        // 3. ANIMATION CONTROL
        if (animator != null)
        {
            float intentSpeed = agent.isStopped ? 0f : agent.desiredVelocity.magnitude;
            bool isActuallyReloading = (shooting != null && shooting.IsReloading);
            bool isMovingToCover = IsMovingToCover;

            // THE RELOAD BRAKE: If we are reloading and NOT moving to cover, STOP physically.
            if (isActuallyReloading && !isMovingToCover)
            {
                if (!agent.isStopped) StopAgent();
            }

            // THE SLIDE FIX: Allow animations if we aren't reloading OR if we are running to cover
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

        // A. RELOAD: If empty, go to wall
        if (shooting != null && shooting.IsOutOfAmmo)
        {
            if (!isInCover) FindAndGoToCover();
            else HandleCoverLogic();
            return;
        }

        // B. COVER: If in cover, stay hidden
        if (isInCover)
        {
            HandleCoverLogic();
            return;
        }

        // C. ENGAGEMENT
        if (isDetected && target != null)
        {
            if (!inShootingRange)
            {
                // CHASE
                IsReadyToShoot = false;
                if (agent.isStopped) agent.isStopped = false;
                agent.SetDestination(target.position);
                FaceTarget();
            }
            else
            {
                // SHOOT
                IsReadyToShoot = true;
                if (!agent.isStopped) StopAgent();
                FaceTarget();
            }
            return;
        }

        IsReadyToShoot = false; // Safety fallback
        // D. WANDER
        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    public bool IsMovingToCover => isInCover && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
    public bool IsInCover => isInCover;
    public EnemyCover.CoverPoint ActiveCover => activeCover;

    private void FindAndGoToCover()
    {
        bool isAmbushing = (currentMode == EnemyMode.Ambush && playerTransform != null);
        if (cover == null || (detection.CurrentTarget == null && !isAmbushing)) 
        {
            Debug.Log("[BRAIN] Aborting cover search: No player seen.");
            shooting.TriggerReload();
            return;
        }

        Vector3 targetPos = isAmbushing ? playerTransform.position : detection.CurrentTarget.position;
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

    private void HandleCoverLogic()
    {
        float distToCover = Vector3.Distance(transform.position, activeCover.position);
        bool inShootingRange = (currentMode == EnemyMode.Ambush) || (detection != null && detection.IsTargetDetected);

        if (distToCover <= 0.2f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            bool isReloading = (shooting != null && shooting.IsReloading);
            bool shouldCrouch = (shooting != null && shooting.IsOutOfAmmo);

            // Turn to face the wall immediately
            if (shouldCrouch || isReloading)
            {
                StopAgent();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);
            }

            // Animation Control
            if (!isReloading)
            {
                if (shouldCrouch)
                {
                    if (animator != null && HasParameter("isCovering", animator))
                    {
                        animator.SetBool("isCovering", true);
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Cover_Crouching"))
                            animator.CrossFade("Cover_Crouching", 0.1f);
                    }
                }
                else if (!shooting.IsOutOfAmmo)
                {
                    // If we have ammo, LEAVE the wall to resume the hunt
                    ExitCover();
                }
            }

            if (shooting != null && shooting.IsOutOfAmmo && !isReloading)
                shooting.TriggerReload();
        }
    }

    private void ExitCover()
    {
        isInCover = false;
        if (animator != null) animator.SetBool("isCovering", false);
        agent.isStopped = false;
    }

    public void FaceTarget()
    {
        Transform t = (currentMode == EnemyMode.Ambush) ? playerTransform : detection.CurrentTarget;
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
}
