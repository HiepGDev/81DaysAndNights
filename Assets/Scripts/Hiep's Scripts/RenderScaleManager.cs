using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class RenderScaleManager : MonoBehaviour
{
    [SerializeField] private Slider scaleSlider;
    [SerializeField] private TextMeshProUGUI scaleValueText;

    private void Start()
    {
        // Load the saved value (Default 1.2)
        float savedScale = PlayerPrefs.GetFloat("RenderScale", 1.2f);
        
        //  Setup the UI
        if (scaleSlider != null)
        {
            scaleSlider.minValue = 0.7f;
            scaleSlider.maxValue = 2.0f;
            scaleSlider.value = savedScale;
        }

        ApplyScale(savedScale);
    }

    public void OnSliderChanged(float value)
    {
        // Round to 1 decimal place for the UI
        float roundedValue = Mathf.Round(value * 10f) / 10f;
        ApplyScale(roundedValue);
        
        // Save it so it's there next time the game opens
        PlayerPrefs.SetFloat("RenderScale", roundedValue);
        PlayerPrefs.Save();
    }

    private void ApplyScale(float value)
    {
        // Access the global pipeline asset
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

        if (urpAsset != null)
        {
            urpAsset.renderScale = value;
        }

        if (scaleValueText != null)
            scaleValueText.text = value.ToString("F1") + "x";
    }
}
