using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class BrightnessSettings : MonoBehaviour
{
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Volume globalVolume; 

    private ColorAdjustments colorAdjustments;

    private void Start()
    {
        // 1. Get the Color Adjustments from the Volume Profile
        if (globalVolume.profile.TryGet(out colorAdjustments))
        {
            // 2. Load saved brightness or default to 0 (Standard)
            float savedBrightness = PlayerPrefs.GetFloat("BrightnessValue", 0f);
            
            if (brightnessSlider != null)
            {
                brightnessSlider.minValue = -0.4f; // Darker
                brightnessSlider.maxValue = 0.8f;  // Brighter
                brightnessSlider.value = savedBrightness;
            }

            ApplyBrightness(savedBrightness);
        }
    }

    public void ApplyBrightness(float value)
    {
        if (colorAdjustments != null)
        {
            // Update the Post Exposure value in real-time
            colorAdjustments.postExposure.value = value;
        }

        // Save for next session
        PlayerPrefs.SetFloat("BrightnessValue", value);
        PlayerPrefs.Save();
    }
}
