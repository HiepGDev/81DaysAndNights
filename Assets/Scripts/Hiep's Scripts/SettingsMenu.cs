using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private GameObject graphicsPanel;

    [Header("Audio Reference")]
    AudioSource audioSource;
    [SerializeField] private AudioClip selectSound;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void OnEnable()
    {
        // Every time the player opens Settings, default to the Video panel
        OpenVideo();
    }

    public void OpenVideo()
    {
        PlaySound();
        videoPanel.SetActive(true);
        audioPanel.SetActive(false);
        controlPanel.SetActive(false);
        graphicsPanel.SetActive(false);
    }

    public void OpenAudio()
    {
        PlaySound();
        videoPanel.SetActive(false);
        audioPanel.SetActive(true);
        controlPanel.SetActive(false);
        graphicsPanel.SetActive(false);
    }

    public void OpenControl()
    {
        PlaySound();
        videoPanel.SetActive(false);
        audioPanel.SetActive(false);
        controlPanel.SetActive(true);
        graphicsPanel.SetActive(false);
    }
    public void OpenGraphics()
    {
        PlaySound();
        videoPanel.SetActive(false);
        audioPanel.SetActive(false);
        controlPanel.SetActive(false);
        graphicsPanel.SetActive(true);
    }

    private void PlaySound()
    {
        // Safety check so the game doesn't crash if these aren't assigned
        if (audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound);
        }
    }
}
