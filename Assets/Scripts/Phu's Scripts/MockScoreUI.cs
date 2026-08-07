using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MockScoreUI : MonoBehaviour
{
    public static MockScoreUI Instance { get; private set; }

    [Header("Mock Canvas Controls")]
    [SerializeField] private bool createOverlayCanvas = true;
    [SerializeField] private Canvas customCanvas;

    private TextMeshProUGUI breakdownText;
    private Transform popupContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (createOverlayCanvas)
        {
            BuildMockUIOverlay();
        }
    }

    private void OnEnable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnKillScoreRecorded += HandleKillScoreRecorded;
            WaveManager.Instance.OnScoreReportUpdated += HandleScoreReportUpdated;
            WaveManager.Instance.OnWaveStateChanged += HandleWaveStateChanged;
        }
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnKillScoreRecorded -= HandleKillScoreRecorded;
            WaveManager.Instance.OnScoreReportUpdated -= HandleScoreReportUpdated;
            WaveManager.Instance.OnWaveStateChanged -= HandleWaveStateChanged;
        }
    }

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            HandleScoreReportUpdated(WaveManager.Instance.GetScoreReport());
        }
    }

    private void BuildMockUIOverlay()
    {
        GameObject canvasObj = new GameObject("MockScoreCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Top-Center Kill Popup Container
        GameObject popupObj = new GameObject("KillPopupContainer");
        popupObj.transform.SetParent(canvasObj.transform, false);
        popupContainer = popupObj.transform;
        RectTransform popupRect = popupObj.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.7f);
        popupRect.anchorMax = new Vector2(0.5f, 0.7f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;

        // Bottom-Right Score Breakdown Panel
        GameObject panelObj = new GameObject("MockBreakdownPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.65f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20);
        panelRect.sizeDelta = new Vector2(340, 220);

        GameObject bdTextObj = new GameObject("BreakdownText");
        bdTextObj.transform.SetParent(panelObj.transform, false);
        breakdownText = bdTextObj.AddComponent<TextMeshProUGUI>();
        breakdownText.fontSize = 14;
        breakdownText.color = Color.white;
        RectTransform bdRect = bdTextObj.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = new Vector2(12, 12);
        bdRect.offsetMax = new Vector2(-12, -12);
    }

    private void HandleKillScoreRecorded(float killScore, string enemyName)
    {
        if (popupContainer != null)
        {
            StartCoroutine(SpawnKillPopupRoutine(killScore, enemyName));
        }
    }

    private IEnumerator SpawnKillPopupRoutine(float score, string enemyName)
    {
        GameObject popObj = new GameObject("PopText");
        popObj.transform.SetParent(popupContainer, false);
        TextMeshProUGUI txt = popObj.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 26;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.cyan;
        txt.text = $"+{Mathf.RoundToInt(score)} PTS ({enemyName})";

        RectTransform r = popObj.GetComponent<RectTransform>();
        Vector2 startPos = new Vector2(Random.Range(-50f, 50f), Random.Range(-20f, 20f));
        r.anchoredPosition = startPos;

        float duration = 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float norm = elapsed / duration;
            r.anchoredPosition = startPos + Vector2.up * (norm * 50f);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, 1f - norm);
            yield return null;
        }

        Destroy(popObj);
    }

    private void HandleScoreReportUpdated(ScoreReport report)
    {

        if (breakdownText != null)
        {
            breakdownText.text =
                $"<b>--- SCORE BREAKDOWN ---</b>\n" +
                $"Wave Kills: <b>{report.waveKills}</b> (Base Pts: {report.baseKillScore:F0})\n" +
                $"Economy Bonus: <b>+{report.economyBonus:F0}</b> (Earned: ${report.moneyGainedInWave:F0} | Spent: ${report.moneySpentInIntermission:F0})\n" +
                $"Wave Multiplier: <b>{report.waveMultiplier:F2}x</b>\n" +
                $"Accuracy: <b>{(report.accuracyPercent * 100f):F1}%</b> (Eff: {report.efficiencyMultiplier:F2}x)\n" +
                $"Shots: {report.shotsHit}/{report.shotsFired}\n" +
                $"WAVE SCORE: <b>+{report.waveScore:N0}</b>\n" +
                $"-------------------------\n" +
                $"<b>GAME TOTAL SCORE: {report.totalGameScore:N0}</b>";
        }
    }

    private void HandleWaveStateChanged(WaveState state)
    {
        if (WaveManager.Instance != null)
        {
            HandleScoreReportUpdated(WaveManager.Instance.GetScoreReport());
        }
    }
}
