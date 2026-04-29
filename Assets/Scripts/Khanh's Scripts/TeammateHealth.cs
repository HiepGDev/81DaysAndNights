using UnityEngine;
using UnityEngine.AI;

public class TeammateHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    private bool isDead = false;

    // Khai báo mảng chứa toàn bộ xương Ragdoll
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private void Awake()
    {
        currentHealth = maxHealth;

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdollState(false);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[TeammateHealth] Hit! Remaining: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[TeammateHealth] Teammate Died. Kích hoạt Ragdoll!");

        gameObject.tag = "Untagged";

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        TeammateAI ai = GetComponent<TeammateAI>();
        if (ai != null) ai.enabled = false;

        TeammateShooting shooting = GetComponent<TeammateShooting>();
        if (shooting != null) shooting.enabled = false;

        TeammateDetection detection = GetComponent<TeammateDetection>();
        if (detection != null) detection.enabled = false;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        Collider rootCol = GetComponent<Collider>();
        if (rootCol != null) rootCol.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        SetRagdollState(true);

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.WakeUp(); 
                rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }

        Destroy(gameObject, 15f);
    }

    private void SetRagdollState(bool active)
    {
        if (ragdollRigidbodies == null) return;

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = !active;
                rb.useGravity = active;
            }
        }

        foreach (var col in ragdollColliders)
        {
            if (col != null && col.gameObject != gameObject)
            {
                col.enabled = active;
            }
        }
    }
}