using UnityEngine;
using UnityEngine.AI;

public class TeammateHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    private static System.Collections.Generic.List<GameObject> ragdollPool = new System.Collections.Generic.List<GameObject>();
    private const int MAX_RAGDOLLS = 10;

    private bool isDead = false;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private void Awake()
    {
        currentHealth = maxHealth;

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdollState(false);
    }
    public void Update()
    {
        //CheckIfDied();
    }
    public void CheckIfDied()
    {
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
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

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        Collider rootCol = GetComponent<Collider>();
        if (rootCol != null) rootCol.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s != null && s != this) s.enabled = false;
        }

        SetRagdollState(true);

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.WakeUp();
                rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }

        ragdollPool.Add(this.gameObject);

        ragdollPool.RemoveAll(item => item == null);

        if (ragdollPool.Count > MAX_RAGDOLLS)
        {
            GameObject oldest = ragdollPool[0];
            ragdollPool.RemoveAt(0);
            if (oldest != null)
            {
                Debug.Log($"[Pool] Limit reached. Destroying oldest teammate ragdoll: {oldest.name}");
                Destroy(oldest);
            }
        }
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