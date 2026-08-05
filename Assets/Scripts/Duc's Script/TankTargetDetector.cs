using UnityEngine;

[RequireComponent(typeof(TankTurretController))]
public class TankTargetDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTurretController turretController;
    [SerializeField] private Transform detectionOrigin;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 40f;

    [Tooltip("Player + Teammate")]
    [SerializeField] private string[] targetTags =
    {
        "Player",
        "Teammate"
    };

    public Transform CurrentTarget { get; private set; }

    private void Awake()
    {
        if (turretController == null)
            turretController = GetComponent<TankTurretController>();

        if (detectionOrigin == null)
            detectionOrigin = transform;
    }

    private void Update()
    {
        FindNearestTarget();
    }

    private void FindNearestTarget()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                detectionOrigin.position,
                detectionRadius);

        Transform nearest = null;

        float nearestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            foreach (string tag in targetTags)
            {
                if (!hit.CompareTag(tag))
                    continue;

                float distance =
                    Vector3.Distance(
                        detectionOrigin.position,
                        hit.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = hit.transform;
                }
            }
        }

        if (nearest != CurrentTarget)
        {
            CurrentTarget = nearest;

            turretController.SetTarget(CurrentTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Transform origin =
            detectionOrigin != null ?
            detectionOrigin :
            transform;

        Gizmos.DrawWireSphere(
            origin.position,
            detectionRadius);

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                origin.position,
                CurrentTarget.position);
        }
    }
}