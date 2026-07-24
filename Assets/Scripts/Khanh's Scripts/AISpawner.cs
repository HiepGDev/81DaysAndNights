using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] aiPrefabs;

    [Tooltip("Số lượng AI tối đa được phép tồn tại cùng lúc trên sân")]
    [SerializeField] private int maxAliveAI = 10;

    [Tooltip("Thời gian chờ giữa các lần kiểm tra và đẻ bù")]
    [SerializeField] private float spawnInterval = 1.0f;

    [SerializeField] private Transform[] spawnPoints;

    private List<GameObject> activeAIs = new List<GameObject>();

    private void Start()
    {
        if (aiPrefabs != null && aiPrefabs.Length > 0)
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
        Debug.Log($"[AISpawner] Bắt đầu duy trì quân số tối đa là: {maxAliveAI} AI...");

        while (true)
        {
            activeAIs.RemoveAll(ai => ai == null || !ai.activeInHierarchy || IsDeadRagdoll(ai));

            // BƯỚC 2: Kiểm tra xem có cần đẻ bù không
            if (activeAIs.Count < maxAliveAI)
            {
                int needed = maxAliveAI - activeAIs.Count;
                Debug.Log($"[AISpawner] Quân số hiện tại: {activeAIs.Count}/{maxAliveAI}. Đang đẻ bù 1 AI...");

                SpawnSingleAI();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnSingleAI()
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
        GameObject newAI = Instantiate(randomPrefab, spawnPos, spawnRot);

        activeAIs.Add(newAI);
    }

    private bool IsDeadRagdoll(GameObject aiObj)
    {
        if (aiObj.CompareTag("Untagged")) return true;

        TeammateHealth health = aiObj.GetComponent<TeammateHealth>();
        if (health != null)
        {
            if (!health.enabled) return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.5f);

        if (spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var pt in spawnPoints)
            {
                if (pt != null) Gizmos.DrawWireSphere(pt.position, 0.3f);
            }
        }
    }
}