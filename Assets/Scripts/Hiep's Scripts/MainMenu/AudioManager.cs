using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Mixer Reference")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Slider References")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // Load saved Default to 0.75 
        float master = PlayerPrefs.GetFloat("MasterVol", 0.75f);
        float music = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVol", 0.75f);

        // Set slider positions
        masterSlider.value = master;
        musicSlider.value = music;
        sfxSlider.value = sfx;

        // Apply to Mixer
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }

    public void SetMasterVolume(float value)
    {
        ApplyVolume("MasterVol", value);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetMusicVolume(float value)
    {
        ApplyVolume("MusicVol", value);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        ApplyVolume("SFXVol", value);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    private void ApplyVolume(string parameterName, float value)
    {
        // Convert 0.0001 to 1.0 into -80dB to 20dB
        // use 0.0001 as min because Log10(0) is mathematically undefined
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat(parameterName, dB);
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }
}
