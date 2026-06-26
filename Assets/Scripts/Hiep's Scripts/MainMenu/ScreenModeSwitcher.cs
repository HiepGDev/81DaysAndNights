using TMPro;
using UnityEngine;

public class ScreenModeSwitcher : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    private void Start()
    {
        // Load saved mode index or default to Fullscreen (0)
        int savedMode = PlayerPrefs.GetInt("ScreenModeIndex", 0);
        
        if (screenModeDropdown != null)
            screenModeDropdown.value = savedMode;

        ApplyScreenMode(savedMode);
    }

    public void ApplyScreenMode(int index)
    {
        switch (index)
        {
            case 0: // Fullscreen
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1: // Borderless
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2: // Windowed
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
        }

        // Save for persistence
        PlayerPrefs.SetInt("ScreenModeIndex", index);
        PlayerPrefs.Save();

        Debug.Log("Screen Mode set to: " + index);
    }
}
