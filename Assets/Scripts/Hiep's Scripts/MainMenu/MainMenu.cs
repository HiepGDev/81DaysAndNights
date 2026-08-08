using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup menuCanvas;
    [SerializeField] private CanvasGroup settingCanvas;
    [SerializeField] private MenuFader menuFader;
    [Header("Sound setting")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private float popInScale = 0.95f;
    AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        menuCanvas.gameObject.SetActive(true);
        menuCanvas.alpha = 1f;
        menuCanvas.blocksRaycasts = true;

        settingCanvas.gameObject.SetActive(false);
        settingCanvas.alpha = 0f;
        settingCanvas.blocksRaycasts = false;
        
        if (menuFader != null) menuFader.gameObject.SetActive(true);
    }

    void Update()
    {
        
    }
    public void StartButton()
    {
        audioSource.PlayOneShot(selectSound);
        //SceneManager.LoadScene("Loading Screen 1");
        menuFader.FadeToScene("Loading Screen 1");
    } 
    public void SettingButton()
    {
        audioSource.PlayOneShot(selectSound);
        TransitionMenu(menuCanvas, settingCanvas);
    }
    public void SurvivalModeButton()
    {
        audioSource.PlayOneShot(selectSound);
        //SceneManager.LoadScene("Loading Screen 1");
        menuFader.FadeToScene("SurvivalMode");
    }
    public void QuitButton()
    {
        Application.Quit();
        audioSource.PlayOneShot(selectSound);
        Debug.Log("Player Quit the game");
    }
    public void SettingReturnButton()
    {
        audioSource.PlayOneShot(selectSound);
        TransitionMenu(settingCanvas, menuCanvas);
    }
    private void TransitionMenu(CanvasGroup hideMenu, CanvasGroup showMenu)
    {
        // Instantly block clicks on the current menu so the player can't double-click
        hideMenu.blocksRaycasts = false;

        // Fade out the current menu
        hideMenu.DOFade(0f, transitionDuration).OnComplete(() => 
        {
            // Turn off the old menu object completely when the fade is done
            hideMenu.gameObject.SetActive(false);
            
            // Turn on the new menu object, but keep it transparent
            showMenu.gameObject.SetActive(true);
            showMenu.alpha = 0f;
            
            // Reset scale and animate a quick pop-in effect
            showMenu.transform.localScale = Vector3.one * popInScale;
            showMenu.transform.DOScale(Vector3.one, transitionDuration).SetEase(Ease.OutBack);
            
            // Fade in the new menu and enable its buttons
            showMenu.DOFade(1f, transitionDuration).OnComplete(() => 
            {
                showMenu.blocksRaycasts = true;
            });
        });
    }
    public void HoverSound()
    {
        audioSource.PlayOneShot(hoverSound);
    }
}
