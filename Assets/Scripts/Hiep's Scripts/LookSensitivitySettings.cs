using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LookSensitivitySettings : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Slider sensitivitySlider;
    // [SerializeField] private TextMeshProUGUI valueText;
    void Start()
    {
        // Load the saved value (matching the default in PlayerMovement)
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 10f);
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1f;  
            sensitivitySlider.maxValue = 50f; 
            sensitivitySlider.value = savedSensitivity;
        }
        ApplySensitivity(savedSensitivity);
    }
    public void ApplySensitivity(float newValue)
    {
        // Update the player script
        if (playerMovement != null)
        {
            playerMovement.UpdateSensitivity(newValue);
        }

        // Update the UI text 
        // if (valueText != null)
        // {
        //     valueText.text = newValue.ToString("F1"); // Rounds to 1 decimal place
        // }

        // Save the setting
        PlayerPrefs.SetFloat("MouseSensitivity", newValue);
        PlayerPrefs.Save();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
