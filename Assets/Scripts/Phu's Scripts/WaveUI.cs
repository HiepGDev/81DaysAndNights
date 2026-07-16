using UnityEngine;
using TMPro;
using System.Collections;

namespace PhuScene
{
    public class WaveUI : MonoBehaviour
    {
        [Header("UI Text Components")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI difficultyText;

        [Header("Banner Settings")]
        [SerializeField] private TextMeshProUGUI bannerText; // Fullscreen warning/welcome text
        [SerializeField] private float bannerDuration = 2.0f;

        [Header("Retrieval Mode")]
        [Tooltip("If true, the UI will actively poll state from the backend WaveManager in its Update loop using GetWaveStatus(). If false, it relies on event subscriptions.")]
        [SerializeField] private bool usePolling = false;

        // Event-driven state caching & subscription state
        private bool isSubscribed = false;
        private bool isSpawningCompletedEvent = false;
        private int lastRemainingCount = 0;
        private int lastTotalCount = 0;

        private void Awake()
        {
            // If text fields are not assigned in the inspector, build a modern fallback overlay Canvas programmatically
            if (waveText == null || stateText == null || enemyCountText == null || countdownText == null || difficultyText == null)
            {
                CreateFallbackUI();
            }
        }

        private void Start()
        {
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
            // Pull a snapshot struct of current state
            WaveStatusReport report = WaveManager.Instance.GetWaveStatus();

            // Apply to UI fields
            if (waveText != null)
            {
                waveText.text = $"WAVE {report.currentWave}";
            }

            if (stateText != null)
            {
                stateText.text = FormatStateString(report.state);
            }

            if (enemyCountText != null)
            {
                if (report.state == WaveState.Preparing || report.state == WaveState.Victory)
                {
                    enemyCountText.text = "ENEMIES: -- / --";
                }
                else
                {
                    string spawnLabel = report.isSpawningCompleted ? "" : " (Spawning...)";
                    enemyCountText.text = $"ENEMIES: {report.remainingEnemies} / {report.totalEnemies}{spawnLabel}";
                }
            }

            if (countdownText != null)
            {
                if (report.state == WaveState.Preparing)
                {
                    countdownText.text = $"NEXT WAVE IN: {Mathf.CeilToInt(report.countdownTime)}s";
                }
                else
                {
                    countdownText.text = "";
                }
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"DIFFICULTY: {report.difficultyMultiplier:F2}x";
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

            isSubscribed = false;
            Debug.Log("[WaveUI] Successfully unsubscribed from WaveManager instance events.");
        }

        // --- Passive Retrieval: Event Callbacks ---
        private void HandleWaveStateChanged(WaveState state)
        {
            if (stateText != null)
            {
                stateText.text = FormatStateString(state);
            }

            if (state == WaveState.Preparing)
            {
                if (enemyCountText != null) enemyCountText.text = "ENEMIES: -- / --";
            }
            else if (state == WaveState.Victory)
            {
                ShowBanner("VICTORY!", Color.yellow);
                if (countdownText != null) countdownText.text = "";
                if (enemyCountText != null) enemyCountText.text = "ALL THREATS ELIMINATED";
            }
            else if (state == WaveState.GameOver)
            {
                ShowBanner("GAME OVER", Color.red);
                if (countdownText != null) countdownText.text = "";
            }
        }

        private void HandleWaveStarted(int waveNum)
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {waveNum}";
            }
            if (difficultyText != null && WaveManager.Instance != null)
            {
                // Active query inside event handler
                difficultyText.text = $"DIFFICULTY: {WaveManager.Instance.DifficultyMultiplier:F2}x";
            }
            if (countdownText != null)
            {
                countdownText.text = "";
            }
            ShowBanner($"WAVE {waveNum} START!", Color.white);
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
            if (enemyCountText != null)
            {
                string spawnLabel = isSpawningCompletedEvent ? "" : " (Spawning...)";
                enemyCountText.text = $"ENEMIES: {remaining} / {total}{spawnLabel}";
            }
        }

        private void HandleCountdownTick(float secondsRemaining)
        {
            if (countdownText != null)
            {
                if (secondsRemaining > 0f)
                {
                    countdownText.text = $"NEXT WAVE IN: {Mathf.CeilToInt(secondsRemaining)}s";
                }
                else
                {
                    countdownText.text = "";
                }
            }
        }

        // --- Initial & Helper Methods ---
        private void UpdateUI()
        {
            if (WaveManager.Instance != null)
            {
                RetrieveStateFromBackendPoll();
            }
            else
            {
                if (waveText != null) waveText.text = "WAVE --";
                if (stateText != null) stateText.text = "WAITING FOR STATE...";
                if (enemyCountText != null) enemyCountText.text = "ENEMIES: -- / --";
                if (countdownText != null) countdownText.text = "";
                if (difficultyText != null) difficultyText.text = "DIFFICULTY: 1.00x";
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

            yield return new WaitForSeconds(bannerDuration);

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
            panelRect.sizeDelta = new Vector2(250, 180);

            UnityEngine.UI.Image bgImage = panelObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);

            if (waveText == null)
                waveText = CreateTextElement("WaveText", panelObj.transform, "WAVE 1", 22, new Vector2(15, -15), true);
            if (stateText == null)
                stateText = CreateTextElement("StateText", panelObj.transform, "PREPARING", 12, new Vector2(15, -45), false, new Color(0.7f, 0.7f, 1f));
            if (enemyCountText == null)
                enemyCountText = CreateTextElement("EnemyCountText", panelObj.transform, "ENEMIES: -- / --", 14, new Vector2(15, -75), false);
            if (countdownText == null)
                countdownText = CreateTextElement("CountdownText", panelObj.transform, "NEXT WAVE IN: --s", 14, new Vector2(15, -105), false, new Color(1f, 0.8f, 0.2f));
            if (difficultyText == null)
                difficultyText = CreateTextElement("DifficultyText", panelObj.transform, "DIFFICULTY: 1.0x", 12, new Vector2(15, -135), false, new Color(0.7f, 0.7f, 0.7f));

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
