using UnityEngine;

[RequireComponent(typeof(TeammateDetection))]
public class TeammateShooting : MonoBehaviour
{
    private TeammateDetection detection;
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
        detection = GetComponent<TeammateDetection>();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (bulletPrefab == null)
            Debug.LogError($"[TeammateShooting] '{gameObject.name}': bulletPrefab chưa được gán!", this);
        if (firePoint == null)
            Debug.LogError($"[TeammateShooting] '{gameObject.name}': firePoint chưa được gán!", this);
        if (detection == null)
            Debug.LogError($"[TeammateShooting] '{gameObject.name}': Không tìm thấy TeammateDetection!", this);
    }

    private void Update()
    {
        if (detection == null || detection.CurrentTarget == null)
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

        if (!isShootingState) StartShooting();

        // Xoay firePoint nhắm vào giữa người địc
        if (firePoint != null)
        {
            Vector3 targetPos = detection.CurrentTarget.position + Vector3.up * 1.2f;
            firePoint.LookAt(targetPos);
        }

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
            animator.SetBool("isShooting", true);
            animator.CrossFade("Enemy_Shooting", 0.1f);
        }
    }

    private void EndShooting()
    {
        isShootingState = false;
        if (animator != null)
            animator.SetBool("isShooting", false);
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[TeammateShooting] Shoot() bị bỏ qua: bulletPrefab là null!");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogWarning("[TeammateShooting] Shoot() bị bỏ qua: firePoint là null!");
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        ParticleSystem ps = bullet.GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Play();

        Destroy(bullet, 3.0f);
    }
}