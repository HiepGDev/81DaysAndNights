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

    [SerializeField] private LayerMask interactionLayers; 
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private float aimSpeed = 10f;
    [SerializeField] private float defaultFOV = 75f;

    [Header("Procedural Kick Settings")]
    [SerializeField] private float kickBackAmount = 0.04f;  // How far the gun moves back
    [SerializeField] private float kickUpAmount = 1.5f;     // How much the barrel tips up
    [SerializeField] private AudioClip emptyClipSound;

    [Header("Procedural Aiming")]
    [SerializeField] private Vector3 adsPosition;     // The target position for aiming
    [SerializeField] private Vector3 adsRotation;

    private GunRecoil recoil;
    private CrosshairController crosshair;
    private SurvivalPlayerMovement playerMovement;
    private SurvivalPlayerHealth playerHealth;
    private AudioSource audioSource; 
    private Animator animator;
    private TMP_Text ammoText;
    private CinemachineCamera virtualCamera; // World Camera
    private GameObject scopeOverlayUI;
    private bool isScoped = false;
    private Camera weaponCamera; // overlay camera

    private InputAction shootAction; 
    private InputAction reloadAction; 
    private InputAction aimAction;

    [HideInInspector] public bool isAiming; 
    private float nextFireTime = 0f;
    private Vector3 hipPosition; 
    private Quaternion hipRotation;
    private bool ammoRestored = false;

    private void Awake() {
        if (gunData != null)
        {
            gunData = Instantiate(gunData);
        }

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
        if (transform != null)
        {
            hipPosition = transform.localPosition;
            hipRotation = transform.localRotation;
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
        playerHealth = GetComponentInParent<SurvivalPlayerHealth>();

        if (transform != null)
        {
            hipPosition = transform.localPosition;
            hipRotation = transform.localRotation;
        }

        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (gunData != null)
        {
            gunData.reloading = false;
            if (!ammoRestored)
            {
                gunData.currentAmmo = gunData.magazineSize;
                gunData.reserveAmmo = gunData.maxReserveAmmo;
            }
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

    public void InitializeGunReferences(
        GunRecoil recoilReference,
        CrosshairController crosshairReference,
        TMP_Text ammoTextReference,
        CinemachineCamera virtualCameraReference,
        Camera weaponCameraReference,
        GameObject scopeOverlayUIReference)
    {
        this.recoil = recoilReference;
        this.crosshair = crosshairReference;
        this.ammoText = ammoTextReference;
        this.virtualCamera = virtualCameraReference;
        this.weaponCamera = weaponCameraReference;
        this.scopeOverlayUI = scopeOverlayUIReference;
        
        if (scopeOverlayUI != null)
        {
            scopeOverlayUI.SetActive(false);
        }

        if (transform != null)
        {
            hipPosition = transform.localPosition;
            hipRotation = transform.localRotation;
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

        UpdateAmmoUI();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        Unscope();
        if (gunData != null) gunData.reloading = false;
        if (virtualCamera != null) virtualCamera.Lens.FieldOfView = defaultFOV;
        if (weaponCamera != null) weaponCamera.fieldOfView = defaultFOV;
        if (transform != null)
        {
            transform.localPosition = hipPosition;
            transform.localRotation = hipRotation;
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
                if (transform != null)
                {
                    transform.localPosition -= Vector3.forward * kickBackAmount; // Push gun backward
                    transform.localRotation *= Quaternion.Euler(-kickUpAmount, 0, 0); // Tip gun upward
                }
                
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
            if (isScoped) Unscope();
            return;
        }

        bool isSprinting = playerMovement != null && playerMovement.isSprinting;

        if (aimAction.IsPressed() && !isSprinting && !gunData.reloading) {
            isAiming = true;
            if (gunData.useScopeOverlay && !isScoped) 
            {
                StartCoroutine(OnScoped());
            }
        } else {
            isAiming = false;
            if (isScoped) Unscope();
        }

        if (transform != null)
        {
            Vector3 targetPos = isAiming ? adsPosition : hipPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * aimSpeed);
            Quaternion targetRot = isAiming ? Quaternion.Euler(adsRotation) : hipRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * aimSpeed);
        }

        float targetFOV = isAiming ? gunData.zoomAmount : defaultFOV;
        if (virtualCamera != null)
        {
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * aimSpeed);
        }
        if (weaponCamera != null && !isScoped)
        {
            weaponCamera.fieldOfView = Mathf.Lerp(weaponCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
        }
    }

    IEnumerator OnScoped()
    {
        isScoped = true;
        yield return new WaitForSeconds(gunData.scopeDelay);

        if (isAiming)
        {
            if (scopeOverlayUI != null) scopeOverlayUI.SetActive(true);
            if (weaponCamera != null) weaponCamera.enabled = false; 
            if (crosshair != null) crosshair.gameObject.SetActive(false); 
        }
    }

    void Unscope()
    {
        isScoped = false;
        if (scopeOverlayUI != null) scopeOverlayUI.SetActive(false);
        if (weaponCamera != null) weaponCamera.enabled = true; 
        if (crosshair != null) crosshair.gameObject.SetActive(true);
    }
    void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = $"{gunData.currentAmmo:D2} / {gunData.reserveAmmo:D3}";
    }

    public int CurrentAmmo
    {
        get => gunData != null ? gunData.currentAmmo : 0;
        set
        {
            if (gunData != null)
            {
                gunData.currentAmmo = value;
                UpdateAmmoUI();
            }
        }
    }

    public int ReserveAmmo
    {
        get => gunData != null ? gunData.reserveAmmo : 0;
        set
        {
            if (gunData != null)
            {
                gunData.reserveAmmo = value;
                UpdateAmmoUI();
            }
        }
    }

    public void SetAmmo(int current, int reserve)
    {
        if (gunData != null)
        {
            gunData.currentAmmo = current;
            gunData.reserveAmmo = reserve;
            ammoRestored = true;
            UpdateAmmoUI();
        }
    }

    public void UpdateDefaultFOV(float newFOV)
    {
        defaultFOV = newFOV;
    }
}
