using UnityEngine;
using TMPro;
using System.Collections;

namespace PhuScene
{
    public class WaveUI : MonoBehaviour
    {
        [Header("HUD Components")]
        [SerializeField] private TextMeshProUGUI waveNumberText;      // format is "WAVE %d"
        [SerializeField] private TextMeshProUGUI waveStatusText;      // format is "%s"; for PREPARING, it is "%s (%d)"
        [SerializeField] private TextMeshProUGUI enemiesRemainingText;// format is "%d/%d"
        [SerializeField] private TextMeshProUGUI moneyText;           // format is "%d"
        [SerializeField] private TextMeshProUGUI pointsText;          // format is "%d"

        [Header("Skip Button Components")]
        [SerializeField] private UnityEngine.UI.Button skipButton; // button for starting the round early
        [SerializeField] private float skip_SlideDuration = 0.3f;
        [SerializeField] private float skip_SlideDistance = 120f;

        [Header("Banner Components")]
        [SerializeField] private TextMeshProUGUI bannerText; // Fullscreen warning/welcome text
        [SerializeField] private float banner_SlideDuration = 2.0f;

        [Header("Retrieval Mode")]
        [Tooltip("If true, the UI will actively poll state from the backend WaveManager in its Update loop using GetWaveStatus(). If false, it relies on event subscriptions.")]
        [SerializeField] private bool usePolling = false;


        private Coroutine skip_SlideCoroutine;
        private Vector2 skip_SlideOutPosition;
        private Vector2 skip_SlideInPosition;
        

        private Coroutine banner_SlideCoroutine;
        private Vector2 banner_SlideOutPosition;
        private Vector2 banner_SlideInPosition;

        private bool isButtonPositionsInitialized = false;

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
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(onSkipClicked);
                skipButton.onClick.AddListener(onSkipClicked);
            }

            // Subscribe here if Instance wasn't ready during Awake/OnEnable
            TrySubscribeEvents();

            // Initial render
            UpdateUI();
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
                    waveStatusText.text = $"PREPARING ({Mathf.CeilToInt(report.countdownTime)})";
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
                pointsText.text = WaveManager.Instance.Points.ToString();
            }

            UpdateSkipButtonState(report.state);
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
                if (state == WaveState.Preparing && WaveManager.Instance != null)
                {
                    waveStatusText.text = $"PREPARING ({Mathf.CeilToInt(WaveManager.Instance.NextWaveCountdown)})";
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
                ShowBanner("VICTORY!", Color.yellow);
            }
            else if (state == WaveState.GameOver)
            {
                ShowBanner("GAME OVER", Color.red);
            }

            UpdateSkipButtonState(state);
        }

        private void HandleWaveStarted(int waveNum)
        {
            ShowBanner($"WAVE {waveNum} START!", Color.white);

            if (waveNumberText != null)
            {
                waveNumberText.text = $"WAVE {waveNum}";
            }

            UpdateSkipButtonState(WaveState.WaveActive);
        }

        private void HandleWaveCompleted(int waveNum)
        {
            ShowBanner($"WAVE {waveNum} CLEAR!", Color.green);
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
            if (waveStatusText != null && WaveManager.Instance != null && WaveManager.Instance.CurrentState == WaveState.Preparing)
            {
                if (secondsRemaining > 0f)
                {
                    waveStatusText.text = $"PREPARING ({Mathf.CeilToInt(secondsRemaining)})";
                }
                else
                {
                    waveStatusText.text = "PREPARING (0)";
                }
            }

            if (WaveManager.Instance != null)
            {
                UpdateSkipButtonState(WaveManager.Instance.CurrentState);
            }
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
                UpdateSkipButtonState(WaveState.WaveActive);
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

        private void ShowBanner(string message, Color color)
        {
            if (bannerText != null)
            {
                StopAllCoroutines();
                StartCoroutine(BannerDisplayRoutine(message, color));
            }
        }

        private IEnumerator BannerDisplayRoutine(string message, Color color)
        {
            bannerText.text = message;
            bannerText.color = color;
            bannerText.gameObject.SetActive(true);

            bannerText.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            float elapsed = 0f;
            float scaleDuration = 0.2f;
            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / scaleDuration;
                bannerText.transform.localScale = Vector3.Lerp(new Vector3(0.5f, 0.5f, 0.5f), Vector3.one, progress);
                yield return null;
            }
            bannerText.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(banner_SlideDuration);

            elapsed = 0f;
            float fadeDuration = 0.5f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeDuration;
                bannerText.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, progress));
                yield return null;
            }

            bannerText.gameObject.SetActive(false);
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
 
            if (skipButton == null)
            {
                GameObject btnObj = new GameObject("WaveStart");
                btnObj.transform.SetParent(panelObj.transform, false);
                
                RectTransform btnRect = btnObj.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0, 1);
                btnRect.anchorMax = new Vector2(0, 1);
                btnRect.pivot = new Vector2(0, 1);
                btnRect.anchoredPosition = new Vector2(15, -230);
                btnRect.sizeDelta = new Vector2(220, 30);
 
                UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
                btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
 
                skipButton = btnObj.AddComponent<UnityEngine.UI.Button>();
                skipButton.onClick.AddListener(onSkipClicked);
                
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                RectTransform lblRect = labelObj.AddComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.sizeDelta = Vector2.zero;
 
                TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.text = "START EARLY";
                labelText.fontSize = 12;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = Color.white;
                labelText.fontStyle = FontStyles.Bold;
            }
 
            if (bannerText == null)
            {
                GameObject bannerObj = new GameObject("BannerText");
                bannerObj.transform.SetParent(canvasObj.transform, false);
                
                RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
                bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
                bannerRect.pivot = new Vector2(0.5f, 0.5f);
                bannerRect.anchoredPosition = new Vector2(0, 120);
                bannerRect.sizeDelta = new Vector2(600, 100);
 
                bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
                bannerText.alignment = TextAlignmentOptions.Center;
                bannerText.fontSize = 42;
                bannerText.fontStyle = FontStyles.Bold;
                bannerText.text = "";
                bannerText.outlineWidth = 0.25f;
                bannerText.outlineColor = Color.black;
            }
        }

        private void TryAssignNewUIElements()
        {
            bool hadButton = skipButton != null;
            
            if (waveNumberText == null) waveNumberText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Wave_Num");
            if (waveStatusText == null) waveStatusText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Wave_Status");
            if (enemiesRemainingText == null) enemiesRemainingText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Enemies_Num");
            if (moneyText == null) moneyText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Money_Num");
            if (pointsText == null) pointsText = FindComponentByNameInHierarchy<TextMeshProUGUI>("Points_Num");
            if (skipButton == null) skipButton = FindComponentByNameInHierarchy<UnityEngine.UI.Button>("WaveStart");

            if (!hadButton && skipButton != null)
            {
                skipButton.onClick.RemoveListener(onSkipClicked);
                skipButton.onClick.AddListener(onSkipClicked);
            }
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
                pointsText.text = value.ToString();
            }
        }

        private void InitializeButtonPositions()
        {
            if (isButtonPositionsInitialized) return;
            TryAssignNewUIElements();
            if (skipButton != null)
            {
                RectTransform rect = skipButton.GetComponent<RectTransform>();
                skip_SlideOutPosition = rect.anchoredPosition;
                skip_SlideInPosition = skip_SlideOutPosition + new Vector2(0, skip_SlideDistance);
                isButtonPositionsInitialized = true;
                Debug.Log($"[WaveUI] Initialized button positions: Out = {skip_SlideOutPosition}, In = {skip_SlideInPosition}");
            }
        }

        private IEnumerator SkipSlideCoroutine(bool show)
        {
            RectTransform rect = skipButton.GetComponent<RectTransform>();
            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = show ? skip_SlideOutPosition : skip_SlideInPosition;
            
            skipButton.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < skip_SlideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / skip_SlideDuration;
                t = t * t * (3f - 2f * t); // Smooth ease-in-ease-out curve
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            rect.anchoredPosition = targetPos;
            skipButton.interactable = show;
        }

        private void UpdateSkipButtonState(WaveState state)
        {
            bool show = (state == WaveState.Preparing) && (WaveManager.Instance != null); //&& WaveManager.Instance.isSpawned

            InitializeButtonPositions();
            if (skipButton == null || !isButtonPositionsInitialized) return;

            if (skip_SlideCoroutine != null)
            {
                StopCoroutine(skip_SlideCoroutine);
            }
            skip_SlideCoroutine = StartCoroutine(SkipSlideCoroutine(show));
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
}
