using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct BonusData
{
    public string title;
    public string detailLine1;
    public string detailLine2;
    public string pointsText;

    public BonusData(string title, string detailLine1, string detailLine2, string pointsText)
    {
        this.title = title;
        this.detailLine1 = detailLine1;
        this.detailLine2 = detailLine2;
        this.pointsText = pointsText;
    }

    public BonusData(string title, string detailLine1, string detailLine2, int pointsValue)
    {
        this.title = title;
        this.detailLine1 = detailLine1;
        this.detailLine2 = detailLine2;
        this.pointsText = pointsValue >= 0 ? $"+{pointsValue}" : pointsValue.ToString();
    }
}

public class BonusUI : MonoBehaviour
{
    public static BonusUI Instance { get; private set; }

    [Header("Prefab & Container Settings")]
    [Tooltip("The Bonus prefab from Assets/Prefabs/Phu's Prefab/UIs/Bonus.prefab")]
    [SerializeField] private GameObject bonusPrefab;

    [Tooltip("Optional parent Transform/Canvas to spawn bonus popups under. If unassigned, auto-locates active Canvas.")]
    [SerializeField] private Transform popupParent;

    [Header("Animation Settings")]
    [SerializeField] private float slideDistance = 400f;     // Horizontal distance offset for slide-in from left
    [SerializeField] private float fadeInDuration = 0.4f;    // Duration of fade-in and slide
    [SerializeField] private float displayDuration = 2.2f;   // Hold duration on screen
    [SerializeField] private float fadeOutDuration = 0.5f;   // Duration of fade-out
    [SerializeField] private float delayBetweenPopups = 0.6f; // Delay before starting next popup in queue

    private Canvas autoCanvas;
    private Queue<BonusData> popupQueue = new Queue<BonusData>();
    private bool isProcessingQueue = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsureContainerExists();
    }

    private void Start()
    {
        // Try auto-loading prefab if not assigned in Inspector
        if (bonusPrefab == null)
        {
#if UNITY_EDITOR
            bonusPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Phu's Prefab/UIs/Bonus.prefab");
#endif
        }
    }

    /// <summary>
    /// Add and display any custom bonus popup.
    /// </summary>
    public void AddBonus(string title, string detailLine1, string detailLine2, string pointsText)
    {
        popupQueue.Enqueue(new BonusData(title, detailLine1, detailLine2, pointsText));
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessQueueRoutine());
        }
    }

    /// <summary>
    /// Add and display any custom bonus popup with integer point value.
    /// </summary>
    public void AddBonus(string title, string detailLine1, string detailLine2, int pointsValue)
    {
        popupQueue.Enqueue(new BonusData(title, detailLine1, detailLine2, pointsValue));
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessQueueRoutine());
        }
    }

    /// <summary>
    /// Specific function to award and show Accuracy Bonus popup.
    /// </summary>
    public void AddAccuracyBonus(float accuracyPercent, int shotsFired, int shotsHit, int bonusPoints)
    {
        string title = $"Accuracy bonus: {(accuracyPercent * 100f):F2}%";
        string line1 = $"- Fired {shotsFired}";
        string line2 = $"- Hit {shotsHit}";
        AddBonus(title, line1, line2, bonusPoints);
    }

    /// <summary>
    /// Specific function to award and show Economy Bonus popup.
    /// </summary>
    public void AddEconomyBonus(float economyBonus, float moneyGained, float moneySpent, int bonusPoints)
    {
        string title = $"Economy bonus: +{economyBonus:F0}";
        string line1 = $"- Earned ${moneyGained:F0}";
        string line2 = $"- Spent ${moneySpent:F0}";
        AddBonus(title, line1, line2, bonusPoints);
    }

    private IEnumerator ProcessQueueRoutine()
    {
        isProcessingQueue = true;
        EnsureContainerExists();

        while (popupQueue.Count > 0)
        {
            BonusData data = popupQueue.Dequeue();
            yield return StartCoroutine(SpawnAndAnimatePopupRoutine(data));

            if (popupQueue.Count > 0)
            {
                yield return new WaitForSeconds(delayBetweenPopups);
            }
        }

        isProcessingQueue = false;
    }

    private IEnumerator SpawnAndAnimatePopupRoutine(BonusData data)
    {
        if (bonusPrefab == null)
        {
            Debug.LogWarning("[BonusUI] bonusPrefab is not assigned!");
            yield break;
        }

        Transform parentTransform = popupParent != null ? popupParent : (autoCanvas != null ? autoCanvas.transform : transform);
        GameObject popup = Instantiate(bonusPrefab, parentTransform, false);
        popup.SetActive(true);

        // Populate Text Elements inside Bonus prefab hierarchy
        SetTextElement(popup.transform, "Reward_Text1", data.title);
        SetTextElement(popup.transform, "Reward_Text2", data.detailLine1);
        SetTextElement(popup.transform, "Reward_Text3", data.detailLine2);
        SetTextElement(popup.transform, "Points_Num", data.pointsText);

        // Target inner child container if present to bypass parent Layout Group position locking on the root clone
        Transform animTarget = (popup.transform.childCount > 0) ? popup.transform.GetChild(0) : popup.transform;

        CanvasGroup cg = animTarget.GetComponent<CanvasGroup>();
        if (cg == null) cg = animTarget.gameObject.AddComponent<CanvasGroup>();

        RectTransform rect = animTarget.GetComponent<RectTransform>();
        Vector2 targetPos = rect != null ? rect.anchoredPosition : Vector2.zero;
        Vector2 startPos = targetPos - new Vector2(slideDistance, 0f);
        Vector2 endPos = targetPos + new Vector2(150f, 0f);

        cg.alpha = 0f;
        if (rect != null)
        {
            rect.anchoredPosition = startPos;
        }

        // Phase 1: Slide Left to Right & Fade In
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cg.alpha = smoothT;
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            }

            yield return null;
        }

        cg.alpha = 1f;
        if (rect != null) rect.anchoredPosition = targetPos;

        // Phase 2: Display Hold
        yield return new WaitForSeconds(displayDuration);

        // Phase 3: Slide Right & Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cg.alpha = 1f - smoothT;
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.Lerp(targetPos, endPos, smoothT);
            }

            yield return null;
        }

        Destroy(popup);
    }

    private void EnsureContainerExists()
    {
        if (popupParent != null || autoCanvas != null) return;

        // Try finding active canvas in scene
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            autoCanvas = existingCanvas;
            return;
        }

        // Fallback: Create dynamic canvas
        GameObject canvasObj = new GameObject("BonusUICanvas");
        autoCanvas = canvasObj.AddComponent<Canvas>();
        autoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void SetTextElement(Transform parent, string elementName, string textValue)
    {
        Transform child = parent.Find(elementName);
        if (child == null)
        {
            var allChildren = parent.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t.name == elementName)
                {
                    child = t;
                    break;
                }
            }
        }

        if (child != null)
        {
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = textValue;
                return;
            }

            var text = child.GetComponent<Text>();
            if (text != null)
            {
                text.text = textValue;
            }
        }
    }
}
