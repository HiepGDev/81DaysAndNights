using UnityEngine;

[RequireComponent(typeof(TeammateAI))]
public class TeammateDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 15.0f;
    [SerializeField] private string[] targetTags = { "Enemy" };
    [SerializeField] private TeammateSO teammateData;

    private TeammateAI teammateAI;

    public bool IsTargetDetected { get; private set; }
    public Transform CurrentTarget { get; private set; }

    private void Awake()
    {
        if (teammateData != null)
        {
            detectionRadius = teammateData.detectionRadius;
        }

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

            if (CurrentTarget != null)
            {
                Debug.Log($"<color=orange>[TeammateDetection]</color> Đã phát hiện mục tiêu: <b>{CurrentTarget.name}</b>");
            }
            else if (previousTarget != null)
            {
                Debug.Log($"<color=orange>[TeammateDetection]</color> Đã tiêu diệt hoặc mất dấu mục tiêu.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}