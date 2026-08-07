using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private EnemySO enemyData;

    public int CurrentHealth => health;
    public int MaxHealth => (enemyData != null) ? enemyData.maxHealth : 100;
    
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
        
        // Disable collisions between the main root collider and all the child ragdoll colliders
        Collider rootCol = GetComponent<Collider>();
        if (rootCol != null)
        {
            foreach (var col in ragdollColliders)
            {
                if (col != null && col != rootCol)
                {
                    Physics.IgnoreCollision(rootCol, col, true);
                }
            }
        }

        SetRagdollState(false);

        if (enemyData != null)
        {
            health = enemyData.maxHealth;
        }
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
        
        // 1. Untag the root GameObject
        gameObject.tag = "Untagged";

        // 2. Disable Main AI Components
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // 3. Disable Animator and unload controllers
        Animator[] anims = GetComponentsInChildren<Animator>();
        foreach (var anim in anims)
        {
            if (anim != null)
            {
                anim.enabled = false;
                anim.runtimeAnimatorController = null; // Unloads controller and fully disables animation logic
            }
        }

        // 4. Disable all AI Logic scripts and stop active coroutines
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            // Only disable other scripts, not this one
            if (s != null && s != this)
            {
                s.StopAllCoroutines(); // Stop any pending reloads, movement cycles, or timers
                s.enabled = false;
            }
        }

        // 5. Explicit cover release (safeguard)
        EnemyCover cover = GetComponent<EnemyCover>();
        if (cover != null)
        {
            cover.ReleaseCover();
        }

        // 6. Enable Physics
        SetRagdollState(true);

        // 7. Disable root collider
        Collider rootCol = GetComponent<Collider>();
        if (rootCol != null) rootCol.enabled = false;

        // 8. Physics Kick: Wake up every single bone and push it
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

        // 9. Close the eyes
        Transform eyeL = FindChildRecursive(transform, "eye.l");
        Transform eyeR = FindChildRecursive(transform, "eye.r");
        CreateClosedEyeCube(eyeL, new Vector3(-0.0045f, 0.001f, 0.0011f), new Vector3(-0.06f, -0.001f, 0.02f));
        CreateClosedEyeCube(eyeR, new Vector3(0.0045f, 0.001f, 0.0011f), new Vector3(-0.06f, -0.001f, 0.02f));

        // 10. THE ENEMY POOL MANAGER: Limit to 30 enemy bodies
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

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void CreateClosedEyeCube(Transform eyeTransform, Vector3 localPos, Vector3 localScale)
    {
        if (eyeTransform == null) return;

        // Create a small primitive cube to act as closed eyelid
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ClosedEyeCover";

        // Remove collider so it doesn't interfere with physics or ragdolls
        Collider col = cube.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Parent to the eye bone
        cube.transform.SetParent(eyeTransform);
        
        // Apply exact local coordinates requested
        cube.transform.localPosition = localPos;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        // Apply custom color #A18783
        Renderer r = cube.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = new Color(0.631f, 0.529f, 0.514f);
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
