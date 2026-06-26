using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VSyncSwitcher : MonoBehaviour
{
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown fpsDropdown;

    private void Start()
    {
        // Load saved state (0 = Off, 1 = On). Default to Off (0)
        int savedVsync = PlayerPrefs.GetInt("VSyncSetting", 0);
        
        if (vsyncToggle != null)
            vsyncToggle.isOn = (savedVsync == 1);

        ApplyVSync(vsyncToggle.isOn);
    }

    public void ApplyVSync(bool isEnabled)
    {
        // QualitySettings.vSyncCount: 
        // 0 = No VSync (Custom FPS Cap works)
        // 1 = Every VBlank (Locked to monitor Hz)
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;

        // Save for next session
        PlayerPrefs.SetInt("VSyncSetting", isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("VSync is now: " + (isEnabled ? "Enabled" : "Disabled"));
    }
}

