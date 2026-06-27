using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhuScene
{
    public enum WaveState
    {
        Preparing,      // Countdown before a wave starts
        WaveActive,     // Combat active (enemies spawning and/or being fought)
        WaveCompleted,  // All enemies defeated, transitional cooldown
        Victory,        // Reached the maximum wave target
        GameOver        // Player has died
    }

    [Serializable]
    public struct WaveStatusReport
    {
        public int currentWave;
        public int totalWaves;
        public WaveState state;
        public int remainingEnemies;
        public int totalEnemies;
        public int spawnedEnemies;
        public bool isSpawningCompleted;
        public float countdownTime;
        public float difficultyMultiplier;
    }

    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave State")]
        [SerializeField] private int currentWave = 0;
        [SerializeField] private WaveState currentState = WaveState.Preparing;
        [SerializeField] private int totalWaves = 10; // Capped at 10 (set to 0 for endless)

        [Header("Timers")]
        [SerializeField] private float prepDuration = 8f;      // Preparation time before first wave
        [SerializeField] private float cooldownDuration = 5f;  // Cooldown time between waves
        private float countdownTimer = 0f;

        [Header("Enemy Configurations")]
        [SerializeField] private GameObject[] enemyPrefabs;    // List of actual enemy prefabs
        [SerializeField] private Transform[] spawnPoints;      // Where to spawn enemies
        [SerializeField] private Transform playerTransform;    // Reference to Player for target updates

        [Header("Batch Spawn Configuration")]
        [Tooltip("Number of enemies spawned together in a single batch.")]
        [SerializeField] private int enemiesPerBatch = 3;
        [Tooltip("Delay in seconds between spawning consecutive batches.")]
        [SerializeField] private float timeBetweenBatches = 4.0f;

        [Header("Difficulty Scaling Configuration")]
        [SerializeField] private int baseEnemyCount = 4;
        [SerializeField] private int enemiesPerWaveIncrease = 2;
        [SerializeField] private float baseSpawnInterval = 2.0f; // Interval between individual spawns inside a batch
        [SerializeField] private float spawnIntervalDecreasePerWave = 0.15f;
        [SerializeField] private float minSpawnInterval = 0.5f;

        [Header("Enemy Stat Scaling")]
        [SerializeField] private float healthScalePerWave = 0.15f;    // +15% health per wave
        [SerializeField] private float damageScalePerWave = 0.10f;    // +10% damage per wave
        [SerializeField] private float speedScalePerWave = 0.05f;     // +5% speed per wave

        [Header("Performance Settings")]
        [Tooltip("Interval in seconds between performing enemy death detection checks to avoid CPU bottlenecks.")]
        [SerializeField] private float deathCheckInterval = 0.2f;      // Run check 5 times/sec instead of every frame

        // Runtime Tracking
        private List<GameObject> activeEnemies = new List<GameObject>();
        private int totalEnemiesToSpawn = 0;
        private int spawnedEnemiesCount = 0;
        private bool isSpawningCompleted = false;
        private Coroutine gameLoopCoroutine;
        private Coroutine deathMonitorCoroutine;

        // --- Frontend API Events (Instance level for better architecture) ---
        public event Action<WaveState> OnWaveStateChanged;
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action<int, int> OnEnemyCountChanged; // (remaining, total)
        public event Action<float> OnCountdownTick;        // remaining seconds
        public event Action<bool> OnSpawningStatusChanged; // fires when spawning finishes

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Find player automatically if not explicitly assigned
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            // Start game loop
            gameLoopCoroutine = StartCoroutine(GameLoop());
        }

        private void CleanUpDeadEnemies()
        {
            int previousCount = activeEnemies.Count;
            activeEnemies.RemoveAll(enemy => IsEnemyDead(enemy));
            
            if (activeEnemies.Count != previousCount)
            {
                OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
                Debug.Log($"[WaveManager] Enemies remaining: {activeEnemies.Count}/{totalEnemiesToSpawn}");
            }
        }

        // --- Optimized Death Monitoring Coroutine (Eliminates Update Overhead) ---
        private IEnumerator MonitorEnemyDeaths()
        {
            // Cache wait delay to avoid recurring GC allocation
            WaitForSeconds waitDelay = new WaitForSeconds(deathCheckInterval);

            while (currentState == WaveState.WaveActive)
            {
                CleanUpDeadEnemies();
                yield return waitDelay;
            }
        }

        // --- Core Game Loop ---
        private IEnumerator GameLoop()
        {
            currentWave = 0;

            while (true)
            {
                // 1. Preparation Phase (Countdown to next wave)
                currentWave++;
                if (totalWaves > 0 && currentWave > totalWaves)
                {
                    SetState(WaveState.Victory);
                    Debug.Log("[WaveManager] Game Victory! Completed all waves.");
                    yield break;
                }

                SetState(WaveState.Preparing);
                countdownTimer = currentWave == 1 ? prepDuration : cooldownDuration;
                
                while (countdownTimer > 0f)
                {
                    if (IsPlayerDead())
                    {
                        SetState(WaveState.GameOver);
                        yield break;
                    }

                    OnCountdownTick?.Invoke(countdownTimer);
                    yield return new WaitForSeconds(1.0f);
                    countdownTimer -= 1.0f;
                }
                OnCountdownTick?.Invoke(0f);

                // 2. Wave Active Phase (Combat starts and spawning begins)
                SetState(WaveState.WaveActive);
                OnWaveStarted?.Invoke(currentWave);
                
                CalculateWaveParameters(out totalEnemiesToSpawn, out float spawnInterval);
                spawnedEnemiesCount = 0;
                isSpawningCompleted = false;
                OnSpawningStatusChanged?.Invoke(false);
                activeEnemies.Clear();
                OnEnemyCountChanged?.Invoke(0, totalEnemiesToSpawn);

                // Start the optimized death monitoring coroutine
                if (deathMonitorCoroutine != null)
                {
                    StopCoroutine(deathMonitorCoroutine);
                }
                deathMonitorCoroutine = StartCoroutine(MonitorEnemyDeaths());

                // Batch spawning loop
                while (spawnedEnemiesCount < totalEnemiesToSpawn)
                {
                    if (IsPlayerDead())
                      {
                        SetState(WaveState.GameOver);
                        yield break;
                    }

                    // Spawn current batch
                    int currentBatchSize = Mathf.Min(enemiesPerBatch, totalEnemiesToSpawn - spawnedEnemiesCount);
                    for (int i = 0; i < currentBatchSize; i++)
                    {
                        SpawnSingleEnemy();
                        spawnedEnemiesCount++;
                        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);

                        // Quick delay between individual spawns inside the same batch
                        yield return new WaitForSeconds(spawnInterval);
                    }

                    if (spawnedEnemiesCount >= totalEnemiesToSpawn)
                    {
                        break;
                    }

                    // Delay between batches
                    float elapsedBatchTime = 0f;
                    while (elapsedBatchTime < timeBetweenBatches)
                    {
                        if (IsPlayerDead())
                        {
                            SetState(WaveState.GameOver);
                            yield break;
                        }
                        yield return null;
                        elapsedBatchTime += Time.deltaTime;
                    }
                }

                isSpawningCompleted = true;
                OnSpawningStatusChanged?.Invoke(true);

                // Wait for all spawned enemies to die
                while (activeEnemies.Count > 0)
                {
                    if (IsPlayerDead())
                    {
                        SetState(WaveState.GameOver);
                        yield break;
                    }

                    // Let the MonitorEnemyDeaths coroutine do the cleanup checks, we just check count
                    yield return new WaitForSeconds(0.2f);
                }

                // Stop the death monitor coroutine once combat ends
                if (deathMonitorCoroutine != null)
                {
                    StopCoroutine(deathMonitorCoroutine);
                    deathMonitorCoroutine = null;
                }

                // 3. Wave Completed Phase
                SetState(WaveState.WaveCompleted);
                OnWaveCompleted?.Invoke(currentWave);
                
                // Extra short delay for visual satisfaction before the transition countdown starts
                yield return new WaitForSeconds(1.5f);
            }
        }

        // --- Enemy Spawning & Configuration ---
        private void SpawnSingleEnemy()
        {
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                spawnPos = sp.position;
                spawnRot = sp.rotation;
            }
            else
            {
                // Fallback to random spot near origin
                spawnPos = new Vector3(UnityEngine.Random.Range(-10f, 10f), 0f, UnityEngine.Random.Range(-10f, 10f));
            }

            GameObject spawnedObj = null;

            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                // Spawn real enemy
                GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
                spawnedObj = Instantiate(prefab, spawnPos, spawnRot);
                spawnedObj.SetActive(true);
            }
            else
            {
                // Spawn mock enemy procedurally
                spawnedObj = CreateProceduralMockEnemy(spawnPos, spawnRot);
            }

            if (spawnedObj != null)
            {
                activeEnemies.Add(spawnedObj);
                ApplyDifficultyScaling(spawnedObj);
            }
        }

        private GameObject CreateProceduralMockEnemy(Vector3 position, Quaternion rotation)
        {
            EnemyType type = ChooseEnemyTypeForWave(currentWave);

            GameObject mock = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mock.name = $"MockEnemy_{type}_Wave{currentWave}_{spawnedEnemiesCount}";
            mock.transform.position = position + Vector3.up * 1f;
            mock.transform.rotation = rotation;
            mock.tag = "Enemy";

            MockEnemy enemyComp = mock.AddComponent<MockEnemy>();
            enemyComp.SetupMock(type, playerTransform);

            Rigidbody rb = mock.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            return mock;
        }

        private EnemyType ChooseEnemyTypeForWave(int wave)
        {
            float rand = UnityEngine.Random.value;

            if (wave == 1) return EnemyType.Basic;
            if (wave == 2) return rand < 0.8f ? EnemyType.Basic : EnemyType.Elite;
            if (wave <= 4) return rand < 0.6f ? EnemyType.Basic : (rand < 0.95f ? EnemyType.Elite : EnemyType.Boss);
            
            return rand < 0.4f ? EnemyType.Basic : (rand < 0.85f ? EnemyType.Elite : EnemyType.Boss);
        }

        private void ApplyDifficultyScaling(GameObject enemy)
        {
            MockEnemy mock = enemy.GetComponent<MockEnemy>();
            if (mock != null)
            {
                mock.ScaleStats(
                    1f + (currentWave - 1) * healthScalePerWave,
                    1f + (currentWave - 1) * damageScalePerWave,
                    Mathf.Min(2.0f, 1f + (currentWave - 1) * speedScalePerWave)
                );
            }
        }

        // --- Difficulty & Wave Calculations ---
        private void CalculateWaveParameters(out int count, out float interval)
        {
            count = baseEnemyCount + (currentWave - 1) * enemiesPerWaveIncrease;
            interval = Mathf.Max(minSpawnInterval, baseSpawnInterval - (currentWave - 1) * spawnIntervalDecreasePerWave);
        }

        public float GetDifficultyMultiplierForWave(int wave)
        {
            return 1.0f + (wave - 1) * 0.25f;
        }

        // --- Decoupled Health/Death Detection ---
        private bool IsEnemyDead(GameObject enemy)
        {
            if (enemy == null) return true;

            // Indicator A: Target GameObject tag changed from "Enemy" to "Untagged" (Phat's death logic)
            if (!enemy.CompareTag("Enemy"))
            {
                return true;
            }

            // Indicator B: NavMeshAgent disabled upon death (Phat's death logic)
            UnityEngine.AI.NavMeshAgent navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && !navAgent.enabled)
            {
                return true;
            }

            return false;
        }

        public void ReportMockEnemyDeath(GameObject mockEnemy)
        {
            if (activeEnemies.Contains(mockEnemy))
            {
                activeEnemies.Remove(mockEnemy);
                OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
            }
        }

        private bool IsPlayerDead()
        {
            if (playerTransform == null) return false;
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            return playerHealth != null && playerHealth.IsDead;
        }

        private void SetState(WaveState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnWaveStateChanged?.Invoke(currentState);
                Debug.Log($"[WaveManager] State transitioned to: {currentState}");
            }
        }

        // --- Frontend API Snapshot Endpoint (Active Retrieval) ---
        public WaveStatusReport GetWaveStatus()
        {
            return new WaveStatusReport
            {
                currentWave = this.currentWave,
                totalWaves = this.totalWaves,
                state = this.currentState,
                remainingEnemies = this.activeEnemies.Count,
                totalEnemies = this.totalEnemiesToSpawn,
                spawnedEnemies = this.spawnedEnemiesCount,
                isSpawningCompleted = this.isSpawningCompleted,
                countdownTime = this.countdownTimer,
                difficultyMultiplier = this.DifficultyMultiplier
            };
        }

        // Keep individual property getters for retrocompatibility
        public int CurrentWave => currentWave;
        public int TotalWaves => totalWaves;
        public WaveState CurrentState => currentState;
        public int RemainingEnemiesCount => activeEnemies.Count;
        public int TotalEnemiesInWave => totalEnemiesToSpawn;
        public float NextWaveCountdown => countdownTimer;
        public float DifficultyMultiplier => GetDifficultyMultiplierForWave(currentWave);
    }
}
