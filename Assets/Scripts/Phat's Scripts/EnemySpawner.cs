using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRadius = 5.0f;

    [Header("Wave Configuration")]
    [SerializeField] private int totalWaves = 3;
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float timeBetweenWaves = 10.0f;
    [SerializeField] private float timeBetweenSpawns = 1.0f;

    [Header("Ambush Settings")]
    [SerializeField] private bool forceAmbushMode = true;

    private int currentWave = 0;
    private int spawnedThisWave = 0;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[Spawner] No Enemy Prefab assigned!");
            return;
        }
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (currentWave < totalWaves)
        {
            currentWave++;
            Debug.Log($"[Spawner] Starting Wave {currentWave}/{totalWaves}");
            
            yield return StartCoroutine(SpawnWave());
            
            if (currentWave < totalWaves)
            {
                Debug.Log($"[Spawner] Wave {currentWave} complete. Waiting {timeBetweenWaves}s for next wave...");
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        Debug.Log("[Spawner] All waves complete.");
    }

    private IEnumerator SpawnWave()
    {
        spawnedThisWave = 0;
        while (spawnedThisWave < enemiesPerWave)
        {
            SpawnEnemy();
            spawnedThisWave++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    private void SpawnEnemy()
    {
        Vector3 randomPos = GetRandomNavMeshPosition();
        if (randomPos != Vector3.zero)
        {
            // 1. Create a copy of the Hierarchy Master
            GameObject enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
            
            // 2. THE HIERARCHY FIX: Ensure the new copy is turned ON
            enemy.SetActive(true);
            
            // 3. Configure behavior
            EnemyBehaviorAgent agent = enemy.GetComponent<EnemyBehaviorAgent>();
            if (agent != null)
            {
                agent.currentMode = forceAmbushMode ? EnemyBehaviorAgent.EnemyMode.Ambush : EnemyBehaviorAgent.EnemyMode.Wander;
            }
        }
    }

    private Vector3 GetRandomNavMeshPosition()
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * spawnRadius;
            NavMeshHit hit;
            // 2.0m search range for a valid floor
            if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        Debug.LogWarning("[Spawner] Failed to find valid NavMesh position for spawn!");
        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
