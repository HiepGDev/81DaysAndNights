using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ScreenModeSwitcher : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [Header("Localized Strings")]
    [SerializeField] private LocalizedString fullscreenString;
    [SerializeField] private LocalizedString borderlessString;
    [SerializeField] private LocalizedString windowedString;
    private void OnEnable()
    {
        // Subscribe to the event so the dropdown updates instantly when the player changes language
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
    private void Start()
    {
        // Load saved mode index or default to Fullscreen (0)
        int savedMode = PlayerPrefs.GetInt("ScreenModeIndex", 0);
        
        if (screenModeDropdown != null)
            screenModeDropdown.value = savedMode;

        ApplyScreenMode(savedMode);
        UpdateDropdownText();
    }
    private void OnLocaleChanged(Locale locale)
    {
        UpdateDropdownText();
    }

    private void UpdateDropdownText()
    {
        if (screenModeDropdown == null) return;

        // Fetch the translated strings
        string full = fullscreenString.GetLocalizedString();
        string border = borderlessString.GetLocalizedString();
        string window = windowedString.GetLocalizedString();

        // Update the dropdown list options 
        // 3 options set up in the Inspector in this exact order
        if (screenModeDropdown.options.Count >= 3)
        {
            screenModeDropdown.options[0].text = full;
            screenModeDropdown.options[1].text = border;
            screenModeDropdown.options[2].text = window;
        }

        // Force the dropdown to visually refresh the currently selected item
        screenModeDropdown.RefreshShownValue();
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
