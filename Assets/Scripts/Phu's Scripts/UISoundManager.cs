using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UISoundManager : MonoBehaviour
{
    private static UISoundManager instance;
    public static UISoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UISoundManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UI Sound Manager");
                    instance = go.AddComponent<UISoundManager>();
                }
            }
            return instance;
        }
    }

    [Header("Default Frame Sound Effects")]
    [Tooltip("Default sound effect played when opening a UI frame, panel, or popup.")]
    [SerializeField] private AudioClip frameOpenSound;
    [SerializeField] private float frameOpenStartOffset = 0f;
    [SerializeField] private float frameOpenEndOffset = 0f;

    [Tooltip("Default sound effect played when closing a UI frame, panel, or popup.")]
    [SerializeField] private AudioClip frameCloseSound;
    [SerializeField] private float frameCloseStartOffset = 0f;
    [SerializeField] private float frameCloseEndOffset = 0f;

    [Header("Default Button Sound Effects")]
    [Tooltip("Default sound effect played when hovering over an interactable UI button.")]
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private float buttonHoverStartOffset = 0f;
    [SerializeField] private float buttonHoverEndOffset = 0f;

    [Tooltip("Default sound effect played when pressing/clicking a UI button.")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float buttonClickStartOffset = 0f;
    [SerializeField] private float buttonClickEndOffset = 0f;

    [Header("Shop Sound Effects")]
    [Tooltip("Default sound effect played when a shop item purchase succeeds.")]
    [SerializeField] private AudioClip purchaseSuccessSound;
    [SerializeField] private float purchaseSuccessStartOffset = 0f;
    [SerializeField] private float purchaseSuccessEndOffset = 0f;

    [Tooltip("Default sound effect played when a shop item purchase fails (e.g., insufficient funds or full).")]
    [SerializeField] private AudioClip purchaseFailSound;
    [SerializeField] private float purchaseFailStartOffset = 0f;
    [SerializeField] private float purchaseFailEndOffset = 0f;

    [Header("Weapon Sound Effects")]
    [Tooltip("Default sound effect played when switching weapons.")]
    [SerializeField] private AudioClip weaponSwitchSound;
    [SerializeField] private float weaponSwitchStartOffset = 0f;
    [SerializeField] private float weaponSwitchEndOffset = 0f;

    [Tooltip("Minimum time (in seconds) between purchase failure sound plays to prevent audio spamming.")]
    [SerializeField] private float purchaseFailDebounceTime = 0.6f;

    private float lastPurchaseFailTime = -999f;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 0.8f;
    [SerializeField] private AudioSource customAudioSource;

    public AudioClip FrameOpenSound => frameOpenSound;
    public AudioClip FrameCloseSound => frameCloseSound;
    public AudioClip ButtonHoverSound => buttonHoverSound;
    public AudioClip ButtonClickSound => buttonClickSound;
    public AudioClip PurchaseSuccessSound => purchaseSuccessSound;
    public AudioClip PurchaseFailSound => purchaseFailSound;

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        if (customAudioSource != null)
        {
            audioSource = customAudioSource;
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D flat sound for UI
    }

    [Tooltip("Minimum time (in seconds) between frame open/close sound plays to prevent duplicate audio.")]
    [SerializeField] private float frameSoundDebounceTime = 0.25f;

    private float lastFrameOpenTime = -999f;
    private float lastFrameCloseTime = -999f;

    public void PlayFrameOpen()
    {
        if (Time.unscaledTime - lastFrameOpenTime < frameSoundDebounceTime) return;
        lastFrameOpenTime = Time.unscaledTime;
        PlaySound(frameOpenSound, frameOpenStartOffset, frameOpenEndOffset);
    }

    public void PlayFrameClose()
    {
        if (Time.unscaledTime - lastFrameCloseTime < frameSoundDebounceTime) return;
        lastFrameCloseTime = Time.unscaledTime;
        PlaySound(frameCloseSound, frameCloseStartOffset, frameCloseEndOffset);
    }

    public void PlayButtonHover()
    {
        PlaySound(buttonHoverSound, buttonHoverStartOffset, buttonHoverEndOffset);
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound, buttonClickStartOffset, buttonClickEndOffset);
    }

    public void PlayPurchaseSuccess()
    {
        PlaySound(purchaseSuccessSound, purchaseSuccessStartOffset, purchaseSuccessEndOffset);
    }

    public void PlayPurchaseFail()
    {
        if (Time.unscaledTime - lastPurchaseFailTime < purchaseFailDebounceTime)
        {
            return; // Ignore rapid spammed failure sound plays
        }

        lastPurchaseFailTime = Time.unscaledTime;
        PlaySound(purchaseFailSound, purchaseFailStartOffset, purchaseFailEndOffset);
    }

    public void PlayWeaponSwitch()
    {
        PlaySound(weaponSwitchSound, weaponSwitchStartOffset, weaponSwitchEndOffset);
    }

    public void PlaySound(AudioClip clip, float startOffset = 0f, float endOffset = 0f)
    {
        if (clip == null) return;
        if (audioSource == null) SetupAudioSource();

        float startTime = Mathf.Clamp(startOffset, 0f, clip.length);
        float endTime = (endOffset <= 0f || endOffset > clip.length) ? clip.length : Mathf.Clamp(endOffset, startTime, clip.length);
        float duration = Mathf.Max(0.01f, endTime - startTime);

        // If no trimming required (starts at 0 and ends at clip length), use standard PlayOneShot
        if (startTime <= 0.001f && Mathf.Abs(endTime - clip.length) <= 0.001f)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip, uiVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, uiVolume);
            }
            return;
        }

        // Trimmed sound playback using temporary audio source
        GameObject tempAudioObj = new GameObject($"UIAudio_{clip.name}");
        AudioSource source = tempAudioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = uiVolume;
        source.spatialBlend = 0f; // 2D flat UI sound
        source.time = startTime;
        source.playOnAwake = false;

        source.Play();
        source.SetScheduledEndTime(AudioSettings.dspTime + duration);

        Destroy(tempAudioObj, duration + 0.1f);
    }

    /// <summary>
    /// Automatically attaches hovering and clicking sound behavior to any UI Button component.
    /// </summary>
    public void AttachButtonSounds(Button button, AudioClip customHover = null, AudioClip customClick = null)
    {
        if (button == null) return;

        UIButtonSound soundComp = button.GetComponent<UIButtonSound>();
        if (soundComp == null)
        {
            soundComp = button.gameObject.AddComponent<UIButtonSound>();
            // Dynamically attached sound component -> fallback to default clips if custom clips are null
            soundComp.SetCustomSounds(customHover, customClick, enableFallback: (customHover == null || customClick == null));
        }
        else
        {
            // If component already exists on the button object, update sounds if provided while retaining fallback setting
            if (customHover != null || customClick != null)
            {
                soundComp.SetCustomSounds(
                    customHover != null ? customHover : soundComp.CustomHoverSound,
                    customClick != null ? customClick : soundComp.CustomClickSound,
                    soundComp.FallbackToDefaultSound
                );
            }
        }
    }

    /// <summary>
    /// Automatically attaches or updates frame open/close sound behavior to any UI Frame/Panel GameObject.
    /// </summary>
    public void AttachFrameSounds(GameObject frameObj, AudioClip customOpen = null, AudioClip customClose = null)
    {
        if (frameObj == null) return;

        UIFrameSound frameSoundComp = frameObj.GetComponent<UIFrameSound>();
        if (frameSoundComp == null)
        {
            frameSoundComp = frameObj.AddComponent<UIFrameSound>();
            frameSoundComp.SetCustomSounds(customOpen, customClose, enableFallback: (customOpen == null || customClose == null));
        }
        else
        {
            if (customOpen != null || customClose != null)
            {
                frameSoundComp.SetCustomSounds(
                    customOpen != null ? customOpen : frameSoundComp.CustomOpenSound,
                    customClose != null ? customClose : frameSoundComp.CustomCloseSound,
                    frameSoundComp.FallbackToDefaultSound
                );
            }
        }
    }
}
