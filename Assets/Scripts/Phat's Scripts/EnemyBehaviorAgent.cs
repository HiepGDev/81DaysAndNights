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
        foreach (Transform child in transform)
        {
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
        transform.position = agent.nextPosition;

        if (animator != null)
        {
            float speed = agent.desiredVelocity.magnitude;
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", speed);
        }

        if (shooting != null && shooting.IsOutOfAmmo && !isInCover && !shooting.IsReloading)
        {
            FindAndGoToCover();
            return;
        }

        if (isInCover)
        {
            HandleCoverLogic();
            return;
        }

        if (detection != null && detection.IsTargetDetected)
        {
            if (shooting != null) shooting.enabled = true;
            StopAgent();
            FaceTarget();
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
        }
        else
        {
            shooting.TriggerReload();
        }
    }

    private void HandleCoverLogic()
    {
        float distToCover = Vector3.Distance(transform.position, activeCover.position);
        if (distToCover <= 1.0f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            if (shooting != null && shooting.IsOutOfAmmo)
            {
                StopAgent();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);

                if (animator != null)
                {
                    animator.SetBool("isCovering", true);
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Cover_Crouching"))
                        animator.CrossFade("Cover_Crouching", 0.1f);
                }

                if (!shooting.IsReloading) shooting.TriggerReload();
            }
            else
            {
                if (animator != null) animator.SetBool("isCovering", false);
                FaceTarget();
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
