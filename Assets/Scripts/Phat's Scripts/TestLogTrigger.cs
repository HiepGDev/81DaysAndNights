using UnityEngine;

public class TestLogTrigger : MonoBehaviour
{
    [Header("Testing Parameters")]
    [SerializeField] private int stageNumber = 999;
    [SerializeField] private bool simulatePlayerDied = false;

    void Update()
    {
        // Press T key to simulate completing the test scene and uploading logs
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[TestLogTrigger] Simulating test completion. Uploading logs...");
            
            // Upload current session logs
            AIEvaluationTracker.SubmitSessionLogs(simulatePlayerDied, stageNumber);
        }
    }
}
