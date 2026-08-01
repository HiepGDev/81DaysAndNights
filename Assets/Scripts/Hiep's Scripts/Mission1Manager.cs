using System.Collections;
using UnityEngine;

public class Mission1Manager : MonoBehaviour
{
    [Header("Mission Dependencies")]
    [SerializeField] private EnemySpawner enemySpawner;
    
    [Header("End Game Actions")]
    [Tooltip("Place the GameObject holding Final CutsceneManager here. Make sure it starts Disabled in the scene!")]
    [SerializeField] private GameObject finalCutsceneObject;

    private void Start()
    {
        if (enemySpawner != null)
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
        while (!enemySpawner.IsDoneSpawning)
        {
            yield return new WaitForSeconds(1f);
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
                EndMission();
                break; 
            }

            // Wait 1 second before checking again to save CPU performance
            yield return new WaitForSeconds(1f);
        }
    }

    private void EndMission()
    {
        Debug.Log("[MissionManager] All enemies eliminated. Fading to final cutscene!");
        
        // Disable all Teammate AI on the battlefield
        GameObject[] teammates = GameObject.FindGameObjectsWithTag("Teammate");
        foreach (GameObject teammate in teammates)
        {
            // This completely hides them and stops all their scripts, 
            // ensuring they don't wander into the cutscene cameras.
            teammate.SetActive(false); 
        }

        //  Safely trigger the smooth fade transition
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
    }
}