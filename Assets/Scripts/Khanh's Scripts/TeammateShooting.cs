using UnityEngine;
using System.Collections;
using static TeammateAI;

[RequireComponent(typeof(TeammateDetection))]
public class TeammateShooting : MonoBehaviour
{
    public enum FireMode { Auto, SemiAuto }

    private TeammateDetection detection;
    private Animator animator;
    [SerializeField] private TeammateSO teammateData;

    [Header("Weapon Stats")]
    [SerializeField] private FireMode currentFireMode = FireMode.SemiAuto;

    // Đã thay thế Bullet Prefab bằng Tracer và Impact VFX (Hitscan)
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private GameObject tracerPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float autoFireRate = 0.12f;
    [SerializeField] private float semiFireRateMin = 0.3f;
    [SerializeField] private float semiFireRateMax = 0.6f;
    [SerializeField] private float fireDistance = 25.0f;
    [SerializeField] private int damagePerShot = 15;

    [Header("Hitscan Settings")]
    [Tooltip("Chọn những Layer mà đạn có thể bắn trúng (Enemy, Ground, Wall...)")]
    [SerializeField] private LayerMask hitLayers;

    [Header("Bloom (Recoil) Settings")]
    [SerializeField] private float minSpread = 0.01f;     
    [SerializeField] private float maxSpread = 0.08f;     
    [SerializeField] private float bloomIncrease = 0.01f;  
    private float currentBloom = 0f;

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField][Range(0f, 1f)] private float shootVolume = 0.8f;

    private int currentAmmo;
    private float nextFireTime;
    private bool isShootingInProgress = false;
    private bool isReloading = false;
    private bool isCrouched = false;

    public bool IsOutOfAmmo => currentAmmo <= 0;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        detection = GetComponent<TeammateDetection>();
        currentAmmo = magazineSize;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (teammateData != null)
        {
            currentFireMode = teammateData.fireMode;
            autoFireRate = teammateData.autoFireRate;
            semiFireRateMin = teammateData.semiFireRateMin;
            semiFireRateMax = teammateData.semiFireRateMax;
            fireDistance = teammateData.fireDistance;
            damagePerShot = teammateData.damagePerShot;
            magazineSize = teammateData.magazineSize;
            reloadTime = teammateData.reloadTime;
            minSpread = teammateData.minSpread;
            maxSpread = teammateData.maxSpread;
            bloomIncrease = teammateData.bloomIncrease;

            if (teammateData.impactVfxPrefab != null) impactVfxPrefab = teammateData.impactVfxPrefab;
            if (teammateData.muzzleFlashPrefab != null) muzzleFlashPrefab = teammateData.muzzleFlashPrefab;
            if (teammateData.tracerPrefab != null) tracerPrefab = teammateData.tracerPrefab;
            if (teammateData.shootSound != null) shootSound = teammateData.shootSound;
            if (teammateData.reloadSound != null) reloadSound = teammateData.reloadSound;
            shootVolume = teammateData.shootVolume;
        }
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isReloading || currentAmmo <= 0) return;

        if (detection == null || detection.CurrentTarget == null)
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

        if (Time.time >= nextFireTime)
        {
            AimAtTarget();
            ShootHitscan(); // Chuyển sang dùng hàm Hitscan

            if (currentFireMode == FireMode.Auto)
                nextFireTime = Time.time + autoFireRate;
            else
            {
                nextFireTime = Time.time + Random.Range(semiFireRateMin, semiFireRateMax);
                currentBloom = 0f;
            }
        }
    }

    private void AimAtTarget()
    {
        if (firePoint == null || detection.CurrentTarget == null) return;

        Vector3 targetPos;
        Collider targetCollider = detection.CurrentTarget.GetComponent<Collider>();

        if (targetCollider != null)
            targetPos = targetCollider.bounds.center;
        else
            targetPos = detection.CurrentTarget.position + Vector3.up * 1.2f;

        Vector3 direction = (targetPos - firePoint.position).normalized;
        firePoint.rotation = Quaternion.LookRotation(direction);
    }

    private void ShootHitscan()
    {
        if (firePoint == null) return;
        currentAmmo--;

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound, shootVolume);

        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            muzzleFlash.transform.SetParent(firePoint);
            Destroy(muzzleFlash, 0.05f);
        }

        // Tính toán Bloom (Độ giật nảy ngẫu nhiên)
        float totalSpread = minSpread + currentBloom;
        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);

        // Áp dụng độ tản mát đạn vào hướng nòng súng
        Vector3 baseDir = firePoint.forward;
        Quaternion bloomRot = Quaternion.Euler(spreadY * 20f, spreadX * 20f, 0);
        Vector3 shootDir = (Quaternion.LookRotation(baseDir) * bloomRot) * Vector3.forward;

        RaycastHit hit;
        Vector3 endPoint = firePoint.position + (shootDir * fireDistance);

        if (Physics.Raycast(firePoint.position, shootDir, out hit, fireDistance, hitLayers))
        {
            endPoint = hit.point;

            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damagePerShot);

            if (impactVfxPrefab != null)
            {
                Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        // Vẽ vệt đạn Laser bay đi
        if (tracerPrefab != null)
        {
            StartCoroutine(SpawnMovingTracer(firePoint.position, endPoint));
        }

        // Tăng độ nở tâm (Sấy càng lâu đạn càng giật)
        currentBloom = Mathf.Min(currentBloom + bloomIncrease, maxSpread);

        if (currentAmmo <= 0) EndShooting();
    }

    private IEnumerator SpawnMovingTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObj = Instantiate(tracerPrefab, start, Quaternion.identity);
        LineRenderer line = tracerObj.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.useWorldSpace = true;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.05f;
            line.endWidth = 0.01f;
            line.startColor = Color.yellow;
            line.endColor = new Color(1f, 1f, 0f, 0f);
        }

        float travelSpeed = 400f;
        float distance = Vector3.Distance(start, end);
        float remainingDistance = distance;

        while (remainingDistance > 1.0f)
        {
            if (tracerObj == null) yield break;
            float moveStep = travelSpeed * Time.deltaTime;
            tracerObj.transform.position = Vector3.MoveTowards(tracerObj.transform.position, end, moveStep);
            remainingDistance -= moveStep;
            yield return null;
        }

        if (tracerObj != null)
        {
            tracerObj.transform.position = end;
            Destroy(tracerObj, 0.1f);
        }
    }

    private void StartShooting()
    {
        isShootingInProgress = true;
        currentBloom = 0f; // Bắt đầu xả đạn thì tâm phải chuẩn
        nextFireTime = Time.time + 0.25f;
        isCrouched = false;

        if (animator != null)
        {
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", true);
            animator.CrossFade("Enemy_Shooting", 0.1f);
        }
    }

    public void EndShooting()
    {
        isShootingInProgress = false;
        currentBloom = 0f; // Ngừng xả đạn -> reset tâm
        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            animator.CrossFade("Enemy_Idle", 0.2f);
        }
    }

    public void TriggerReload()
    {
        if (!isReloading) StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        isShootingInProgress = false;
        currentBloom = 0f; 

        if(audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound, shootVolume);

        TeammateAI behavior = GetComponent<TeammateAI>();
        bool inCover = (behavior != null && behavior.CurrentState == TeammateAI.TeammateState.SeekingCover);
        bool shouldCrouchReload = isCrouched || inCover;

        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", true);

            if (shouldCrouchReload)
            {
                animator.CrossFade("crouching_reload", 0.1f);
                float animDuration = 3.2f;
                yield return new WaitForSeconds(animDuration);

                if (isReloading && inCover)
                {
                    animator.CrossFade("Cover_Crouching", 0.2f);
                }

                float remaining = Mathf.Max(0, reloadTime - animDuration);
                if (remaining > 0) yield return new WaitForSeconds(remaining);
            }
            else
            {
                animator.CrossFade("reload_standing", 0.1f);
                yield return new WaitForSeconds(reloadTime);
            }
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        currentAmmo = magazineSize;
        isReloading = false;

        if (animator != null)
        {
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", false);

            if (!inCover)
            {
                if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
                animator.CrossFade("Enemy_Idle", 0.1f);
            }
        }

        Debug.Log("[Teammate Weapon] Reload Complete.");
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }
}