using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SSAOManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Toggle aoToggle;

    [Header("Renderer Reference")]
    [SerializeField] private UniversalRendererData rendererData; 
    private ScriptableRendererFeature aoFeature;

    private void Start()
    {
        //  Search for the feature by its TYPE
        if (rendererData != null)
        {
            aoFeature = rendererData.rendererFeatures.FirstOrDefault(f => f is ScreenSpaceAmbientOcclusion);
        }

        if (aoFeature == null)
        {
            Debug.LogError($"[SSAO] Could not find an SSAO Feature in {rendererData.name}! Make sure you added it to the Renderer Features list.");
            return;
        }

        //  Load saved state (default is On)
        int savedState = PlayerPrefs.GetInt("AOEnabled", 1);
        bool isEnabled = (savedState == 1);

        if (aoToggle != null)
            aoToggle.isOn = isEnabled;

        ApplyAO(isEnabled);
    }

    public void OnToggleChanged(bool isEnabled)
    {
        ApplyAO(isEnabled);
        
        PlayerPrefs.SetInt("AOEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyAO(bool isEnabled)
{
    if (aoFeature != null)
    {
        aoFeature.SetActive(isEnabled);

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(rendererData);
        #endif
        
        Debug.Log($"[SSAO] Ambient Occlusion is now {(isEnabled ? "ON" : "OFF")}");
    }
}
}
