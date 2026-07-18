using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using PurrNet;

public class SurvivalGameOverManager : MonoBehaviour
{
    [Header("Quote Setting")]
    [SerializeField]
    private string[] quotes = new string[]
    {
        "Chính lòng yêu nước, chứ không phải lý tưởng cộng sản, là nguồn cảm hứng cho tôi. - Hồ Chí Minh ",
        "Các vua Hùng đã có công dựng nước, Bác cháu ta phải cùng nhau giữ lấy nước. - Hồ Chí Minh",
        "Không có gì quý hơn độc lập, tự do. - Hồ Chí Minh"
    };
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI quoteText;
    [SerializeField] private float fadeInDuration = 1f;
    
    // Runtime
    private Color originalTextColor;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 4;

    private void Awake()
    {
        if (quoteText == null)
            quoteText = GetComponentInChildren<TextMeshProUGUI>();
        if (quotes == null || quotes.Length == 0)
        {
            Debug.LogError("SurvivalGameOverManager: Add quotes to array!");
            return;
        }
        
        // Store original color for fade
        if (quoteText != null)
            originalTextColor = quoteText.color;
        quoteText.autoSizeTextContainer = true;
        quoteText.horizontalAlignment = HorizontalAlignmentOptions.Center;  // Center each line
        quoteText.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private void OnEnable()
    {
        // Trigger random quote + pop-up on every activation (death)
        ShowRandomQuote();

        // Only auto-reload locally if offline (singleplayer)
        bool isNetworkActive = NetworkManager.main != null && (NetworkManager.main.isServer || NetworkManager.main.isClient);
        if (isNetworkActive)
        {
            // Networked: WaveManager handles global scene reload when everyone dies
            Debug.Log("[SurvivalGameOverManager] Network active. Local reload suppressed.");
        }
        else
        {
            // Offline/Singleplayer: Auto reload local scene index
            StartCoroutine(ReloadScene());
        }
    }

    private void ShowRandomQuote()
    {
        if (quoteText == null || quotes.Length == 0) return;

        // Random quote 
        string randomQuote = quotes[Random.Range(0, quotes.Length)];
        quoteText.text = $"\"{randomQuote}\""; // Add " " quotes around text

        // Reset transform/color for animation
        quoteText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);  // Alpha 0

        // Start pop-up coroutine
        StartCoroutine(PopUpAnimation());
    }

    private IEnumerator PopUpAnimation()
    {
        float timer = 0f;
        Color transparentColor = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            // Fade alpha in
            quoteText.color = Color.Lerp(transparentColor, originalTextColor, timer / fadeInDuration);
            yield return null;
        }

        // Final full size/opaque
        quoteText.color = originalTextColor;
    }

    private IEnumerator ReloadScene()
    {
        yield return new WaitForSeconds(respawnDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reload current scene
    }
}
