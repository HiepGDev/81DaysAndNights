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

    [Tooltip("Spawn points dedicated for spawning this specific enemy type.")]
    public Transform[] spawnPoints;
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

[System.Serializable]
public struct ScoreReport
{
    public float baseKillScore;
    public float totalKillScore;
    public int totalKills;
    public int waveKills;
    public float economyBonus;
    public float economyDecayFactor;
    public float moneyGainedInWave;
    public float moneySpentInIntermission;
    public int totalMoneyGained;
    public float unspentMoney;
    public float waveMultiplier;
    public float accuracyPercent;
    public float efficiencyMultiplier;
    public int waveScore;
    public int totalGameScore;
    public int shotsFired;
    public int shotsHit;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave State")]
    private int currentWave = 0;
    private WaveState currentState = (WaveState)(-1);
    [SerializeField] private int totalWaves = 10; // Capped at 10 (set to 0 for endless)

    [Header("Scoring Formula Parameters & Multipliers")]
    [Tooltip("Multiplier applied to net wave savings (Money Gained - Money Spent) for Economy Bonus.")]
    [SerializeField] private float economyBonusMultiplier = 2f;

    [Tooltip("Total lifetime money earned threshold where Economy Bonus efficiency diminishes by 50%. Higher values allow the bonus to stay active longer.")]
    [SerializeField] private float economyDecayThreshold = 5000;

    [Tooltip("Weight multiplied by accuracy percentage for Efficiency Multiplier: 1 + (Accuracy% * Weight).")]
    [SerializeField] private float accuracyEfficiencyWeight = 0.25f;

    [Tooltip("Divisor for quadratic wave multiplier: 1 + (waves_survived^2 / Divisor).")]
    [SerializeField] private float waveMultiplierDivisor = 100f;

    [Tooltip("Per-wave scaling factor for kill score: basePoints * typeMultiplier * (1 + (wave * Factor)).")]
    [SerializeField] private float killScoreWaveScaleFactor = 0.05f;

    [Tooltip("Default base points awarded per kill if not specified by EnemyReward.")]
    [SerializeField] private float defaultBaseKillPoints = 100f;

    [Header("Enemy Type Multipliers")]
    [SerializeField] private float basicEnemyMultiplier = 1.0f;
    [SerializeField] private float eliteEnemyMultiplier = 1.5f;
    [SerializeField] private float bossEnemyMultiplier = 2.0f;

    [Header("Economy System")]
    [SerializeField] private int money = 0;
    [SerializeField] private int points = 0;

    [Header("Scoring System Metrics")]
    [SerializeField] private int cumulativeGameScore = 0;
    [SerializeField] private float waveKillScore = 0f;
    [SerializeField] private int waveKills = 0;
    [SerializeField] private int totalMoneyGained = 0;
    [SerializeField] private int totalMoneySpent = 0;
    [SerializeField] private int moneyGainedInWave = 0;
    [SerializeField] private int moneySpentInIntermission = 0;
    [SerializeField] private int totalKills = 0;
    [SerializeField] private float totalKillScore = 0f;
    [SerializeField] private int shotsFired = 0;
    [SerializeField] private int shotsHit = 0;

    public int CumulativeGameScore => cumulativeGameScore;
    public float WaveKillScore => waveKillScore;
    public int WaveKills => waveKills;
    public int TotalMoneyGained => totalMoneyGained;
    public int TotalMoneySpent => totalMoneySpent;
    public int MoneyGainedInWave => moneyGainedInWave;
    public int MoneySpentInIntermission => moneySpentInIntermission;
    public int TotalKills => totalKills;
    public float TotalKillScore => totalKillScore;
    public int ShotsFired => shotsFired;
    public int ShotsHit => shotsHit;

    public float AccuracyPercent => shotsFired > 0 ? (float)shotsHit / (float)shotsFired : 0f;
    public float EfficiencyMultiplier => 1f + (AccuracyPercent * accuracyEfficiencyWeight);
    public float NetWaveSavings => Mathf.Max(0f, moneyGainedInWave - moneySpentInIntermission);
    public float EconomyDecayFactor => 1f / (1f + ((float)totalMoneyGained / Mathf.Max(1f, economyDecayThreshold)));
    public float EconomyBonus => NetWaveSavings * economyBonusMultiplier * EconomyDecayFactor;
    public float WaveMultiplier => 1f + (Mathf.Pow(Mathf.Max(0, currentWave), 2f) / Mathf.Max(1f, waveMultiplierDivisor));

    public int CalculateCurrentWaveScore()
    {
        float subtotal = waveKillScore + EconomyBonus;
        float finalScore = subtotal * WaveMultiplier * EfficiencyMultiplier;
        return Mathf.RoundToInt(finalScore);
    }

    public int CalculateTotalGameScore()
    {
        return cumulativeGameScore + CalculateCurrentWaveScore();
    }

    public ScoreReport GetScoreReport()
    {
        return new ScoreReport
        {
            baseKillScore = waveKillScore,
            totalKillScore = totalKillScore,
            totalKills = totalKills,
            waveKills = waveKills,
            economyBonus = EconomyBonus,
            economyDecayFactor = EconomyDecayFactor,
            moneyGainedInWave = moneyGainedInWave,
            moneySpentInIntermission = moneySpentInIntermission,
            totalMoneyGained = totalMoneyGained,
            unspentMoney = NetWaveSavings,
            waveMultiplier = WaveMultiplier,
            accuracyPercent = AccuracyPercent,
            efficiencyMultiplier = EfficiencyMultiplier,
            waveScore = CalculateCurrentWaveScore(),
            totalGameScore = CalculateTotalGameScore(),
            shotsFired = shotsFired,
            shotsHit = shotsHit
        };
    }

    [Header("Timers")]
    [SerializeField] private float prepDuration = 15f;      // Preparation time before first wave
    [SerializeField] private float cooldownDuration = 30f;  // Cooldown time between waves
    private float countdownTimer = 0f;

    [Header("Enemy Configurations")]
    [SerializeField] private List<EnemySpawnRule> enemyRules = new List<EnemySpawnRule>();
    [Tooltip("Minimum distance from the player to consider a spawn point valid (when multiple options exist).")]
    [SerializeField] private float minSpawnDistanceFromPlayer = 10f;

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
    public event System.Action<float, string> OnKillScoreRecorded; // (killScore, enemyName)
    public event System.Action<ScoreReport> OnScoreReportUpdated;

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

        if (FindFirstObjectByType<MockScoreUI>() == null)
        {
            GameObject mockUI = new GameObject("MockScoreUI");
            mockUI.AddComponent<MockScoreUI>();
        }

        gameLoopCoroutine = StartCoroutine(GameLoop());
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

            // Award completion money reward FIRST before calculating end-of-wave bonuses
            int completionMoney = 100 * currentWave;
            money += completionMoney;
            totalMoneyGained += completionMoney;
            OnMoneyChanged?.Invoke(money);

            SetState(WaveState.WaveCompleted);
            OnWaveCompleted?.Invoke(currentWave);
            
            yield return new WaitForSeconds(1.5f);
        }
    }

    private Transform SelectBestSpawnPoint(List<Transform> validPoints, Vector3 playerPos)
    {
        if (validPoints == null || validPoints.Count == 0) return null;
        if (validPoints.Count == 1) return validPoints[0];

        // Filter points that meet minimum distance threshold
        List<Transform> candidatePoints = new List<Transform>();
        foreach (var pt in validPoints)
        {
            if (pt != null && Vector3.Distance(playerPos, pt.position) >= minSpawnDistanceFromPlayer)
            {
                candidatePoints.Add(pt);
            }
        }

        // If no points pass the minimum distance threshold, use all valid points as candidate pool
        if (candidatePoints.Count == 0)
        {
            candidatePoints = validPoints;
        }

        if (candidatePoints.Count == 1) return candidatePoints[0];

        // Pick 2 candidate points and select the one further away from the player
        int indexA = UnityEngine.Random.Range(0, candidatePoints.Count);
        int indexB = UnityEngine.Random.Range(0, candidatePoints.Count);
        int attempts = 0;
        while (indexB == indexA && candidatePoints.Count > 1 && attempts < 10)
        {
            indexB = UnityEngine.Random.Range(0, candidatePoints.Count);
            attempts++;
        }

        Transform pointA = candidatePoints[indexA];
        Transform pointB = candidatePoints[indexB];

        float distA = Vector3.Distance(playerPos, pointA.position);
        float distB = Vector3.Distance(playerPos, pointB.position);

        return distA >= distB ? pointA : pointB;
    }

    private void SpawnSingleEnemy()
    {
        Vector3 playerPos = Vector3.zero;
        if (playerTransform != null)
        {
            playerPos = playerTransform.position;
        }
        else
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null)
            {
                playerTransform = pObj.transform;
                playerPos = playerTransform.position;
            }
        }

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        GameObject spawnedObj = null;

        if (TryChooseEnemyRuleForWave(currentWave, out EnemySpawnRule rule))
        {
            if (rule.spawnPoints != null && rule.spawnPoints.Length > 0)
            {
                List<Transform> validPoints = new List<Transform>();
                foreach (var sp in rule.spawnPoints)
                {
                    if (sp != null) validPoints.Add(sp);
                }

                Transform chosenPoint = SelectBestSpawnPoint(validPoints, playerPos);
                if (chosenPoint != null)
                {
                    spawnPos = chosenPoint.position;
                    spawnRot = chosenPoint.rotation;
                }
                else
                {
                    Vector3 offset = UnityEngine.Random.onUnitSphere;
                    offset.y = 0;
                    if (offset.sqrMagnitude > 0.001f) offset.Normalize();
                    spawnPos = playerPos + offset * UnityEngine.Random.Range(minSpawnDistanceFromPlayer, minSpawnDistanceFromPlayer + 15f);
                }
            }
            else
            {
                Vector3 offset = UnityEngine.Random.onUnitSphere;
                offset.y = 0;
                if (offset.sqrMagnitude > 0.001f) offset.Normalize();
                spawnPos = playerPos + offset * UnityEngine.Random.Range(minSpawnDistanceFromPlayer, minSpawnDistanceFromPlayer + 15f);
            }

            if (rule.enemyPrefab != null)
            {
                spawnedObj = Instantiate(rule.enemyPrefab, spawnPos, spawnRot);
                spawnedObj.SetActive(true);
            }
            else
            {
                spawnedObj = CreateProceduralMockEnemy(spawnPos, spawnRot);
            }
        }
        else
        {
            Vector3 offset = UnityEngine.Random.onUnitSphere;
            offset.y = 0;
            if (offset.sqrMagnitude > 0.001f) offset.Normalize();
            spawnPos = playerPos + offset * UnityEngine.Random.Range(minSpawnDistanceFromPlayer, minSpawnDistanceFromPlayer + 15f);
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

    public void ReportMockEnemyDeath(GameObject mockEnemy)
    {
        if (mockEnemy != null && activeEnemies.Contains(mockEnemy))
        {
            AwardDeathRewards(mockEnemy);

            activeEnemies.Remove(mockEnemy);
            remainingEnemies = activeEnemies.Count;
            OnEnemyCountChanged?.Invoke(activeEnemies.Count, spawnedEnemiesCount);
        }
    }

    public void ReportMockEnemyDeath(MockEnemy mockEnemy)
    {
        if (mockEnemy != null)
        {
            ReportMockEnemyDeath(mockEnemy.gameObject);
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

    private void AwardDeathRewards(GameObject enemy)
    {
        int moneyReward = 10;
        float basePoints = defaultBaseKillPoints;
        float enemyTypeMultiplier = basicEnemyMultiplier;
        string enemyName = "Enemy";

        if (enemy != null)
        {
            enemyName = enemy.name;

            EnemyReward rewardComp = enemy.GetComponent<EnemyReward>();
            if (rewardComp != null)
            {
                moneyReward = rewardComp.moneyAwarded;
                basePoints = rewardComp.pointsAwarded;
            }

            MockEnemy mockComp = enemy.GetComponent<MockEnemy>();
            if (mockComp != null)
            {
                moneyReward = mockComp.moneyAwarded;
                basePoints = mockComp.pointsAwarded;
                if (mockComp.Type == EnemyType.Elite) enemyTypeMultiplier = eliteEnemyMultiplier;
                else if (mockComp.Type == EnemyType.Boss) enemyTypeMultiplier = bossEnemyMultiplier;
            }
            else if (enemy.name.ToLower().Contains("elite"))
            {
                enemyTypeMultiplier = eliteEnemyMultiplier;
            }
            else if (enemy.name.ToLower().Contains("boss") || enemy.name.ToLower().Contains("sniper"))
            {
                enemyTypeMultiplier = bossEnemyMultiplier;
            }
        }

        // Calculation: kill_score = basePoints * enemyTypeMultiplier * (1 + (waves_survived * killScoreWaveScaleFactor))
        float waveScale = 1f + (Mathf.Max(1, currentWave) * killScoreWaveScaleFactor);
        float killScore = basePoints * enemyTypeMultiplier * waveScale;

        waveKills++;
        totalKills++;
        waveKillScore += killScore;
        totalKillScore += killScore;
        totalMoneyGained += moneyReward;
        moneyGainedInWave += moneyReward;
        money += moneyReward;

        // In active combat, live points increase with total game score
        points = CalculateTotalGameScore();

        OnMoneyChanged?.Invoke(money);
        OnPointsChanged?.Invoke(points);
        OnKillScoreRecorded?.Invoke(killScore, enemyName);
        OnScoreReportUpdated?.Invoke(GetScoreReport());
    }

    public void RecordShotFired()
    {
        shotsFired++;
    }

    public void RecordShotHit()
    {
        shotsHit++;
    }

    public void RecalculatePoints()
    {
        points = CalculateTotalGameScore();
        OnPointsChanged?.Invoke(points);
        OnScoreReportUpdated?.Invoke(GetScoreReport());
    }

    private bool IsPlayerDead()
    {
        // First check for PlayerHealth (since Survival_Player uses it)
        var netPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
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

            // When entering intermission (Preparing phase), reset per-wave economy & wave metrics
            if (currentState == WaveState.Preparing)
            {
                moneyGainedInWave = 0;
                moneySpentInIntermission = 0;
                waveKillScore = 0f;
                waveKills = 0;
            }

            // Reset accuracy shot counters per wave when combat starts
            if (currentState == WaveState.WaveActive)
            {
                shotsFired = 0;
                shotsHit = 0;
                OnScoreReportUpdated?.Invoke(GetScoreReport());
            }

            // Apply full formula multipliers & bonuses at wave end or game over/victory
            if (currentState == WaveState.WaveCompleted)
            {
                int waveScore = CalculateCurrentWaveScore();
                cumulativeGameScore += waveScore;
                RecalculatePoints();
            }
            else if (currentState == WaveState.GameOver || currentState == WaveState.Victory)
            {
                RecalculatePoints();
            }

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
        totalMoneyGained += moneyAwarded;
        moneyGainedInWave += moneyAwarded;
        OnMoneyChanged?.Invoke(money);
        RecalculatePoints();
    }

    public bool TrySpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            totalMoneySpent += amount;
            moneySpentInIntermission += amount;
            OnMoneyChanged?.Invoke(money);
            RecalculatePoints();
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

    private bool TryChooseEnemyRuleForWave(int wave, out EnemySpawnRule selectedRule)
    {
        selectedRule = default;
        List<EnemySpawnRule> activeRules = new List<EnemySpawnRule>();
        float totalWeight = 0f;

        foreach (var rule in enemyRules)
        {
            if (wave >= rule.startWave && rule.spawnChanceWeight > 0f)
            {
                activeRules.Add(rule);
                totalWeight += rule.spawnChanceWeight;
            }
        }

        if (activeRules.Count == 0)
        {
            return false;
        }

        // Weighted random selection
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var rule in activeRules)
        {
            currentSum += rule.spawnChanceWeight;
            if (randomValue <= currentSum)
            {
                selectedRule = rule;
                return true;
            }
        }

        selectedRule = activeRules[0];
        return true;
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
            var player = FindFirstObjectByType<PlayerHealth>();
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
