using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private GameObject FadeCanvas;
    [Header("Sound setting")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    
    AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        menuCanvas.SetActive(true);
        settingCanvas.SetActive(false);
        FadeCanvas.SetActive(true);
    }

    void Update()
    {
        
    }
    public void StartButton()
    {
        audioSource.PlayOneShot(selectSound);
        SceneManager.LoadScene("Loading Screen 1");
    } 
    public void SettingButton()
    {
        audioSource.PlayOneShot(selectSound);
        settingCanvas.SetActive(true);
        menuCanvas.SetActive(false);
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
        settingCanvas.SetActive(false);
        menuCanvas.SetActive(true);
    }
    public void HoverSound()
    {
        audioSource.PlayOneShot(hoverSound);
    }
}
