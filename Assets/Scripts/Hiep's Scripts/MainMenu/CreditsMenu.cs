using UnityEngine;

public class CreditsMenu : MonoBehaviour
{
    [SerializeField] AudioClip selectSound;
    [SerializeField] GameObject creditsMenu;
    [SerializeField] GameObject creditsButton; // gotta turn this off when player click on developer share
    // [SerializeField] GameObject developerBoard;
    // [SerializeField] Animator CreditMenuAnimator;
    AudioSource audioSource;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        creditsMenu.SetActive(false);
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
        audioSource.PlayOneShot(selectSound);
        creditsMenu.SetActive(true);
    }
    public void turnOffCredits()
    {
        audioSource.PlayOneShot(selectSound);
        creditsMenu.SetActive(false);
    }
}
