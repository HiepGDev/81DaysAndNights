using UnityEngine;

public class TankTurretController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform target;

    [Header("Rotation Settings")]
    [SerializeField, Min(0f)] private float rotationSpeed = 45f;

    [Tooltip("Dùng khi hướng forward của model bị lệch.")]
    [SerializeField] private float rotationOffsetY = 0f;

    public Transform CurrentTarget => target;

    private void Update()
    {
        RotateTurret();
    }

    private void RotateTurret()
    {
        if (turretPivot == null || target == null)
            return;

        Vector3 direction = target.position - turretPivot.position;

        // Chỉ quay ngang, không nghiêng tháp pháo lên xuống.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);

        Quaternion offsetRotation =
            Quaternion.Euler(0f, rotationOffsetY, 0f);

        Quaternion desiredRotation = lookRotation * offsetRotation;

        turretPivot.rotation = Quaternion.RotateTowards(
            turretPivot.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ClearTarget()
    {
        target = null;
    }

    private void OnValidate()
    {
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
    }
}