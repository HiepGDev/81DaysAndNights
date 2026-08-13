using UnityEngine;

public class UIFrameSound : MonoBehaviour
{
    [Header("Custom Frame Sound Overrides")]
    [Tooltip("Custom open sound for this specific frame. If unassigned and fallback is disabled, produces intentional silence.")]
    [SerializeField] private AudioClip customOpenSound;
    [Tooltip("Start offset in seconds for open sound. If 0 or less, starts from beginning.")]
    [SerializeField] private float openStartOffset = 0f;
    [Tooltip("End offset in seconds for open sound. If 0 or less, plays to the end.")]
    [SerializeField] private float openEndOffset = 0f;

    [Tooltip("Custom close sound for this specific frame. If unassigned and fallback is disabled, produces intentional silence.")]
    [SerializeField] private AudioClip customCloseSound;
    [Tooltip("Start offset in seconds for close sound. If 0 or less, starts from beginning.")]
    [SerializeField] private float closeStartOffset = 0f;
    [Tooltip("End offset in seconds for close sound. If 0 or less, plays to the end.")]
    [SerializeField] private float closeEndOffset = 0f;

    [Header("Auto Trigger Options")]
    [Tooltip("If true, automatically plays open sound when this GameObject is activated (OnEnable).")]
    [SerializeField] private bool playSoundOnEnable = false;

    [Tooltip("If true, automatically plays close sound when this GameObject is deactivated (OnDisable).")]
    [SerializeField] private bool playSoundOnDisable = false;

    [Header("Fallback Settings")]
    [Tooltip("If true, falls back to UISoundManager default frame sounds when custom sound is unassigned.")]
    [SerializeField] private bool fallbackToDefaultSound = false;

    public AudioClip CustomOpenSound { get => customOpenSound; set => customOpenSound = value; }
    public AudioClip CustomCloseSound { get => customCloseSound; set => customCloseSound = value; }
    public bool FallbackToDefaultSound { get => fallbackToDefaultSound; set => fallbackToDefaultSound = value; }

    private bool isInitialized = false;

    private void Awake()
    {
        isInitialized = true;
    }

    private void Start()
    {
        isInitialized = true;
    }

    public void SetCustomSounds(AudioClip open, AudioClip close, bool enableFallback = true)
    {
        customOpenSound = open;
        customCloseSound = close;
        fallbackToDefaultSound = enableFallback;
    }

    private void OnEnable()
    {
        // Don't play auto sound on initial frame startup
        if (playSoundOnEnable && isInitialized)
        {
            PlayOpen();
        }
    }

    private void OnDisable()
    {
        // Don't play auto sound on initial frame startup
        if (playSoundOnDisable && isInitialized)
        {
            PlayClose();
        }
    }

    /// <summary>
    /// Triggers frame open sound (or default fallback if enabled).
    /// </summary>
    public void PlayOpen()
    {
        if (customOpenSound != null)
        {
            if (UISoundManager.Instance != null) UISoundManager.Instance.PlaySound(customOpenSound, openStartOffset, openEndOffset);
        }
        else if (fallbackToDefaultSound && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayFrameOpen();
        }
    }

    /// <summary>
    /// Triggers frame close sound (or default fallback if enabled).
    /// </summary>
    public void PlayClose()
    {
        if (customCloseSound != null)
        {
            if (UISoundManager.Instance != null) UISoundManager.Instance.PlaySound(customCloseSound, closeStartOffset, closeEndOffset);
        }
        else if (fallbackToDefaultSound && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayFrameClose();
        }
    }
}
