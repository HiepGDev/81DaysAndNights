using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TeammateAI : MonoBehaviour
{
    public enum TeammateState { Idle, Following, Combat }

    [Header("References")]
    [SerializeField] private Transform playerTarget;

    [Header("Follow Settings")]
    [SerializeField] private float followTriggerDistance = 10f;  // Bắt đầu chạy theo khi > khoảng này
    [SerializeField] private float stopFollowDistance = 2.5f; // Dừng lại khi <= khoảng này

    [Header("Combat Settings")]
    [SerializeField] private float rotationSpeed = 8f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform enemyTarget;
    private TeammateState currentState = TeammateState.Idle;

    private Vector3 lastDestination;
    private const float DESTINATION_THRESHOLD = 0.5f;
    private float lastLoggedSpeed = -1f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        agent.acceleration = 15f;
        agent.angularSpeed = 300f;
        agent.stoppingDistance = stopFollowDistance;

        // Luôn tắt tự động xoay của NavMesh, chúng ta tự code xoay bằng SmoothRotateToward
        agent.updateRotation = false;
    }

    private void Start()
    {
        // Force all children to center
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Fire") || child.name.Contains("Point")) continue;

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }

        if (playerTarget == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }

        lastDestination = transform.position;
    }

    private void Update()
    {
        if (!agent.isOnNavMesh || playerTarget == null) return;

        UpdateState();
        HandleMovement();
        HandleRotation();
        UpdateAnimation();
    }

    private void UpdateState()
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case TeammateState.Idle:
                // Có enemy → Combat
                if (enemyTarget != null)
                {
                    currentState = TeammateState.Combat;
                    break;
                }
                // Player đi quá xa → Follow
                if (distToPlayer > followTriggerDistance)
                    currentState = TeammateState.Following;
                break;

            case TeammateState.Following:
                // Đã đến gần đủ → Idle
                if (distToPlayer <= stopFollowDistance)
                {
                    currentState = TeammateState.Idle;
                    StopAgent();
                }
                // (Tùy chọn) Nếu đang chạy theo mà phát hiện địch, có thể chuyển sang Combat luôn
                else if (enemyTarget != null)
                {
                    currentState = TeammateState.Combat;
                    StopAgent();
                }
                break;

            case TeammateState.Combat:
                // Mất enemy → Idle
                if (enemyTarget == null)
                {
                    currentState = TeammateState.Idle;
                    StopAgent();
                    break;
                }
                // Player đi quá xa khi đang combat → Follow trước (Bỏ đồng đội chạy theo người chơi)
                if (distToPlayer > followTriggerDistance)
                    currentState = TeammateState.Following;
                break;
        }
    }

    private void HandleMovement()
    {
        switch (currentState)
        {
            case TeammateState.Following:
                agent.isStopped = false;
                // Chỉ gọi SetDestination khi đích thay đổi đáng kể (giảm overhead)
                if (Vector3.Distance(playerTarget.position, lastDestination) > DESTINATION_THRESHOLD)
                {
                    lastDestination = playerTarget.position;
                    agent.SetDestination(lastDestination);
                }
                break;

            case TeammateState.Idle:
            case TeammateState.Combat:
                // Đứng yên hoàn toàn – không gọi SetDestination
                break;
        }
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
                if (agent.velocity.sqrMagnitude > 0.1f)
                    SmoothRotateToward(transform.position + agent.velocity);
                break;

            case TeammateState.Combat:
                if (enemyTarget != null)
                    SmoothRotateToward(enemyTarget.position);
                break;

                //case TeammateState.Idle:
                //    if (playerTarget != null)
                //    {
                //        SmoothRotateToward(playerTarget.position);
                //        SmoothRotateToward(transform.position + playerTarget.forward);
                //    }
                //    break;
        }
    }

    private void SmoothRotateToward(Vector3 targetWorldPos)
    {
        Vector3 dir = (targetWorldPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, target,
            Time.deltaTime * rotationSpeed
        );
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        if (HasParameter("Speed", animator))
        {
            float currentMoveSpeed = agent.desiredVelocity.magnitude;
            animator.SetFloat("Speed", currentMoveSpeed);

            // LOGIC DEBUG THÔNG MINH: Chỉ in ra Console nếu tốc độ thay đổi đáng kể
            // (vượt ngưỡng 0.1f để tránh spam khi NavMesh nhích từng chút một)
            if (Mathf.Abs(currentMoveSpeed - lastLoggedSpeed) > 0.1f)
            {
                if (currentMoveSpeed > 0.1f)
                {
                    Debug.Log($"[TeammateAI - Animation] Đang di chuyển! Speed = {currentMoveSpeed:F2}");
                }
                else if (currentMoveSpeed <= 0.1f && lastLoggedSpeed > 0.1f)
                {
                    Debug.Log($"[TeammateAI - Animation] Đã dừng lại! Speed = {currentMoveSpeed:F2}");
                }

                // Lưu lại tốc độ hiện tại để so sánh cho khung hình sau
                lastLoggedSpeed = currentMoveSpeed;
            }
        }
        else
        {
            // Cảnh báo nếu Animator không có biến "Speed" (chống lỗi)
            Debug.LogWarning("[TeammateAI - Animation] CẢNH BÁO: Không tìm thấy Parameter tên là 'Speed' trong Animator!");
        }
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private bool HasAnimParam(string paramName)
    {
        foreach (var p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    public void SetEnemyTarget(Transform enemy)
    {
        enemyTarget = enemy;
    }

    public TeammateState CurrentState => currentState;

    private void OnDrawGizmosSelected()
    {
        // Vòng tròn trigger follow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followTriggerDistance);

        // Vòng tròn dừng lại
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopFollowDistance);
    }
}