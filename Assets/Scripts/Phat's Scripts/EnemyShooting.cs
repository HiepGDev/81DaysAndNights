using UnityEngine;
using System.Collections;

public class EnemyShooting : MonoBehaviour
{
    private EnemyDetection detection;
    private Animator animator;

    [Header("Weapon Stats")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.12f;
    [SerializeField] private float fireDistance = 25.0f;

    [Header("CS:GO Style Spray Pattern")]
    [Tooltip("X = Horizontal Offset, Y = Vertical Offset. Bullets follow this sequence.")]
    [SerializeField] private Vector2[] sprayPattern = new Vector2[] 
    {
        new Vector2(0, 0),       // Shot 1
        new Vector2(0, 0.2f),    // Shot 2: Tiny bit up
        new Vector2(0, 0.5f),    // Shot 3
        new Vector2(-0.2f, 0.8f),// Shot 4: Slight Left
        new Vector2(-0.4f, 1.0f),// Shot 5
        new Vector2(0.2f, 1.2f), // Shot 6: Back Right
        new Vector2(0.5f, 1.3f), // Shot 7
        new Vector2(0.3f, 1.1f), // Shot 8
        new Vector2(-0.1f, 0.9f),// Shot 9
        new Vector2(-0.3f, 1.0f) // Shot 10
    };
    [SerializeField] private float patternScale = 0.5f; // Lower default scale

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.5f;
    
    private int currentAmmo;
    private int recoilIndex = 0; // Tracks which shot in the pattern we are on
    private float nextFireTime;
    private bool isShootingInProgress = false;
    private bool isReloading = false;
    private bool isCrouched = false;

    private void Awake()
    {
        detection = GetComponent<EnemyDetection>();
        currentAmmo = magazineSize;
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isReloading) return;

        if (detection == null || !detection.IsTargetDetected || detection.CurrentTarget == null)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, detection.CurrentTarget.position);
        if (distanceToTarget > fireDistance)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        if (!isShootingInProgress) StartShooting();

        AimAtTarget();

        if (Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void AimAtTarget()
    {
        if (firePoint == null) return;
        
        float aimHeight = isCrouched ? 0.6f : 1.2f; 
        Vector3 targetPos = detection.CurrentTarget.position + Vector3.up * aimHeight;
        firePoint.LookAt(targetPos);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;
        currentAmmo--;

        // 1. Get the current offset from our spray pattern
        Vector2 patternOffset = Vector2.zero;
        if (sprayPattern != null && sprayPattern.Length > 0)
        {
            // Loop the pattern if it's shorter than the magazine
            patternOffset = sprayPattern[recoilIndex % sprayPattern.Length] * patternScale;
        }

        // 2. Apply the pattern to the rotation
        // Vertical offset (Y) affects pitch (Rotation X)
        // Horizontal offset (X) affects yaw (Rotation Y)
        Quaternion recoilRotation = Quaternion.Euler(-patternOffset.y, patternOffset.x, 0);
        Quaternion finalRotation = firePoint.rotation * recoilRotation;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, finalRotation);
        
        ParticleSystem ps = bullet.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.Play();
        }

        // 3. Move to the next shot in the pattern
        recoilIndex++;

        Destroy(bullet, 2.0f);
    }

    private void StartShooting()
    {
        isShootingInProgress = true;
        recoilIndex = 0; // RESET spray pattern when starting to fire
        isCrouched = Random.value > 0.5f;

        if (animator != null)
        {
            animator.SetBool("isShooting", false);
            animator.SetBool("isCrouching", false);

            if (isCrouched)
            {
                animator.SetBool("isCrouching", true);
                animator.CrossFade("Enemy_CrouchShooting", 0.1f);
            }
            else
            {
                animator.SetBool("isShooting", true);
                animator.CrossFade("Enemy_Shooting", 0.1f);
            }
        }
    }

    private void EndShooting()
    {
        isShootingInProgress = false;
        recoilIndex = 0; // RESET spray pattern when target lost
        if (animator != null)
        {
            animator.SetBool("isShooting", false);
            animator.SetBool("isCrouching", false);
            animator.CrossFade("Enemy_Idle", 0.2f);
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        recoilIndex = 0; // RESET spray pattern on reload
        if (animator != null)
        {
            animator.SetBool("isShooting", false);
            animator.SetBool("isCrouching", false);
            animator.SetBool("isReloading", true);
            animator.CrossFade("Enemy_Reload", 0.1f);
        }
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        isReloading = false;
        if (animator != null) animator.SetBool("isReloading", false);
    }
}
