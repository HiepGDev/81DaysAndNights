using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShadowManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown shadowDropdown;

    private void Start()
    {
        // Load default to Medium
        int savedIndex = PlayerPrefs.GetInt("ShadowQualityIndex", 1);
        
        if (shadowDropdown != null)
            shadowDropdown.value = savedIndex;

        ApplyShadowQuality(savedIndex);
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
