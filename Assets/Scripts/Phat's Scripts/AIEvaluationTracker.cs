using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class AIEvaluationTracker : MonoBehaviour
{
    private static List<AiCombatLogData> collectedLogs = new List<AiCombatLogData>();
    private static string sessionId = System.Guid.NewGuid().ToString();
    private static bool sessionEnded = false;
    private static string playerId;

    public static string GetPlayerId()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = PlayerPrefs.GetString("AI_PlayerID", "");
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("AI_PlayerID", playerId);
                PlayerPrefs.Save();
            }
        }
        return playerId;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSceneEvents()
    {
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        sessionEnded = false;
    }

    private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene currentScene)
    {
        // Automatically submit logs when transitioning scenes (next stage)
        if (collectedLogs.Count > 0)
        {
            SubmitSessionLogs(false, currentScene.buildIndex);
        }
    }

    private EnemyHealth health;
    private EnemyShooting shooting;
    private EnemyBehaviorAgent behavior;

    private float timeAlive = 0f;
    private int initialHealth;
    private bool logRegistered = false;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        shooting = GetComponent<EnemyShooting>();
        behavior = GetComponent<EnemyBehaviorAgent>();
    }

    private void Start()
    {
        if (health != null)
        {
            initialHealth = health.MaxHealth;
        }
    }

    private void Update()
    {
        timeAlive += Time.deltaTime;

        // Register log once when they die
        if (health != null && health.CurrentHealth <= 0 && !logRegistered)
        {
            RegisterLog(true);
        }
    }

    private void OnDestroy()
    {
        // If they get destroyed (e.g. stage transition) and haven't logged yet, register them as alive
        if (!logRegistered)
        {
            RegisterLog(false);
        }
    }

    private void RegisterLog(bool died, bool force = false)
    {
        if (sessionEnded && !force) return;
        logRegistered = true;
        
        int damageTaken = health != null ? (initialHealth - health.CurrentHealth) : 0;
        int damageDealt = shooting != null ? shooting.totalDamageDealt : 0;
        bool diedInCover = died && (behavior != null && behavior.IsInCover);
        string enemyType = behavior != null ? behavior.currentMode.ToString() : "Unknown";

        Debug.Log($"[AIEvaluationTracker] RegisterLog for {gameObject.name}: Type={enemyType}, Died={died}, timeAlive={timeAlive}s, dmgDealt={damageDealt}, dmgTaken={damageTaken}");

        var log = new AiCombatLogData
        {
            session_id = sessionId,
            enemy_type = enemyType,
            damage_dealt = damageDealt,
            damage_taken = damageTaken,
            time_alive = timeAlive,
            died_in_cover = diedInCover,
            stage_number = 1, // Default stage 1, can be overridden globally
            player_died = false, // Will be updated by SubmitLogs
            player_id = GetPlayerId()
        };

        lock (collectedLogs)
        {
            collectedLogs.Add(log);
        }
    }

    // STATIC INTERFACE FOR GAME MANAGERS TO TRIGGER POST
    public static void SubmitSessionLogs(bool playerDied, int stageNumber, string submitUrl = "http://127.0.0.1:5093/api/aistats/session-results")
    {
        if (sessionEnded) return;
        sessionEnded = true;

        Debug.Log($"[AIEvaluationTracker] SubmitSessionLogs triggered: playerDied={playerDied}, stage={stageNumber}, initial queue count={collectedLogs.Count}");

        // Gather any remaining active trackers that haven't registered yet
        AIEvaluationTracker[] activeTrackers = Object.FindObjectsByType<AIEvaluationTracker>(FindObjectsSortMode.None);
        Debug.Log($"[AIEvaluationTracker] Found {activeTrackers.Length} active enemy trackers in scene.");

        foreach (var tracker in activeTrackers)
        {
            if (!tracker.logRegistered)
            {
                tracker.RegisterLog(false, true);
            }
        }

        // Set final session results
        foreach (var log in collectedLogs)
        {
            log.player_died = playerDied;
            log.stage_number = stageNumber;
        }

        // Run coroutine via a temporary dummy GameObject or a static dispatcher
        if (collectedLogs.Count > 0)
        {
            GameObject dispatcher = new GameObject("LogDispatcher");
            Object.DontDestroyOnLoad(dispatcher);
            dispatcher.AddComponent<LogDispatcherComponent>().StartSubmit(submitUrl, collectedLogs);
            collectedLogs = new List<AiCombatLogData>(); // Reset for next session
            sessionId = System.Guid.NewGuid().ToString(); // New session id
        }
    }

    [System.Serializable]
    public class AiCombatLogData
    {
        public string session_id;
        public string enemy_type;
        public int damage_dealt;
        public int damage_taken;
        public float time_alive;
        public bool died_in_cover;
        public int stage_number;
        public bool player_died;
        public string player_id;
    }

    private class LogDispatcherComponent : MonoBehaviour
    {
        public void StartSubmit(string url, List<AiCombatLogData> data)
        {
            StartCoroutine(PostLogs(url, data));
        }

        private IEnumerator PostLogs(string url, List<AiCombatLogData> data)
        {
            string json = JsonHelper.ToJsonArray(data);
            Debug.Log("[AIEvaluationTracker] Sending session combat logs to server: " + json);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[AIEvaluationTracker] Successfully submitted combat logs to server!");
                }
                else
                {
                    Debug.LogWarning("[AIEvaluationTracker] Failed to send combat logs: " + request.error);
                }
            }

            Destroy(gameObject); // Cleanup
        }
    }
}

// Helper because Unity's JsonUtility does not support raw array serialization natively
public static class JsonHelper
{
    public static string ToJsonArray<T>(List<T> list)
    {
        string wrapper = "[\n";
        for (int i = 0; i < list.Count; i++)
        {
            wrapper += JsonUtility.ToJson(list[i]);
            if (i < list.Count - 1) wrapper += ",\n";
        }
        wrapper += "\n]";
        return wrapper;
    }
}
