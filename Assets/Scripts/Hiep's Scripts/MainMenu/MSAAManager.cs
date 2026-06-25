using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MSAAManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown msaaDropdown; 
    void Start()
    {
        // load default disabled (0)
        int savedIndex = PlayerPrefs.GetInt("MSAAIndex",0);
        if (msaaDropdown != null)
        msaaDropdown.value = savedIndex;

        ApplyMSAA(savedIndex);
    }
    public void ApplyMSAA(int index)
    {
        // Access the current URP Asset
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

        if (urpAsset == null) return;

        // Map the dropdown index to MSAA sample counts
        // 1 = Disabled, 2 = 2x, 4 = 4x, 8 = 8x :))
        switch (index)
        {
            case 0: // Disabled
                urpAsset.msaaSampleCount = 1; 
                break;
            case 1: // 2x
                urpAsset.msaaSampleCount = 2;
                break;
            case 2: // 4x
                urpAsset.msaaSampleCount = 4;
                break;
            case 3: // 8x
                urpAsset.msaaSampleCount = 8;
                break;
        }

        PlayerPrefs.SetInt("MSAAIndex", index);
        PlayerPrefs.Save();

        Debug.Log($"MSAA Sample Count set to: {urpAsset.msaaSampleCount}x");
    }
}

