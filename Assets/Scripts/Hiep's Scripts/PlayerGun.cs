using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    [SerializeField] WeaponSO gunData;
    [SerializeField] private GunRecoil recoil;
    private PlayerMovement playerMovement;
    InputAction shootAction; 
    InputAction reloadAction; 
    private AudioSource audioSource; 
    [SerializeField] private LayerMask interactionLayers; 
    [SerializeField] private ParticleSystem muzzleFlash; 
    private float nextFireTime = 0f;
    private Animator animator;
    // public CinemachineImpulseSource weaponImpulseSource; 
    [SerializeField] TMP_Text ammoText;

    private void Awake() {
      shootAction = InputSystem.actions.FindAction("Shoot");
      reloadAction = InputSystem.actions.FindAction("Reload");
      shootAction.Enable();
      reloadAction.Enable();
    }
    void Start()
    {
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (recoil == null) recoil = GetComponentInParent<GunRecoil>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (gunData != null)
        {
            gunData.currentAmmo = gunData.magazineSize;
            gunData.reserveAmmo = gunData.maxReserveAmmo;
            UpdateAmmoUI();
        }

    }

    void Update()
    {
        HandleShoot();
        HandleReload();
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
        // Decrease ammo and update next allowed fire time
        gunData.currentAmmo--;
        UpdateAmmoUI();
  
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
            var enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(gunData.Damage);
            }
            // Instantiate(gunData.HitVfxPrefab, hit.point, Quaternion.identity,hit.collider.gameObject.transform);
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
    void UpdateAmmoUI()
    {
        ammoText.text = $"{gunData.currentAmmo:D2} / {gunData.reserveAmmo:D3}";
    }
}
