using UnityEngine;
using System.Collections;

public class BombSpawner : MonoBehaviour
{
    public GameObject explosionPrefab;

    public float rangeX = 40f;
    public float rangeZ = 40f;

    public LayerMask groundLayer;

    [Header("Spawn Settings")]
    public bool infiniteSpawn = false;
    public float spawnDuration = 20f;
    [Header("Rate & Timing Controls")]
    [Tooltip("Minimum number of bombs dropped in one quick cluster")]
    public int minBurstCount = 2;
    [Tooltip("Maximum number of bombs dropped in one quick cluster")]
    public int maxBurstCount = 6;

    [Tooltip("Minimum delay (seconds) between individual bombs in a cluster")]
    public float minTimeBetweenBombs = 0.05f;
    [Tooltip("Maximum delay (seconds) between individual bombs in a cluster")]
    public float maxTimeBetweenBombs = 0.15f;

    [Tooltip("Minimum wait time (seconds) before the next cluster starts")]
    public float minTimeBetweenBursts = 0.12f;
    [Tooltip("Maximum wait time (seconds) before the next cluster starts")]
    public float maxTimeBetweenBursts = 0.35f;
    void Start()
    {
        StartCoroutine(SpawnBombs());
    }

    IEnumerator SpawnBombs()
    {
        float timer = 0f;

        while (infiniteSpawn || timer < spawnDuration)
        {
            int burstCount = Random.Range(minBurstCount, maxBurstCount + 1);

            for (int i = 0; i < burstCount; i++)
            {
                SpawnExplosion();

                yield return new WaitForSeconds(
                    Random.Range(minTimeBetweenBombs, maxTimeBetweenBombs)
                );
            }

            float waitTime = Random.Range(minTimeBetweenBursts, maxTimeBetweenBursts);

            yield return new WaitForSeconds(waitTime);

            if (!infiniteSpawn)
            {
                timer += waitTime;
            }
        }
    }

    void SpawnExplosion()
    {
        float x = transform.position.x + Random.Range(-rangeX, rangeX);
        float z = transform.position.z + Random.Range(-rangeZ, rangeZ);

        Vector3 rayStart = new Vector3(x, transform.position.y + 100f, z);

        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            Mathf.Infinity,
            groundLayer))
        {
            Instantiate(
                explosionPrefab,
                hit.point,
                Quaternion.identity
            );
        }
    }
    private void OnDrawGizmosSelected()
    {
        // Set the color to a semi-transparent red
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 spawnAreaSize = new Vector3(rangeX * 2f, 2f, rangeZ * 2f);

        Gizmos.DrawCube(transform.position, spawnAreaSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
}