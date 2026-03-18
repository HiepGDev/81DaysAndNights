using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")] 
    [SerializeField] private Image staminaBar;
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float drainRate = 1f;
    [SerializeField] private float regenRate = 0.5f;
    [SerializeField] private float regenDelay = 2f;

    private float currentStamina;
    private float regenTimer = 0f;

    public float CurrentStamina => currentStamina; // Public way for other scripts to check

    private void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    public void HandleStamina(bool isSprinting)
    {
        if (isSprinting)
        {
            // Drain stamina
            currentStamina -= drainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            regenTimer = 0f;
        }
        else
        {
            // Regen stamina after delay
            if (currentStamina < maxStamina)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= regenDelay)
                {
                    currentStamina += regenRate * Time.deltaTime;
                    currentStamina = Mathf.Min(currentStamina, maxStamina);
                }
            }
        }
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaBar != null)
            staminaBar.fillAmount = currentStamina / maxStamina;
    }

    // Helper method for other scripts to check if we can sprint
    public bool HasStamina()
    {
        return currentStamina > 0.1f;
    }
}
