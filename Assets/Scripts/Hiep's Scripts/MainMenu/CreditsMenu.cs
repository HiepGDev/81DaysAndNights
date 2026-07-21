using DG.Tweening;
using UnityEngine;

public class CreditsMenu : MonoBehaviour
{
    [SerializeField] AudioClip selectSound;
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] GameObject creditsButton; // gotta turn this off when player click on developer share
    // [SerializeField] GameObject developerBoard;
    // [SerializeField] Animator CreditMenuAnimator;
    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private float popInScale = 0.95f;
    AudioSource audioSource;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.gameObject.SetActive(false);
            creditsCanvasGroup.alpha = 0f;
            creditsCanvasGroup.blocksRaycasts = false;
        }
        // developerBoard.SetActive(false);
    }
    // public void developerShareButton()
    // {
    //     audioSource.PlayOneShot(selectSound);
    //     //developerBoard.SetActive(true);
    //     creditsButton.SetActive(false);
    //     // audioSource.PlayOneShot(openSound, openSoundVolume);
    // }
    // public void turnOffDeveloperBoard()
    // {
    //     audioSource.PlayOneShot(selectSound);
    // }
    public void creditsMenuOn()
    {
        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);

        if (creditsCanvasGroup != null)
        {
            // Prep the canvas for animation
            creditsCanvasGroup.gameObject.SetActive(true);
            creditsCanvasGroup.alpha = 0f;
            creditsCanvasGroup.transform.localScale = Vector3.one * popInScale;

            // Animate In (Fade & Scale)
            creditsCanvasGroup.DOFade(1f, transitionDuration);
            
            // SetEase(Ease.OutBack) gives it that nice bouncy pop-in effect
            creditsCanvasGroup.transform.DOScale(Vector3.one, transitionDuration).SetEase(Ease.OutBack).OnComplete(() =>
            {
                // Allow clicks only after the animation finishes
                creditsCanvasGroup.blocksRaycasts = true;
            });
        }
    }
    public void turnOffCredits()
    {
        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);

        if (creditsCanvasGroup != null)
        {
            // Block clicks immediately so the player can't double-click the close button
            creditsCanvasGroup.blocksRaycasts = false;

            // Animate Out (Fade & Scale shrink)
            creditsCanvasGroup.DOFade(0f, transitionDuration);
            
            // SetEase(Ease.InBack) makes it shrink slightly before disappearing
            creditsCanvasGroup.transform.DOScale(Vector3.one * popInScale, transitionDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                // Turn off the GameObject entirely
                creditsCanvasGroup.gameObject.SetActive(false);
            });
        }
    }
}
