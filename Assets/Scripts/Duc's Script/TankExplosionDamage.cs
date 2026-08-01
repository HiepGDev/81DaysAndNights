using System.Collections.Generic;
using UnityEngine;

public class TankExplosionDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField, Min(0f)] private float damage = 50f;
    [SerializeField, Min(0f)] private float explosionRadius = 5f;

    [Header("Target Tags")]
    [SerializeField] private string[] damageableTags =
    {
        "Player",
        "Teammate"
    };

    [Header("Optional Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;

    private readonly HashSet<GameObject> damagedTargets = new();

    public void Configure(float newDamage, float newRadius)
    {
        damage = Mathf.Max(0f, newDamage);
        explosionRadius = Mathf.Max(0f, newRadius);
    }

    public void Explode(Vector3 explosionPosition)
    {
        damagedTargets.Clear();

        Collider[] hits = Physics.OverlapSphere(
            explosionPosition,
            explosionRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            GameObject targetRoot = ResolveDamageTarget(hit);

            if (targetRoot == null)
                continue;

            // Một nhân vật có thể có nhiều collider.
            // Chỉ gây damage một lần cho mỗi nhân vật.
            if (!damagedTargets.Add(targetRoot))
                continue;

            ApplyDamage(targetRoot);
        }

        if (explosionEffectPrefab != null)
        {
            Instantiate(
                explosionEffectPrefab,
                explosionPosition,
                Quaternion.identity
            );
        }

        Debug.Log(
            $"[TankExplosionDamage] BOOM tại {explosionPosition}, " +
            $"Damage: {damage}, Radius: {explosionRadius}"
        );
    }

    private GameObject ResolveDamageTarget(Collider hit)
    {
        PlayerHealth playerHealth =
            hit.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null &&
            HasAllowedTag(playerHealth.gameObject))
        {
            return playerHealth.gameObject;
        }

        TeammateHealth teammateHealth =
            hit.GetComponentInParent<TeammateHealth>();

        if (teammateHealth != null &&
            HasAllowedTag(teammateHealth.gameObject))
        {
            return teammateHealth.gameObject;
        }

        return null;
    }

    private void ApplyDamage(GameObject target)
    {
        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            return;
        }

        TeammateHealth teammateHealth =
            target.GetComponent<TeammateHealth>();

        if (teammateHealth != null)
        {
            teammateHealth.TakeDamage(damage);
        }
    }

    private bool HasAllowedTag(GameObject target)
    {
        foreach (string allowedTag in damageableTags)
        {
            if (string.IsNullOrWhiteSpace(allowedTag))
                continue;

            if (target.CompareTag(allowedTag))
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        explosionRadius = Mathf.Max(0f, explosionRadius);
    }
}