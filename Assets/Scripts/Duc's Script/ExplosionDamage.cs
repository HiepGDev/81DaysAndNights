using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float radius = 10f;
    public float damage = 30f;
    public float upwardOffset = 3f;

    [Header("Cylinder Settings")]
    public float maxHeightDifference = 2f;

    [Header("Layer Settings")]
    public LayerMask damageLayer;

    [Header("Debug")]
    public bool showDebugLog = true;

    void Start()
    {
        ApplyExplosionDamage();
    }

    void ApplyExplosionDamage()
    {
        // Tâm nổ được nâng lên
        Vector3 explosionCenter =
            transform.position + Vector3.up * upwardOffset;

        // Quét collider trong vùng nổ
        Collider[] hits =
            Physics.OverlapSphere(explosionCenter, radius, damageLayer);

        foreach (Collider hit in hits)
        {
            // Chỉ damage player
            if (!hit.CompareTag("Player"))
                continue;

            // Giả lập hình trụ bằng giới hạn chiều cao
            float heightDifference =
                Mathf.Abs(hit.transform.position.y - transform.position.y);

            if (heightDifference > maxHeightDifference)
                continue;

            PlayerHealth hp =
                hit.GetComponent<PlayerHealth>();

            if (hp == null)
                continue;

            // Damage giảm theo khoảng cách
            float distance =
                Vector3.Distance(explosionCenter, hit.transform.position);

            float t =
                Mathf.Clamp01(distance / radius);

            float finalDamage =
                Mathf.Lerp(damage, 0f, t);

            // Bỏ qua damage quá nhỏ
            if (finalDamage <= 0.5f)
                continue;

            hp.TakeDamage(finalDamage);

            if (showDebugLog)
            {
                Debug.Log(
                    $"Player took {finalDamage:F1} damage | " +
                    $"Distance: {distance:F2} | " +
                    $"HeightDiff: {heightDifference:F2}"
                );
            }

            break;
        }
    }

    // Hiển thị vùng ảnh hưởng trong Scene
    void OnDrawGizmosSelected()
    {
        Vector3 explosionCenter =
            transform.position + Vector3.up * upwardOffset;

        // Radius chính
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(explosionCenter, radius);

        // Giới hạn chiều cao
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            new Vector3(
                transform.position.x,
                transform.position.y + maxHeightDifference / 2f,
                transform.position.z
            ),
            new Vector3(
                radius * 2,
                maxHeightDifference,
                radius * 2
            )
        );
    }
}