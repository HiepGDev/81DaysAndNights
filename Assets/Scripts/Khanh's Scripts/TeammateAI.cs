using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TeammateAI : MonoBehaviour
{
    public enum TeammateState { Idle, Following, Combat, Patrolling, SeekingCover }
    public TeammateState CurrentState => currentState;

    [SerializeField] private TeammateSO teammateData;

    public enum AIMode { Follower, Patroller, Defender }

    [Header("Teammate Mode")]
    [SerializeField] private AIMode aiMode = AIMode.Follower;

    [Header("References")]
    [SerializeField] private Transform playerTarget;

    [Header("Defend Settings (Defender only)")]
    [SerializeField] private Transform defendPoint;
    private Vector3 originalDefendPosition;
    private DefendPoint claimedDefendPoint;

    [Header("Follow Settings (Follower only)")]
    [SerializeField] private float followTriggerDistance = 10f;
    [SerializeField] private float stopFollowDistance = 2.5f;

    [Header("Combat Settings")]
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Patrol Settings (Patroller only)")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointStopDistance = 0.5f;
    [SerializeField] private float waypointWaitTime = 1.5f;

    [Tooltip("Tick: Đi vòng tròn (0->1->0) | Bỏ Tick: Đi một lèo 0->1 rồi dừng hẳn")]
    [SerializeField] private bool loopPatrol = false;

    [Tooltip("Khoảng cách tối đa để Player kích hoạt NPC đi tuần tra")]
    [SerializeField] private float patrolStartDistance = 15f;

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

        if (teammateData != null)
        {
            followTriggerDistance = teammateData.followTriggerDistance;
            stopFollowDistance = teammateData.stopFollowDistance;
            rotationSpeed = teammateData.rotationSpeed;
            waypointStopDistance = teammateData.waypointStopDistance;
            waypointWaitTime = teammateData.waypointWaitTime;
            loopPatrol = teammateData.loopPatrol;
        }
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

        originalDefendPosition = transform.position;
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
                currentState = TeammateState.Idle;
                break;
            case AIMode.Defender:
                agent.stoppingDistance = 0.2f;
                currentState = TeammateState.Idle;

                if (defendPoint == null)
                {
                    FindAvailableDefendPoint();
                }
                break;
        }
    }

    private void FindAvailableDefendPoint()
    {
        DefendPoint[] allPoints = FindObjectsOfType<DefendPoint>();
        float closestDistance = float.MaxValue;
        DefendPoint bestPoint = null;

        foreach (DefendPoint point in allPoints)
        {
            if (point.isOccupied) continue;

            float distanceToPoint = Vector3.Distance(transform.position, point.transform.position);
            if (distanceToPoint < closestDistance)
            {
                closestDistance = distanceToPoint;
                bestPoint = point;
            }
        }

        if (bestPoint != null)
        {
            claimedDefendPoint = bestPoint;
            claimedDefendPoint.isOccupied = true;
            defendPoint = claimedDefendPoint.transform;
        }
    }

    private void OnDestroy()
    {
        if (claimedDefendPoint != null)
        {
            claimedDefendPoint.isOccupied = false;
        }
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;
        if ((aiMode == AIMode.Follower || aiMode == AIMode.Patroller) && playerTarget == null) return;

        if (shooting != null && shooting.IsOutOfAmmo)
        {
            if (currentState != TeammateState.SeekingCover && !shooting.IsReloading)
            {
                FindAndGoToCover();
            }
            else if (currentState == TeammateState.SeekingCover)
            {
                HandleCoverLogic();
            }

            UpdateAnimation();
            return;
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
                return;
            }
        }

        shooting.TriggerReload();
    }

    private void HandleCoverLogic()
    {
        float distToCover = Vector3.Distance(transform.position, activeCover.position);

        if (distToCover <= 0.3f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            StopAgent();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);

            if (!shooting.IsReloading)
            {
                shooting.TriggerReload();
            }
        }
    }

    private void UpdateState()
    {
        if (currentState == TeammateState.SeekingCover) currentState = TeammateState.Idle;

        switch (aiMode)
        {
            case AIMode.Follower: UpdateFollowerState(); break;
            case AIMode.Patroller: UpdatePatrollerState(); break;
            case AIMode.Defender: UpdateDefenderState(); break;
        }
    }

    private void UpdateDefenderState()
    {
        switch (currentState)
        {
            case TeammateState.Idle:
                if (enemyTarget != null) { currentState = TeammateState.Combat; StopAgent(); }
                break;
            case TeammateState.Combat:
                if (enemyTarget == null) { currentState = TeammateState.Idle; }
                break;
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
        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        bool hasFinishedPatrol = (!loopPatrol && currentPatrolIndex >= patrolPoints.Length - 1);

        switch (currentState)
        {
            case TeammateState.Idle:
                if (enemyTarget != null) { currentState = TeammateState.Combat; break; }

                if (!hasFinishedPatrol && distToPlayer <= patrolStartDistance && patrolPoints != null && patrolPoints.Length > 0)
                {
                    currentState = TeammateState.Patrolling;
                    ResumePatrol();
                }
                break;

            case TeammateState.Patrolling:
                if (enemyTarget != null) { currentState = TeammateState.Combat; StopAgent(); break; }

                if (distToPlayer > patrolStartDistance)
                {
                    currentState = TeammateState.Idle;
                    StopAgent();
                }
                break;

            case TeammateState.Combat:
                if (enemyTarget == null)
                {
                    if (!hasFinishedPatrol && distToPlayer <= patrolStartDistance)
                    {
                        currentState = TeammateState.Patrolling;
                        ResumePatrol();
                    }
                    else
                    {
                        currentState = TeammateState.Idle;
                    }
                }
                break;
        }
    }

    private void ResumePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!loopPatrol && currentPatrolIndex >= patrolPoints.Length - 1)
        {
            currentState = TeammateState.Idle;
            return;
        }

        if (currentPatrolIndex == -1)
        {
            GoToNextWaypoint();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    private void HandleMovement()
    {
        switch (currentState)
        {
            case TeammateState.Idle:
                if (aiMode == AIMode.Defender)
                {
                    Vector3 targetPos = defendPoint != null ? defendPoint.position : originalDefendPosition;

                    if (Vector3.Distance(transform.position, targetPos) > agent.stoppingDistance + 0.1f)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(targetPos);
                    }
                    else if (!agent.isStopped)
                    {
                        StopAgent();
                    }
                }
                break;

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
            bool isLastPoint = currentPatrolIndex >= patrolPoints.Length - 1;

            if (isLastPoint && !loopPatrol)
            {
                currentState = TeammateState.Idle;
                StopAgent();
            }
            else
            {
                isWaitingAtWaypoint = true;
                waypointWaitTimer = waypointWaitTime;
                StopAgent();
            }
        }
    }

    private void GoToNextWaypoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!loopPatrol && currentPatrolIndex >= patrolPoints.Length - 1)
        {
            currentState = TeammateState.Idle;
            StopAgent();
            return;
        }

        currentPatrolIndex++;

        if (loopPatrol && currentPatrolIndex >= patrolPoints.Length)
        {
            currentPatrolIndex = 0;
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

            case TeammateState.Idle:
                if (agent.velocity.sqrMagnitude > 0.1f)
                {
                    SmoothRotateToward(transform.position + agent.velocity);
                }
                else if (aiMode == AIMode.Defender && defendPoint != null)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, defendPoint.rotation, Time.deltaTime * rotationSpeed);
                }
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