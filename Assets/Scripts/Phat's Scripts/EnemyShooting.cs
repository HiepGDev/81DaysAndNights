using UnityEngine;
using System.Collections;

public class EnemyShooting : MonoBehaviour
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
    private float stopShootingTimer = 0f;

    [HideInInspector] public bool allowFiring = true;
    [HideInInspector] public int totalDamageDealt = 0;

    public bool IsOutOfAmmo => currentAmmo <= 0;
    public bool IsReloading => isReloading;
    public float FireDistance => fireDistance;
    public bool IsShootingInProgress => isShootingInProgress;

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

        // Exclude the "Invisible wall" layer from being hit by enemy bullets
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
        
        // THE RACE CONDITION FIX: Wait a bit longer for the player to fully wake up
        yield return new WaitForSeconds(0.2f);
        TryFindAudioSource();
    }

    private void TryFindAudioSource()
    {
        if (audioSource != null) return;

        // 1. Try local first
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Ensure the gunshot sounds 3D
        audioSource.maxDistance = fireDistance * 2; // Hear it from afar
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

        // 3. FALLBACK: Search by name if tags are broken
        GameObject namePlayer = GameObject.Find("Player");
        if (namePlayer != null)
        {
            audioSource = namePlayer.GetComponentInChildren<AudioSource>();
            if (audioSource != null) return;
        }

        // 4. LAST RESORT: Find ANY AudioSource in the world
        if (audioSource == null)
        {
            audioSource = Object.FindFirstObjectByType<AudioSource>();
        }

        if (audioSource == null)
            Debug.LogWarning("[Enemy Audio] Still could not find an AudioSource anywhere in the scene!");
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
            float distToDest = 0f;
            var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && navAgent.isOnNavMesh) 
                distToDest = navAgent.remainingDistance;

            if (distToDest >= 3.0f)
            {
                isCrouched = false; // Stand up to run long distance to cover
                SetShootingLayerWeight(0f); // Disable shooting layer weight immediately when running
            }

            if (isShootingInProgress) EndShooting();
            return;
        }

        // 2. TARGET SELECTION: Sync with Behavior Agent
        Transform target = null;
        if (isAmbushing)
        {
            target = (behaviorAgent != null) ? behaviorAgent.CurrentAmbushTarget : null;
        }
        else if (detection != null && detection.IsTargetDetected)
        {
            target = detection.CurrentTarget;
        }

        if (target == null)
        {
            if (isShootingInProgress) EndShooting();
            return;
        }

        // 3. MOVEMENT SYNC: Only shoot if the behavior agent is physically ready
        if (behaviorAgent == null || !behaviorAgent.IsReadyToShoot)
        {
            // Only stand up to run if we are moving a significant distance (>= 3.0 meters)
            float distToDest = 0f;
            if (behaviorAgent != null)
            {
                var navAgent = behaviorAgent.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null && navAgent.isOnNavMesh) 
                    distToDest = navAgent.remainingDistance;
            }

            if (distToDest >= 3.0f)
            {
                isCrouched = false; // Stand up to run long distance
                SetShootingLayerWeight(0f); // Disable shooting layer weight immediately when running
            }

            if (isShootingInProgress)
            {
                EndShooting();
            }
            return;
        }

        stopShootingTimer = 0f; // Reset debounce timer when ready
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
            audioSource.pitch = Random.Range(0.95f, 1.0f);
            audioSource.PlayOneShot(shootSound, shootVolume);
        }

        // THE MUZZLE FLASH FIX: Explicitly play ParticleSystems
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            
            // Check if it's a particle system and play it
            ParticleSystem ps = flash.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            else
            {
                // If it's a list of children particles, play all of them
                foreach (var childPs in flash.GetComponentsInChildren<ParticleSystem>())
                    childPs.Play();
            }

            // Destroy only after a few seconds to let the particles fade naturally
            Destroy(flash, 1.0f); 
        }
        
        float totalSpread = minSpread + currentBloom;
        float spreadX = Random.Range(-totalSpread, totalSpread);
        float spreadY = Random.Range(-totalSpread, totalSpread);

        Vector3 targetPoint = target.position + Vector3.up * 0.5f;
        
        // THE PEEK WALL FIX: If the enemy is in cover, start the damage raycast from their eyes (head level) to prevent clipping walls!
        // Visually, the muzzle flash and tracer will still start at the gun (firePoint.position), but the actual raycast originates from their eyes.
        EnemyBehaviorAgent behavior = GetComponent<EnemyBehaviorAgent>();
        Vector3 rayOrigin = (behavior != null && behavior.IsInCover) ? (transform.position + Vector3.up * 1.5f) : firePoint.position;
        Vector3 baseDir = (targetPoint - rayOrigin).normalized;

        Quaternion bloomRot = Quaternion.Euler(spreadY * 20f, spreadX * 20f, 0);
        Vector3 shootDir = (Quaternion.LookRotation(baseDir) * bloomRot) * Vector3.forward;

        RaycastHit hit;
        Vector3 endPoint = rayOrigin + (shootDir * fireDistance);

        if (Physics.Raycast(rayOrigin, shootDir, out hit, fireDistance, hitLayers))
        {
            endPoint = hit.point;
            var player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damagePerShot);
                totalDamageDealt += damagePerShot;
            }
            
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.gameObject != gameObject)
            {
                enemy.TakeDamage(damagePerShot);
                totalDamageDealt += damagePerShot;
            }

            var teammate = hit.collider.GetComponentInParent<TeammateHealth>();
            if (teammate != null && teammate.gameObject != gameObject)
            {
                int finalDamage = damagePerShot;
                var bodyPart = hit.collider.GetComponent<TeammateBodyPart>();
                if (bodyPart != null)
                {
                    finalDamage = Mathf.RoundToInt(damagePerShot * bodyPart.damageMultiplier);
                }
                teammate.TakeDamage(finalDamage);
                totalDamageDealt += finalDamage;
            }

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
        if (isReloading) return;
        SetShootingLayerWeight(1f); // Make sure shooting/upper-body layer is enabled for reloading
        EndShooting();
        StartCoroutine(ReloadRoutine());
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
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", shouldCrouchReload);
            if (HasParameter("isCovering", animator)) animator.SetBool("isCovering", inCover);
            if (HasParameter("isReloading", animator)) animator.SetBool("isReloading", true);
        }

        if (animator != null)
        {
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

        if (!isShootingInProgress)
        {
            SetShootingLayerWeight(0f); // Disable layer if we are not actively shooting
        }
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
        SetShootingLayerWeight(1f); // Enable shooting/upper-body layer

        bool canCrouchShoot = false;
        EnemyBehaviorAgent behaviorAgent = GetComponent<EnemyBehaviorAgent>();
        if (behaviorAgent != null)
        {
            // Check if the agent is actively moving
            bool isMoving = behaviorAgent.GetComponent<UnityEngine.AI.NavMeshAgent>().velocity.sqrMagnitude > 0.1f;
            // Allow crouching in the open if they are a Sniper, an Ambush unit, or currently behind cover
            // Do not allow crouching if they are running!
            canCrouchShoot = !isMoving && (behaviorAgent.currentMode == EnemyBehaviorAgent.EnemyMode.Sniper || 
                              behaviorAgent.currentMode == EnemyBehaviorAgent.EnemyMode.Ambush || 
                              behaviorAgent.IsInCover);
        }

        // If we are already crouched, preserve the stance. Otherwise, roll a new one.
        if (!isCrouched)
        {
            isCrouched = canCrouchShoot && (Random.value > 0.5f);
        }

        if (animator != null)
        {
            if (isCrouched)
            {
                if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", true);
                if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", true);
                animator.CrossFade("Enemy_CrouchShooting", 0.1f);
            }
            else
            {
                if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", false);
                if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", true);
                animator.CrossFade("Enemy_Shooting", 0.1f);
            }
        }
    }

    private void EndShooting()
    {
        isShootingInProgress = false;
        currentBloom = 0; // RESET RECOIL
        SetShootingLayerWeight(0f); // Disable shooting layer weight
        if (animator != null)
        {
            if (HasParameter("isShooting", animator)) animator.SetBool("isShooting", false);
            
            // If the enemy is in cover or was already crouched, they should stay crouched instead of standing up
            EnemyBehaviorAgent behaviorAgent = GetComponent<EnemyBehaviorAgent>();
            // Don't crouch if they are still physically running to the cover
            bool isMovingToCover = (behaviorAgent != null && behaviorAgent.IsMovingToCover);
            bool shouldCrouch = (behaviorAgent != null && (behaviorAgent.IsInCover || isCrouched)) && !isMovingToCover;
            if (HasParameter("isCrouching", animator)) animator.SetBool("isCrouching", shouldCrouch);

            animator.CrossFade(shouldCrouch ? "Cover_Crouching" : "Enemy_Idle", 0.2f);
        }
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }

    private void SetShootingLayerWeight(float weight)
    {
        if (animator != null && animator.layerCount > 1)
        {
            animator.SetLayerWeight(1, weight);
        }
    }
}
