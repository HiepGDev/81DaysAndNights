using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PhuScene;

public enum WaveState
{
    Preparing,      // Countdown before a wave starts
    WaveActive,     // Combat active (enemies spawning and/or being fought)
    WaveCompleted,  // All enemies defeated, transitional cooldown
    Victory,        // Reached the maximum wave target
    GameOver        // Player has died
}

[System.Serializable]
public struct EnemySpawnRule
{
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject enemyPrefab;

    [Tooltip("The wave number at which this enemy starts spawning (inclusive).")]
    public int startWave;

    [Tooltip("Relative weight of spawning this enemy type when active.")]
    [Range(0f, 100f)] public float spawnChanceWeight;
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

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave State")]
    private int currentWave = 0;
    private WaveState currentState = (WaveState)(-1);
    [SerializeField] private int totalWaves = 10; // Capped at 10 (set to 0 for endless)

    [Header("Economy System")]
    [SerializeField] private int money = 0;
    [SerializeField] private int points = 0;

    [Header("Timers")]
    [SerializeField] private float prepDuration = 15f;      // Preparation time before first wave
    [SerializeField] private float cooldownDuration = 30f;  // Cooldown time between waves
    private float countdownTimer = 0f;

    [Header("Enemy Configurations")]
    [SerializeField] private List<EnemySpawnRule> enemyRules = new List<EnemySpawnRule>();
    [SerializeField] private Transform[] spawnPoints;      // Where to spawn enemies

    [Header("Ally Configurations")]
    [SerializeField] private Transform[] allySpawnPoints;

    public Transform[] AllySpawnPoints => allySpawnPoints;
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
    private int remainingEnemies = 0;
    private Coroutine gameLoopCoroutine;
    private Coroutine deathMonitorCoroutine;

    // --- Frontend API Events ---
    public event System.Action<WaveState> OnWaveStateChanged;
    public event System.Action<int> OnWaveStarted;
    public event System.Action<int> OnWaveCompleted;
    public event System.Action<int, int> OnEnemyCountChanged; // (remaining, total)
    public event System.Action<float> OnCountdownTick;        // remaining seconds
    public event System.Action<bool> OnSpawningStatusChanged; // fires when spawning finishes
    public event System.Action<int> OnMoneyChanged;
    public event System.Action<int> OnPointsChanged;

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

        gameLoopCoroutine = StartCoroutine(GameLoop());
    }

    private void CleanUpDeadEnemies()
    {
        int previousCount = activeEnemies.Count;

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && IsEnemyDead(enemy))
            {
                AwardDeathRewards(enemy);
            }
        }

        activeEnemies.RemoveAll(enemy => IsEnemyDead(enemy));
        
        if (activeEnemies.Count != previousCount)
        {
            remainingEnemies = activeEnemies.Count;
            OnEnemyCountChanged?.Invoke(activeEnemies.Count, spawnedEnemiesCount);
            Debug.Log($"[WaveManager] Enemies remaining: {activeEnemies.Count}/{spawnedEnemiesCount}");
        }
    }

    private IEnumerator MonitorEnemyDeaths()
    {
        WaitForSeconds waitDelay = new WaitForSeconds(deathCheckInterval);

        while (currentState == WaveState.WaveActive)
        {
            CleanUpDeadEnemies();
            yield return waitDelay;
        }
    }

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
            OnWaveStarted?.Invoke(currentWave);
            countdownTimer = currentWave == 1 ? prepDuration : cooldownDuration;
            
            while (countdownTimer > 0f)
            {
                if (IsPlayerDead())
                {
                    SetState(WaveState.GameOver);
                    yield break;
                }

                OnCountdownTick?.Invoke(countdownTimer);

                float elapsed = 0f;
                while (elapsed < 1.0f && countdownTimer > 0f)
                {
                    yield return new WaitForSeconds(0.05f);
                    elapsed += 0.05f;
                }

                if (countdownTimer > 0f)
                {
                    countdownTimer -= 1.0f;
                }
            }
            OnCountdownTick?.Invoke(0f);

            // 2. Wave Active Phase (Combat starts and spawning begins)
            SetState(WaveState.WaveActive);
            
            int count;
            float spawnInterval;
            CalculateWaveParameters(out count, out spawnInterval);
            totalEnemiesToSpawn = count;
            spawnedEnemiesCount = 0;
            isSpawningCompleted = false;
            OnSpawningStatusChanged?.Invoke(false);
            activeEnemies.Clear();
            remainingEnemies = 0;
            OnEnemyCountChanged?.Invoke(0, spawnedEnemiesCount);

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

                int currentBatchSize = Mathf.Min(enemiesPerBatch, totalEnemiesToSpawn - spawnedEnemiesCount);
                for (int i = 0; i < currentBatchSize; i++)
                {
                    SpawnSingleEnemy();
                    spawnedEnemiesCount++;
                    remainingEnemies = activeEnemies.Count;
                    OnEnemyCountChanged?.Invoke(activeEnemies.Count, spawnedEnemiesCount);

                    yield return new WaitForSeconds(spawnInterval);
                }

                if (spawnedEnemiesCount >= totalEnemiesToSpawn)
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

            isSpawningCompleted = true;
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
            AddMoneyAndPoints(100 * currentWave, 500 * currentWave);
            OnWaveCompleted?.Invoke(currentWave);
            
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

        GameObject prefab = ChooseEnemyPrefabForWave(currentWave);

        if (prefab != null)
        {
            spawnedObj = Instantiate(prefab, spawnPos, spawnRot);
            spawnedObj.SetActive(true);
        }
        else
        {
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

    private void CalculateWaveParameters(out int count, out float interval)
    {
        count = baseEnemyCount + (currentWave - 1) * enemiesPerWaveIncrease;
        interval = Mathf.Max(minSpawnInterval, baseSpawnInterval - (currentWave - 1) * spawnIntervalDecreasePerWave);
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
            AwardDeathRewards(mockEnemy);

            activeEnemies.Remove(mockEnemy);
            remainingEnemies = activeEnemies.Count;
            OnEnemyCountChanged?.Invoke(activeEnemies.Count, spawnedEnemiesCount);
        }
    }

    private void AwardDeathRewards(GameObject enemy)
    {
        int moneyReward = 10;
        int pointsReward = 50;

        // Try getting reward from EnemyReward component
        EnemyReward rewardComp = enemy.GetComponent<EnemyReward>();
        if (rewardComp != null)
        {
            moneyReward = rewardComp.moneyAwarded;
            pointsReward = rewardComp.pointsAwarded;
        }
        else
        {
            // Try getting reward from MockEnemy component
            MockEnemy mockComp = enemy.GetComponent<MockEnemy>();
            if (mockComp != null)
            {
                moneyReward = mockComp.moneyAwarded;
                pointsReward = mockComp.pointsAwarded;
            }
        }

        AddMoneyAndPoints(moneyReward, pointsReward);
    }

    private bool IsPlayerDead()
    {
        // First check for SurvivalPlayerHealth (since Survival_Player uses it)
        var netPlayers = FindObjectsByType<SurvivalPlayerHealth>(FindObjectsSortMode.None);
        if (netPlayers.Length > 0)
        {
            foreach (var p in netPlayers)
            {
                if (!p.IsDead) return false;
            }
            return true;
        }

        // Fallback to original PlayerHealth
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        if (players.Length > 0)
        {
            foreach (var p in players)
            {
                if (!p.IsDead) return false;
            }
            return true;
        }

        return false;
    }

    private void SetState(WaveState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnWaveStateChanged?.Invoke(currentState);
            Debug.Log($"[WaveManager] State transitioned to: {currentState}");

            if (currentState == WaveState.GameOver)
            {
                StartCoroutine(GameOverReloadRoutine());
            }
        }
    }

    private IEnumerator GameOverReloadRoutine()
    {
        yield return new WaitForSeconds(5f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public WaveStatusReport GetWaveStatus()
    {
        return new WaveStatusReport
        {
            currentWave = this.currentWave,
            totalWaves = this.totalWaves,
            state = this.currentState,
            remainingEnemies = this.remainingEnemies,
            totalEnemies = this.totalEnemiesToSpawn,
            spawnedEnemies = this.spawnedEnemiesCount,
            isSpawningCompleted = this.isSpawningCompleted,
            countdownTime = this.countdownTimer,
            difficultyMultiplier = this.DifficultyMultiplier
        };
    }

    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public WaveState CurrentState => currentState;
    public int RemainingEnemiesCount => remainingEnemies;
    public int TotalEnemiesInWave => totalEnemiesToSpawn;
    public float NextWaveCountdown => countdownTimer;
    public float DifficultyMultiplier => GetDifficultyMultiplierForWave(currentWave);
    public int Money => money;
    public int Points => points;

    public void AddMoneyAndPoints(int moneyAwarded, int pointsAwarded)
    {
        money += moneyAwarded;
        points += pointsAwarded;
        OnMoneyChanged?.Invoke(money);
        OnPointsChanged?.Invoke(points);
    }

    public bool TrySpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            OnMoneyChanged?.Invoke(money);
            return true;
        }
        return false;
    }

    public void SkipCountdownEarly()
    {
        if (currentState == WaveState.Preparing)
        {
            countdownTimer = 0f;
            Debug.Log("[WaveManager] SkipCountdown: countdown set to 0.");
        }
    }

    private GameObject ChooseEnemyPrefabForWave(int wave)
    {
        List<EnemySpawnRule> activeRules = new List<EnemySpawnRule>();
        float totalWeight = 0f;

        foreach (var rule in enemyRules)
        {
            if (rule.enemyPrefab != null && wave >= rule.startWave && rule.spawnChanceWeight > 0f)
            {
                activeRules.Add(rule);
                totalWeight += rule.spawnChanceWeight;
            }
        }

        if (activeRules.Count == 0)
        {
            return null;
        }

        // Weighted random selection
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var rule in activeRules)
        {
            currentSum += rule.spawnChanceWeight;
            if (randomValue <= currentSum)
            {
                return rule.enemyPrefab;
            }
        }

        return activeRules[0].enemyPrefab;
    }

    public void SpawnAlly(GameObject allyPrefab)
    {
        if (allyPrefab == null) return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        bool spawnFound = false;

        if (allySpawnPoints != null && allySpawnPoints.Length > 0)
        {
            int randIndex = UnityEngine.Random.Range(0, allySpawnPoints.Length);
            Transform chosenSpawnPoint = allySpawnPoints[randIndex];
            if (chosenSpawnPoint != null)
            {
                spawnPos = chosenSpawnPoint.position;
                spawnRot = chosenSpawnPoint.rotation;
                spawnFound = true;
            }
        }

        if (!spawnFound)
        {
            var player = FindFirstObjectByType<SurvivalPlayerHealth>();
            if (player != null)
            {
                spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 0.5f;
                spawnRot = player.transform.rotation;
            }
            else
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    spawnPos = mainCam.transform.position + mainCam.transform.forward * 2f;
                    spawnPos.y = 0f;
                }
            }
        }

        GameObject spawnedAlly = Instantiate(allyPrefab, spawnPos, spawnRot);
        spawnedAlly.SetActive(true);
        
        Debug.Log($"[WaveManager] Spawned new ally from shop at: {spawnPos}");
    }
}
