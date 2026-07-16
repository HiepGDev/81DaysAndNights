using UnityEngine;

public class ExplosionDamageNoCover : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float radius = 10f;
    public float damage = 30f;
    public float upwardOffset = 3f;

    [Header("Layer Settings")]
    public LayerMask damageLayer;

    [Header("Debug")]
    public bool showDebugLog = true;

    [Header("Audio")]
    public AudioClip explosionSound;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        ApplyExplosionDamage();

        if (explosionSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.02f);
            audioSource.PlayOneShot(explosionSound);
        }
    }

    void ApplyExplosionDamage()
    {
        // Tâm nổ được nâng lên
        Vector3 explosionCenter =
            transform.position + Vector3.up * upwardOffset;
        
        // Quét collider trong vùng nổ
        Collider[] hits = Physics.OverlapSphere(explosionCenter, radius);

        foreach (Collider hit in hits)
        {
            // Chỉ damage player
            if (!hit.CompareTag("Player"))
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
                    $"Distance: {distance:F2} | Ignore Cover"
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(explosionCenter, radius);
    }
}