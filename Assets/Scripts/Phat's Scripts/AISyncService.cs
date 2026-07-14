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
        StartCoroutine(FetchLatestAIConfig());
    }

    private IEnumerator FetchLatestAIConfig()
    {
        string requestUrl = $"{configUrl}?playerId={AIEvaluationTracker.GetPlayerId()}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log("[AISyncService] Successfully loaded latest AI configuration: " + jsonResponse);
                
                AiGenerationConfigData data = JsonUtility.FromJson<AiGenerationConfigData>(jsonResponse);
                ApplyConfig(data);
            }
            else
            {
                Debug.LogWarning("[AISyncService] Failed to load config from server, using default local assets. Error: " + webRequest.error);
            }
        }
    }

    private void ApplyConfig(AiGenerationConfigData data)
    {
        if (enemyConfigs == null) return;

        foreach (var enemySO in enemyConfigs)
        {
            if (enemySO == null) continue;

            // Apply the globally evolved traits to the ScriptableObject instances
            enemySO.maxHealth = data.base_health;
            enemySO.minSpread = data.min_spread;
            enemySO.maxSpread = data.max_spread;
            enemySO.pushProbability = data.push_probability;
            enemySO.coverProbability = data.cover_probability;

            Debug.Log($"[AISyncService] Applied dynamic DNA (Gen {data.generation_number}) to {enemySO.name}: Health={enemySO.maxHealth}, MinSpread={enemySO.minSpread}, MaxSpread={enemySO.maxSpread}, PushWeight={enemySO.pushProbability}");
        }
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
    }
}
