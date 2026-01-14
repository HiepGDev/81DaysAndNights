using UnityEngine.UI;
using UnityEngine;
using Unity.Cinemachine;

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
    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource; // yeh impulse also need get component 
    private AudioSource audioSource;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        UpdateHealthUI();
    }

    void Update()
    {
        HealthRegen();
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

    void UpdateHealthUI()
    {
        if (healthBar != null)
        healthBar.fillAmount = currentHealth / maxHealth;
    }

    public void TakeDamage(float damage)
    {
        //ignore invalid damage
        if (damage <= 0f) return;

        //Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // no negative health
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
        // reset regen timer 
        regenTimer = 0f;
        // Immediate UI update
        UpdateHealthUI();
        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (playerMovement != null)
        playerMovement.enabled = false;
        if (characterController != null)
        characterController.enabled = false;
    }
} 
