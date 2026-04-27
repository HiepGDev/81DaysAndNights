using UnityEngine;
using System.Collections;

public class EnemyShooting : MonoBehaviour
{
    private EnemyDetection detection;
    private Animator animator;

    [Header("Weapon Stats")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float fireDistance = 25.0f;

    [Header("CS:GO Style Spray Pattern")]
    [SerializeField] private Vector2[] sprayPattern = new Vector2[] 
    {
        new Vector2(0, 0), new Vector2(0, 0.2f), new Vector2(0, 0.5f), 
        new Vector2(-0.2f, 0.8f), new Vector2(-0.4f, 1.0f), new Vector2(0.2f, 1.2f)
    };
    [SerializeField] private float patternScale = 0.5f;

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.5f;
    
    private int currentAmmo;
    private int recoilIndex = 0; 
    private float nextFireTime;
    private bool isShootingInProgress = false;
    private bool isReloading = false;
    private bool isCrouched = false;

    // THE 1-BULLET FIX: Allow external control (Peek script)
    [HideInInspector] public bool allowFiring = true;

    public bool IsOutOfAmmo => currentAmmo <= 0;
    public bool IsReloading => isReloading;
    public float FireDistance => fireDistance;

    private void Awake()
    {
        detection = GetComponent<EnemyDetection>();
        currentAmmo = magazineSize;
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isReloading) return;

        EnemyBehaviorAgent agent = GetComponent<EnemyBehaviorAgent>();
        if (agent != null && agent.IsMovingToCover)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

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

        if (Time.time >= nextFireTime && currentAmmo > 0 && allowFiring)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
        else if (currentAmmo <= 0 && !isReloading)
        {
            if (isShootingInProgress) EndShooting();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        currentAmmo--;
        Debug.Log($"[Enemy Weapon] Shot Fired! Ammo: {currentAmmo}/{magazineSize}");

        Vector2 patternOffset = Vector2.zero;
        if (sprayPattern != null && sprayPattern.Length > 0)
        {
            patternOffset = sprayPattern[recoilIndex % sprayPattern.Length] * patternScale;
        }

        Quaternion recoilRotation = Quaternion.Euler(-patternOffset.y, patternOffset.x, 0);
        Quaternion finalRotation = firePoint.rotation * recoilRotation;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, finalRotation);
        
        ParticleSystem ps = bullet.GetComponent<ParticleSystem>();
        if (ps == null) ps = bullet.GetComponentInChildren<ParticleSystem>();
        
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { });
            ps.Emit(1);
        }

        recoilIndex++;
        Destroy(bullet, 2.0f);
    }

    public void TriggerReload()
    {
        if (!isReloading) StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        recoilIndex = 0;
        Debug.Log($"[Enemy Weapon] Reloading...");

        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            
            if (HasParameter("isReloading", animator)) 
            {
                animator.SetBool("isReloading", true);
                animator.CrossFade("Enemy_Reload", 0.1f);
            }
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
        
        if (animator != null && HasParameter("isReloading", animator)) 
            animator.SetBool("isReloading", false);
            
        Debug.Log("[Enemy Weapon] Reload Complete.");
    }

    private void AimAtTarget()
    {
        if (firePoint == null) return;
        float aimHeight = isCrouched ? 0.6f : 1.2f; 
        Vector3 targetPos = detection.CurrentTarget.position + Vector3.up * aimHeight;
        firePoint.LookAt(targetPos);
    }

    private void StartShooting()
    {
        isShootingInProgress = true;
        recoilIndex = 0;
        isCrouched = Random.value > 0.5f;
        if (animator != null)
        {
            if (isCrouched)
            {
                if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", true);
                animator.CrossFade("Enemy_CrouchShooting", 0.1f);
            }
            else
            {
                if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", true);
                animator.CrossFade("Enemy_Shooting", 0.1f);
            }
        }
    }

    private void EndShooting()
    {
        isShootingInProgress = false;
        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            animator.CrossFade("Enemy_Idle", 0.2f);
        }
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }
}
