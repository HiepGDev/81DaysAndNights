using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;
using PhuScene;

public class SurvivalPlayerGun : NetworkBehaviour
{
    [SerializeField] WeaponSO gunData;
    public WeaponSO WeaponData => gunData;
    [SerializeField] private GunRecoil recoil;
    [SerializeField] private CrosshairController crosshair;
    private SurvivalPlayerMovement playerMovement;
    InputAction shootAction; 
    InputAction reloadAction; 
    InputAction aimAction;
    public bool isAiming; 
    private AudioSource audioSource; 
    [SerializeField] private LayerMask interactionLayers; 
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject bloodEffectPrefab;
    private float nextFireTime = 0f;
    private Animator animator;
    [SerializeField] TMP_Text ammoText;
    [Header("Procedural Aiming")]
    [SerializeField] private CinemachineCamera virtualCamera; // World Camera
    [SerializeField] private Camera weaponCamera; // overlay camera
    [SerializeField] private Transform weaponTransform; 
    [SerializeField] private Vector3 adsPosition;     // The target position for aiming
    [SerializeField] private Vector3 adsRotation;
    private Vector3 hipPosition; 
    private Quaternion hipRotation;
    [SerializeField] private float aimSpeed = 10f;
    private float defaultFOV = 75f;
    [Header("Procedural Kick Settings")]
    [SerializeField] private float kickBackAmount = 0.04f;  // How far the gun moves back
    [SerializeField] private float kickUpAmount = 1.5f;     // How much the barrel tips up
    [SerializeField] private AudioClip emptyClipSound;

    private SurvivalPlayerHealth playerHealth;

    private void Awake() {
        shootAction = InputSystem.actions.FindAction("Player/Shoot");
        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        aimAction = InputSystem.actions.FindAction("Player/Aim");
        Debug.Log("Gun Awake");
        shootAction?.Enable();
        reloadAction?.Enable();
        aimAction?.Enable();

        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (muzzleFlash == null) muzzleFlash = GetComponentInChildren<ParticleSystem>();
        if (weaponTransform != null)
        {
            hipPosition = weaponTransform.localPosition;
            hipRotation = weaponTransform.localRotation;
        }
    }
    private void OnEnable()
    {
        if (isSpawned && !isOwner) return;
        shootAction?.Enable();
        reloadAction?.Enable();
        aimAction?.Enable();
    }
    void Start()
    {
        if (isSpawned && !isOwner)
        {
            shootAction?.Disable();
            reloadAction?.Disable();
            aimAction?.Disable();
            return;
        }

        if (playerMovement == null) playerMovement = GetComponentInParent<SurvivalPlayerMovement>();
        if (recoil == null) recoil = GetComponentInParent<GunRecoil>();
        if (crosshair == null) crosshair = FindFirstObjectByType<CrosshairController>();
        if (ammoText == null) ammoText = FindFirstObjectByType<TMP_Text>();
        if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        playerHealth = GetComponentInParent<SurvivalPlayerHealth>();

        // Find the Overlay Weapon Camera specifically
        if (weaponCamera == null)
        {
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in allCameras)
            {
                if (cam.name.Contains("Weapon")) // Find "WeaponCamera"
                {
                    weaponCamera = cam;
                    break;
                }
            }
        }
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (gunData != null)
        {
            // Duplicate SO instance so it's not shared in memory across clients
            gunData = Instantiate(gunData);
            gunData.reloading = false;
            gunData.currentAmmo = gunData.magazineSize;
            gunData.reserveAmmo = gunData.maxReserveAmmo;
            UpdateAmmoUI();
        }
        
        defaultFOV = PlayerPrefs.GetFloat("PlayerFOV", 75f);
        if (virtualCamera != null)
        {
            virtualCamera.Lens.FieldOfView = defaultFOV;
        }
        if (weaponCamera != null)
        {
            weaponCamera.fieldOfView = defaultFOV;
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (gunData != null) gunData.reloading = false;
        if (virtualCamera != null) virtualCamera.Lens.FieldOfView = defaultFOV;
        if (weaponCamera != null) weaponCamera.fieldOfView = defaultFOV;
        if (weaponTransform != null)
        {
            weaponTransform.localPosition = hipPosition;
            weaponTransform.localRotation = hipRotation;
        }
        shootAction?.Disable();
        reloadAction?.Disable();
        aimAction?.Disable();
    }

    void Update()
    {
        if (isSpawned && !isOwner) return;
        if (Time.timeScale == 0) return;
        if (playerHealth != null && playerHealth.IsDead) return;
        HandleShoot();
        HandleReload();
        HandleAim();
    }
    void HandleShoot()
    {
        bool isSprinting = playerMovement != null && playerMovement.isSprinting;
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        bool shootInput = (gunData.isAutomatic ? shootAction.IsPressed() : shootAction.triggered) && !isPointerOverUI;
        animator.SetBool("isShoot", shootInput && gunData.currentAmmo > 0 && !gunData.reloading);
        if (shootInput && !isSprinting && !gunData.reloading && Time.time >= nextFireTime )
        {
            if (gunData.currentAmmo > 0)
            {
                if (recoil != null) recoil.FireRecoil();
                weaponTransform.localPosition -= Vector3.forward * kickBackAmount; // Push gun backward
                weaponTransform.localRotation *= Quaternion.Euler(-kickUpAmount, 0, 0); // Tip gun upward
                
                gunData.currentAmmo--;
                UpdateAmmoUI();
                if (crosshair != null) crosshair.FireKick(15f);
                nextFireTime = Time.time + gunData.fireRate;
                muzzleFlash.Play();
                audioSource.PlayOneShot(gunData.GunSound);

                if (isSpawned)
                {
                    ServerShoot(Camera.main.transform.position, Camera.main.transform.forward);
                }
                else
                {
                    FireRayCast();
                }
            }
            else
            {
                if (emptyClipSound != null)
                {
                    audioSource.PlayOneShot(emptyClipSound);
                }
                
                nextFireTime = Time.time + gunData.fireRate; 
            }
        }
    }

    [ServerRpc]
    private void ServerShoot(Vector3 cameraPosition, Vector3 cameraForward)
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraPosition, cameraForward, out hit, gunData.MaxDistance, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            float finalDamage = gunData.Damage;
            var bodyPart = hit.collider.GetComponent<EnemyBodyPart>();
            if (bodyPart != null)
            {
                finalDamage *= bodyPart.damageMultiplier;
            }

            if (hit.collider.CompareTag("Enemy"))
            {
                var mockEnemy = hit.collider.GetComponentInParent<MockEnemy>();
                if (mockEnemy != null)
                {
                    mockEnemy.TakeDamage(finalDamage);
                }

                var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage((int)finalDamage);
                }
            }
            
            ObserversSpawnHitEffect(hit.point, hit.normal, hit.collider.CompareTag("Enemy"));
        }
        
        ObserversPlayShootEffects();
    }

    [ObserversRpc(excludeOwner: true)]
    private void ObserversPlayShootEffects()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource != null && gunData != null) audioSource.PlayOneShot(gunData.GunSound);
    }

    [ObserversRpc]
    private void ObserversSpawnHitEffect(Vector3 point, Vector3 normal, bool isEnemy)
    {
        if (isEnemy)
        {
            if (bloodEffectPrefab != null)
            {
                Instantiate(bloodEffectPrefab, point, Quaternion.LookRotation(normal));
            }
        }
        else
        {
            if (gunData != null && gunData.HitVfxPrefab != null)
            {
                Instantiate(gunData.HitVfxPrefab, point, Quaternion.LookRotation(normal));
            }
        }
    }

    void FireRayCast()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, gunData.MaxDistance, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            float finalDamage = gunData.Damage;
            var bodyPart = hit.collider.GetComponent<EnemyBodyPart>();
            if (bodyPart != null)
            {
                finalDamage *= bodyPart.damageMultiplier;
            }

            if (hit.collider.CompareTag("Enemy"))
            {
                var mock = hit.collider.GetComponentInParent<MockEnemy>();
                if (mock != null)
                {
                    mock.TakeDamage(finalDamage);
                }
                var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage((int)finalDamage);
                }
                if (bloodEffectPrefab != null)
                {
                    Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            else
            {
                if (gunData.HitVfxPrefab != null)
                {
                    Instantiate(gunData.HitVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
        } 
    }
    void HandleReload()
    {
        if(reloadAction.triggered && !gunData.reloading && gunData.currentAmmo < gunData.magazineSize && gunData.reserveAmmo > 0)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }
    IEnumerator ReloadCoroutine()
    {
        gunData.reloading = true;
        if (playerMovement != null) playerMovement.canSprint = false;
        if (animator != null) animator.SetTrigger("ReloadTrig");
        audioSource.PlayOneShot(gunData.reloadSound);
        yield return new WaitForSeconds(gunData.reloadTime);

        int ammoNeeded = gunData.magazineSize - gunData.currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, gunData.reserveAmmo);

        gunData.currentAmmo += ammoToLoad;
        gunData.reserveAmmo -= ammoToLoad;
        gunData.reserveAmmo = Mathf.Max(0, gunData.reserveAmmo);
        gunData.reloading = false;
        if (playerMovement != null) playerMovement.canSprint = true;
        UpdateAmmoUI();
    }
    void HandleAim() {
        if (!gunData.canZoom)
        {
            isAiming = false;
            return;
        }

        bool isSprinting = playerMovement != null && playerMovement.isSprinting;

        if (aimAction.IsPressed() && !isSprinting && !gunData.reloading) {
            isAiming = true;
        } else {
            isAiming = false;
        }

        Vector3 targetPos = isAiming ? adsPosition : hipPosition;
        weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, targetPos, Time.deltaTime * aimSpeed);
        Quaternion targetRot = isAiming ? Quaternion.Euler(adsRotation) : hipRotation;
        weaponTransform.localRotation = Quaternion.Slerp(weaponTransform.localRotation, targetRot, Time.deltaTime * aimSpeed);

        float targetFOV = isAiming ? gunData.zoomAmount : defaultFOV;
        if (virtualCamera != null)
        {
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * aimSpeed);
        }
        if (weaponCamera != null)
        {
            weaponCamera.fieldOfView = Mathf.Lerp(weaponCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
        }
    }
    void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = $"{gunData.currentAmmo:D2} / {gunData.reserveAmmo:D3}";
    }
    public void UpdateDefaultFOV(float newFOV)
    {
        defaultFOV = newFOV;
    }
}
