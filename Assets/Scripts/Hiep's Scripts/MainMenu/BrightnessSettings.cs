using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class BrightnessSettings : MonoBehaviour
{
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Volume menuVolume; 

    private ColorAdjustments colorAdjustments;

    private void Start()
    {
        // Get the Color Adjustments from the Volume Profile
        // Load saved brightness or default to 0 (Standard)
        float savedBrightness = PlayerPrefs.GetFloat("BrightnessValue", 0f);
            
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -0.4f; // Darker
            brightnessSlider.maxValue = 0.8f;  // Brighter
            brightnessSlider.value = savedBrightness;
        }
        if (menuVolume != null && menuVolume.profile.TryGet(out colorAdjustments))
        {
            ApplyBrightness(savedBrightness);
        }
    }
    public void OnSliderChanged(float value)
    {
        ApplyBrightness(value);
        
        // Save the value so other scenes can read it
        PlayerPrefs.SetFloat("BrightnessValue", value);
        PlayerPrefs.Save();
    }
    public void ApplyBrightness(float value)
    {
        if (colorAdjustments != null)
        {
            // Update the Post Exposure value in real-time
            colorAdjustments.postExposure.value = value;
        }
    }
}
