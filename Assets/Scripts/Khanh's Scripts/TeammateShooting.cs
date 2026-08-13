using UnityEngine;
using System.Collections;
using static TeammateAI;

[RequireComponent(typeof(TeammateDetection))]
public class TeammateShooting : MonoBehaviour
{
    public enum FireMode { Auto, Single }

    private TeammateDetection detection;
    private Animator animator;
    [SerializeField] private TeammateSO teammateData;

    [Header("Weapon Stats")]
    [SerializeField] private FireMode currentFireMode = FireMode.Single;

    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private GameObject tracerPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject muzzleFlashPrefab;

    [SerializeField] private float autoFireRate = 0.12f;
    [SerializeField] private float singleFireRateMin = 0.3f;
    [SerializeField] private float singleFireRateMax = 0.6f;
    [SerializeField] private float fireDistance = 25.0f;
    [SerializeField] private int damagePerShot = 15;

    [Header("Hitscan Settings")]
    [SerializeField] private LayerMask hitLayers;

    [Header("Bloom (Recoil) - Single Mode")]
    [SerializeField] private float singleMinSpread = 0.03f;
    [SerializeField] private float singleMaxSpread = 0.15f;
    [SerializeField] private float singleBloomIncrease = 0.02f;

    [Header("Bloom (Recoil) - Auto Mode")]
    [SerializeField] private float autoMinSpread = 0.05f;
    [SerializeField] private float autoMaxSpread = 0.25f;
    [SerializeField] private float autoBloomIncrease = 0.035f;

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
            singleFireRateMin = teammateData.singleFireRateMin;
            singleFireRateMax = teammateData.singleFireRateMax;
            fireDistance = teammateData.fireDistance;
            damagePerShot = teammateData.damagePerShot;
            magazineSize = teammateData.magazineSize;
            reloadTime = teammateData.reloadTime;

            // Load dữ liệu Bloom
            singleMinSpread = teammateData.singleMinSpread;
            singleMaxSpread = teammateData.singleMaxSpread;
            singleBloomIncrease = teammateData.singleBloomIncrease;

            autoMinSpread = teammateData.autoMinSpread;
            autoMaxSpread = teammateData.autoMaxSpread;
            autoBloomIncrease = teammateData.autoBloomIncrease;

            if (teammateData.impactVfxPrefab != null) impactVfxPrefab = teammateData.impactVfxPrefab;
            if (teammateData.muzzleFlashPrefab != null) muzzleFlashPrefab = teammateData.muzzleFlashPrefab;
            if (teammateData.tracerPrefab != null) tracerPrefab = teammateData.tracerPrefab;
            if (teammateData.shootSound != null) shootSound = teammateData.shootSound;
            if (teammateData.reloadSound != null) reloadSound = teammateData.reloadSound;
            shootVolume = teammateData.shootVolume;
        }
        int invisibleWallLayer = LayerMask.NameToLayer("Invisible wall");
        if (invisibleWallLayer != -1)
        {
            hitLayers &= ~(1 << invisibleWallLayer);
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

        if (!isShootingInProgress) StartShooting();

        if (Time.time >= nextFireTime)
        {
            AimAtTarget();
            ShootHitscan();

            if (currentFireMode == FireMode.Auto)
                nextFireTime = Time.time + autoFireRate;
            else
            {
                nextFireTime = Time.time + Random.Range(singleFireRateMin, singleFireRateMax);
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
        {
            audioSource.pitch = Random.Range(0.95f, 1.0f);
            audioSource.PlayOneShot(shootSound, shootVolume);
        }

        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);

            ParticleSystem ps = flash.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            else
            {
                foreach (var childPs in flash.GetComponentsInChildren<ParticleSystem>())
                    childPs.Play();
            }

            Destroy(flash, 1.0f);
        }

        float activeMinSpread = currentFireMode == FireMode.Single ? singleMinSpread : autoMinSpread;
        float activeMaxSpread = currentFireMode == FireMode.Single ? singleMaxSpread : autoMaxSpread;
        float activeBloomIncrease = currentFireMode == FireMode.Single ? singleBloomIncrease : autoBloomIncrease;

        float totalSpread = activeMinSpread + currentBloom;
        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);

        Vector3 baseDir = firePoint.forward;
        Quaternion bloomRot = Quaternion.Euler(spreadY * 20f, spreadX * 20f, 0);
        Vector3 shootDir = (Quaternion.LookRotation(baseDir) * bloomRot) * Vector3.forward;

        RaycastHit hit;
        Vector3 endPoint = firePoint.position + (shootDir * fireDistance);

        if (Physics.Raycast(firePoint.position, shootDir, out hit, fireDistance, hitLayers))
        {
            endPoint = hit.point;

            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            var finalDamage = damagePerShot;
            var bodyPart = hit.collider.GetComponent<EnemyBodyPart>();
            if (bodyPart != null)
            {
                finalDamage = Mathf.RoundToInt(damagePerShot * bodyPart.damageMultiplier);
            }

            if (enemy != null) enemy.TakeDamage(finalDamage);

            if (impactVfxPrefab != null)
            {
                Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        if (tracerPrefab != null)
        {
            StartCoroutine(SpawnMovingTracer(firePoint.position, endPoint));
        }

        // Tăng độ giật cho viên tiếp theo
        currentBloom = Mathf.Min(currentBloom + activeBloomIncrease, activeMaxSpread);

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
        currentBloom = 0f;
        nextFireTime = Time.time + 0.4f;
        isCrouched = false;

        if (animator != null)
        {
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", true);
            animator.CrossFade("Enemy_Shooting", 0.1f, 0, 0f);
        }
    }

    public void EndShooting()
    {
        isShootingInProgress = false;
        currentBloom = 0f;
        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (currentAmmo > 0)
            {
                animator.CrossFade("Enemy_Idle", 0.2f);
            }
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

        if (audioSource != null && reloadSound != null)
        {
            audioSource.pitch = 1f; 
            audioSource.PlayOneShot(reloadSound, shootVolume);
        }
        
        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", true);

            animator.CrossFade("crouching_reload", 0.1f);

            float animDuration = 2.5f;
            yield return new WaitForSeconds(animDuration);

            float remaining = Mathf.Max(0, reloadTime - animDuration);
            if (remaining > 0) yield return new WaitForSeconds(remaining);
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        currentAmmo = magazineSize;

        if (animator != null)
        {
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);

            if (detection == null || detection.CurrentTarget == null)
            {
                animator.CrossFade("Enemy_Idle", 0.1f);
            }
        }

        yield return null;

        isReloading = false;

        Debug.Log("[Teammate Weapon] Tactical Crouch Reload Complete.");
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }
}