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

        while (timer < 20f) // 20 giây
        {
            SpawnExplosion();

            float waitTime = Random.Range(0.5f, 1.5f);
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