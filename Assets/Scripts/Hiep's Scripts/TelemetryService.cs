using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TelemetryService : MonoBehaviour
{
    private const string TelemetryUrl = "https://81dnn-rl.azurewebsites.net/api/telemetry/performance";
    private const string CrashUrl = "https://81dnn-rl.azurewebsites.net/api/telemetry/crash";

    public static void SendPerformanceData(PerformancePayload data)
    {
        string json = JsonUtility.ToJson(data);
        CreateDispatcher().StartCoroutine(PostRequest(TelemetryUrl, json));
    }

    public static void SendCrashReport(CrashPayload data)
    {
        string json = JsonUtility.ToJson(data);
        CreateDispatcher().StartCoroutine(PostRequest(CrashUrl, json));
    }

    private static TelemetryService CreateDispatcher()
    {
        GameObject go = new GameObject("TelemetryDispatcher");
        DontDestroyOnLoad(go);
        return go.AddComponent<TelemetryService>();
    }

    private static IEnumerator PostRequest(string url, string json)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[TelemetryService] Failed to send payload: {request.error}");
            }
        }
    }

    [System.Serializable]
    public class PerformancePayload
    {
        public string session_id;
        public string player_id;
        public float avg_fps;
        public float memory_usage_mb;
        public string screen_resolution;
        public string cpu_type;
        public string gpu_name;
        public int system_ram_mb;
        public string os_info;
    }

    [System.Serializable]
    public class CrashPayload
    {
        public string session_id;
        public string player_id;
        public string error_type;
        public string error_message;
        public string stack_trace;
        public string scene_name;
    }
}