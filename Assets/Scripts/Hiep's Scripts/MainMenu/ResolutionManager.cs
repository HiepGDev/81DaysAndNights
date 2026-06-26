using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionManager : MonoBehaviour
{
    private const string PREF_RES_INDEX   = "resolutionIndex";
    private const string PREF_FULLSCREEN  = "isFullscreen";

    [SerializeField] private TMP_Dropdown resDropDown;
    [SerializeField] private Toggle fullScreenToggle;

    private List<Resolution> availableResolutions = new List<Resolution>();

    void Start()
    {
        // Build resolution list & dropdown options
        availableResolutions.Clear();
        var options = new List<string>();
        foreach (var res in Screen.resolutions)
        {
            string label = res.width + " x " + res.height;
            if (!options.Contains(label))
            {
                options.Add(label);
                availableResolutions.Add(res);
            }
        }
        resDropDown.ClearOptions();
        resDropDown.AddOptions(options);

        // Load saved settings (or use current desktop res if none)
        int savedIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, -1);
        bool savedFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;

        if (savedIndex >= 0 && savedIndex < availableResolutions.Count)
        {
            ApplyResolution(savedIndex, savedFullscreen);
        }
        else
        {
            // no saved preference → use desktop default
            var desktop = Screen.currentResolution;
            savedIndex = availableResolutions.FindIndex(r => 
                r.width == desktop.width && r.height == desktop.height);
            if (savedIndex < 0) savedIndex = 0;
            ApplyResolution(savedIndex, savedFullscreen);
        }

        // Initialize UI to match
        resDropDown.value = savedIndex;
        resDropDown.RefreshShownValue();
        fullScreenToggle.isOn = savedFullscreen;

        resDropDown.onValueChanged.AddListener(OnResolutionChanged);
        fullScreenToggle.onValueChanged.AddListener(OnFullScreenToggled);
    }

    private void OnResolutionChanged(int dropdownIndex)
    {
        bool isFullscreen = fullScreenToggle.isOn;
        ApplyResolution(dropdownIndex, isFullscreen);
        PlayerPrefs.SetInt(PREF_RES_INDEX, dropdownIndex);
        PlayerPrefs.Save();
    }

    private void OnFullScreenToggled(bool isFullscreen)
    {
        int idx = resDropDown.value;
        ApplyResolution(idx, isFullscreen);
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyResolution(int index, bool fullscreen)
    {
        var res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, fullscreen);
    }
}
