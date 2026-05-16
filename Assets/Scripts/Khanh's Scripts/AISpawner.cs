using System.Collections;
using UnityEngine;

public class AISpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] aiPrefabs;

    [SerializeField] private int totalToSpawn = 10;

    [SerializeField] private float spawnInterval = 1.0f;

    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        if (aiPrefabs != null && aiPrefabs.Length >0)
        {
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogError($"[AISpawner] '{gameObject.name}' chưa được gán AI Prefab!");
        }
    }

    private IEnumerator SpawnRoutine()
    {
        Debug.Log($"[AISpawner] Bắt đầu đẻ {totalToSpawn} AI...");

        for (int i = 0; i < totalToSpawn; i++)
        {
            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                spawnPos = randomPoint.position;
                spawnRot = randomPoint.rotation;
            }

            GameObject randomPrefab = aiPrefabs[Random.Range(0, aiPrefabs.Length)];

            Instantiate(randomPrefab, spawnPos, spawnRot);

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log($"[AISpawner] Đã hoàn thành đẻ {totalToSpawn} AI.");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}