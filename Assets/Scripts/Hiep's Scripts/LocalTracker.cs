using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class LocalTracker : MonoBehaviour
{
    public static LocalTracker Instance;

    [Header("Performance Data")]
    public float currentFPS;
    public float averageFPS;
    public float allocatedMemoryMB;
    
    private int frameCount = 0;
    private float accumulatedFPS = 0f;
    private float fpsUpdateTimer = 0f;

    private string logFilePath;
    private string sessionId;
    private string playerId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Unique IDs for this run and this player
            sessionId = System.Guid.NewGuid().ToString();
            playerId = AIEvaluationTracker.GetPlayerId();

            // Local file setup
            logFilePath = Path.Combine(Application.persistentDataPath, "LocalTracker.txt");
            
            // Log specs locally too!
            string specs = $"OS: {SystemInfo.operatingSystem} | CPU: {SystemInfo.processorType} | GPU: {SystemInfo.graphicsDeviceName} | RAM: {SystemInfo.systemMemorySize}MB";
            File.AppendAllText(logFilePath, $"\n\n--- NEW SESSION STARTED: {System.DateTime.Now} ---\n{specs}\n");
            Debug.Log($"[LocalTracker] Logging data to: {logFilePath}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        TrackPerformance();
    }

    private void TrackPerformance()
    {
        currentFPS = 1.0f / Time.unscaledDeltaTime;
        
        // Skip the first second of the game loading so it doesn't log 1 FPS during startup lag
        fpsUpdateTimer += Time.unscaledDeltaTime;
        if (fpsUpdateTimer > 1.0f)
        {
            accumulatedFPS += currentFPS;
            frameCount++;
            averageFPS = accumulatedFPS / frameCount;
        }

        long memoryBytes = System.GC.GetTotalMemory(false);
        allocatedMemoryMB = memoryBytes / (1024f * 1024f);
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] [{type}] {logString}\n";
        
        if (type == LogType.Exception || type == LogType.Error)
        {
            logEntry += $"STACK TRACE: {stackTrace}\n";

            // SEND CRASH REPORT TO SERVER
            TelemetryService.CrashPayload crashData = new TelemetryService.CrashPayload
            {
                session_id = sessionId,
                player_id = playerId,
                error_type = type.ToString(),
                error_message = logString,
                stack_trace = stackTrace,
                scene_name = SceneManager.GetActiveScene().name
            };

            TelemetryService.SendCrashReport(crashData);
        }

        File.AppendAllText(logFilePath, logEntry);
    }

    private void OnApplicationQuit()
    {
        string summary = $"\n--- SESSION ENDED ---\n";
        summary += $"Average FPS: {averageFPS:F1}\n";
        summary += $"Final Memory Usage: {allocatedMemoryMB:F1} MB\n";
        summary += "--------------------------\n";
        File.AppendAllText(logFilePath, summary);

        // SEND FULL SPECS TO SERVER
        TelemetryService.PerformancePayload perfData = new TelemetryService.PerformancePayload
        {
            session_id = sessionId,
            player_id = playerId,
            avg_fps = averageFPS,
            memory_usage_mb = allocatedMemoryMB,
            screen_resolution = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}",
            cpu_type = SystemInfo.processorType,
            gpu_name = SystemInfo.graphicsDeviceName,
            system_ram_mb = SystemInfo.systemMemorySize,
            os_info = SystemInfo.operatingSystem
        };

        TelemetryService.SendPerformanceData(perfData);
    }
}