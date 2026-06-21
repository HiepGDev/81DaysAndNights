using UnityEngine.UI;
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float regenRate = 10f;
    [SerializeField] private float regenDelay = 2f;
    [SerializeField] private Image healthBar;

    // Runtime vars
    private float regenTimer = 0f;
    private PlayerMovement playerMovement;
    private PlayerFootstep playerFootstep;
    private PlayerGun playerGun;
    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource; // yeh impulse also need get component 
    [SerializeField] GameObject gameOverCanvas; 
    private bool isDead = false;
    public bool IsDead => isDead;
    // private Collider playerCollider;
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
    // [SerializeField] private AudioClip breathingSound;   
    [SerializeField] private float injuredThreshold = 40f;
    [SerializeField] private float maxInjuredAlpha = 0.35f;
    [SerializeField] private float effectTransitionSpeed = 1.5f;
    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerFootstep = GetComponent<PlayerFootstep>();
        playerGun = GetComponentInChildren<PlayerGun>();
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();
        // playerCollider = GetComponentInChildren<Collider>();
        playerRigidbody = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

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
        UpdateHealthUI();
    }

    void Update()
    {
        if (!isDead)
        {
            // no regen after death eyy
            HealthRegen();
            HandleInjuredEffect();
        }
    }

    void HealthRegen()
    {
        // Only regen if health is below max but above 0 (not dead)
        if (currentHealth > 0f && currentHealth < maxHealth)
        {
            // Accumulate delay timer
            regenTimer += Time.deltaTime;
            // once delay passed , regen health
            if (regenTimer >= regenDelay)
            {
                float healthBefore = currentHealth;
                currentHealth += regenRate * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth); // cap at max
                // Only update UI if the health actually changed
                if (currentHealth != healthBefore)
                {
                    UpdateHealthUI();
                }
            }
        }
    }
    void HandleInjuredEffect()
    {
        if (injuredOverlay == null) return;

        float targetIntensity = 0f;
        if (currentHealth < injuredThreshold)
        {
            // This math makes 40HP = 0 intensity and 0HP = 1 intensity
            targetIntensity = 1f - (currentHealth / injuredThreshold);
        }

        //  Smoothly lerp the Alpha of the red screen
        float currentAlpha = injuredOverlay.color.a;
        float targetAlpha = targetIntensity * maxInjuredAlpha;
        float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * effectTransitionSpeed);
        injuredOverlay.color = new Color(injuredOverlay.color.r, injuredOverlay.color.g, injuredOverlay.color.b, newAlpha);
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
        healthBar.fillAmount = currentHealth / maxHealth;
    }

    public void TakeDamage(float damage)
    {
        //ignore invalid damage
        if (damage <= 0f || isDead) return;

        //Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // no negative health
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
        // reset regen timer 
        regenTimer = 0f;
        // hit sfx
        if (audioSource != null && hitsfx != null)
        {
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(hitsfx);
        }
        // Damage Flash 
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(DamageFlash());
        // Immediate UI update
        UpdateHealthUI();
        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    private IEnumerator DamageFlash()
    {
        if (damageFlashImage == null) yield break;

        Color origColor = damageFlashImage.color;
        float timer = 0f;
        // Fade in/out with sine wave (peaks at 50% time)
        while (timer < damageFlashDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / damageFlashDuration;
            float sineAlpha = Mathf.Sin(normalizedTime * Mathf.PI);  // 0->1->0 curve
            float alpha = sineAlpha * maxFlashAlpha;
            damageFlashImage.color = new Color(origColor.r, origColor.g, origColor.b, alpha);
            yield return null;
        }
        // Ensure reset to alpha 0
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
        // if (playerCollider != null) playerCollider.enabled = false;

            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = false;  // Physics on

                // Fall
                playerRigidbody.linearVelocity = new Vector3(
                    Random.Range(-2f, 2f),  // Side velocity
                    Random.Range(-1f, 1f),  // Up/down kick
                    Random.Range(-2f, 2f)   // Forward/back
                );
            }
        audioSource.PlayOneShot(deathSound);
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Player die !");
    }
} 
