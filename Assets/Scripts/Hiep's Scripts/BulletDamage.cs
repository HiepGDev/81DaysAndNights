using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    const string PLAYER_STRING = "Player";
    [SerializeField] private float damageAmount = 3;

    public bool isTeammateBullet = false;

    private void OnParticleCollision(GameObject other)
    {
        if (isTeammateBullet) return;

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
