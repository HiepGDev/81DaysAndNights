using UnityEngine;

[RequireComponent(typeof(TeammateAI))]
public class TeammateDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 15.0f;
    [SerializeField] private string[] targetTags = { "Enemy" }; 

    private TeammateAI teammateAI;

    public bool IsTargetDetected { get; private set; }
    public Transform CurrentTarget { get; private set; }

    private void Awake()
    {
        teammateAI = GetComponent<TeammateAI>();
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

        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (var target in targets)
        {
            foreach (var targetTag in targetTags)
            {
                if (string.IsNullOrEmpty(targetTag)) continue;

                try
                {
                    if (target.CompareTag(targetTag))
                    {
                        IsTargetDetected = true;
                        CurrentTarget = target.transform;
                        goto TargetFound; 
                    }
                }
                catch (System.Exception)
                {
                }
            }
        }

    TargetFound:

        if (previousTarget != CurrentTarget)
        {
            teammateAI.SetEnemyTarget(CurrentTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}