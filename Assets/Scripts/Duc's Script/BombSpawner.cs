using UnityEngine;
using System.Collections;

public class BombSpawner : MonoBehaviour
{
    public GameObject explosionPrefab;

    public float rangeX = 40f;
    public float rangeZ = 40f;

    public LayerMask groundLayer;

    void Start()
    {
        StartCoroutine(SpawnBombs());
    }

    IEnumerator SpawnBombs()
    {
        float timer = 0f;

        while (timer < 20f) // 20 giây pháo kích
        {
            // Mỗi đợt 2 đến 5 quả liên tiếp
            int burstCount = Random.Range(2, 6);

            for (int i = 0; i < burstCount; i++)
            {
                SpawnExplosion();

                // giữa từng quả rất ngắn
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }

            // nghỉ rất ngắn rồi tiếp tục
            float waitTime = Random.Range(0.12f, 0.35f);
            yield return new WaitForSeconds(waitTime);

            timer += waitTime;
        }
    }

    void SpawnExplosion()
    {
        float x = Random.Range(-rangeX, rangeX);
        float z = Random.Range(-rangeZ, rangeZ);

        Vector3 rayStart = new Vector3(x, 50f, z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            Instantiate(explosionPrefab, hit.point, Quaternion.identity);
        }
    }
}