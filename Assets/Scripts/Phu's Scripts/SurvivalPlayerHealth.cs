using UnityEngine.UI;
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using PurrNet;

public class SurvivalPlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private SyncVar<float> currentHealth = new(100f);
    [SerializeField] private float regenRate = 10f;
    [SerializeField] private float regenDelay = 2f;
    [SerializeField] private Image healthBar;

    // Runtime vars
    private float regenTimer = 0f;
    private SurvivalPlayerMovement playerMovement;
    private PlayerFootstep playerFootstep;
    private SurvivalPlayerGun playerGun;
    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource; // impulse needs get component 
    [SerializeField] GameObject gameOverCanvas; 
    private bool isDead = false;
    public bool IsDead => isDead;
    private Rigidbody playerRigidbody;

    [Header("Audio")] 
    private AudioSource audioSource;
    [SerializeField] AudioClip hitsfx;
    [SerializeField] private float pitchVariation = 0.1f;
    [SerializeField] AudioClip deathSound;
    
    [Header("Damage Flash")] 
    [SerializeField] Image damageFlashImage;
    private Coroutine flashCoroutine;  // Track to stop overlapping flashes
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private float maxFlashAlpha = 0.15f;

    [Header("Injured Effect")] 
    [SerializeField] private Image injuredOverlay;   
    [SerializeField] private float injuredThreshold = 40f;
    [SerializeField] private float maxInjuredAlpha = 0.35f;
    [SerializeField] private float effectTransitionSpeed = 1.5f;

    private void Awake()
    {
        playerMovement = GetComponent<SurvivalPlayerMovement>();
        playerFootstep = GetComponent<PlayerFootstep>();
        playerGun = GetComponentInChildren<SurvivalPlayerGun>();
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();
        playerRigidbody = GetComponent<Rigidbody>();
        
        currentHealth.value = maxHealth;

        // Disable singleplayer legacy PlayerGun to prevent shooting twice
        var legacyGuns = GetComponentsInChildren<PlayerGun>(true);
        foreach (var lg in legacyGuns)
        {
            lg.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;  // No physics until Die()
            playerRigidbody.useGravity = true;
        }
        
        // Setup damage flash Image (keep active, start alpha=0)
        if (damageFlashImage != null)
        {
            damageFlashImage.gameObject.SetActive(true);  // Always active for fade
            Color flashColor = damageFlashImage.color;
            damageFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);  // Alpha 0
        }

        if (injuredOverlay != null)
        {
            Color c = injuredOverlay.color;
            injuredOverlay.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    void Start()
    {
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
        
        if (!isSpawned)
        {
            currentHealth.value = maxHealth;
        }
        UpdateHealthUI();
    }

    protected override void OnSpawned()
    {
        currentHealth.onChangedWithOld += OnHealthChanged;
        
        // Hide overlay UI components for non-owners in multiplayer
        if (isSpawned && !isOwner)
        {
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (damageFlashImage != null) damageFlashImage.gameObject.SetActive(false);
            if (injuredOverlay != null) injuredOverlay.gameObject.SetActive(false);
            if (gameOverCanvas != null) gameOverCanvas.gameObject.SetActive(false);
        }

        if (isServer)
        {
            currentHealth.value = maxHealth;
        }

        UpdateHealthUI();
    }

    protected override void OnDespawned()
    {
        currentHealth.onChangedWithOld -= OnHealthChanged;
    }

    void Update()
    {
        if (currentHealth.value <= 0f && !isDead)
        {
            Die();
            return;
        }

        if (!isDead)
        {
            // Only server handles regeneration
            if (!isSpawned || isServer)
            {
                HealthRegen();
            }

            // Only local owner handles screen effects
            if (!isSpawned || isOwner)
            {
                HandleInjuredEffect();
            }
        }
    }

    void HealthRegen()
    {
        // Only regen if health is below max but above 0 (not dead)
        if (currentHealth.value > 0f && currentHealth.value < maxHealth)
        {
            // Accumulate delay timer
            regenTimer += Time.deltaTime;
            // once delay passed , regen health
            if (regenTimer >= regenDelay)
            {
                float healthBefore = currentHealth.value;
                currentHealth.value += regenRate * Time.deltaTime;
                currentHealth.value = Mathf.Min(currentHealth.value, maxHealth); // cap at max
                
                if (!isSpawned)
                {
                    OnHealthChanged(healthBefore, currentHealth.value);
                }
            }
        }
    }

    void HandleInjuredEffect()
    {
        if (injuredOverlay == null) return;

        float targetIntensity = 0f;
        if (currentHealth.value < injuredThreshold)
        {
            targetIntensity = 1f - (currentHealth.value / injuredThreshold);
        }

        float currentAlpha = injuredOverlay.color.a;
        float targetAlpha = targetIntensity * maxInjuredAlpha;
        float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * effectTransitionSpeed);
        injuredOverlay.color = new Color(injuredOverlay.color.r, injuredOverlay.color.g, injuredOverlay.color.b, newAlpha);
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth.value / maxHealth;
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        UpdateHealthUI();

        if (newValue < oldValue)
        {
            if (!isSpawned || isServer)
            {
                regenTimer = 0f;
            }

            // Play hit sfx for all clients
            if (audioSource != null && hitsfx != null)
            {
                audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                audioSource.PlayOneShot(hitsfx);
            }

            // Flash screen and impulse only for local owner
            if (!isSpawned || isOwner)
            {
                if (impulseSource != null)
                {
                    impulseSource.GenerateImpulse();
                }

                if (flashCoroutine != null)
                    StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(DamageFlash());
            }
        }

        if (newValue <= 0f && !isDead)
        {
            Die();
        }
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || isDead) return;
        if (isSpawned && !isServer) return; // Only server handles damage

        float healthBefore = currentHealth.value;
        currentHealth.value = Mathf.Max(0f, currentHealth.value - damage);
        
        if (!isSpawned)
        {
            OnHealthChanged(healthBefore, currentHealth.value);
        }
    }

    private IEnumerator DamageFlash()
    {
        if (damageFlashImage == null) yield break;

        Color origColor = damageFlashImage.color;
        float timer = 0f;
        while (timer < damageFlashDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / damageFlashDuration;
            float sineAlpha = Mathf.Sin(normalizedTime * Mathf.PI);  // 0->1->0 curve
            float alpha = sineAlpha * maxFlashAlpha;
            damageFlashImage.color = new Color(origColor.r, origColor.g, origColor.b, alpha);
            yield return null;
        }
        damageFlashImage.color = new Color(origColor.r, origColor.g, origColor.b, 0f);
    }

    private void Die()
    {
        isDead = true;
        if (playerGun != null)
        {
            playerGun.enabled = false;
            playerGun.gameObject.SetActive(false);
        }
        // Disable movement/physics
        if (playerMovement != null) playerMovement.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (playerFootstep != null) playerFootstep.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;  // Physics on
            playerRigidbody.useGravity = true;

            // Fall
            playerRigidbody.linearVelocity = new Vector3(
                Random.Range(-2f, 2f),  // Side velocity
                Random.Range(-1f, 1f),  // Up/down kick
                Random.Range(-2f, 2f)   // Forward/back
            );
        }
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (!isSpawned || isOwner)
        {
            if (gameOverCanvas != null)
            {
                var oldMgr = gameOverCanvas.GetComponent<GameOverManager>();
                if (oldMgr != null)
                {
                    oldMgr.enabled = false;
                    Destroy(oldMgr);
                }
                if (gameOverCanvas.GetComponent<SurvivalGameOverManager>() == null)
                {
                    gameOverCanvas.AddComponent<SurvivalGameOverManager>();
                }
                gameOverCanvas.SetActive(true);
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        Debug.Log("Player die !");
    }
}
