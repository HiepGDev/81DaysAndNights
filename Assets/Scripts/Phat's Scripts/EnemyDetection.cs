using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10.0f;
    [SerializeField] private string[] targetTags = { "Player", "Teammate" };
    [SerializeField] private LayerMask detectionLayer;

    public bool IsTargetDetected { get; private set; }
    public Transform CurrentTarget { get; private set; }

    private void Update()
    {
        DetectTargets();
    }

    private void DetectTargets()
    {
        IsTargetDetected = false;
        CurrentTarget = null;
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);

        foreach (var target in targets)
        {
            foreach (var targetTag in targetTags)
            {
                if (string.IsNullOrEmpty(targetTag)) continue;

                try
                {
                    // Using CompareTag is faster, but we wrap it to catch the "not defined" error
                    if (target.CompareTag(targetTag))
                    {
                        IsTargetDetected = true;
                        CurrentTarget = target.transform;
                        return; // Found at least one target (OR logic), so we can stop looking
                    }
                }
                catch (System.Exception)
                {
                    // This catches the "Tag not defined" error so your game keeps running
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
