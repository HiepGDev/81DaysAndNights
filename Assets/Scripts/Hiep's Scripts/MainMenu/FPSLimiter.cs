using TMPro;
using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown fpsDropdown;

    private void Start()
    {
        // Load the saved setting or default to 60 FPS (index 1)
        int savedIndex = PlayerPrefs.GetInt("FPSLimitIndex", 1);
        fpsDropdown.value = savedIndex;
        ApplyFPSLimit(savedIndex);
    }

    // called this in the Dropdown's OnValueChanged event
    public void ApplyFPSLimit(int index)
    {
        // 1. Disable VSync so the custom cap can work
        // 0 = Don't Sync, 1 = Every VBlank, 2 = Every Second VBlank
        QualitySettings.vSyncCount = 0;

        switch (index)
        {
            case 0:
                Application.targetFrameRate = 30;
                break;
            case 1:
                Application.targetFrameRate = 60;
                break;
            case 2:
                Application.targetFrameRate = 90;
                break;
            case 3:
                Application.targetFrameRate = 120;
                break;
            case 4:
                // -1 tells Unity to run as fast as possible (Unlimited)
                Application.targetFrameRate = -1;
                break;
        }

        // Save the setting so it persists when the game restarts
        PlayerPrefs.SetInt("FPSLimitIndex", index);
        PlayerPrefs.Save();
        
        Debug.Log("FPS Limit set to: " + Application.targetFrameRate);
    }
}
