using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int health = 100;
    
    // Track all dead bodies across the whole game
    private static System.Collections.Generic.List<GameObject> ragdollPool = new System.Collections.Generic.List<GameObject>();
    private const int MAX_RAGDOLLS = 30;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isDead = false;

    private void Awake()
    {
        // Gather all bone physics components
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        
        SetRagdollState(false);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        health -= damageAmount;
        Debug.Log($"{gameObject.name} took {damageAmount} damage! Current health: {health}");
        
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"{gameObject.name} triggering physical death.");
        gameObject.tag = "Untagged"; 

        // 1. Disable Main AI Components
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // 2. Disable Animator 
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        // 3. Disable all AI Logic scripts
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            // Only disable other scripts, not this one
            if (s != null && s != this) s.enabled = false;
        }

        // 4. Enable Physics
        SetRagdollState(true);

        // 5. Disable root collider
        Collider rootCol = GetComponent<Collider>();
        if (rootCol != null) rootCol.enabled = false;

        // 6. Physics Kick: Wake up every single bone and push it
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.WakeUp();
                // Random nudge to break the animation pose
                rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }

        // 7. THE ENEMY POOL MANAGER: Limit to 30 enemy bodies
        ragdollPool.Add(this.gameObject);

        // Remove any dead bodies that were destroyed by other means (falling off map, etc)
        ragdollPool.RemoveAll(item => item == null);

        if (ragdollPool.Count > MAX_RAGDOLLS)
        {
            GameObject oldest = ragdollPool[0];
            ragdollPool.RemoveAt(0);
            if (oldest != null) 
            {
                Debug.Log($"[Pool] Limit reached. Destroying oldest enemy ragdoll: {oldest.name}");
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
                col.enabled = true; 
            }
        }
    }
}
