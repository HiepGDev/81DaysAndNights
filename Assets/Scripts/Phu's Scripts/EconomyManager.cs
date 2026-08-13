using UnityEngine;
using System;
using PhuScene;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Economy System Parameters")]
    [Tooltip("Multiplier applied to net intermission savings: NetWaveSavings * multiplier * EconomyDecayFactor.")]
    [SerializeField] private float economyBonusMultiplier = 0.5f;

    [Tooltip("Total lifetime money earned threshold where Economy Bonus efficiency diminishes by 50%.")]
    [SerializeField] private float economyDecayThreshold = 5000f;

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

    [Header("Economy System State")]
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

    private ScoreReport lastCompletedWaveReport;

    public event Action<int> OnMoneyChanged;
    public event Action<int> OnPointsChanged;
    public event Action<float, string> OnKillScoreRecorded;
    public event Action<ScoreReport> OnScoreReportUpdated;

    public int Money => money;
    public int Points => points;
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
    public ScoreReport LastCompletedWaveReport => lastCompletedWaveReport;

    public float AccuracyPercent => shotsFired > 0 ? (float)shotsHit / (float)shotsFired : 0f;
    public float EfficiencyMultiplier => 1f + (AccuracyPercent * accuracyEfficiencyWeight);
    public float NetWaveSavings => Mathf.Max(0f, moneyGainedInWave - moneySpentInIntermission);
    public float EconomyDecayFactor => 1f / (1f + ((float)totalMoneyGained / Mathf.Max(1f, economyDecayThreshold)));
    public float EconomyBonus => NetWaveSavings * economyBonusMultiplier * EconomyDecayFactor;

    public float GetWaveMultiplier(int currentWave)
    {
        return 1f + (Mathf.Pow(Mathf.Max(0, currentWave), 2f) / Mathf.Max(1f, waveMultiplierDivisor));
    }

    public float GetAccuracyBonus(int currentWave)
    {
        return (waveKillScore + EconomyBonus) * GetWaveMultiplier(currentWave) * (EfficiencyMultiplier - 1f);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStateChanged += HandleWaveStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStateChanged -= HandleWaveStateChanged;
        }
    }

    private void HandleWaveStateChanged(WaveState state)
    {
        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;

        if (state == WaveState.Preparing)
        {
            moneyGainedInWave = 0;
            moneySpentInIntermission = 0;
            waveKillScore = 0f;
            waveKills = 0;
        }
        else if (state == WaveState.WaveActive)
        {
            shotsFired = 0;
            shotsHit = 0;
            OnScoreReportUpdated?.Invoke(GetScoreReport(currentWave, state));
        }
        else if (state == WaveState.WaveCompleted)
        {
            lastCompletedWaveReport = GetScoreReport(currentWave, state);
            int waveScore = CalculateCurrentWaveScore(currentWave);
            cumulativeGameScore += waveScore;
            waveKillScore = 0f;
            RecalculatePoints(currentWave, state);
        }
        else if (state == WaveState.GameOver || state == WaveState.Victory)
        {
            RecalculatePoints(currentWave, state);
        }
    }

    public int CalculateCurrentWaveScore(int currentWave)
    {
        float subtotal = waveKillScore + EconomyBonus;
        float finalScore = subtotal * GetWaveMultiplier(currentWave) * EfficiencyMultiplier;
        return Mathf.RoundToInt(finalScore);
    }

    public int CalculateTotalGameScore(int currentWave, WaveState currentState)
    {
        if (currentState == WaveState.WaveCompleted)
        {
            return cumulativeGameScore;
        }
        return cumulativeGameScore + Mathf.RoundToInt(waveKillScore);
    }

    public ScoreReport GetScoreReport(int currentWave, WaveState currentState)
    {
        return new ScoreReport
        {
            baseKillScore = waveKillScore,
            totalKillScore = totalKillScore,
            totalKills = totalKills,
            waveKills = waveKills,
            accuracyBonus = GetAccuracyBonus(currentWave),
            economyBonus = EconomyBonus,
            economyDecayFactor = EconomyDecayFactor,
            moneyGainedInWave = moneyGainedInWave,
            moneySpentInIntermission = moneySpentInIntermission,
            totalMoneyGained = totalMoneyGained,
            unspentMoney = NetWaveSavings,
            waveMultiplier = GetWaveMultiplier(currentWave),
            accuracyPercent = AccuracyPercent,
            efficiencyMultiplier = EfficiencyMultiplier,
            waveScore = CalculateCurrentWaveScore(currentWave),
            totalGameScore = CalculateTotalGameScore(currentWave, currentState),
            shotsFired = shotsFired,
            shotsHit = shotsHit
        };
    }

    public void AwardDeathRewards(GameObject enemy, int currentWave, WaveState currentState)
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

        float waveScale = 1f + (Mathf.Max(1, currentWave) * killScoreWaveScaleFactor);
        float killScore = basePoints * enemyTypeMultiplier * waveScale;

        waveKills++;
        totalKills++;
        waveKillScore += killScore;
        totalKillScore += killScore;
        totalMoneyGained += moneyReward;
        moneyGainedInWave += moneyReward;
        money += moneyReward;

        points = CalculateTotalGameScore(currentWave, currentState);

        OnMoneyChanged?.Invoke(money);
        OnPointsChanged?.Invoke(points);
        OnKillScoreRecorded?.Invoke(killScore, enemyName);
        OnScoreReportUpdated?.Invoke(GetScoreReport(currentWave, currentState));
    }

    public void RecordShotFired()
    {
        shotsFired++;
    }

    public void RecordShotHit()
    {
        shotsHit++;
    }

    public void RecalculatePoints(int currentWave, WaveState currentState)
    {
        points = CalculateTotalGameScore(currentWave, currentState);
        OnPointsChanged?.Invoke(points);
        OnScoreReportUpdated?.Invoke(GetScoreReport(currentWave, currentState));
    }

    public void AddMoneyAndPoints(int moneyAwarded, int pointsAwarded, int currentWave, WaveState currentState)
    {
        money += moneyAwarded;
        totalMoneyGained += moneyAwarded;
        moneyGainedInWave += moneyAwarded;
        OnMoneyChanged?.Invoke(money);
        RecalculatePoints(currentWave, currentState);
    }

    public bool TrySpendMoney(int amount, int currentWave, WaveState currentState)
    {
        if (money >= amount)
        {
            money -= amount;
            totalMoneySpent += amount;
            moneySpentInIntermission += amount;
            OnMoneyChanged?.Invoke(money);
            RecalculatePoints(currentWave, currentState);
            return true;
        }
        return false;
    }
}
