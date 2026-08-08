using System.Collections;
using UnityEngine;

public class Mission1Manager : MonoBehaviour
{
    [Header("Mission Dependencies")]
    [SerializeField] private EnemySpawner[] enemySpawners;
    
    [Header("End Game Actions")]
    [Tooltip("Place the GameObject holding Final CutsceneManager here. Make sure it starts Disabled in the scene!")]
    [SerializeField] private GameObject finalCutsceneObject;

    [SerializeField] private float fadeToBlackDuration = 1.5f; // Adjust this to match CutsceneManager's fade duration
    [Header("Audio Controls")]
    [Tooltip("Drag the GameObject with combat music AudioSource here.")]
    [SerializeField] private AudioSource combatMusic;
    [Tooltip("How long it takes for the music to fade to zero.")]
    [SerializeField] private float musicFadeDuration = 3f;
    private void Start()
    {
        if (enemySpawners != null && enemySpawners.Length > 0)
        {
            // Start checking the mission status in the background
            StartCoroutine(MonitorMissionStatus());
        }
        else
        {
            Debug.LogWarning("[MissionManager] EnemySpawner is not assigned!");
        }
    }

   private IEnumerator MonitorMissionStatus()
    {
        // Wait until the EnemySpawner has pushed out every single wave
        bool allSpawnersDone = false;
        while (!allSpawnersDone)
        {
            allSpawnersDone = true; // Assume they are done, then prove it wrong
            foreach (EnemySpawner spawner in enemySpawners)
            {
                // If find even one spawner that isn't done yet, break and keep waiting
                if (spawner != null && !spawner.IsDoneSpawning)
                {
                    allSpawnersDone = false;
                    break; 
                }
            }
            
            // If they aren't all done, wait a second and check again
            if (!allSpawnersDone)
            {
                yield return new WaitForSeconds(1f);
            }
        }

        // The waves are done. Now wait for the player to eliminate the remaining enemies.
        while (true)
        {
            // Find all EnemyBehaviorAgents in the scene (including dead ragdolls)
            EnemyBehaviorAgent[] allEnemies = FindObjectsByType<EnemyBehaviorAgent>(FindObjectsSortMode.None);
            
            int aliveCount = 0;
            // Count only the enemies that still have their AI turned ON
            foreach (var enemy in allEnemies)
            {
                // EnemyHealth script disables this component when they die.
                // If it is still enabled, the enemy is still alive!
                if (enemy.enabled)
                {
                    aliveCount++;
                }
            }

            if (aliveCount == 0)
            {
                // All active AI are dead! Trigger the end of the mission.
                StartCoroutine(EndMissionRoutine());
                break; 
            }

            // Wait 1 second before checking again to save CPU performance
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator EndMissionRoutine()
    {
        Debug.Log("[MissionManager] All enemies eliminated. Fading to final cutscene!");

        if (combatMusic != null && combatMusic.isPlaying)
        {
            StartCoroutine(FadeOutMusic(combatMusic, musicFadeDuration));
        }
        
        if (finalCutsceneObject != null)
        {
            // Turn the GameObject on (but the cutscene won't play yet)
            finalCutsceneObject.SetActive(true);
            
            // Grab the manager and tell it to start the fade-out process
            CutsceneManager cm = finalCutsceneObject.GetComponent<CutsceneManager>();
            if (cm != null)
            {
                cm.PlayCutsceneExternally();
            }
        }

        //  WAIT for the screen to actually turn black
        yield return new WaitForSeconds(fadeToBlackDuration);

        // NOW disable all Teammate AI on the battlefield while the player can't see them
        GameObject[] teammates = GameObject.FindGameObjectsWithTag("Teammate");
        foreach (GameObject teammate in teammates)
        {
            // This completely hides them and stops all their scripts, 
            // ensuring they don't wander into the cutscene cameras.
            teammate.SetActive(false); 
        }
    }
    // Coroutine to smoothly lower the volume to 0
    private IEnumerator FadeOutMusic(AudioSource audioSource, float fadeTime)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null; // Wait for the next frame
        }
        audioSource.Stop();
        audioSource.volume = startVolume; // Reset the volume so it's ready for the next time it plays
    }
}