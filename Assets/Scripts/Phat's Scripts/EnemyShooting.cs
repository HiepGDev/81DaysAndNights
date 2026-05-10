using UnityEngine;
using System.Collections;

public class EnemyShooting : MonoBehaviour
{
    private EnemyDetection detection;
    private Animator animator;

    [Header("Weapon Stats")]
    [SerializeField] private GameObject impactVfxPrefab; 
    [SerializeField] private GameObject tracerPrefab;   
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [Range(0, 1)] [SerializeField] private float shootVolume = 0.4f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float fireDistance = 25.0f; // Reduced from 50.0f
    [SerializeField] private int damagePerShot = 5;

    [Header("Hitscan Settings")]
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Bloom (Recoil) Settings")]
    [SerializeField] private float minSpread = 0.01f;      // Precision of first shot
    [SerializeField] private float maxSpread = 0.08f;      // Max inaccuracy
    [SerializeField] private float bloomIncrease = 0.01f;  // Growth per bullet
    private float currentBloom = 0f;

    [Header("Ammo Settings")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 3.0f;
    
    private int currentAmmo;
    private float nextFireTime;
    private bool isShootingInProgress = false;
    private bool isReloading = false;
    private bool isCrouched = false;

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

        // THE AMBUSH BYPASS: If in Ambush mode, we don't need the detection script!
        EnemyBehaviorAgent behaviorAgent = GetComponent<EnemyBehaviorAgent>();
        bool isAmbushing = (behaviorAgent != null && behaviorAgent.currentMode == EnemyBehaviorAgent.EnemyMode.Ambush);

        // 1. Ammo Check (Always priority)
        if (currentAmmo <= 0)
        {
            TriggerReload();
            return;
        }

        if (behaviorAgent != null && behaviorAgent.IsMovingToCover)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        // 2. TARGET SELECTION: Use detection OR Ambush Target
        Transform target = null;
        if (isAmbushing)
        {
            // Find player directly (Cheating for Ambush Mode)
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
            else if (Time.frameCount % 60 == 0) Debug.LogWarning("[SHOOT] Ambush Mode ON but Player TAG is missing!");
        }
        else if (detection != null && detection.IsTargetDetected)
        {
            target = detection.CurrentTarget;
        }

        if (target == null)
        {
            if (isShootingInProgress) EndShooting();
            if (isAmbushing && Time.frameCount % 60 == 0) Debug.Log("[SHOOT] No target found in Ambush mode.");
            return;
        }

        // 3. MOVEMENT SYNC: Only shoot if the behavior agent is physically ready
        if (behaviorAgent == null || !behaviorAgent.IsReadyToShoot)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        if (!isShootingInProgress) StartShooting();
        
        // Face and Aim
        AimAtTargetManual(target);

        if (Time.time >= nextFireTime && currentAmmo > 0 && allowFiring)
        {
            ShootManual(target);
            nextFireTime = Time.time + fireRate;
        }
    }

    private void AimAtTargetManual(Transform target)
    {
        if (firePoint == null || target == null) return;
        Vector3 targetPos = target.position + Vector3.up * 0.5f;
        firePoint.LookAt(targetPos);
    }

    private void ShootManual(Transform target)
    {
        if (firePoint == null || target == null) return;
        
        currentAmmo--;

        // THE SOUND FIX: Play gunshot sound with volume control
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }
        
        float totalSpread = minSpread + currentBloom;
        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);

        Vector3 targetPoint = target.position + Vector3.up * 0.5f;
        Vector3 baseDir = (targetPoint - firePoint.position).normalized;

        Quaternion bloomRot = Quaternion.Euler(spreadY * 20f, spreadX * 20f, 0);
        Vector3 shootDir = (Quaternion.LookRotation(baseDir) * bloomRot) * Vector3.forward;

        RaycastHit hit;
        Vector3 endPoint = firePoint.position + (shootDir * fireDistance);

        if (Physics.Raycast(firePoint.position, shootDir, out hit, fireDistance, hitLayers))
        {
            endPoint = hit.point;
            var player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null) player.TakeDamage(damagePerShot);
            
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.gameObject != gameObject) enemy.TakeDamage(damagePerShot);

            if (impactVfxPrefab != null)
            {
                Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        if (tracerPrefab != null)
        {
            StartCoroutine(SpawnMovingTracer(firePoint.position, endPoint));
        }

        currentBloom = Mathf.Min(currentBloom + bloomIncrease, maxSpread);
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

    public void TriggerReload()
    {
        if (!isReloading) StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        isShootingInProgress = false; 
        currentBloom = 0; // RESET RECOIL

        EnemyBehaviorAgent behavior = GetComponent<EnemyBehaviorAgent>();
        bool inCover = (behavior != null && behavior.IsInCover);
        bool shouldCrouchReload = isCrouched || inCover;

        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", true);

            if (shouldCrouchReload)
            {
                animator.CrossFade("crouching_reload", 0.1f);
                float animDuration = 1.5f; 
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
        
        if (animator != null && HasParameter("isReloading", animator)) 
            animator.SetBool("isReloading", false);
    }

    private void AimAtTarget()
    {
        if (firePoint == null || detection.CurrentTarget == null) return;
        Vector3 targetPos = detection.CurrentTarget.position + Vector3.up * 0.5f;
        firePoint.LookAt(targetPos);
    }

    private void StartShooting()
    {
        isShootingInProgress = true;
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
        currentBloom = 0; // RESET RECOIL
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
