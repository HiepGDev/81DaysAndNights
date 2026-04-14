using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroSequence : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 2f; 
    [SerializeField] private float waitTime = 4f; 
    [SerializeField] private string nextSceneName = "MainMenu"; 
    
    private bool isSkipping = false;

    private void Start()
    {
        // Start the sequence
        StartCoroutine(RunSequence());
    }

    private void Update()
    {
        if (!isSkipping)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
                Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                isSkipping = true;
                StopAllCoroutines();
                StartCoroutine(FadeAndLoad());
            }
        }
    }

    private IEnumerator RunSequence()
    {
        canvasGroup.alpha = 0;
        // Fade In
        yield return StartCoroutine(Fade(1,fadeInDuration)); 
        // Wait for player to read
        yield return new WaitForSeconds(waitTime);
        // Fade Out and Load
        yield return StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        yield return StartCoroutine(Fade(0,fadeOutDuration));
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}
