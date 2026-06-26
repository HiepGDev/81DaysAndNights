using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuFader : MonoBehaviour
{
    
    private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1.5f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // Start fully black and blocking any button clicks
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
    // private void OnEnable()
    // {
        
    // } 
    private void Start()
    {
        // Automatically start the Fade In
        StartCoroutine(Fade(0f));
    }

    public void FadeToScene(string sceneName)
    {
        // Start the Fade Out then change scenes
        StartCoroutine(ProcessFadeOut(sceneName));
    }

    private IEnumerator ProcessFadeOut(string sceneName)
    {
        canvasGroup.blocksRaycasts = true; // Block buttons so player can't click
        yield return StartCoroutine(Fade(1f));
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        // If just finished fading IN, stop blocking the mouse so player can click menu buttons
        if (targetAlpha <= 0f)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }
}
