using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Selectable))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [Header("Custom Sound Overrides")]
    [Tooltip("Custom hover sound for this specific button. If left empty/null, no hover sound will play.")]
    [SerializeField] private AudioClip customHoverSound;
    [Tooltip("Start offset in seconds for hover sound. If 0 or less, starts from beginning.")]
    [SerializeField] private float hoverStartOffset = 0f;
    [Tooltip("End offset in seconds for hover sound. If 0 or less, plays to the end.")]
    [SerializeField] private float hoverEndOffset = 0f;

    [Tooltip("Custom press/click sound for this specific button. If left empty/null, no click sound will play.")]
    [SerializeField] private AudioClip customClickSound;
    [Tooltip("Start offset in seconds for click sound. If 0 or less, starts from beginning.")]
    [SerializeField] private float clickStartOffset = 0f;
    [Tooltip("End offset in seconds for click sound. If 0 or less, plays to the end.")]
    [SerializeField] private float clickEndOffset = 0f;

    [Header("Fallback Settings")]
    [Tooltip("If true, falls back to UISoundManager default sounds when custom sounds are unassigned.")]
    [SerializeField] private bool fallbackToDefaultSound = false;

    public AudioClip CustomHoverSound { get => customHoverSound; set => customHoverSound = value; }
    public AudioClip CustomClickSound { get => customClickSound; set => customClickSound = value; }
    public bool FallbackToDefaultSound { get => fallbackToDefaultSound; set => fallbackToDefaultSound = value; }

    public void SetCustomSounds(AudioClip hover, AudioClip click, bool enableFallback = true)
    {
        customHoverSound = hover;
        customClickSound = click;
        fallbackToDefaultSound = enableFallback;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable) return;

        if (customHoverSound != null)
        {
            if (UISoundManager.Instance != null) UISoundManager.Instance.PlaySound(customHoverSound, hoverStartOffset, hoverEndOffset);
        }
        else if (fallbackToDefaultSound && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayButtonHover();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.interactable) return;

        if (customClickSound != null)
        {
            if (UISoundManager.Instance != null) UISoundManager.Instance.PlaySound(customClickSound, clickStartOffset, clickEndOffset);
        }
        else if (fallbackToDefaultSound && UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayButtonClick();
        }
    }
}
