using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
public class GameOverManager : MonoBehaviour
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

    private void Awake()
    {
        if (quoteText == null)
            quoteText = GetComponentInChildren<TextMeshProUGUI>();
        if (quotes == null || quotes.Length == 0)
        {
            Debug.LogError("GameOverManager: Add quotes to array!");
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
    }
    private void ShowRandomQuote()
    {
        if (quoteText == null || quotes.Length == 0) return;

        // Random quote 
        string randomQuote = quotes[Random.Range(0, quotes.Length)];
        quoteText.text = $"\"{randomQuote}\""; // Add " " quotes around text
        

        // Reset transform/color for animation
        // quoteText.transform.localScale = Vector3.zero;  
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
            // Smooth scale up (ease-out)
            //float scaleProgress = Mathf.Sin(timer / fadeInDuration * Mathf.PI * 0.5f);
            // quoteText.transform.localScale = Vector3.one * scaleProgress;

            // Fade alpha in
           quoteText.color = Color.Lerp(transparentColor, originalTextColor, timer / fadeInDuration);
            yield return null;
        }

        // Final full size/opaque
        // quoteText.transform.localScale = Vector3.one;
        quoteText.color = originalTextColor;
    }
}

