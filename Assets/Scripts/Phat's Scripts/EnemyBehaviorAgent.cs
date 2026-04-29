using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviorAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyDetection detection;
    private EnemyShooting shooting;
    private EnemyCover cover;
    private Animator animator;

    [Header("Behavior Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float idleTime = 2f;
    
    private Vector3 spawnPoint;
    private float idleTimer;
    private bool isIdle = false;
    private bool isInCover = false;
    private Vector3 currentTarget; 
    private EnemyCover.CoverPoint activeCover;

    // --- State Tracking ---
    private bool wasTargetInShootingRangeLastFrame = false;
    private bool isChasing = false;
    private bool isHidingFromRange = false; // NEW: Track safety hiding

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        shooting = GetComponent<EnemyShooting>();
        cover = GetComponent<EnemyCover>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null) {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        agent.updatePosition = false; 
        agent.updateRotation = true;
        agent.acceleration = 30f; 
        agent.angularSpeed = 600f; 
        agent.stoppingDistance = 0.5f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void Start()
    {
        // THE SKELETON PROTECTOR:
        // We only reset the main functional points, but we MUST NOT 
        // reset the Hips or any bones, otherwise hit detection and ragdolls break.
        foreach (Transform child in transform)
        {
            // Only reset top-level containers that aren't part of the actual character skeleton
            if (child.name.ToLower().Contains("weapon") || child.name.ToLower().Contains("target")) 
            {
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
            }
        }
        spawnPoint = transform.position;
        if (agent.isOnNavMesh) agent.Warp(transform.position);
        StartIdle();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;
        transform.position = agent.nextPosition;

        if (animator != null)
        {
            // THE RELOAD PROTECTION FIX:
            // If the AI is reloading, we MUST NOT update the 'Speed' parameter,
            // otherwise the Walk animation will instantly cancel the Reload animation.
            bool isActuallyReloading = (shooting != null && shooting.IsReloading);
            
            if (!isActuallyReloading)
            {
                float speed = agent.desiredVelocity.magnitude;
                if (HasParameter("Speed", animator))
                    animator.SetFloat("Speed", speed);
            }
            else
            {
                // Force speed to 0 so he stays in the reload pose
                if (HasParameter("Speed", animator))
                    animator.SetFloat("Speed", 0f);
            }
        }

        // 1. DETECTION & RANGE CHECK (Primary priority to allow breaking cover)
        bool isDetected = (detection != null && detection.IsTargetDetected);
        bool inShootingRange = false;
        if (isDetected && shooting != null)
        {
            float dist = Vector3.Distance(transform.position, detection.CurrentTarget.position);
            inShootingRange = dist <= shooting.FireDistance;
        }

        // 2. THE 50/50 "OUT OF RANGE" TRIGGER
        if (wasTargetInShootingRangeLastFrame && !inShootingRange)
        {
            if (Random.value > 0.5f)
            {
                // CHASE: Break out of cover and run!
                isChasing = true;
                isInCover = false; 
                isHidingFromRange = false;
                if (animator != null) animator.SetBool("isCovering", false);
                
                agent.isStopped = false;
                Vector3 targetPos = isDetected ? detection.CurrentTarget.position : detection.LastKnownPosition;
                agent.SetDestination(targetPos);
                Debug.Log("[Agent] Target lost: Breaking cover to CHASE!");
                
                wasTargetInShootingRangeLastFrame = inShootingRange;
                return; 
            }
            else
            {
                // SEEK COVER
                isChasing = false;
                isHidingFromRange = true; 
                if (!isInCover) FindAndGoToCover();
                Debug.Log("[Agent] Target lost: Seeking/Staying in cover.");
            }
        }

        // 3. OUT OF AMMO logic
        if (shooting != null && shooting.IsOutOfAmmo && !shooting.IsReloading)
        {
            if (!isInCover) FindAndGoToCover();
            else HandleCoverLogic();
            wasTargetInShootingRangeLastFrame = inShootingRange;
            return;
        }

        // 4. ACTIVE COVER logic
        if (isInCover)
        {
            HandleCoverLogic();
            wasTargetInShootingRangeLastFrame = inShootingRange;
            return;
        }

        // 5. Handle Chasing
        if (isChasing)
        {
            if (inShootingRange) 
            {
                isChasing = false; 
            }
            else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isChasing = false;
                Debug.Log("[Agent] Chase ended. Resuming patrol.");
            }
            else
            {
                wasTargetInShootingRangeLastFrame = inShootingRange;
                return;
            }
        }

        // 6. NORMAL COMBAT / IDLE / PATROL
        if (inShootingRange)
        {
            if (shooting != null) shooting.enabled = true;
            StopAgent();
            wasTargetInShootingRangeLastFrame = true;
            FaceTarget();
            return;
        }

        wasTargetInShootingRangeLastFrame = inShootingRange;
        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    public bool IsMovingToCover => isInCover && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
    public bool IsInCover => isInCover;
    public EnemyCover.CoverPoint ActiveCover => activeCover;

    private void FindAndGoToCover()
    {
        if (cover == null || detection.CurrentTarget == null && !isChasing) 
        {
            shooting.TriggerReload();
            return;
        }

        // If we lost them, use last known pos to find cover
        Vector3 targetPosForCover = (detection.CurrentTarget != null) ? detection.CurrentTarget.position : detection.LastKnownPosition;
        activeCover = cover.FindNearestCover(targetPosForCover);
        
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
        
        if (distToCover <= 0.2f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            bool isActuallyReloading = (shooting != null && shooting.IsReloading);
            bool shouldCrouch = (shooting != null && (shooting.IsOutOfAmmo || isHidingFromRange));

            // THE ROTATION FIX: Always allow him to face the wall, even if reloading
            if (shouldCrouch)
            {
                StopAgent();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);
            }

            // THE ANIMATION LOCK: Only touch animator params if NOT reloading
            if (!isActuallyReloading)
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
                else
                {
                    if (animator != null && HasParameter("isCovering", animator)) 
                        animator.SetBool("isCovering", false);
                    
                    if (agent.isStopped) agent.isStopped = false;
                    FaceTarget();
                }
            }

            // RELOAD logic (Check remains active to allow TriggerReload to start)
            if (shooting != null && shooting.IsOutOfAmmo && !isActuallyReloading)
            {
                shooting.TriggerReload();
            }

            // EXIT logic
            if (!isActuallyReloading)
            {
                if (isHidingFromRange)
                {
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0)
                    {
                        ExitCover();
                    }
                }
            }
        }
    }

    private void ExitCover()
    {
        isInCover = false;
        isHidingFromRange = false; // Reset the flag
        if (animator != null) animator.SetBool("isCovering", false);
        
        if (detection.IsTargetDetected)
        {
            StopAgent();
            FaceTarget();
        }
        else
        {
            PickNewWanderPoint();
        }
    }

    public void FaceTarget()
    {
        if (detection.CurrentTarget != null)
        {
            Vector3 lookPos = detection.CurrentTarget.position - transform.position;
            lookPos.y = 0;
            if (lookPos != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }

    private bool HasParameter(string paramName, Animator animator)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
            if (param.name == paramName) return true;
        return false;
    }

    private void HandleWandering()
    {
        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
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
        for (int i = 0; i < 15; i++)
        {
            Vector3 randomPos = spawnPoint + Random.insideUnitSphere * wanderRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 5.0f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                agent.CalculatePath(hit.position, path);
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    currentTarget = hit.position;
                    isIdle = false;
                    agent.isStopped = false;
                    agent.SetDestination(currentTarget);
                    return;
                }
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
            agent.nextPosition = transform.position; 
        }
    }
}
