using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    const string PLAYER_STRING = "Player";
    const string TEAMMATE_STRING = "Teammate";
    [SerializeField] private float damageAmount = 3;

    public bool isTeammateBullet = false;

    private void OnParticleCollision(GameObject other)
    {
        if (isTeammateBullet) return;

        if(other.CompareTag(PLAYER_STRING))
        {
            var playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                Debug.Log($"Player take {damageAmount} damage");
            }
        }
        else if (other.CompareTag(TEAMMATE_STRING))
        {
            var teammateHealth = other.GetComponentInParent<TeammateHealth>();
            if (teammateHealth != null)
            {
                teammateHealth.TakeDamage(damageAmount);
            }
        }

        if (!other.CompareTag(PLAYER_STRING))
            return;
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null)
            return;
        health.TakeDamage(damageAmount);
        Debug.Log($"Player take {damageAmount} damage");
    }
    void Start()
    {
        
    }

}
