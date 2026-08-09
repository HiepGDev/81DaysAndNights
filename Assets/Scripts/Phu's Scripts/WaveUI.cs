using UnityEngine;
using TMPro;
using System.Collections;

public class WaveUI : MonoBehaviour
{
    [Header("HUD Components")]
    [SerializeField] private TextMeshProUGUI waveNumberText;      // format is "WAVE %d"
    [SerializeField] private TextMeshProUGUI waveStatusText;      // format is "%s"; for PREPARING, it is "%s (%d)"
    [SerializeField] private TextMeshProUGUI enemiesRemainingText;// format is "%d/%d"
    [SerializeField] private TextMeshProUGUI moneyText;           // format is "%d"
    [SerializeField] private TextMeshProUGUI pointsText;          // format is "%d"

    [Header("Countdown UI Components & Animations")]
    [SerializeField] private RectTransform waveCountdownPanel;
    [SerializeField] private UnityEngine.UI.Button waveStartButton;
    [SerializeField] private TextMeshProUGUI countdownNumberText;
    [SerializeField] private float countdown_SlideDistance = 120f;
    [SerializeField] private float waveStart_SlideDistance = 120f;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private float pulseScaleAmount = 1.5f;
    [SerializeField] private float pulseDuration = 0.25f;

    [Header("WaveCleared Components")]
    [SerializeField] private RectTransform waveClearedPanel;
    [SerializeField] private float waveCleared_SlideDuration = 0.5f;
    [SerializeField] private float waveCleared_HoldDuration = 2.0f;
    [SerializeField] private float waveCleared_SlideDistance = 800f;
    [SerializeField] private float waveCleared_CreepDistance = 40f;

    [Header("Retrieval Mode")]
    [Tooltip("If true, the UI will actively poll state from the backend WaveManager in its Update loop using GetWaveStatus(). If false, it relies on event subscriptions.")]
    [SerializeField] private bool usePolling = false;

    [Header("Hint List References")]
    [SerializeField] private HintUI hintList;

    [Header("Objective Banner Components & Animation")]
    [Tooltip("GameObject containing the objective banner. Plays figure-8 sine wave motion on game start then fades out.")]
    [SerializeField] private GameObject objectiveBannerObj;
    [SerializeField] private float objectiveDisplayDuration = 5.0f;
    [SerializeField] private float objectiveFadeOutDuration = 1.0f;
    [SerializeField] private float figureEightWidth = 30.0f;
    [SerializeField] private float figureEightHeight = 15.0f;
    [SerializeField] private float figureEightSpeed = 2.0f;

    private Coroutine objectiveBannerCoroutine;

    [Header("Wave 2 Auto Shop Open Settings")]
    [Tooltip("If true, automatically opens the Shop after a delay during wave 2 intermission.")]
    [SerializeField] private bool autoOpenShopWave2 = true;
    [SerializeField] private float autoOpenShopDelay = 2.0f;

    private bool hasAutoOpenedShopForWave2 = false;
    private Coroutine autoOpenShopCoroutine;

    [Header("UI Sound Effects")]
    [Tooltip("Sound played when UI frames, banners, or panels open.")]
    [SerializeField] private AudioClip frameOpenSound;
    [Tooltip("Sound played when UI frames or banners close/fade out.")]
    [SerializeField] private AudioClip frameCloseSound;
    [Tooltip("Sound played when hovering over buttons in the HUD.")]
    [SerializeField] private AudioClip buttonHoverSound;
    [Tooltip("Sound played when clicking buttons in the HUD.")]
    [SerializeField] private AudioClip buttonClickSound;


    private Coroutine transitionCoroutine;
    private Vector2 countdown_SlideOutPosition;
    private Vector2 countdown_SlideInPosition;
    private Vector2 waveStart_SlideOutPosition;
    private Vector2 waveStart_SlideInPosition;
    private Coroutine punchScaleCoroutine;
    private int lastCountdownInt = -1;

    private Coroutine waveClearedCoroutine;

    private bool isPositionsInitialized = false;
    private bool isCountdownUIVisible = false;
    private bool hasInitializedVisibility = false;

    // Event-driven state caching & subscription state
    private bool isSubscribed = false;
    private bool isSpawningCompletedEvent = false;
    private int lastRemainingCount = 0;
    private int lastTotalCount = 0;

    private void Awake()
    {
        TryAssignNewUIElements();

        // If text fields are not assigned in the inspector, build a modern fallback overlay Canvas programmatically
        if (waveNumberText == null || waveStatusText == null || enemiesRemainingText == null || moneyText == null || pointsText == null)
        {
            CreateFallbackUI();
        }
    }

    private void Start()
    {
        TryAssignNewUIElements();
        if (waveStartButton != null)
        {
            waveStartButton.onClick.RemoveListener(onSkipClicked);
            waveStartButton.onClick.AddListener(onSkipClicked);
            if (UISoundManager.Instance != null)
            {
                UISoundManager.Instance.AttachButtonSounds(waveStartButton, buttonHoverSound, buttonClickSound);
            }
        }

        // Subscribe here if Instance wasn't ready during Awake/OnEnable
        TrySubscribeEvents();

        // Initial render
        UpdateUI();

        // Show objective banner animation upon start of game
        ShowObjectiveBanner();
    }

    private void OnEnable()
    {
        TrySubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        // Active retrieval: Pull fresh snapshot from backend every frame
        if (usePolling && WaveManager.Instance != null)
        {
            RetrieveStateFromBackendPoll();
        }

        // Press Tab to press the start wave button too
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            onSkipClicked();
        }
    }

    // --- Active Retrieval: Polling (Backend to Frontend) ---
    private void RetrieveStateFromBackendPoll()
    {
        TryAssignNewUIElements();
        // Pull a snapshot struct of current state
        WaveStatusReport report = WaveManager.Instance.GetWaveStatus();

        // --- New UI System Updates ---
        if (waveNumberText != null)
        {
            waveNumberText.text = $"WAVE {report.currentWave}";
        }

        if (waveStatusText != null)
        {
            if (report.state == WaveState.Preparing)
            {
                waveStatusText.text = "PREPARING";
            }
            else
            {
                waveStatusText.text = FormatStateString(report.state);
            }
        }

        if (enemiesRemainingText != null)
        {
            if (report.state == WaveState.Preparing || report.state == WaveState.Victory)
            {
                enemiesRemainingText.text = "0/0";
            }
            else
            {
                enemiesRemainingText.text = $"{report.remainingEnemies}/{report.spawnedEnemies}";
            }
        }

        if (moneyText != null)
        {
            moneyText.text = WaveManager.Instance.Money.ToString();
        }

        if (pointsText != null)
        {
            pointsText.text = WaveManager.Instance.Points.ToString("N0");
        }

        CheckAutoOpenShopWave2(report.state, report.currentWave);

        if (report.state == WaveState.Preparing)
        {
            HandleCountdownTick(report.countdownTime);
        }
        else
        {
            UpdateCountdownUIState(report.state);
        }
    }

    // --- Event Subscription Management ---
    private void TrySubscribeEvents()
    {
        // Only subscribe if not polling, not already subscribed, and backend singleton is ready
        if (usePolling || isSubscribed || WaveManager.Instance == null) return;

        WaveManager.Instance.OnWaveStateChanged += HandleWaveStateChanged;
        WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
        WaveManager.Instance.OnWaveCompleted += HandleWaveCompleted;
        WaveManager.Instance.OnEnemyCountChanged += HandleEnemyCountChanged;
        WaveManager.Instance.OnCountdownTick += HandleCountdownTick;
        WaveManager.Instance.OnSpawningStatusChanged += HandleSpawningStatusChanged;
        WaveManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        WaveManager.Instance.OnPointsChanged += HandlePointsChanged;

        isSubscribed = true;
        Debug.Log("[WaveUI] Successfully subscribed to WaveManager instance events.");
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed || WaveManager.Instance == null) return;

        WaveManager.Instance.OnWaveStateChanged -= HandleWaveStateChanged;
        WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
        WaveManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
        WaveManager.Instance.OnEnemyCountChanged -= HandleEnemyCountChanged;
        WaveManager.Instance.OnCountdownTick -= HandleCountdownTick;
        WaveManager.Instance.OnSpawningStatusChanged -= HandleSpawningStatusChanged;
        WaveManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        WaveManager.Instance.OnPointsChanged -= HandlePointsChanged;

        isSubscribed = false;
        Debug.Log("[WaveUI] Successfully unsubscribed from WaveManager instance events.");
    }

    // --- Passive Retrieval: Event Callbacks ---
    private void HandleWaveStateChanged(WaveState state)
    {
        // --- New UI System Updates ---
        if (waveStatusText != null)
        {
            if (state == WaveState.Preparing)
            {
                waveStatusText.text = "PREPARING";
            }
            else
            {
                waveStatusText.text = FormatStateString(state);
            }
        }

        if (state == WaveState.Preparing)
        {
            if (enemiesRemainingText != null) enemiesRemainingText.text = "0/0";
        }
        else if (state == WaveState.Victory)
        {
            ShowWaveCleared("VICTORY!", Color.yellow);
        }

        CheckAutoOpenShopWave2(state, WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : -1);

        UpdateCountdownUIState(state);
    }

    private void HandleWaveStarted(int waveNum)
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"WAVE {waveNum}";
        }

        UpdateCountdownUIState(WaveState.WaveActive);
    }

    private void HandleWaveCompleted(int waveNum)
    {
        ShowWaveCleared($"WAVE {waveNum} CLEAR!", Color.green);

        if (BonusUI.Instance != null && WaveManager.Instance != null)
        {
            ScoreReport report = WaveManager.Instance.LastCompletedWaveReport;

            BonusUI.Instance.AddAccuracyBonus(
                report.accuracyPercent,
                report.shotsFired,
                report.shotsHit,
                Mathf.RoundToInt(report.accuracyBonus)
            );

            BonusUI.Instance.AddEconomyBonus(
                report.economyBonus,
                report.moneyGainedInWave,
                report.moneySpentInIntermission,
                Mathf.RoundToInt(report.economyBonus)
            );
        }
    }

    private void HandleEnemyCountChanged(int remaining, int total)
    {
        lastRemainingCount = remaining;
        lastTotalCount = total;
        UpdateEnemyCountUI(remaining, total);
    }

    private void HandleSpawningStatusChanged(bool completed)
    {
        isSpawningCompletedEvent = completed;
        UpdateEnemyCountUI(lastRemainingCount, lastTotalCount);
    }

    private void UpdateEnemyCountUI(int remaining, int total)
    {

        if (enemiesRemainingText != null)
        {
            enemiesRemainingText.text = $"{remaining}/{total}";
        }
    }

    private void HandleCountdownTick(float secondsRemaining)
    {
        int currentSec = Mathf.CeilToInt(secondsRemaining);

        // Update wave status text (without the countdown number)
        if (waveStatusText != null && WaveManager.Instance != null && WaveManager.Instance.CurrentState == WaveState.Preparing)
        {
            waveStatusText.text = "PREPARING";
        }

        if (countdownNumberText != null)
        {
            if (secondsRemaining > 0f)
            {
                countdownNumberText.text = currentSec.ToString();
                
                // Trigger pulse effect if 10 seconds or less remain, and the second has changed
                if (currentSec <= 10 && currentSec != lastCountdownInt)
                {
                    lastCountdownInt = currentSec;
                    if (punchScaleCoroutine != null)
                    {
                        StopCoroutine(punchScaleCoroutine);
                    }
                    punchScaleCoroutine = StartCoroutine(PunchCountdownTextScale());
                }
            }
            else
            {
                countdownNumberText.text = "0";
            }
        }

        // Reset lastCountdownInt if timer goes above 10 or stops
        if (currentSec > 10)
        {
            lastCountdownInt = -1;
        }

        if (WaveManager.Instance != null)
        {
            UpdateCountdownUIState(WaveManager.Instance.CurrentState);
        }
    }

    private IEnumerator PunchCountdownTextScale()
    {
        if (countdownNumberText == null) yield break;

        RectTransform rect = countdownNumberText.GetComponent<RectTransform>();
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * pulseScaleAmount;

        float elapsed = 0f;
        // Half duration to scale up, half to scale down
        float halfDuration = pulseDuration / 2f;

        // Scale Up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rect.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        rect.localScale = targetScale;
        elapsed = 0f;

        // Scale Down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rect.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        rect.localScale = originalScale;
    }

    // --- Initial & Helper Methods ---
    private void UpdateUI()
    {
        TryAssignNewUIElements();
        if (WaveManager.Instance != null)
        {
            RetrieveStateFromBackendPoll();
        }
        else
        {

            if (waveNumberText != null) waveNumberText.text = "WAVE --";
            if (waveStatusText != null) waveStatusText.text = "WAITING FOR STATE...";
            if (enemiesRemainingText != null) enemiesRemainingText.text = "0/0";
            if (moneyText != null) moneyText.text = "0";
            if (pointsText != null) pointsText.text = "0";
            UpdateCountdownUIState(WaveState.WaveActive);
        }
    }

    private string FormatStateString(WaveState state)
    {
        switch (state)
        {
            case WaveState.Preparing: return "PREPARING";
            case WaveState.WaveActive: return "HOSTILES DETECTED";
            case WaveState.WaveCompleted: return "AREA SECURED";
            case WaveState.Victory: return "ALL WAVES CLEARED";
            case WaveState.GameOver: return "OPERATION FAILED";
            default: return "UNKNOWN";
        }
    }

    private void ShowWaveCleared(string message, Color color)
    {
        if (waveClearedPanel != null)
        {
            if (waveClearedCoroutine != null)
            {
                StopCoroutine(waveClearedCoroutine);
            }
            waveClearedCoroutine = StartCoroutine(WaveClearedDisplayRoutine());
        }
    }

    private IEnumerator WaveClearedDisplayRoutine()
    {
        waveClearedPanel.gameObject.SetActive(true);

        UIFrameSound frameSoundComp = waveClearedPanel.GetComponent<UIFrameSound>();
        if (frameSoundComp != null)
        {
            frameSoundComp.PlayOpen();
        }
        else if (frameOpenSound != null && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlaySound(frameOpenSound);
        }
        else if (UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayFrameOpen();
        }

        RectTransform rect = waveClearedPanel;
        float posY = rect.anchoredPosition.y;

        float startX = -waveCleared_SlideDistance;
        float centerStart = -waveCleared_CreepDistance / 2f;
        float centerEnd = waveCleared_CreepDistance / 2f;
        float endX = waveCleared_SlideDistance;

        // 1. Slide In (Left to Center Start): Ease out (fast to slow)
        float elapsed = 0f;
        while (elapsed < waveCleared_SlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / waveCleared_SlideDuration;
            float ease = 1f - (1f - t) * (1f - t); // Quadratic ease-out (fast to slow)
            rect.anchoredPosition = new Vector2(Mathf.Lerp(startX, centerStart, ease), posY);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(centerStart, posY);

        // 2. Slow Creep (Center Start to Center End): Linear creep over hold duration
        elapsed = 0f;
        while (elapsed < waveCleared_HoldDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / waveCleared_HoldDuration;
            rect.anchoredPosition = new Vector2(Mathf.Lerp(centerStart, centerEnd, t), posY);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(centerEnd, posY);

        // 3. Slide Out (Center End to Right): Ease in (slow to fast)
        elapsed = 0f;
        while (elapsed < waveCleared_SlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / waveCleared_SlideDuration;
            float ease = t * t; // Quadratic ease-in (slow to fast)
            rect.anchoredPosition = new Vector2(Mathf.Lerp(centerEnd, endX, ease), posY);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(endX, posY);

        waveClearedPanel.gameObject.SetActive(false);

        if (frameSoundComp != null)
        {
            frameSoundComp.PlayClose();
        }
        else if (frameCloseSound != null && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlaySound(frameCloseSound);
        }
        else if (UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayFrameClose();
        }
    }

    private void CreateFallbackUI()
    {
        Debug.Log("[WaveUI] Building fallback HUD Canvas procedurally...");

        GameObject canvasObj = new GameObject("WaveHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject panelObj = new GameObject("HUD_Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(250, 210);

        UnityEngine.UI.Image bgImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        if (waveClearedPanel == null)
        {
            GameObject bannerObj = new GameObject("WaveCleared");
            bannerObj.transform.SetParent(canvasObj.transform, false);
            
            waveClearedPanel = bannerObj.AddComponent<RectTransform>();
            waveClearedPanel.anchorMin = new Vector2(0.5f, 0.5f);
            waveClearedPanel.anchorMax = new Vector2(0.5f, 0.5f);
            waveClearedPanel.pivot = new Vector2(0.5f, 0.5f);
            waveClearedPanel.anchoredPosition = new Vector2(0, 120);
            waveClearedPanel.sizeDelta = new Vector2(600, 100);

            UnityEngine.UI.Image img = bannerObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(bannerObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI label = txtObj.AddComponent<TextMeshProUGUI>();
            label.text = "WAVE CLEAR";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 36;
            label.fontStyle = FontStyles.Bold;
            
            bannerObj.SetActive(false);
        }

        if (waveNumberText == null)
            waveNumberText = CreateTextElement("Wave_Num", panelObj.transform, "WAVE --", 20, new Vector2(15, -15), true);
        if (waveStatusText == null)
            waveStatusText = CreateTextElement("Wave_Status", panelObj.transform, "PREPARING (8)", 12, new Vector2(15, -45), false, new Color(0.7f, 0.7f, 1f));
        if (enemiesRemainingText == null)
            enemiesRemainingText = CreateTextElement("Enemies_Num", panelObj.transform, "0/0", 14, new Vector2(15, -75), false);
        if (moneyText == null)
            moneyText = CreateTextElement("Money_Num", panelObj.transform, "0", 14, new Vector2(15, -105), false, new Color(0.2f, 1f, 0.2f));
        if (pointsText == null)
            pointsText = CreateTextElement("Points_Num", panelObj.transform, "0", 14, new Vector2(15, -135), false, new Color(1f, 0.8f, 0.2f));

        if (waveCountdownPanel == null)
        {
            // Set up fallback panel if needed, but in redesign it's typically manually assigned
        }
    }

    private void TryAssignNewUIElements()
    {
        bool hadStartButton = waveStartButton != null;
        
        if (waveNumberText == null) waveNumberText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Wave_Num");
        if (waveStatusText == null) waveStatusText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Wave_Status");
        if (enemiesRemainingText == null) enemiesRemainingText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Enemies_Num");
        if (moneyText == null) moneyText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Money_Num");
        if (pointsText == null) pointsText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Points_Num");
        if (waveCountdownPanel == null) waveCountdownPanel = FindComponentByNameInHierarchy<RectTransform>("WaveCountdown");
        if (waveStartButton == null) waveStartButton = FindComponentByNameInHierarchy<UnityEngine.UI.Button>("WaveStart");
        if (countdownNumberText == null) countdownNumberText = FindComponentByNameInHierarchy<TextMeshProUGUI>("CountdownText");
        if (waveClearedPanel == null) waveClearedPanel = FindComponentByNameInHierarchy<RectTransform>("WaveCleared");
        if (hintList == null) hintList = FindComponentByNameInHierarchy<HintUI>("HintList");
        if (hintList == null) hintList = FindAnyObjectByType<HintUI>();
        if (objectiveBannerObj == null) objectiveBannerObj = FindGameObjectByNameInHierarchy("Objective");

        if (!hadStartButton && waveStartButton != null)
        {
            waveStartButton.onClick.RemoveListener(onSkipClicked);
            waveStartButton.onClick.AddListener(onSkipClicked);
        }
    }

    public void ShowObjectiveBanner()
    {
        if (objectiveBannerObj == null)
        {
            objectiveBannerObj = FindGameObjectByNameInHierarchy("Objective");
        }

        if (objectiveBannerObj == null) return;

        if (objectiveBannerCoroutine != null)
        {
            StopCoroutine(objectiveBannerCoroutine);
        }

        objectiveBannerCoroutine = StartCoroutine(AnimateObjectiveBannerRoutine());
    }

    private IEnumerator AnimateObjectiveBannerRoutine()
    {
        objectiveBannerObj.SetActive(true);

        RectTransform rect = objectiveBannerObj.GetComponent<RectTransform>();
        CanvasGroup cg = objectiveBannerObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = objectiveBannerObj.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        Vector2 basePos = rect != null ? rect.anchoredPosition : Vector2.zero;

        float timer = 0f;

        // Phase 1: Figure-8 sine wave motion during display duration
        while (timer < objectiveDisplayDuration)
        {
            timer += Time.deltaTime;
            float t = timer * figureEightSpeed;

            if (rect != null)
            {
                float offsetX = Mathf.Sin(t) * figureEightWidth;
                float offsetY = Mathf.Sin(t * 2f) * figureEightHeight;
                rect.anchoredPosition = basePos + new Vector2(offsetX, offsetY);
            }

            yield return null;
        }

        // Phase 2: Continue figure-8 sine wave motion while fading out
        float fadeTimer = 0f;
        while (fadeTimer < objectiveFadeOutDuration)
        {
            fadeTimer += Time.deltaTime;
            timer += Time.deltaTime;
            float t = timer * figureEightSpeed;

            cg.alpha = 1f - Mathf.Clamp01(fadeTimer / objectiveFadeOutDuration);

            if (rect != null)
            {
                float offsetX = Mathf.Sin(t) * figureEightWidth;
                float offsetY = Mathf.Sin(t * 2f) * figureEightHeight;
                rect.anchoredPosition = basePos + new Vector2(offsetX, offsetY);
            }

            yield return null;
        }

        cg.alpha = 0f;
        if (rect != null) rect.anchoredPosition = basePos;
        objectiveBannerObj.SetActive(false);
    }

    private GameObject FindGameObjectByNameInHierarchy(string name)
    {
        Transform t = FindInChildRecursive<Transform>(transform.root, name);
        return t != null ? t.gameObject : null;
    }

    private T FindComponentByNameInHierarchy<T>(string name) where T : Component
    {
        T comp = FindInChildRecursive<T>(transform.root, name);
        return comp;
    }

    private T FindInChildRecursive<T>(Transform parent, string name) where T : Component
    {
        if (parent.name == name)
        {
            T comp = parent.GetComponent<T>();
            if (comp != null) return comp;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            T found = FindInChildRecursive<T>(parent.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    private void onSkipClicked()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.SkipCountdownEarly();
        }

        // Start the hide transition immediately for visual responsiveness
        if (waveCountdownPanel != null || waveStartButton != null)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionUISequence(false));
        }
    }

    private void HandleMoneyChanged(int value)
    {
        if (moneyText != null)
        {
            moneyText.text = value.ToString();
        }
    }

    private void HandlePointsChanged(int value)
    {
        if (pointsText != null)
        {
            pointsText.text = value.ToString("N0");
        }
    }

    private void InitializePositions()
    {
        if (isPositionsInitialized) return;
        TryAssignNewUIElements();

        // 1. Countdown Panel
        if (waveCountdownPanel != null)
        {
            countdown_SlideOutPosition = waveCountdownPanel.anchoredPosition;
            // Slide UP to hide: Y + distance
            countdown_SlideInPosition = countdown_SlideOutPosition + new Vector2(0f, countdown_SlideDistance);
        }

        // 2. Start Button
        if (waveStartButton != null)
        {
            RectTransform rect = waveStartButton.GetComponent<RectTransform>();
            waveStart_SlideOutPosition = rect.anchoredPosition;
            // Slide LEFT to hide: X - distance
            waveStart_SlideInPosition = waveStart_SlideOutPosition - new Vector2(waveStart_SlideDistance, 0f);
        }

        isPositionsInitialized = true;
    }

    private IEnumerator TransitionUISequence(bool show)
    {
        InitializePositions();
        
        RectTransform waveStartRect = waveStartButton != null ? waveStartButton.GetComponent<RectTransform>() : null;
        RectTransform countdownRect = waveCountdownPanel;

        if (show)
        {
            // --- SHOW SEQUENCE: Slide down and right simultaneously ---
            // Set initial positions to hidden just before sliding in
            if (countdownRect != null) countdownRect.anchoredPosition = countdown_SlideInPosition;
            if (waveStartRect != null) waveStartRect.anchoredPosition = waveStart_SlideInPosition;

            if (waveStartButton != null)
            {
                waveStartButton.gameObject.SetActive(true);
                waveStartButton.interactable = true;
            }
            if (waveCountdownPanel != null) waveCountdownPanel.gameObject.SetActive(true);

            float elapsed = 0f;
            Vector2 startCountdownPos = countdownRect != null ? countdownRect.anchoredPosition : Vector2.zero;
            Vector2 startButtonPos = waveStartRect != null ? waveStartRect.anchoredPosition : Vector2.zero;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideDuration;
                t = t * t * (3f - 2f * t); // Smooth step

                if (countdownRect != null)
                    countdownRect.anchoredPosition = Vector2.Lerp(startCountdownPos, countdown_SlideOutPosition, t);
                if (waveStartRect != null)
                    waveStartRect.anchoredPosition = Vector2.Lerp(startButtonPos, waveStart_SlideOutPosition, t);

                yield return null;
            }

            if (countdownRect != null) countdownRect.anchoredPosition = countdown_SlideOutPosition;
            if (waveStartRect != null) waveStartRect.anchoredPosition = waveStart_SlideOutPosition;
        }
        else
        {
            // --- HIDE SEQUENCE: Slide button left, then countdown up (sequential) ---
            // if (waveStartButton != null) 
            //     waveStartButton.interactable = false;

            // 1. Slide Start Button Left
            if (waveStartRect != null)
            {
                Vector2 startButtonPos = waveStartRect.anchoredPosition;
                float elapsed = 0f;
                while (elapsed < slideDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / slideDuration;
                    t = t * t * (3f - 2f * t);

                    waveStartRect.anchoredPosition = Vector2.Lerp(startButtonPos, waveStart_SlideInPosition, t);
                    yield return null;
                }
                waveStartRect.anchoredPosition = waveStart_SlideInPosition;
                waveStartButton.gameObject.SetActive(false);
            }

            // 2. Slide Countdown Panel Up
            if (countdownRect != null)
            {
                Vector2 startCountdownPos = countdownRect.anchoredPosition;
                float elapsed = 0f;
                while (elapsed < slideDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / slideDuration;
                    t = t * t * (3f - 2f * t);

                    countdownRect.anchoredPosition = Vector2.Lerp(startCountdownPos, countdown_SlideInPosition, t);
                    yield return null;
                }
                countdownRect.anchoredPosition = countdown_SlideInPosition;
                waveCountdownPanel.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateCountdownUIState(WaveState state)
    {
        bool show = (state == WaveState.Preparing) && (WaveManager.Instance != null);

        // Only trigger the transition if target visibility state changes, or on initial setup
        if (!hasInitializedVisibility || show != isCountdownUIVisible)
        {
            hasInitializedVisibility = true;
            isCountdownUIVisible = show;

            InitializePositions();
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionUISequence(show));
        }
    }



    private void CheckAutoOpenShopWave2(WaveState state, int waveNum)
    {
        if (!autoOpenShopWave2 || hasAutoOpenedShopForWave2) return;

        if (state == WaveState.Preparing && waveNum == 2)
        {
            hasAutoOpenedShopForWave2 = true;
            if (autoOpenShopCoroutine != null)
            {
                StopCoroutine(autoOpenShopCoroutine);
            }
            autoOpenShopCoroutine = StartCoroutine(AutoOpenShopRoutine());
        }
    }

    private IEnumerator AutoOpenShopRoutine()
    {
        yield return new WaitForSeconds(autoOpenShopDelay);

        if (ShopUI.Instance != null)
        {
            ShopUI.Instance.OpenShop();
        }
    }

    private TextMeshProUGUI CreateTextElement(string name, Transform parent, string initialText, float fontSize, Vector2 anchoredPos, bool bold, Color? textColor = null)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(220, 25);

        TextMeshProUGUI tmPro = textObj.AddComponent<TextMeshProUGUI>();
        tmPro.text = initialText;
        tmPro.fontSize = fontSize;
        tmPro.color = textColor ?? Color.white;
        tmPro.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return tmPro;
    }
}
