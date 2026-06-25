using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessLoader : MonoBehaviour
{
    void Start()
    {
        Volume volume = GetComponent<Volume>();
        if (volume != null && volume.profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            // Load the value saved by the menu
            float savedBrightness = PlayerPrefs.GetFloat("BrightnessValue", 0f);
            // Apply it to this scene's volume
            colorAdjustments.postExposure.value = savedBrightness;
            Debug.Log($"Scene Loaded: Brightness set to {savedBrightness}");
        }
    }
}
