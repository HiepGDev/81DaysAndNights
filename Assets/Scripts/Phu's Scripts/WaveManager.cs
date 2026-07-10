using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PurrNet;

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

    [System.Serializable]
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

    public class WaveManager : NetworkBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave State")]
        [SerializeField] private SyncVar<int> currentWave = new(0);
        [SerializeField] private SyncVar<WaveState> currentState = new(WaveState.Preparing);
        [SerializeField] private int totalWaves = 10; // Capped at 10 (set to 0 for endless)

        [Header("Timers")]
        [SerializeField] private float prepDuration = 8f;      // Preparation time before first wave
        [SerializeField] private float cooldownDuration = 5f;  // Cooldown time between waves
        private SyncVar<float> countdownTimer = new(0f);

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
        private SyncVar<int> totalEnemiesToSpawn = new(0);
        private SyncVar<int> spawnedEnemiesCount = new(0);
        private SyncVar<bool> isSpawningCompleted = new(false);
        private SyncVar<int> remainingEnemies = new(0);
        private Coroutine gameLoopCoroutine;
        private Coroutine deathMonitorCoroutine;

        // --- Frontend API Events ---
        public event System.Action<WaveState> OnWaveStateChanged;
        public event System.Action<int> OnWaveStarted;
        public event System.Action<int> OnWaveCompleted;
        public event System.Action<int, int> OnEnemyCountChanged; // (remaining, total)
        public event System.Action<float> OnCountdownTick;        // remaining seconds
        public event System.Action<bool> OnSpawningStatusChanged; // fires when spawning finishes

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
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            // Start game loop locally if offline
            if (!isSpawned)
            {
                gameLoopCoroutine = StartCoroutine(GameLoop());
            }
        }

        protected override void OnSpawned()
        {
            if (isServer)
            {
                gameLoopCoroutine = StartCoroutine(GameLoop());
            }

            if (isClient && !isServer)
            {
                currentState.onChanged += (state) => OnWaveStateChanged?.Invoke(state);
                currentWave.onChangedWithOld += (oldWave, newWave) => {
                    if (newWave > oldWave) OnWaveStarted?.Invoke(newWave);
                };
                countdownTimer.onChanged += (time) => OnCountdownTick?.Invoke(time);
                isSpawningCompleted.onChanged += (comp) => OnSpawningStatusChanged?.Invoke(comp);
                
                remainingEnemies.onChanged += (rem) => OnEnemyCountChanged?.Invoke(rem, totalEnemiesToSpawn.value);
                totalEnemiesToSpawn.onChanged += (tot) => OnEnemyCountChanged?.Invoke(remainingEnemies.value, tot);
            }
        }

        private void CleanUpDeadEnemies()
        {
            int previousCount = activeEnemies.Count;
            activeEnemies.RemoveAll(enemy => IsEnemyDead(enemy));
            
            if (activeEnemies.Count != previousCount)
            {
                remainingEnemies.value = activeEnemies.Count;
                OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn.value);
                Debug.Log($"[WaveManager] Enemies remaining: {activeEnemies.Count}/{totalEnemiesToSpawn.value}");
            }
        }

        private IEnumerator MonitorEnemyDeaths()
        {
            WaitForSeconds waitDelay = new WaitForSeconds(deathCheckInterval);

            while (currentState.value == WaveState.WaveActive)
            {
                CleanUpDeadEnemies();
                yield return waitDelay;
            }
        }

        private IEnumerator GameLoop()
        {
            currentWave.value = 0;

            while (true)
            {
                // 1. Preparation Phase (Countdown to next wave)
                currentWave.value++;
                if (totalWaves > 0 && currentWave.value > totalWaves)
                {
                    SetState(WaveState.Victory);
                    Debug.Log("[WaveManager] Game Victory! Completed all waves.");
                    yield break;
                }

                SetState(WaveState.Preparing);
                countdownTimer.value = currentWave.value == 1 ? prepDuration : cooldownDuration;
                
                while (countdownTimer.value > 0f)
                {
                    if (IsPlayerDead())
                    {
                        SetState(WaveState.GameOver);
                        yield break;
                    }

                    OnCountdownTick?.Invoke(countdownTimer.value);
                    yield return new WaitForSeconds(1.0f);
                    countdownTimer.value -= 1.0f;
                }
                OnCountdownTick?.Invoke(0f);

                // 2. Wave Active Phase (Combat starts and spawning begins)
                SetState(WaveState.WaveActive);
                OnWaveStarted?.Invoke(currentWave.value);
                
                int count;
                float spawnInterval;
                CalculateWaveParameters(out count, out spawnInterval);
                totalEnemiesToSpawn.value = count;
                spawnedEnemiesCount.value = 0;
                isSpawningCompleted.value = false;
                OnSpawningStatusChanged?.Invoke(false);
                activeEnemies.Clear();
                remainingEnemies.value = 0;
                OnEnemyCountChanged?.Invoke(0, totalEnemiesToSpawn.value);

                if (deathMonitorCoroutine != null)
                {
                    StopCoroutine(deathMonitorCoroutine);
                }
                deathMonitorCoroutine = StartCoroutine(MonitorEnemyDeaths());

                // Batch spawning loop
                while (spawnedEnemiesCount.value < totalEnemiesToSpawn.value)
                {
                    if (IsPlayerDead())
                    {
                        SetState(WaveState.GameOver);
                        yield break;
                    }

                    int currentBatchSize = Mathf.Min(enemiesPerBatch, totalEnemiesToSpawn.value - spawnedEnemiesCount.value);
                    for (int i = 0; i < currentBatchSize; i++)
                    {
                        SpawnSingleEnemy();
                        spawnedEnemiesCount.value++;
                        remainingEnemies.value = activeEnemies.Count;
                        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn.value);

                        yield return new WaitForSeconds(spawnInterval);
                    }

                    if (spawnedEnemiesCount.value >= totalEnemiesToSpawn.value)
                    {
                        break;
                    }

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

                isSpawningCompleted.value = true;
                OnSpawningStatusChanged?.Invoke(true);

                while (activeEnemies.Count > 0)
                {
                    if (IsPlayerDead())
                    {
                        SetState(WaveState.GameOver);
                        yield break;
                    }

                    yield return new WaitForSeconds(0.2f);
                }

                if (deathMonitorCoroutine != null)
                {
                    StopCoroutine(deathMonitorCoroutine);
                    deathMonitorCoroutine = null;
                }

                SetState(WaveState.WaveCompleted);
                OnWaveCompleted?.Invoke(currentWave.value);
                
                yield return new WaitForSeconds(1.5f);
            }
        }

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
                spawnPos = new Vector3(UnityEngine.Random.Range(-10f, 10f), 0f, UnityEngine.Random.Range(-10f, 10f));
            }

            GameObject spawnedObj = null;

            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
                spawnedObj = Instantiate(prefab, spawnPos, spawnRot);
                spawnedObj.SetActive(true);
            }
            else
            {
                if (isSpawned)
                {
                    Debug.LogError("[WaveManager] Cannot spawn procedural capsules in multiplayer. Please assign enemy prefabs in the inspector.");
                }
                else
                {
                    spawnedObj = CreateProceduralMockEnemy(spawnPos, spawnRot);
                }
            }

            if (spawnedObj != null)
            {
                activeEnemies.Add(spawnedObj);
                ApplyDifficultyScaling(spawnedObj);

                if (isSpawned)
                {
                    NetworkManager.main.Spawn(spawnedObj);
                }
            }
        }

        private GameObject CreateProceduralMockEnemy(Vector3 position, Quaternion rotation)
        {
            EnemyType type = ChooseEnemyTypeForWave(currentWave.value);

            GameObject mock = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mock.name = $"MockEnemy_{type}_Wave{currentWave.value}_{spawnedEnemiesCount.value}";
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
                    1f + (currentWave.value - 1) * healthScalePerWave,
                    1f + (currentWave.value - 1) * damageScalePerWave,
                    Mathf.Min(2.0f, 1f + (currentWave.value - 1) * speedScalePerWave)
                );
            }
        }

        private void CalculateWaveParameters(out int count, out float interval)
        {
            count = baseEnemyCount + (currentWave.value - 1) * enemiesPerWaveIncrease;
            interval = Mathf.Max(minSpawnInterval, baseSpawnInterval - (currentWave.value - 1) * spawnIntervalDecreasePerWave);
        }

        public float GetDifficultyMultiplierForWave(int wave)
        {
            return 1.0f + (wave - 1) * 0.25f;
        }

        private bool IsEnemyDead(GameObject enemy)
        {
            if (enemy == null) return true;

            if (!enemy.CompareTag("Enemy"))
            {
                return true;
            }

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
                remainingEnemies.value = activeEnemies.Count;
                OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn.value);
            }
        }

        private bool IsPlayerDead()
        {
            if (isSpawned)
            {
                var players = FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
                if (players.Length == 0) return false;
                foreach (var p in players)
                {
                    if (!p.IsDead) return false;
                }
                return true;
            }
            else
            {
                if (playerTransform == null) return false;
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                return playerHealth != null && playerHealth.IsDead;
            }
        }

        private void SetState(WaveState newState)
        {
            if (currentState.value != newState)
            {
                currentState.value = newState;
                OnWaveStateChanged?.Invoke(currentState.value);
                Debug.Log($"[WaveManager] State transitioned to: {currentState.value}");
            }
        }

        public WaveStatusReport GetWaveStatus()
        {
            return new WaveStatusReport
            {
                currentWave = this.currentWave.value,
                totalWaves = this.totalWaves,
                state = this.currentState.value,
                remainingEnemies = this.remainingEnemies.value,
                totalEnemies = this.totalEnemiesToSpawn.value,
                spawnedEnemies = this.spawnedEnemiesCount.value,
                isSpawningCompleted = this.isSpawningCompleted.value,
                countdownTime = this.countdownTimer.value,
                difficultyMultiplier = this.DifficultyMultiplier
            };
        }

        public int CurrentWave => currentWave.value;
        public int TotalWaves => totalWaves;
        public WaveState CurrentState => currentState.value;
        public int RemainingEnemiesCount => remainingEnemies.value;
        public int TotalEnemiesInWave => totalEnemiesToSpawn.value;
        public float NextWaveCountdown => countdownTimer.value;
        public float DifficultyMultiplier => GetDifficultyMultiplierForWave(currentWave.value);
    }
}
