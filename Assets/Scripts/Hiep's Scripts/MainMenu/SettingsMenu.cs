using DG.Tweening;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    [Header("Panel RectTransforms")]
    [SerializeField] private RectTransform videoPanel;
    [SerializeField] private RectTransform audioPanel;
    [SerializeField] private RectTransform controlPanel;
    [SerializeField] private RectTransform graphicsPanel;

    [Header("Transition Settings")]
    [SerializeField] private float transitionTime = 0.3f;
    [SerializeField] private float slideDistance = 50f;

    private RectTransform currentPanel;

    [Header("Audio Reference")]
    AudioSource audioSource;
    [SerializeField] private AudioClip selectSound;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Hide all panels initially at the start of the game
        InitialHide(videoPanel);
        InitialHide(graphicsPanel);
        InitialHide(audioPanel);
        InitialHide(controlPanel);
    }
    private void OnEnable()
    {
        // Every time the player opens Settings, default to the Video panel
        OpenTab(videoPanel);
    }
    public void OpenVideo() => OpenTab(videoPanel);
    public void OpenGraphics() => OpenTab(graphicsPanel);
    public void OpenAudio() => OpenTab(audioPanel);
    public void OpenControl() => OpenTab(controlPanel);

    private void OpenTab(RectTransform targetPanel)
    {
        if (targetPanel == currentPanel) return;

        PlaySound();

        //  Animate the OLD panel out
        if (currentPanel != null)
        {
            RectTransform oldPanel = currentPanel;
            CanvasGroup oldGroup = oldPanel.GetComponent<CanvasGroup>();

            oldPanel.DOAnchorPosX(slideDistance, transitionTime).SetEase(Ease.InQuad);
            if (oldGroup != null) oldGroup.DOFade(0, transitionTime);
            
            // Turn off GameObject after animation finishes
            oldPanel.gameObject.SetActive(false);
        }

        // Animate the NEW panel in
        currentPanel = targetPanel;
        currentPanel.gameObject.SetActive(true);
        CanvasGroup newGroup = currentPanel.GetComponent<CanvasGroup>();

        // Start it slightly to the right and transparent
        currentPanel.anchoredPosition = new Vector2(-slideDistance, 0);
        if (newGroup != null) newGroup.alpha = 0;

        // Tween to center and full opacity
        currentPanel.DOAnchorPosX(0, transitionTime).SetEase(Ease.OutQuad);
        if (newGroup != null) newGroup.DOFade(1, transitionTime);
    }

    private void InitialHide(RectTransform panel)
    {
        panel.gameObject.SetActive(false);
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 0;
    }

    // public void OpenVideo()
    // {
    //     PlaySound();
    //     videoPanel.SetActive(true);
    //     audioPanel.SetActive(false);
    //     controlPanel.SetActive(false);
    //     graphicsPanel.SetActive(false);
    // }

    // public void OpenAudio()
    // {
    //     PlaySound();
    //     videoPanel.SetActive(false);
    //     audioPanel.SetActive(true);
    //     controlPanel.SetActive(false);
    //     graphicsPanel.SetActive(false);
    // }

    // public void OpenControl()
    // {
    //     PlaySound();
    //     videoPanel.SetActive(false);
    //     audioPanel.SetActive(false);
    //     controlPanel.SetActive(true);
    //     graphicsPanel.SetActive(false);
    // }
    // public void OpenGraphics()
    // {
    //     PlaySound();
    //     videoPanel.SetActive(false);
    //     audioPanel.SetActive(false);
    //     controlPanel.SetActive(false);
    //     graphicsPanel.SetActive(true);
    // }
    private void PlaySound()
    {
        // Safety check so the game doesn't crash if these aren't assigned
        if (audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound);
        }
    }
}
