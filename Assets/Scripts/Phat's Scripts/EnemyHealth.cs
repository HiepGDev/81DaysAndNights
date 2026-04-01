using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    private bool isDead = false;
    private NavMeshAgent agent;
    private EnemyBehaviorAgent behavior;
    private EnemyShooting shooting;
    private Rigidbody rb;

    private void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        behavior = GetComponent<EnemyBehaviorAgent>();
        shooting = GetComponent<EnemyShooting>();
        rb = GetComponent<Rigidbody>();

        // Ensure Rigidbody is kinematic during life
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[EnemyHealth] Hit! Remaining: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[EnemyHealth] Enemy Died.");

        // 1. Disable AI and Shooting
        if (agent != null) agent.enabled = false;
        if (behavior != null) behavior.enabled = false;
        if (shooting != null) shooting.enabled = false;

        // 2. Disable CharacterController if exists
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 3. Enable Physics for the death fall
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Add a small random push so they fall over
            rb.AddForce(new Vector3(Random.Range(-1, 1), 2, Random.Range(-1, 1)), ForceMode.Impulse);
            rb.AddTorque(new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5)), ForceMode.Impulse);
        }

        // 4. Stop Animator
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;
    }
}
