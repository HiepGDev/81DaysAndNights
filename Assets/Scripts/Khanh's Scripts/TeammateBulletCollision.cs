using UnityEngine;

public class TeammateBulletCollision : MonoBehaviour
{
    private static readonly string[] BLOCKED_TAGS = { "Player", "Teammate" };
    [SerializeField] private int damageAmount = 3;

    private void OnParticleCollision(GameObject other)
    {
        foreach (var tag in BLOCKED_TAGS)
        {
            if (other.CompareTag(tag))
            {
                Destroy(gameObject);
                return;
            }
        }
        if(other.CompareTag("Enemy"))
        {
            var health = other.GetComponentInParent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damageAmount); 
                Debug.Log($"Enemy take {damageAmount} damage from teammate bullet");
            }
            Destroy(gameObject);
        }
    }
}