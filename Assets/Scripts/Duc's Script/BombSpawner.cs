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

    void Start()
    {
        StartCoroutine(SpawnBombs());
    }

    IEnumerator SpawnBombs()
    {
        float timer = 0f;

        while (infiniteSpawn || timer < spawnDuration)
        {
            int burstCount = Random.Range(2, 6);

            for (int i = 0; i < burstCount; i++)
            {
                SpawnExplosion();

                yield return new WaitForSeconds(
                    Random.Range(0.05f, 0.15f)
                );
            }

            float waitTime = Random.Range(0.12f, 0.35f);

            yield return new WaitForSeconds(waitTime);

            if (!infiniteSpawn)
            {
                timer += waitTime;
            }
        }
    }

    void SpawnExplosion()
    {
        float x = Random.Range(-rangeX, rangeX);
        float z = Random.Range(-rangeZ, rangeZ);

        Vector3 rayStart = new Vector3(x, 50f, z);

        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            100f,
            groundLayer))
        {
            Instantiate(
                explosionPrefab,
                hit.point,
                Quaternion.identity
            );
        }
    }
}