using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ShadowManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown shadowDropdown;
    [Header("Localized Strings")]
    [SerializeField] private LocalizedString lowString;
    [SerializeField] private LocalizedString mediumString;
    [SerializeField] private LocalizedString highString;
    private void OnEnable()
    {
        // Subscribe to the language change event
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
    private void Start()
    {
        // Load default to Medium
        int savedIndex = PlayerPrefs.GetInt("ShadowQualityIndex", 1);
        
        if (shadowDropdown != null)
            shadowDropdown.value = savedIndex;

        ApplyShadowQuality(savedIndex);
        UpdateDropdownText();
    }
    private void OnLocaleChanged(Locale locale)
    {
        UpdateDropdownText();
    }

    private void UpdateDropdownText()
    {
        if (shadowDropdown == null) return;

        // Ensure we have exactly 3 options in the dropdown before replacing text to avoid out-of-bounds errors
        if (shadowDropdown.options.Count >= 3)
        {
            shadowDropdown.options[0].text = lowString.GetLocalizedString();
            shadowDropdown.options[1].text = mediumString.GetLocalizedString();
            shadowDropdown.options[2].text = highString.GetLocalizedString();
        }

        // Force the dropdown to visually refresh the UI
        shadowDropdown.RefreshShownValue();
    }
    public void ApplyShadowQuality(int index)
    {
        //  Access the current URP Asset
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        if (urpAsset == null) return;

        // Map the index to specific resolutions
        switch (index)
        {
            case 0: // Low
                urpAsset.mainLightShadowmapResolution = 2048;
                break;
            case 1: // Medium
                urpAsset.mainLightShadowmapResolution = 4096;
                break;
            case 2: // High
                urpAsset.mainLightShadowmapResolution = 8192;
                break;
        }

        // Save for next launch
        PlayerPrefs.SetInt("ShadowQualityIndex", index);
        PlayerPrefs.Save();

        Debug.Log($"Shadow Resolution set to: {urpAsset.mainLightShadowmapResolution}");
    }
}
