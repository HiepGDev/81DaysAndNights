using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int health = 100;
    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log($"{gameObject.name} took {damageAmount} damage! Current health: {health}");
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject); 
    }
}
