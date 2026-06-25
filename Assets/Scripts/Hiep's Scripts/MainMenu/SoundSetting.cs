using UnityEngine;
using UnityEngine.UI;

public class SoundSetting : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    private const string VolumeKey = "musicVolume";
    private const float DefaultVolume = 1f;

    void Start()
    {
        // Load saved volume (or default)
        float saved = PlayerPrefs.HasKey(VolumeKey) ? PlayerPrefs.GetFloat(VolumeKey) : DefaultVolume;

        // Set slider value without firing OnValueChanged callbacks
        volumeSlider.SetValueWithoutNotify(saved);

        // Apply to global volume
        AudioListener.volume = saved;

        // Add listener for user changes
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float newValue)
    {
        // Apply immediately
        AudioListener.volume = newValue;

        // Store in PlayerPrefs and flush to disk right away
        PlayerPrefs.SetFloat(VolumeKey, newValue);
        PlayerPrefs.Save();
    }

    void OnDisable()
    {
        // Remove listener to avoid leaks
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        // Ensure prefs are saved when component gets disabled (safety)
        PlayerPrefs.Save();
    }
}
