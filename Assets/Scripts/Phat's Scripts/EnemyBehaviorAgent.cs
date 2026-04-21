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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        shooting = GetComponent<EnemyShooting>();
        cover = GetComponent<EnemyCover>();
        
        // Find animator on model (check children first)
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        // Configure Agent
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
        // RESTORED: Force children to center, but SKIP functional objects
        foreach (Transform child in transform)
        {
            // Do not reset the M4 or the FirePoint
            if (child.name.Contains("Fire") || child.name.Contains("Point") || 
                child.name.Contains("M4") || child.name.Contains("Gun")) 
                continue;

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }

        spawnPoint = transform.position;
        if (agent.isOnNavMesh) agent.Warp(transform.position);
        StartIdle();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        // Force object to follow Agent simulation (Fusion Fix)
        transform.position = agent.nextPosition;

        // 1. UPDATE ANIMATION (Speed)
        if (animator != null)
        {
            float speed = agent.desiredVelocity.magnitude;
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", speed);
        }

        // 2. CHECK FOR COVER NEED (Out of Ammo)
        if (shooting != null && shooting.IsOutOfAmmo && !isInCover && !shooting.IsReloading)
        {
            FindAndGoToCover();
            return;
        }

        // 3. HANDLE COVER STATE
        if (isInCover)
        {
            HandleCoverLogic();
            return;
        }

        // 4. DETECTION LOGIC
        if (detection != null && detection.IsTargetDetected)
        {
            StopAgent();
            FaceTarget();
            return;
        }

        // 5. PATROL LOGIC
        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    public bool IsMovingToCover => isInCover && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    private void FindAndGoToCover()
    {
        if (cover == null || detection.CurrentTarget == null) 
        {
            shooting.TriggerReload();
            return;
        }

        activeCover = cover.FindNearestCover(detection.CurrentTarget.position);
        
        if (activeCover.found)
        {
            isInCover = true;
            isIdle = false;
            agent.isStopped = false;
            agent.SetDestination(activeCover.position);
            Debug.Log("[Agent] Moving to cover...");
        }
        else
        {
            Debug.Log("[Agent] No cover found, reloading in place.");
            shooting.TriggerReload();
        }
    }

    private void HandleCoverLogic()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StopAgent();
            
            // Aim out from cover
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);

            if (animator != null)
            {
                animator.SetBool("isCovering", true);
                animator.SetBool("isCoverRight", activeCover.isRightSide);
                
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Cover_Crouching"))
                {
                    animator.CrossFade("Cover_Crouching", 0.1f);
                }
            }

            if (!shooting.IsReloading && shooting.IsOutOfAmmo)
            {
                shooting.TriggerReload();
            }

            if (!shooting.IsReloading && !shooting.IsOutOfAmmo)
            {
                ExitCover();
            }
        }
    }

    private void ExitCover()
    {
        isInCover = false;
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

    private void FaceTarget()
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

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
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
