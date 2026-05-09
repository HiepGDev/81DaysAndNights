using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TeammateAI : MonoBehaviour
{
    public enum TeammateState { Idle, Following, Combat, Patrolling, SeekingCover } // Thêm SeekingCover
    public TeammateState CurrentState => currentState;

    public enum AIMode { Follower, Patroller }

    [Header("Teammate Mode")]
    [SerializeField] private AIMode aiMode = AIMode.Follower;

    [Header("References")]
    [SerializeField] private Transform playerTarget;

    [Header("Follow Settings")]
    [SerializeField] private float followTriggerDistance = 10f;
    [SerializeField] private float stopFollowDistance = 2.5f;

    [Header("Combat Settings")]
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointStopDistance = 0.5f;
    [SerializeField] private float waypointWaitTime = 1.5f;
    [SerializeField] private bool loopPatrol = true;

    private int currentPatrolIndex = -1;
    private float waypointWaitTimer = 0f;
    private bool isWaitingAtWaypoint = false;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform enemyTarget;
    private TeammateState currentState = TeammateState.Idle;

    private Vector3 lastDestination;
    private const float DESTINATION_THRESHOLD = 0.5f;
    private float lastLoggedSpeed = -1f;

    // Các Component mới
    private TeammateShooting shooting;
    private TeammateCover cover;
    private TeammateCover.CoverPoint activeCover;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        shooting = GetComponent<TeammateShooting>();
        cover = GetComponent<TeammateCover>();

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        agent.acceleration = 15f;
        agent.angularSpeed = 300f;
        agent.updateRotation = false;
    }

    private void Start()
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Fire") || child.name.Contains("Point")) continue;
            if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null) continue;
            if (child.GetComponent<Animator>() != null) continue;
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }

        if (playerTarget == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }

        lastDestination = transform.position;
        InitializeState();
    }

    private void InitializeState()
    {
        switch (aiMode)
        {
            case AIMode.Follower:
                agent.stoppingDistance = stopFollowDistance;
                currentState = TeammateState.Idle;
                break;
            case AIMode.Patroller:
                agent.stoppingDistance = waypointStopDistance;
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    currentState = TeammateState.Patrolling;
                    GoToNextWaypoint();
                }
                else currentState = TeammateState.Idle;
                break;
        }
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;
        if (aiMode == AIMode.Follower && playerTarget == null) return;

        // ƯU TIÊN SỐ 1: XỬ LÝ HẾT ĐẠN -> TÌM CHỖ NẤP
        if (shooting != null && shooting.IsOutOfAmmo)
        {
            // Nếu chưa tìm chỗ nấp và chưa nạp đạn, thì tìm chỗ nấp
            if (currentState != TeammateState.SeekingCover && !shooting.IsReloading)
            {
                FindAndGoToCover();
            }
            // Nếu đang trong quá trình chạy đi nấp
            else if (currentState == TeammateState.SeekingCover)
            {
                HandleCoverLogic();
            }

            UpdateAnimation();
            return; // Đang chạy đi nạp đạn thì bỏ qua các logic Follow/Patrol
        }

        UpdateState();
        HandleMovement();
        HandleRotation();
        UpdateAnimation();
    }

    private void FindAndGoToCover()
    {
        if (cover != null && enemyTarget != null)
        {
            activeCover = cover.FindNearestCover(enemyTarget.position);
            if (activeCover.found)
            {
                currentState = TeammateState.SeekingCover;
                agent.isStopped = false;
                agent.SetDestination(activeCover.position);
                Debug.Log("[TeammateAI] Hết đạn! Đang chạy đi tìm chỗ nấp...");
                return;
            }
        }

        // Nếu không có tường nào gần đó để nấp, đành đứng im nạp đạn giữa đường
        shooting.TriggerReload();
    }

    private void HandleCoverLogic()
    {
        float distToCover = Vector3.Distance(transform.position, activeCover.position);

        // Nếu đã chạy đến nơi nấp an toàn
        if (distToCover <= 0.3f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            StopAgent();
            // Quay mặt áp vào tường hoặc nhìn ra ngoài (theo góc khuất)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);

            // Gọi súng ra lệnh: Mày ngồi xuống nạp đạn đi!
            if (!shooting.IsReloading)
            {
                shooting.TriggerReload();
            }
        }
    }

    private void UpdateState()
    {
        // Khôi phục lại trạng thái cũ sau khi nạp đạn xong
        if (currentState == TeammateState.SeekingCover) currentState = TeammateState.Idle;

        switch (aiMode)
        {
            case AIMode.Follower: UpdateFollowerState(); break;
            case AIMode.Patroller: UpdatePatrollerState(); break;
        }
    }

    private void UpdateFollowerState()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        switch (currentState)
        {
            case TeammateState.Idle:
                if (enemyTarget != null) { currentState = TeammateState.Combat; break; }
                if (distToPlayer > followTriggerDistance) currentState = TeammateState.Following;
                break;
            case TeammateState.Following:
                if (enemyTarget != null) { currentState = TeammateState.Combat; StopAgent(); break; }
                if (distToPlayer <= stopFollowDistance) { currentState = TeammateState.Idle; StopAgent(); }
                break;
            case TeammateState.Combat:
                if (enemyTarget == null) { currentState = TeammateState.Idle; StopAgent(); break; }
                if (distToPlayer > followTriggerDistance) currentState = TeammateState.Following;
                break;
        }
    }

    private void UpdatePatrollerState()
    {
        switch (currentState)
        {
            case TeammateState.Patrolling:
                if (enemyTarget != null) { currentState = TeammateState.Combat; StopAgent(); }
                break;
            case TeammateState.Combat:
                if (enemyTarget == null)
                {
                    currentState = TeammateState.Patrolling;
                    GoToNextWaypoint();
                }
                break;
            case TeammateState.Idle:
                if (enemyTarget != null) { currentState = TeammateState.Combat; break; }
                break;
        }
    }

    private void HandleMovement()
    {
        switch (currentState)
        {
            case TeammateState.Following:
                agent.isStopped = false;
                if (Vector3.Distance(playerTarget.position, lastDestination) > DESTINATION_THRESHOLD)
                {
                    lastDestination = playerTarget.position;
                    agent.SetDestination(lastDestination);
                }
                break;
            case TeammateState.Patrolling:
                HandlePatrolMovement();
                break;
        }
    }

    private void HandlePatrolMovement()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (isWaitingAtWaypoint)
        {
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                isWaitingAtWaypoint = false;
                GoToNextWaypoint();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= waypointStopDistance)
        {
            bool isLastPoint = currentPatrolIndex == patrolPoints.Length - 1;
            if (loopPatrol || !isLastPoint)
            {
                isWaitingAtWaypoint = true;
                waypointWaitTimer = waypointWaitTime;
                StopAgent();
            }
            else
            {
                currentState = TeammateState.Idle;
                StopAgent();
            }
        }
    }

    private void GoToNextWaypoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        if (!loopPatrol && currentPatrolIndex == 0 && patrolPoints.Length > 1)
        {
            currentState = TeammateState.Idle;
            StopAgent();
            return;
        }
        if (patrolPoints[currentPatrolIndex] == null) return;

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void StopAgent()
    {
        if (!agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void HandleRotation()
    {
        switch (currentState)
        {
            case TeammateState.Following:
            case TeammateState.Patrolling:
                if (agent.velocity.sqrMagnitude > 0.1f)
                    SmoothRotateToward(transform.position + agent.velocity);
                break;
            case TeammateState.Combat:
                if (enemyTarget != null)
                    SmoothRotateToward(enemyTarget.position);
                break;
        }
    }

    private void SmoothRotateToward(Vector3 targetWorldPos)
    {
        Vector3 dir = (targetWorldPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * rotationSpeed);
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.desiredVelocity.magnitude;

        if (HasParameter("isRunning", animator))
        {
            animator.SetBool("isRunning", speed > 2.5f);
        }

        bool isActuallyReloading = (shooting != null && shooting.IsReloading);
        if (!isActuallyReloading)
        {
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", speed);
        }
        else
        {
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", 0f);
        }

        lastLoggedSpeed = speed;
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }

    public void SetEnemyTarget(Transform enemy) => enemyTarget = enemy;
}