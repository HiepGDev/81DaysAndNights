using UnityEngine;
using UnityEngine.AI;

public class TankHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField, Min(1)] private int maxHealth = 500;
    [SerializeField] private int currentHealth;

    [Header("Death Settings")]
    [SerializeField] private GameObject destructionEffectPrefab;
    [SerializeField, Min(0f)] private float destroyDelay = 0f;

    private bool isDestroyed;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => isDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDestroyed || damageAmount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        Debug.Log(
            $"[TankHealth] {name} nhận {damageAmount} damage. " +
            $"HP: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
        {
            DestroyTank();
        }
    }

    private void DestroyTank()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        DisableTankSystems();

        if (destructionEffectPrefab != null)
        {
            Instantiate(
                destructionEffectPrefab,
                transform.position,
                transform.rotation
            );
        }

        Debug.Log($"[TankHealth] {name} đã bị phá hủy.");

        if (destroyDelay <= 0f)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void DisableTankSystems()
    {
        TankWaypointMovement movement =
            GetComponent<TankWaypointMovement>();

        TankTargetDetector detector =
            GetComponent<TankTargetDetector>();

        TankTurretController turret =
            GetComponent<TankTurretController>();

        TankWeaponController weapon =
            GetComponent<TankWeaponController>();

        NavMeshAgent agent =
            GetComponent<NavMeshAgent>();

        if (movement != null)
            movement.enabled = false;

        if (detector != null)
            detector.enabled = false;

        if (turret != null)
            turret.enabled = false;

        if (weapon != null)
            weapon.enabled = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        destroyDelay = Mathf.Max(0f, destroyDelay);
    }
}