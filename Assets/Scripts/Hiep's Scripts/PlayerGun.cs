using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    [SerializeField] WeaponSO gunData;
    [SerializeField] private GunRecoil recoil;
    [SerializeField] private CrosshairController crosshair;
    private PlayerMovement playerMovement;
    InputAction shootAction; 
    InputAction reloadAction; 
    InputAction aimAction;
    public bool isAiming;
    private AudioSource audioSource; 
    [SerializeField] private LayerMask interactionLayers; 
    [SerializeField] private ParticleSystem muzzleFlash; 
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

    private void Awake() {
      shootAction = InputSystem.actions.FindAction("Player/Shoot");
      reloadAction = InputSystem.actions.FindAction("Player/Reload");
      aimAction = InputSystem.actions.FindAction("Player/Aim");
      Debug.Log("Gun Awake");
      shootAction?.Enable();
      reloadAction?.Enable();
      aimAction?.Enable();
    }
    void Start()
    {
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (recoil == null) recoil = GetComponentInParent<GunRecoil>();

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (gunData != null)
        {
            gunData.reloading = false;
            gunData.currentAmmo = gunData.magazineSize;
            gunData.reserveAmmo = gunData.maxReserveAmmo;
            UpdateAmmoUI();
        }
        if (weaponTransform != null)
        {
            hipPosition = weaponTransform.localPosition;
            hipRotation = weaponTransform.localRotation;
        } 

        defaultFOV = virtualCamera.Lens.FieldOfView;
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
        if (weaponTransform != null) weaponTransform.gameObject.SetActive(false);
        shootAction?.Disable();
        reloadAction?.Disable();
        aimAction?.Disable();
    }

    void Update()
    {
        HandleShoot();
        HandleReload();
        HandleAim();
    }
    void HandleShoot()
    {
        // Check if the player is sprinting from the movement script
        bool isSprinting = playerMovement != null && playerMovement.isSprinting;
        bool shootInput = gunData.isAutomatic? shootAction.IsPressed(): shootAction.triggered;
        animator.SetBool("isShoot", shootInput && gunData.currentAmmo > 0 && !gunData.reloading);
        if (shootInput && !isSprinting && !gunData.reloading && Time.time >= nextFireTime && gunData.currentAmmo > 0  )
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
        // weaponImpulseSource.GenerateImpulse();
        audioSource.PlayOneShot(gunData.GunSound);

        //  Raycast / damage logic 
        FireRayCast();
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
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)finalDamage);
            }

            if (gunData.HitVfxPrefab != null)
            {
                Instantiate(gunData.HitVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
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
            return;
        }

        bool isSprinting = playerMovement != null && playerMovement.isSprinting;

        if (aimAction.IsPressed() && !isSprinting && !gunData.reloading) {
            isAiming = true;
        } else {
            isAiming = false;
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
    void UpdateAmmoUI()
    {
        ammoText.text = $"{gunData.currentAmmo:D2} / {gunData.reserveAmmo:D3}";
    }
}
