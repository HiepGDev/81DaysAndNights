using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Localization.Settings;

// INotificationReceiver allows this script to "hear" Timeline markers
public class SubtitleManager : MonoBehaviour, INotificationReceiver
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [Header("Localization Settings")]
    [Tooltip("The exact name of string table collection")]
    [SerializeField] private string dialogueTableName = "CutsceneSubtitles";
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
            // subtitleText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Subtitles", subtitleMarker.subtitleKey);
            string localizedSubtitle = LocalizationSettings.StringDatabase.GetLocalizedString(dialogueTableName, subtitleMarker.subtitleKey);
            subtitleText.text = localizedSubtitle;

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