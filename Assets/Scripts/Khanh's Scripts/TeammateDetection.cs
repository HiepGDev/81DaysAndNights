using UnityEngine;

[RequireComponent(typeof(TeammateAI))]
public class TeammateDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 25.0f;
    [SerializeField] private string[] targetTags = { "Enemy" };

    [Tooltip("Layer chứa mục tiêu (Enemy) để tối ưu Physics.OverlapSphere")]
    [SerializeField] private LayerMask detectionLayer;

    [Tooltip("Layer chứa các vật cản che tầm nhìn")]
    [SerializeField] private LayerMask obstacleLayer;

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
            obstacleLayer = teammateData.obstacleLayer;
            //if (teammateData.obstacleTags != null && teammateData.obstacleTags.Length > 0)
            //{
            //    obstacleTags = teammateData.obstacleTags;
            //}
        }

        teammateAI = GetComponent<TeammateAI>();
        
        if(eyesTransform == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            foreach(Transform child in allChildren)
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
                        Vector3 eyePosition = eyesTransform != null ? eyesTransform.position : (transform.position + Vector3.up * 1.5f);
                        Vector3 targetPosition = target.bounds.center;
                        Vector3 directionToTarget = targetPosition - eyePosition;
                        float distanceToTarget = directionToTarget.magnitude;

                        // Bắn tia quét xem có vướng vật cản không
                        if (Physics.Raycast(eyePosition, directionToTarget.normalized, distanceToTarget, obstacleLayer))
                        {
                            // Tia chạm vào vật cản (tường) trước khi tới đích -> Kẻ địch đang nấp
                            break; 
                        }
                        // -----------------------------------------------

                        IsTargetDetected = true;
                        CurrentTarget = target.transform.root;
                        LastKnownPosition = CurrentTarget.position;

                        foundTarget = true;
                        break;
                    }
                }
                catch (System.Exception)
                {
                }
            }

            if (foundTarget) break;
        }

        if (previousTarget != CurrentTarget)
        {
            teammateAI.SetEnemyTarget(CurrentTarget);

            Detecting = CurrentTarget != null ? CurrentTarget.gameObject : null;

            if (CurrentTarget != null)
            {
                Debug.Log($"{gameObject.name} Đã phát hiện mục tiêu: <b>{CurrentTarget.name}</b>");
            }
            else if (previousTarget != null)
            {
                Debug.Log($"{gameObject.name} Đã tiêu diệt hoặc mất dấu mục tiêu.");
            }
        }

        if (CurrentTarget == null && Detecting != null)
        {
            Detecting = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}