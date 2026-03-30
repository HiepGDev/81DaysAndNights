using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviorAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyDetection detection;
    private Animator animator;

    [Header("Behavior Settings")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float idleTime = 2f;
    
    private Vector3 spawnPoint;
    private float idleTimer;
    private bool isIdle = false;
    private Vector3 currentTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();

        // Find the animator on this object or its children
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Configure Agent
        agent.updatePosition = false; 
        agent.updateRotation = true;
        agent.acceleration = 20f; 
        agent.angularSpeed = 450f; 
        agent.stoppingDistance = 0.5f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; }
    }

    private void Start()
    {
        // Force all children to center
        foreach (Transform child in transform)
        {
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }

        spawnPoint = transform.position;
        agent.Warp(transform.position);
        StartIdle();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        // Sync Position
        transform.position = agent.nextPosition;

        // Update Animation Speed
        if (animator != null && HasParameter("Speed", animator))
        {
            float currentMoveSpeed = agent.desiredVelocity.magnitude;
            animator.SetFloat("Speed", currentMoveSpeed);
        }

        if (detection != null && detection.IsTargetDetected)
        {
            StopAgent();
            return;
        }

        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private void HandleWandering()
    {
        if (!agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                StartIdle();
            }
        }
    }

    private void HandleIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0)
        {
            PickNewWanderPoint();
        }
    }

    private void PickNewWanderPoint()
    {
        if (!agent.isOnNavMesh) return;

        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPos = spawnPoint + Random.insideUnitSphere * wanderRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 4.0f, NavMesh.AllAreas))
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
