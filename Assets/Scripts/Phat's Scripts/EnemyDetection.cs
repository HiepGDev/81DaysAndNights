using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10.0f;
    [SerializeField] private string[] targetTags = { "Player", "Teammate" };

    public float DetectionRadius
    {
        get => detectionRadius;
        set => detectionRadius = value;
    }
    [SerializeField] private LayerMask detectionLayer;

    public bool IsTargetDetected { get; private set; }
    public Transform CurrentTarget { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public string[] TargetTags => targetTags;

    private void Update()
    {
        DetectTargets();
    }

    private void DetectTargets()
    {
        IsTargetDetected = false;
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);

        foreach (var target in targets)
        {
            foreach (var targetTag in targetTags)
            {
                if (string.IsNullOrEmpty(targetTag)) continue;

                try
                {
                    if (target.CompareTag(targetTag))
                    {
                        // Check Line of Sight (LOS)
                        Vector3 origin = transform.position + Vector3.up * 1.5f; // Eye level of enemy
                        Vector3 targetCenter = target.transform.position + Vector3.up * 1.0f; // Target center/chest
                        Vector3 direction = targetCenter - origin;
                        float distance = direction.magnitude;

                        // Raycast to check for obstacles, ignoring trigger colliders
                        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            // If the ray hit an obstacle that is not the target and not the enemy itself
                            if (hit.transform.root != target.transform.root && hit.transform.root != transform.root)
                            {
                                continue; // Blocked by wall/house/castle/terrain, skip this target
                            }
                        }

                        IsTargetDetected = true;
                        CurrentTarget = target.transform;
                        LastKnownPosition = CurrentTarget.position; // Record position while visible
                        return; // Found at least one visible target, stop searching
                    }
                }
                catch (System.Exception)
                {
                    // This catches the "Tag not defined" error so your game keeps running
                }
            }
        }
        
        // If we reach here, no target was found this frame
        CurrentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
