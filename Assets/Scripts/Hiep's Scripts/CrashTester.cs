using UnityEngine;

public class CrashTester : MonoBehaviour
{
    private void Update()
    {
        // Press F9 to test a soft crash (Exception)
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.LogError("Intentional Error Triggered!");
            throw new System.Exception("Intentional Exception Triggered!");
        }

        // Press F10 to test a hard crash (Kills Editor)
        if (Input.GetKeyDown(KeyCode.F10))
        {
            UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
        }
    }
}