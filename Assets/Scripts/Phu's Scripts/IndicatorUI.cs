using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhuScene;

[System.Serializable]
public struct EnemyIndicatorConfig
{
    public EnemyType enemyType;
    public Color indicatorColor;
    public AudioClip spawnSound;
    public Sprite customSprite;
}

public class IndicatorUI : MonoBehaviour
{
    public static IndicatorUI Instance { get; private set; }

    [Header("Prefab & Display Settings")]
    [Tooltip("World space canvas prefab for the spawn indicator. If unassigned, creates a procedural world-space billboard.")]
    [SerializeField] private GameObject indicatorPrefab;

    [Tooltip("Minimum distance required between spawn points to trigger a new indicator in the same wave.")]
    [SerializeField] private float minDistanceBetweenIndicators = 100f;

    [Tooltip("Height offset above spawn location for the indicator.")]
    [SerializeField] private float heightOffset = 40.0f;

    [Header("Per-Enemy Type Indicator Configurations")]
    [Tooltip("Configurable list of colors, sounds, and sprites per EnemyType.")]
    [SerializeField] private List<EnemyIndicatorConfig> enemyIndicatorConfigs = new List<EnemyIndicatorConfig>
    {
        new EnemyIndicatorConfig { enemyType = EnemyType.Basic, indicatorColor = new Color(0.95f, 0.5f, 0.1f, 0.85f) },
        new EnemyIndicatorConfig { enemyType = EnemyType.Elite, indicatorColor = new Color(0.9f, 0.15f, 0.15f, 0.85f) },
        new EnemyIndicatorConfig { enemyType = EnemyType.Boss, indicatorColor = new Color(0.85f, 0.1f, 0.85f, 0.85f) }
    };

    [Header("Distance Scaling & Billboard Settings")]
    [SerializeField] private bool useDistanceScaling = true;
    [SerializeField] private float distanceScaleFactor = 0.04f;
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 2.5f;
    [Tooltip("Vertical screen-space pixel offset applied to the projected UI image position so it appears above the spawn point.")]
    [SerializeField] private float uiScreenVerticalOffset = 50f;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Camera Reference")]
    [Tooltip("Reference to the main camera. If unassigned, automatically finds Camera.main.")]
    [SerializeField] private Camera mainCam;

    private List<Vector3> activeIndicatedPositions = new List<Vector3>();
    private Dictionary<EnemyType, EnemyIndicatorConfig> configDict = new Dictionary<EnemyType, EnemyIndicatorConfig>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsureCameraReference();
        InitializeConfigDictionary();
    }

    private void Start()
    {
        EnsureCameraReference();
        InitializeConfigDictionary();
    }

    public void InitializeConfigDictionary()
    {
        configDict.Clear();
        foreach (var cfg in enemyIndicatorConfigs)
        {
            configDict[cfg.enemyType] = cfg;
        }
    }

    public void EnsureCameraReference()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
        }
        if (mainCam == null)
        {
            mainCam = FindFirstObjectByType<Camera>();
        }
    }

    /// <summary>
    /// Reset indicated locations list (called at the start of a new wave).
    /// </summary>
    public void ResetWaveIndicators()
    {
        activeIndicatedPositions.Clear();
    }

    /// <summary>
    /// Checks if spawn location is far enough from previously indicated positions in current wave,
    /// and instantiates an indicator if far enough.
    /// </summary>
    public bool TryShowSpawnIndicator(Vector3 spawnWorldPos, EnemyType enemyType = EnemyType.Basic)
    {
        foreach (Vector3 prevPos in activeIndicatedPositions)
        {
            if (Vector3.Distance(spawnWorldPos, prevPos) < minDistanceBetweenIndicators)
            {
                // Too close to a previously indicated spawn point in this wave
                return false;
            }
        }

        activeIndicatedPositions.Add(spawnWorldPos);
        ShowEnemySpawnIndicator(spawnWorldPos, enemyType);
        return true;
    }

    /// <summary>
    /// Public function to instantiate an indicator at the given world position with enemy type settings.
    /// </summary>
    public void ShowEnemySpawnIndicator(Vector3 worldPosition, EnemyType enemyType = EnemyType.Basic)
    {
        EnsureCameraReference();
        Vector3 spawnPos = worldPosition + Vector3.up * heightOffset;

        // Fetch config for this enemy type
        if (configDict.Count == 0) InitializeConfigDictionary();

        EnemyIndicatorConfig config;
        if (!configDict.TryGetValue(enemyType, out config))
        {
            config = new EnemyIndicatorConfig
            {
                enemyType = enemyType,
                indicatorColor = new Color(0.9f, 0.15f, 0.15f, 0.85f)
            };
        }

        // 1. Play Sound
        if (config.spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(config.spawnSound, worldPosition);
        }

        // 2. Instantiate Indicator UI
        GameObject indicatorObj = null;
        if (indicatorPrefab != null)
        {
            indicatorObj = Instantiate(indicatorPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            indicatorObj = CreateProceduralWorldSpaceIndicator(spawnPos, config);
        }

        if (indicatorObj != null)
        {
            // Apply Image Color & Sprite to UI Images on indicator
            ApplyConfigToImages(indicatorObj, config);

            Canvas canvas = indicatorObj.GetComponent<Canvas>();
            if (canvas == null) canvas = indicatorObj.GetComponentInChildren<Canvas>();
            if (canvas != null && mainCam != null)
            {
                canvas.worldCamera = mainCam;
            }

            StartCoroutine(AnimateBillboardIndicator(indicatorObj, spawnPos));
        }
    }

    private void ApplyConfigToImages(GameObject indicatorObj, EnemyIndicatorConfig config)
    {
        Image[] images = indicatorObj.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            img.color = config.indicatorColor;
            if (config.customSprite != null)
            {
                img.sprite = config.customSprite;
            }
        }
    }

    private IEnumerator AnimateBillboardIndicator(GameObject indicatorObj, Vector3 targetWorldPos)
    {
        CanvasGroup cg = indicatorObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = indicatorObj.AddComponent<CanvasGroup>();

        Canvas canvas = indicatorObj.GetComponent<Canvas>();
        if (canvas == null) canvas = indicatorObj.GetComponentInChildren<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCam;
        }

        cg.alpha = 0f;
        float timer = 0f;

        // Phase 1: Fade In & Position/Billboard Update
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float currentAlpha = Mathf.Clamp01(timer / fadeInDuration);
            UpdateIndicatorPositionAndRotation(indicatorObj, targetWorldPos, currentAlpha, cg);
            yield return null;
        }

        // Phase 2: Display Hold & Position/Billboard Update
        timer = 0f;
        while (timer < displayDuration)
        {
            timer += Time.deltaTime;
            UpdateIndicatorPositionAndRotation(indicatorObj, targetWorldPos, 1.0f, cg);
            yield return null;
        }

        // Phase 3: Fade Out & Position/Billboard Update
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float currentAlpha = 1f - Mathf.Clamp01(timer / fadeOutDuration);
            UpdateIndicatorPositionAndRotation(indicatorObj, targetWorldPos, currentAlpha, cg);
            yield return null;
        }

        Destroy(indicatorObj);
    }

    private void UpdateIndicatorPositionAndRotation(GameObject indicatorObj, Vector3 targetWorldPos, float targetAlpha, CanvasGroup cg)
    {
        if (indicatorObj == null) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 screenPoint = mainCam.WorldToScreenPoint(targetWorldPos);
        bool isBehindCamera = screenPoint.z < 0;

        if (isBehindCamera)
        {
            if (cg != null) cg.alpha = 0f;
            return;
        }

        if (cg != null) cg.alpha = targetAlpha;

        Canvas canvas = indicatorObj.GetComponent<Canvas>();
        if (canvas == null) canvas = indicatorObj.GetComponentInChildren<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            indicatorObj.transform.position = targetWorldPos;
            indicatorObj.transform.rotation = Quaternion.LookRotation(indicatorObj.transform.position - mainCam.transform.position);

            if (useDistanceScaling)
            {
                float dist = Vector3.Distance(mainCam.transform.position, targetWorldPos);
                float scale = Mathf.Clamp(dist * distanceScaleFactor, minScale, maxScale);
                indicatorObj.transform.localScale = Vector3.one * scale;
            }
        }
        else
        {
            RectTransform rect = indicatorObj.GetComponent<RectTransform>();
            if (rect == null) rect = indicatorObj.GetComponentInChildren<RectTransform>();

            if (rect != null)
            {
                rect.position = screenPoint + new Vector3(0f, uiScreenVerticalOffset, 0f);
            }
        }
    }

    private GameObject CreateProceduralWorldSpaceIndicator(Vector3 spawnPos, EnemyIndicatorConfig config)
    {
        GameObject canvasObj = new GameObject("WorldSpaceSpawnIndicator");
        canvasObj.transform.position = spawnPos;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 2f);

        // Warning Icon Background
        GameObject imgObj = new GameObject("WarningBackground");
        imgObj.transform.SetParent(canvasObj.transform, false);

        Image img = imgObj.AddComponent<Image>();
        img.color = config.indicatorColor;
        if (config.customSprite != null) img.sprite = config.customSprite;

        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.sizeDelta = new Vector2(1.6f, 1.6f);
        imgRect.localRotation = Quaternion.Euler(0f, 0f, 45f); // Diamond shape

        // Inner Exclamation Text
        GameObject textObj = new GameObject("WarningText");
        textObj.transform.SetParent(canvasObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "!";
        tmp.fontSize = 2.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(1.6f, 1.6f);

        return canvasObj;
    }
}
