using UnityEngine;

[RequireComponent(typeof(TankTargetDetector))]
public class TankWeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTargetDetector targetDetector;
    [SerializeField] private Transform firePoint;
    [SerializeField] private TankShell shellPrefab;

    [Header("Fire Timing")]
    [SerializeField, Min(0f)] private float minimumFireInterval = 5f;
    [SerializeField, Min(0f)] private float maximumFireInterval = 7f;

    [Header("Shell Settings")]
    [SerializeField, Min(0f)] private float shellSpeed = 30f;
    [SerializeField, Min(0f)] private float shellDamage = 50f;
    [SerializeField, Min(0f)] private float explosionRadius = 5f;

    [Header("Aiming")]
    [SerializeField, Range(0f, 180f)]
    private float allowedAimError = 8f;

    [Header("Optional Effects")]
    [SerializeField] private GameObject muzzleEffectPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;

    private float nextFireTime;

    private void Awake()
    {
        if (targetDetector == null)
        {
            targetDetector =
                GetComponent<TankTargetDetector>();
        }
    }

    private void Start()
    {
        ScheduleNextShot();
    }

    private void Update()
    {
        if (!CanFire())
            return;

        Fire();
        ScheduleNextShot();
    }

    private bool CanFire()
    {
        if (Time.time < nextFireTime)
            return false;

        if (targetDetector == null ||
            targetDetector.CurrentTarget == null)
        {
            return false;
        }

        if (firePoint == null || shellPrefab == null)
            return false;

        Vector3 aimDirection =
    GetTargetPosition(targetDetector.CurrentTarget) -
    firePoint.position;

// Hiện tại tháp pháo chỉ quay ngang,
// nên kiểm tra độ chính xác trên mặt phẳng ngang.
aimDirection.y = 0f;

Vector3 fireForward = firePoint.forward;
fireForward.y = 0f;

if (aimDirection.sqrMagnitude < 0.001f ||
    fireForward.sqrMagnitude < 0.001f)
{
    return false;
}

float aimError = Vector3.Angle(
    fireForward.normalized,
    aimDirection.normalized
);

return aimError <= allowedAimError;
    }

    private void Fire()
    {
        Transform currentTarget =
            targetDetector.CurrentTarget;

        if (currentTarget == null)
            return;

        Vector3 targetPosition =
            GetTargetPosition(currentTarget);

        Vector3 fireDirection =
            (targetPosition - firePoint.position).normalized;

        TankShell spawnedShell = Instantiate(
            shellPrefab,
            firePoint.position,
            Quaternion.LookRotation(fireDirection)
        );

        spawnedShell.Initialize(
            fireDirection,
            shellSpeed,
            shellDamage,
            explosionRadius,
            transform
        );

        SpawnOptionalEffects();

        Debug.Log(
            $"[TankWeaponController] Fired at " +
            $"{currentTarget.name}"
        );
    }

    private Vector3 GetTargetPosition(Transform target)
    {
        Collider targetCollider =
            target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
            return targetCollider.bounds.center;

        return target.position;
    }

    private void ScheduleNextShot()
    {
        float minInterval =
            Mathf.Min(
                minimumFireInterval,
                maximumFireInterval
            );

        float maxInterval =
            Mathf.Max(
                minimumFireInterval,
                maximumFireInterval
            );

        nextFireTime =
            Time.time + Random.Range(
                minInterval,
                maxInterval
            );
    }

private void SpawnOptionalEffects()
{
    if (muzzleEffectPrefab != null)
    {
        GameObject muzzleEffect = Instantiate(
            muzzleEffectPrefab,
            firePoint.position,
            firePoint.rotation,
            firePoint
        );

        // Play toàn bộ Particle System bên trong Tank_Blast_VFX
        ParticleSystem[] particles =
            muzzleEffect.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }

        // Xóa VFX sau khi chạy xong
        Destroy(muzzleEffect, 3f);
    }

    if (audioSource != null && fireSound != null)
    {
        audioSource.PlayOneShot(fireSound);
    }
}

    private void OnValidate()
    {
        minimumFireInterval =
            Mathf.Max(0f, minimumFireInterval);

        maximumFireInterval =
            Mathf.Max(0f, maximumFireInterval);

        shellSpeed = Mathf.Max(0f, shellSpeed);
        shellDamage = Mathf.Max(0f, shellDamage);
        explosionRadius = Mathf.Max(0f, explosionRadius);
    }
}