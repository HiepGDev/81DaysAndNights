using UnityEngine;
using System.Collections;

public class SurvivalEnemyShooting : MonoBehaviour
{
    private EnemyDetection detection;
    private Animator animator;

    [SerializeField] private EnemySO enemyData;

    [Header("Weapon Stats")]
    [SerializeField] private GameObject impactVfxPrefab; 
    [SerializeField] private GameObject muzzleFlashPrefab; 
    [SerializeField] private GameObject tracerPrefab;   
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [Range(0, 1)] [SerializeField] private float shootVolume = 0.4f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float fireDistance = 25.0f; 
    [SerializeField] private int damagePerShot = 5;

    [Header("Hitscan Settings")]
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Bloom (Recoil) Settings")]
    [SerializeField] private float minSpread = 0.01f;      
    [SerializeField] private float maxSpread = 0.08f;      
    [SerializeField] private float bloomIncrease = 0.01f;  
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
        if (enemyData != null)
        {
            fireRate = enemyData.fireRate;
            fireDistance = enemyData.fireDistance;
            damagePerShot = enemyData.damagePerShot;
            magazineSize = enemyData.magazineSize;
            reloadTime = enemyData.reloadTime;

            minSpread = enemyData.minSpread;
            maxSpread = enemyData.maxSpread;
            bloomIncrease = enemyData.bloomIncrease;

            if (enemyData.impactVfxPrefab != null) impactVfxPrefab = enemyData.impactVfxPrefab;
            if (enemyData.muzzleFlashPrefab != null) muzzleFlashPrefab = enemyData.muzzleFlashPrefab;
            if (enemyData.tracerPrefab != null) tracerPrefab = enemyData.tracerPrefab;
            if (enemyData.shootSound != null) shootSound = enemyData.shootSound;
            shootVolume = enemyData.shootVolume;
        }
        currentAmmo = magazineSize;

        int invisibleWallLayer = LayerMask.NameToLayer("Invisible wall");
        if (invisibleWallLayer != -1)
        {
            hitLayers &= ~(1 << invisibleWallLayer);
        }
    }

    private IEnumerator Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();
        
        yield return new WaitForSeconds(0.2f);
        TryFindAudioSource();
    }

    private void TryFindAudioSource()
    {
        if (audioSource != null) return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource != null) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            audioSource = p.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = p.GetComponentInChildren<AudioSource>();
            if (audioSource != null) break;
        }
    }

    private void Update()
    {
        if (detection == null || firePoint == null) return;

        SurvivalEnemyBehaviorAgent behaviorAgent = GetComponent<SurvivalEnemyBehaviorAgent>();
        bool isAmbushing = (behaviorAgent != null && behaviorAgent.currentMode == SurvivalEnemyBehaviorAgent.EnemyMode.Ambush);
        
        Transform currentTarget = isAmbushing ? behaviorAgent.CurrentAmbushTarget : (detection.IsTargetDetected ? detection.CurrentTarget : null);

        if (currentTarget == null || !allowFiring)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        AimAtTarget(currentTarget);

        if (Time.time >= nextFireTime && !isReloading)
        {
            if (currentAmmo > 0)
            {
                if (!isShootingInProgress) StartShooting();
                FireWeapon(currentTarget);
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                TriggerReload();
            }
        }
    }

    private void FireWeapon(Transform target)
    {
        currentAmmo--;

        if (audioSource != null && shootSound != null)
        {
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
        
        float totalSpread = minSpread + currentBloom;
        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);

        Vector3 targetPoint = target.position + Vector3.up * 0.5f;
        Vector3 baseDir = (targetPoint - firePoint.position).normalized;

        Quaternion bloomRot = Quaternion.Euler(spreadY * 20f, spreadX * 20f, 0);
        Vector3 shootDir = (Quaternion.LookRotation(baseDir) * bloomRot) * Vector3.forward;

        Vector3 endPoint = firePoint.position + (shootDir * fireDistance);
        RaycastHit[] hits = Physics.RaycastAll(firePoint.position, shootDir, fireDistance, hitLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool gotHit = false;
        RaycastHit hit = default;

        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            hit = h;
            gotHit = true;
            break;
        }

        if (gotHit)
        {
            endPoint = hit.point;
            
            var player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                Debug.Log($"[SurvivalEnemyShooting] Hit player (singleplayer health): {player.gameObject.name}, applying {damagePerShot} damage.");
                player.TakeDamage(damagePerShot);
            }

            var netPlayer = hit.collider.GetComponentInParent<SurvivalPlayerHealth>();
            if (netPlayer != null)
            {
                Debug.Log($"[SurvivalEnemyShooting] Hit player (net health): {netPlayer.gameObject.name}, applying {damagePerShot} damage. IsServer: {netPlayer.isServer}");
                netPlayer.TakeDamage(damagePerShot);
            }
            
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.gameObject != gameObject)
            {
                enemy.TakeDamage(damagePerShot);
            }

            var teammate = hit.collider.GetComponentInParent<TeammateHealth>();
            if (teammate != null && teammate.gameObject != gameObject)
            {
                teammate.TakeDamage(damagePerShot);
            }

            if (impactVfxPrefab != null)
            {
                Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            Debug.Log($"[SurvivalEnemyShooting] Raycast missed all targets. Start: {firePoint.position}, Direction: {shootDir}");
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
        currentBloom = 0; 

        SurvivalEnemyBehaviorAgent behavior = GetComponent<SurvivalEnemyBehaviorAgent>();
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

    private void AimAtTarget(Transform target)
    {
        if (firePoint == null || target == null) return;
        Vector3 targetPos = target.position + Vector3.up * 0.5f;
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
        currentBloom = 0; 
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
