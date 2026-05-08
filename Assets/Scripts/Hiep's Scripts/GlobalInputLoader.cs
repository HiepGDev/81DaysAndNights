using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputLoader : MonoBehaviour
{
    void Awake()
    {
        // Load the string from the computer's memory
        string savedOverrides = PlayerPrefs.GetString("InputOverrides");

        // If the string isn't empty, tell the rulebook to use the new keys
        if (!string.IsNullOrEmpty(savedOverrides))
        {
            // This applies to Movement, Gun, and any other action i think :3
            InputSystem.actions.LoadBindingOverridesFromJson(savedOverrides);
        }
    }
}
