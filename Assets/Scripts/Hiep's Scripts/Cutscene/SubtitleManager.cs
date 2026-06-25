using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

// INotificationReceiver allows this script to "hear" Timeline markers
public class SubtitleManager : MonoBehaviour, INotificationReceiver
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    
    private Coroutine clearCoroutine;

    void Start()
    {
        // Ensure subtitles are clear when the game starts
        if (subtitleText != null) subtitleText.text = "";
    }

    // This function is triggered AUTOMATICALLY by the Timeline
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        // Check if the marker that just passed is SubtitleMarker
        if (notification is SubtitleMarker subtitleMarker)
        {
            // --- LOCALIZATION READY ---
            // Later, when you add Unity Localization, you will change this line to something like:
            // subtitleText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Subtitles", subtitleMarker.subtitleKey);
            
            // For now, it just prints exactly what type in the Timeline inspector:
            subtitleText.text = subtitleMarker.subtitleKey; 

            // Stop any previous timers so subtitles don't overlap weirdly
            if (clearCoroutine != null) StopCoroutine(clearCoroutine);
            // Start the timer to clear the text
            clearCoroutine = StartCoroutine(ClearSubtitleAfterDelay(subtitleMarker.duration));
        }
    }

    private IEnumerator ClearSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (subtitleText != null) subtitleText.text = "";
    }
}