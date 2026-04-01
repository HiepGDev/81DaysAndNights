using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    private EnemyDetection detection;
    private Animator animator;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f; 
    [SerializeField] private float fireDistance = 20.0f;

    private float nextFireTime;
    private bool isShootingState = false;

    private void Awake()
    {
        detection = GetComponent<EnemyDetection>();
    }

    private void Start()
    {
        // Find the animator the same way the Agent does
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            Debug.Log($"[Shooting] Linked to Animator on: {animator.gameObject.name}");
        }
    }

    private void Update()
    {
        if (detection == null || !detection.IsTargetDetected || detection.CurrentTarget == null)
        {
            if (isShootingState) EndShooting();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, detection.CurrentTarget.position);
        
        if (distanceToTarget > fireDistance)
        {
            if (isShootingState) EndShooting();
            return;
        }

        // We are in range and target is detected!
        if (!isShootingState) StartShooting();

        // Aim at target
        Vector3 targetPos = detection.CurrentTarget.position + Vector3.up * 1.2f;
        if (firePoint != null) firePoint.LookAt(targetPos);

        // Fire rate logic
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void StartShooting()
    {
        isShootingState = true;
        if (animator != null)
        {
            Debug.Log("[Shooting] Animation START: Setting isShooting to TRUE");
            animator.SetBool("isShooting", true);
            animator.CrossFade("Enemy_Shooting", 0.1f);
        }
    }

    private void EndShooting()
    {
        isShootingState = false;
        if (animator != null)
        {
            Debug.Log("[Shooting] Animation END: Setting isShooting to FALSE");
            animator.SetBool("isShooting", false);
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        ParticleSystem ps = bullet.GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Play();

        Destroy(bullet, 3.0f);
    }
}
