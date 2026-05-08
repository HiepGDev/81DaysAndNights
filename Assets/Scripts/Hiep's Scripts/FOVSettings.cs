using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class FOVSettings : MonoBehaviour
{
    [SerializeField] private Slider fovSlider;

    private void Start()
    {
        // load the default value 
        float savedFOV = PlayerPrefs.GetFloat("PlayerFOV", 75f);
        
        if (fovSlider != null)
        {
            fovSlider.minValue = 60f;
            fovSlider.maxValue = 85f;
            fovSlider.value = savedFOV;
        }
    }

    public void OnSliderChanged(float value)
    {
        //  Save the value This "broadcasts" the change to the whole game
        PlayerPrefs.SetFloat("PlayerFOV", value);
        PlayerPrefs.Save();
    }
}
