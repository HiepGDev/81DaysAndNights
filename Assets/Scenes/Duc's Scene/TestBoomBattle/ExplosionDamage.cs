using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float radius = 6f;
    public float damage = 20f;

    [Header("Debug")]
    public bool showDebugLog = true;

    void Start()
    {
        ApplyExplosionDamage();
    }

    void ApplyExplosionDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            PlayerHealth hp = hit.GetComponent<PlayerHealth>();

            if (hp == null)
                continue;

            // Gây damage
            hp.TakeDamage(damage);

            // PlayerHealth.cs sẽ tự xử lý:
            // ✅ Flash đỏ
            // ✅ Camera shake
            // ✅ Sound hit
            // ✅ UI máu
            // nếu đã gán đúng references

            // Log đẹp hơn
            if (showDebugLog)
            {
                Debug.Log($"Player took {damage} damage | Hit by explosion radius {radius}");
            }

            // Vì chỉ có 1 player nên dừng luôn
            break;
        }
    }

    // Hiển thị vùng nổ trong Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}