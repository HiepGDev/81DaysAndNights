using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(TankExplosionDamage))]
public class TankShell : MonoBehaviour
{
    [Header("Shell Settings")]
    [SerializeField, Min(0f)] private float speed = 30f;
    [SerializeField, Min(0.1f)] private float maxLifetime = 8f;

    private Rigidbody shellRigidbody;
    private TankExplosionDamage explosionDamage;

    private bool hasExploded;
    private float destroyTime;

    private void Awake()
    {
        shellRigidbody = GetComponent<Rigidbody>();
        explosionDamage = GetComponent<TankExplosionDamage>();
    }

    public void Initialize(
        Vector3 direction,
        float newSpeed,
        float damage,
        float explosionRadius,
        Transform ownerRoot)
    {
        speed = Mathf.Max(0f, newSpeed);

        explosionDamage.Configure(
            damage,
            explosionRadius
        );

        IgnoreOwnerCollisions(ownerRoot);

        Vector3 normalizedDirection = direction.normalized;

        transform.rotation =
            Quaternion.LookRotation(normalizedDirection);

        shellRigidbody.linearVelocity =
            normalizedDirection * speed;

        destroyTime = Time.time + maxLifetime;
    }

    private void Update()
    {
        if (!hasExploded && Time.time >= destroyTime)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded)
            return;

        Vector3 explosionPosition =
            collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

        Explode(explosionPosition);
    }

    private void Explode()
    {
        Explode(transform.position);
    }

    private void Explode(Vector3 explosionPosition)
    {
        if (hasExploded)
            return;

        hasExploded = true;

        explosionDamage.Explode(explosionPosition);

        Destroy(gameObject);
    }

    private void IgnoreOwnerCollisions(Transform ownerRoot)
    {
        if (ownerRoot == null)
            return;

        Collider shellCollider = GetComponent<Collider>();

        Collider[] ownerColliders =
            ownerRoot.GetComponentsInChildren<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            if (ownerCollider == null)
                continue;

            Physics.IgnoreCollision(
                shellCollider,
                ownerCollider,
                true
            );
        }
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        maxLifetime = Mathf.Max(0.1f, maxLifetime);
    }
}