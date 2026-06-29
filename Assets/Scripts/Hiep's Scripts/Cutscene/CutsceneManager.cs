using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private PlayableDirector cutsceneTimeline;
    [Tooltip("Check this if this is the opening level cutscene. Uncheck for mid-level cutscenes.")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool seamlessMidLevel = true;

    [Header("Post-Cutscene Placement")]
    [Tooltip("Create an Empty GameObject where player stand and face when cutscene end")]
    [SerializeField] private Transform postCutsceneSpawn;
    
    [Header("Player & NPC")]
    [SerializeField] private GameObject realPlayer; 
    [SerializeField] private GameObject cutsceneNpc; 
    
    [Header("Cameras")]
    [SerializeField] private GameObject cutsceneCamera; 

    [Header("UI & Canvases")]
    [SerializeField] private GameObject playerCanvas;
    [SerializeField] private GameObject cutsceneCanvas;
    [SerializeField] private Image blackScreenImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Cinematic Bars")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private float barSlideDuration = 1.5f;

    [Header("Level State Changes (Optional)")]
    [SerializeField] private GameObject[] objectsToTurnOff;
    [SerializeField] private GameObject[] objectsToTurnOn;

    [Header("Post-Cutscene State Changes")]
    [SerializeField] private GameObject[] objectsToTurnOffAfter;
    [SerializeField] private GameObject[] objectsToTurnOnAfter;

    [Header("Weapon Control (Optional)")]
    private WeaponSwitchManager weaponManager;
    [Tooltip("What should the player hold when cutscene end")]
    [SerializeField] private WeaponSwitchManager.HandState handStateAfterCutscene = WeaponSwitchManager.HandState.NormalArm;

    private Vector2 topBarOriginalPos;
    private Vector2 bottomBarOriginalPos;
    private bool hasPlayed = false; // Prevents the cutscene from playing twice
    private bool isPlaying = false;
    private bool isSkipping = false;
    private Coroutine skipCoroutine;
    void Start()
    {
        // Record bar positions
        if (topBar != null) topBarOriginalPos = topBar.anchoredPosition;
        if (bottomBar != null) bottomBarOriginalPos = bottomBar.anchoredPosition;
        weaponManager = FindFirstObjectByType<WeaponSwitchManager>(FindObjectsInactive.Include);
        // If this is the Intro Cutscene, set it up immediately
        if (playOnStart)
        {
            SetupAndPlayCutscene(true);
        }
    }
    void Update()
    {
        if (isPlaying && !isSkipping && Input.GetKeyDown(KeyCode.Backspace))
        {
            SkipCutscene();
        }
    }
    //  allows the cutscene to trigger when the player walks into an invisible wall
    private void OnTriggerEnter(Collider other)
    {
        if (!playOnStart && !hasPlayed && other.CompareTag("Player"))
        {
            // Stop the player from moving while we fade out
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null) pm.canMove = false;

            StartCoroutine(TransitionIntoMidLevelCutscene());
        }
    } 
    private void SetupAndPlayCutscene(bool isIntro)
    {
        hasPlayed = true;
        isPlaying = true;
        if (realPlayer != null) realPlayer.SetActive(false);
        if (cutsceneNpc != null) cutsceneNpc.SetActive(true);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);
        
        if (playerCanvas != null) playerCanvas.SetActive(false);
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(true);

        // Reset the cinematic bars to their default positions just in case a previous cutscene moved them
        if (topBar != null) topBar.anchoredPosition = topBarOriginalPos;
        if (bottomBar != null) bottomBar.anchoredPosition = bottomBarOriginalPos;

        foreach (GameObject obj in objectsToTurnOff)
        {
            if (obj != null) obj.SetActive(false);
        }
        foreach (GameObject obj in objectsToTurnOn)
        {
            if (obj != null) obj.SetActive(true);
        }
        
        if (isIntro)
        {
            if (blackScreenImage != null) blackScreenImage.color = new Color(0, 0, 0, 1f);
            StartCoroutine(FadeFromBlack());
        }

        if (cutsceneTimeline != null)
        {
            cutsceneTimeline.stopped += OnTimelineFinished;
            cutsceneTimeline.Play(); 
        }
    }

    //  Handles fading out gameplay BEFORE starting a mid-level cutscene
    private IEnumerator TransitionIntoMidLevelCutscene()
    {
        if (!seamlessMidLevel)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        
        // Once screen is black, do the magic swap and start the timeline
        SetupAndPlayCutscene(false);
        if (!seamlessMidLevel)
        {
            yield return StartCoroutine(FadeFromBlack());
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        isPlaying = false;
        // Clean up the event listener immediately
        if (isSkipping && skipCoroutine != null)
        {
            StopCoroutine(skipCoroutine);
            isSkipping = false;
        }

        cutsceneTimeline.stopped -= OnTimelineFinished;
        StartCoroutine(TransitionToGameplay());
    }

    private IEnumerator TransitionToGameplay()
    {
        if (blackScreenImage != null && blackScreenImage.color.a < 0.99f)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        
        // The Swap back to gameplay
        if (cutsceneNpc != null) cutsceneNpc.SetActive(false);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (weaponManager != null)
        {
            weaponManager.SetHandState(handStateAfterCutscene);
        }
        // Trigger the "Post-Cutscene" state changes
        foreach (GameObject obj in objectsToTurnOffAfter)
        {
            if (obj != null) obj.SetActive(false);
        }
        foreach (GameObject obj in objectsToTurnOnAfter)
        {
            if (obj != null) obj.SetActive(true);
        }

        if (realPlayer != null && postCutsceneSpawn != null)
        {
            CharacterController cc = realPlayer.GetComponent<CharacterController>();
            
            // Turn off the CharacterController so it doesn't block the teleport
            if (cc != null) cc.enabled = false;

            // Teleport and Rotate
            realPlayer.transform.position = postCutsceneSpawn.position;
            realPlayer.transform.rotation = postCutsceneSpawn.rotation;

            // Turn the CharacterController back on
            if (cc != null) cc.enabled = true;
        }
        if (realPlayer != null) realPlayer.SetActive(true);
        if (playerCanvas != null) playerCanvas.SetActive(true);

        // Ensure the player can move again (in case they were frozen by a mid-level trigger)
        PlayerMovement pm = realPlayer.GetComponent<PlayerMovement>();
        if (pm != null) pm.canMove = true;

        yield return StartCoroutine(FadeFromBlack());

        // Slide the cinematic bars out of view smoothly
        if (topBar != null && bottomBar != null)
        {
            float timer = 0f;
            float topTargetY = topBarOriginalPos.y + topBar.rect.height; 
            float bottomTargetY = bottomBarOriginalPos.y - bottomBar.rect.height;

            while (timer < barSlideDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, timer / barSlideDuration);

                topBar.anchoredPosition = new Vector2(topBarOriginalPos.x, Mathf.Lerp(topBarOriginalPos.y, topTargetY, progress));
                bottomBar.anchoredPosition = new Vector2(bottomBarOriginalPos.x, Mathf.Lerp(bottomBarOriginalPos.y, bottomTargetY, progress));
                
                yield return null;
            }
        }

        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);
        isSkipping = false; // Reset for the next cutscene
    }

    private IEnumerator FadeToBlack()
    {
        if (blackScreenImage == null) yield break;
        float timer = 0f;
        float startAlpha = blackScreenImage.color.a;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            blackScreenImage.color = new Color(0, 0, 0, Mathf.Lerp(startAlpha, 1f, timer / fadeDuration));
            yield return null; 
        }
        blackScreenImage.color = new Color(0, 0, 0, 1f); // Guarantee absolute black
    }

    private IEnumerator FadeFromBlack()
    {
        if (blackScreenImage == null) yield break;
        float timer = 0f;
        float startAlpha = blackScreenImage.color.a;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            blackScreenImage.color = new Color(0, 0, 0, Mathf.Lerp(startAlpha, 0f, timer / fadeDuration));
            yield return null; 
        }
        blackScreenImage.color = new Color(0, 0, 0, 0f); // Guarantee absolute clear
    }
    private void SkipCutscene()
    {
        if (!isPlaying || isSkipping) return;
        
        isSkipping = true; 
        skipCoroutine = StartCoroutine(SkipRoutine());
    }
    private IEnumerator SkipRoutine()
    {
        // Fade to black at normal speed while the timeline continues playing
        yield return StartCoroutine(FadeToBlack());
        
        // The screen is pitch black. It is safe to stop the timeline.
        if (cutsceneTimeline != null)
        {
            cutsceneTimeline.Stop(); 
        }
    }
}
