using System.Collections;
using UnityEngine;
using TMPro;

public class HintUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject hintPrefab;
    [SerializeField] private GameObject longHintPrefab;

    [Header("Settings")]
    [SerializeField] private Transform listPanel;
    [SerializeField] private float hintDuration = 4f;
    [SerializeField] private float longHintDuration = 7f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeDuration = 1.5f;

    /// <summary>
    /// Spawn and display a standard short hint.
    /// </summary>
    /// <param name="message">The text message to display</param>
    public void AddHint(string message)
    {
        SpawnNotification(hintPrefab, message, hintDuration);
    }

    /// <summary>
    /// Spawn and display a longer formatted hint.
    /// </summary>
    /// <param name="message">The text message to display</param>
    public void AddLongHint(string message)
    {
        SpawnNotification(longHintPrefab, message, longHintDuration);
    }

    private void SpawnNotification(GameObject prefab, string message, float duration)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[HintList] Spawn notification failed: prefab reference is null.");
            return;
        }

        // Instantiate clone under this container (normally has VerticalLayoutGroup)
        GameObject clone = Instantiate(prefab, listPanel);
        clone.gameObject.SetActive(true);

        // Find text component to set message
        TextMeshProUGUI textMesh = clone.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            textMesh.text = message;
        }
        else
        {
            Debug.LogWarning("[HintList] No TextMeshProUGUI found in spawned hint prefab structure.");
        }

        // Start lifetime coroutine
        StartCoroutine(NotificationLifetimeRoutine(clone, duration));
    }

    private IEnumerator NotificationLifetimeRoutine(GameObject notification, float duration)
    {
        // Add or get a CanvasGroup for fading opacity
        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = notification.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        // Fade In
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            if (notification == null) yield break; // Safeguard if destroyed early
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Hold visible
        yield return new WaitForSeconds(duration);

        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (notification == null) yield break; // Safeguard if destroyed early
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        if (notification != null)
        {
            Destroy(notification);
        }
    }
}
