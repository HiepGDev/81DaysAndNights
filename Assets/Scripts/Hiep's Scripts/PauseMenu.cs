using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pauseCanvas;
    [SerializeField] AudioClip selectSound;
    [SerializeField] private PlayerHealth playerHealth;
    AudioSource audioSource;
    // private AudioSource musicAudioSource; 
    bool isPause;
    void Start()
    {
        pauseCanvas.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        // Music musicScript = FindFirstObjectByType<Music>();
        // if (musicScript != null)
        // {
        //     musicAudioSource = musicScript.GetComponent<AudioSource>();
        // }
        
        // if (musicAudioSource == null)
        // {
        //     Debug.LogWarning("PauseMenu could not find the Music's AudioSource!");
        // }
    }
    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        { 
            if (isPause)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    
    public void Pause()
    {
        pauseCanvas.SetActive(true);
        Time.timeScale = 0f;
        // musicAudioSource.Pause();
        isPause = true;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public void Resume()
    {
       pauseCanvas.SetActive(false);
       Time.timeScale = 1f;
       // musicAudioSource.UnPause();
       isPause = false;
       AudioListener.pause = false;
       if (audioSource != null && selectSound != null)
       audioSource.PlayOneShot(selectSound);
       Cursor.visible = false;
       Cursor.lockState = CursorLockMode.Locked;
    }
    public void QuitToMenu()
    {
     audioSource.PlayOneShot(selectSound);
     AudioListener.pause = false;
     Time.timeScale = 1f;
     SceneManager.LoadScene("MainMenu");
    }
}
