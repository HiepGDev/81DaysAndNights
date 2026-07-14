using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AISyncService : MonoBehaviour
{
    [Header("Web Service Settings")]
    [SerializeField] private string configUrl = "http://127.0.0.1:5093/api/aiconfig";

    [Header("Scriptable Objects to Update")]
    [SerializeField] private EnemySO[] enemyConfigs;

    private void Start()
    {
        if (enemyConfigs != null)
        {
            foreach (var enemySO in enemyConfigs)
            {
                if (enemySO != null)
                {
                    StartCoroutine(FetchConfigForRole(enemySO));
                }
            }
        }
    }

    private IEnumerator FetchConfigForRole(EnemySO enemySO)
    {
        string enemyType = enemySO.defaultMode.ToString();
        string requestUrl = $"{configUrl}?playerId={AIEvaluationTracker.GetPlayerId()}&enemyType={enemyType}";
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"[AISyncService] Successfully loaded latest AI config for {enemyType}: {jsonResponse}");
                
                AiGenerationConfigData data = JsonUtility.FromJson<AiGenerationConfigData>(jsonResponse);
                ApplyConfig(enemySO, data);
            }
            else
            {
                Debug.LogWarning($"[AISyncService] Failed to load config for {enemyType} from server. Using local assets. Error: {webRequest.error}");
            }
        }
    }

    private void ApplyConfig(EnemySO enemySO, AiGenerationConfigData data)
    {
        enemySO.maxHealth = data.base_health;
        enemySO.minSpread = data.min_spread;
        enemySO.maxSpread = data.max_spread;
        enemySO.pushProbability = data.push_probability;
        enemySO.coverProbability = data.cover_probability;
        enemySO.peekCooldown = data.peek_cooldown;
        enemySO.peekDuration = data.peek_duration;

        Debug.Log($"[AISyncService] Applied dynamic DNA (Gen {data.generation_number}) to {enemySO.name} ({enemySO.defaultMode}): Health={enemySO.maxHealth}, MinSpread={enemySO.minSpread}, MaxSpread={enemySO.maxSpread}, PushWeight={enemySO.pushProbability}, PeekCooldown={enemySO.peekCooldown}, PeekDuration={enemySO.peekDuration}");
    }

    [System.Serializable]
    private class AiGenerationConfigData
    {
        public int generation_number;
        public int base_health;
        public float min_spread;
        public float max_spread;
        public float push_probability;
        public float cover_probability;
        public float peek_cooldown;
        public float peek_duration;
    }
}
