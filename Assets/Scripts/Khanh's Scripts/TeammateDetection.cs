using UnityEngine;

[RequireComponent(typeof(TeammateAI))]
public class TeammateDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 25.0f;
    [SerializeField] private string[] targetTags = { "Enemy" };

    [Tooltip("Layer chứa mục tiêu (Enemy) để tối ưu Physics.OverlapSphere")]
    [SerializeField] private LayerMask detectionLayer;

    [SerializeField] private TeammateSO teammateData;
    [SerializeField] private Transform eyesTransform;

    public GameObject Detecting;

    private TeammateAI teammateAI;

    public float DetectionRadius
    {
        get => detectionRadius;
        set => detectionRadius = value;
    }
    public bool IsTargetDetected { get; private set; }
    public Transform CurrentTarget { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public string[] TargetTags => targetTags;

    private void Awake()
    {
        if (teammateData != null)
        {
            detectionRadius = teammateData.detectionRadius;
        }

        teammateAI = GetComponent<TeammateAI>();

        if (eyesTransform == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name.Contains("Pupils"))
                {
                    eyesTransform = child;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        DetectTargets();
    }

    private void DetectTargets()
    {
        IsTargetDetected = false;
        Transform previousTarget = CurrentTarget;
        CurrentTarget = null;

        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);

        bool foundTarget = false;

        foreach (var target in targets)
        {
            foreach (var targetTag in targetTags)
            {
                if (string.IsNullOrEmpty(targetTag)) continue;

                try
                {
                    if (target.CompareTag(targetTag))
                    {
                        // ✅ FIX 1: Eye position dùng fixed height thay vì bone
                        // Đảm bảo tia xuất phát từ trên bao cát
                        Vector3 eyePosition;
                        if (eyesTransform != null)
                        {
                            // Lấy điểm cao nhất giữa bone và fixed height
                            float fixedEyeHeight = transform.position.y + 1.6f;
                            eyePosition = eyesTransform.position;
                            if (eyePosition.y < fixedEyeHeight)
                                eyePosition.y = fixedEyeHeight;
                        }
                        else
                        {
                            eyePosition = transform.position + Vector3.up * 1.6f;
                        }

                        // ✅ FIX 2: Multi-point LOS - check nhiều điểm trên cơ thể địch
                        Vector3 rootPos = target.transform.root.position;
                        Vector3[] checkPoints = new Vector3[]
                        {
                        rootPos + Vector3.up * 1.0f, // ngực
                        rootPos + Vector3.up * 1.6f, // đầu
                        rootPos + Vector3.up * 0.5f, // bụng
                        };

                        bool hasLOS = false;
                        foreach (var checkPoint in checkPoints)
                        {
                            Vector3 dir = checkPoint - eyePosition;
                            float dist = dir.magnitude;

                            if (dist < 0.1f)
                            {
                                hasLOS = true;
                                break;
                            }

                            if (!Physics.Raycast(
                                    eyePosition,
                                    dir.normalized,
                                    out RaycastHit hit,
                                    dist,
                                    Physics.DefaultRaycastLayers,
                                    QueryTriggerInteraction.Ignore)
                                || hit.transform.root == target.transform.root
                                || hit.transform.root == transform.root)
                            {
                                hasLOS = true;
                                break; // Thấy được ít nhất 1 điểm → detect thành công
                            }
                        }

                        if (!hasLOS) break; // Tất cả điểm đều bị chặn → skip target này

                        IsTargetDetected = true;
                        CurrentTarget = target.transform.root;
                        LastKnownPosition = CurrentTarget.position;
                        foundTarget = true;
                        break;
                    }
                }
                catch (System.Exception) { }
            }

            if (foundTarget) break;
        }

        if (previousTarget != CurrentTarget)
        {
            teammateAI.SetEnemyTarget(CurrentTarget);
            Detecting = CurrentTarget != null ? CurrentTarget.gameObject : null;

            if (CurrentTarget != null)
                Debug.Log($"{gameObject.name} Đã phát hiện mục tiêu: <b>{CurrentTarget.name}</b>");
            else if (previousTarget != null)
                Debug.Log($"{gameObject.name} Đã tiêu diệt hoặc mất dấu mục tiêu.");
        }

        if (CurrentTarget == null && Detecting != null)
            Detecting = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}