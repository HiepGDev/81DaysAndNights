using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    [SerializeField] WeaponSO gunData;
    public WeaponSO WeaponData => gunData;
    [SerializeField] private GunRecoil recoil;
    [SerializeField] private CrosshairController crosshair;
    private PlayerMovement playerMovement;
    InputAction shootAction; 
    InputAction reloadAction; 
    InputAction aimAction;
    public bool isAiming; 
    private AudioSource audioSource; 
    [SerializeField] private AudioClip emptyClipSound;
    [SerializeField] private LayerMask interactionLayers; 
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject bloodEffectPrefab;
    private float nextFireTime = 0f;
    private Animator animator;
    // public CinemachineImpulseSource weaponImpulseSource; 
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
    // [SerializeField] private float recoilReturnSpeed = 20f;
    [Header("Sniper Scope Overlay")]
    [SerializeField] private GameObject scopeOverlayUI; // Drag Scope UI Image here
    private bool isScoped = false;

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
        shootAction?.Enable();
        reloadAction?.Enable();
        aimAction?.Enable();
    }
    void Start()
    {
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (recoil == null) recoil = GetComponentInParent<GunRecoil>();
        if (crosshair == null) crosshair = FindFirstObjectByType<CrosshairController>();
        if (ammoText == null) ammoText = FindFirstObjectByType<TMP_Text>();
        if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();

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

        if (scopeOverlayUI != null) scopeOverlayUI.SetActive(false);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        Unscope();
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
        // If the game is paused, stop all gun logic
        if (Time.timeScale == 0) return;
        HandleShoot();
        HandleReload();
        HandleAim();
    }
    void HandleShoot()
    {
        // Check if the player is sprinting from the movement script
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
                // Decrease ammo and update next allowed fire time
                gunData.currentAmmo--;
                UpdateAmmoUI();
                if (crosshair != null) crosshair.FireKick(15f);
                nextFireTime = Time.time + gunData.fireRate;
                muzzleFlash.Play();
                audioSource.PlayOneShot(gunData.GunSound);
                //  Raycast / damage logic 
                FireRayCast();
            }
            else
            {
                if (emptyClipSound != null)
                {
                    audioSource.PlayOneShot(emptyClipSound);
                }
                
                // set the nextFireTime here as well
                // This prevents the click sound from spamming 60 times a second if the player holds down the mouse button
                nextFireTime = Time.time + gunData.fireRate; 
            }
        }
    }
    void FireRayCast()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, gunData.MaxDistance, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            // Calculate Multiplier based on Body Part
            float finalDamage = gunData.Damage;
            var bodyPart = hit.collider.GetComponent<EnemyBodyPart>();
            if (bodyPart != null)
            {
                finalDamage *= bodyPart.damageMultiplier;
                Debug.Log($"Hit {bodyPart.partName}! Damage Multiplier: {bodyPart.damageMultiplier}");
            }

            //  Apply Damage to Health
            if (hit.collider.CompareTag("Enemy"))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage((int)finalDamage);
                }
                // INSTANTIATE BLOOD
                if (bloodEffectPrefab != null)
                {
                    // Spawn blood at hit point, facing away from the wound
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
            Debug.Log("Hit: " + hit.collider.name);
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

        // Calculate how many rounds to load
        int ammoNeeded = gunData.magazineSize - gunData.currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, gunData.reserveAmmo);

        gunData.currentAmmo += ammoToLoad;
        gunData.reserveAmmo -= ammoToLoad;
        // make sure that ammo never < 0
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

        // Move the Gun Transform
        Vector3 targetPos = isAiming ? adsPosition : hipPosition;
        weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, targetPos, Time.deltaTime * aimSpeed);
        Quaternion targetRot = isAiming ? Quaternion.Euler(adsRotation) : hipRotation;
        weaponTransform.localRotation = Quaternion.Slerp(weaponTransform.localRotation, targetRot, Time.deltaTime * aimSpeed);

        // Zoom the FOV
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
    IEnumerator OnScoped()
    {
        isScoped = true;
        
        // Wait a fraction of a second for the gun to reach the center of the screen
        yield return new WaitForSeconds(gunData.scopeDelay);

        // Double check that the player didn't let go of the right mouse button early (Quick-scoping)
        if (isAiming)
        {
            if (scopeOverlayUI != null) scopeOverlayUI.SetActive(true);
            // Turn off the weapon camera so the 3D gun model disappears
            if (weaponCamera != null) weaponCamera.enabled = false; 
            // Hide the standard crosshair while using the sniper scope
            if (crosshair != null) crosshair.gameObject.SetActive(false); 
        }
    }

    void Unscope()
    {
        isScoped = false;
        // Turn off the UI Image
        if (scopeOverlayUI != null) scopeOverlayUI.SetActive(false);
        // Turn the weapon camera back on so we can see the 3D gun lower back to hip-fire
        if (weaponCamera != null) weaponCamera.enabled = true; 
        // Turn the standard crosshair back on
        if (crosshair != null) crosshair.gameObject.SetActive(true);
    }
    void UpdateAmmoUI()
    {
        ammoText.text = $"{gunData.currentAmmo:D2} / {gunData.reserveAmmo:D3}";
    }
    public void UpdateDefaultFOV(float newFOV)
    {
        defaultFOV = newFOV;
    }

    public void AddAmmo(int amount)
    {
        if (gunData != null)
        {
            gunData.reserveAmmo += amount;
            
            // Clamp the ammo so it doesn't exceed the max reserve limit
            if (gunData.reserveAmmo > gunData.maxReserveAmmo)
            {
                gunData.reserveAmmo = gunData.maxReserveAmmo;
            }
            
            UpdateAmmoUI();
            Debug.Log($"Ammo picked up! Current Reserve: {gunData.reserveAmmo}");
        }
    }
}

