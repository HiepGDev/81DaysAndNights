using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindButton : MonoBehaviour
{
    [SerializeField] private string actionName; 
    [SerializeField] private string bindingPartName;
    [SerializeField] private TMP_Text displayAttribute; // The text on the button showing the key
    private InputAction action;
    private int bindingIndex;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    void Start()
    {
        // Find the action from your global actions
        action = InputSystem.actions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Action {actionName} not found! Check the spelling in the Inspector.");
            return;
        }
        FindBindingIndex();
        UpdateDisplay();
    }
    private void FindBindingIndex()
    {
        // If it's a simple button (not a composite), index is 0
        if (string.IsNullOrEmpty(bindingPartName))
        {
            bindingIndex = 0;
            return;
        }
        // Look through bindings to find the one named "up", "down", etc.
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].name.Equals(bindingPartName, System.StringComparison.OrdinalIgnoreCase))
            {
                bindingIndex = i;
                return;
            }
        }
        Debug.LogWarning($"Part '{bindingPartName}' not found in action '{actionName}'. Defaulting to index 0.");
        bindingIndex = 0;
    }

    public void StartRebinding()
    {
        if (action == null) return;
        displayAttribute.text = "Waiting for input...";
        // Disable the action while remapping it
        action.Disable();

        rebindOperation = action.PerformInteractiveRebinding(bindingIndex) 
            .WithControlsExcluding("<Mouse>/delta") 
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebind())
            .Start();
    }

    private void FinishRebind()
    {
        UpdateDisplay();
        rebindOperation.Dispose();
        action.Enable();

        // SAVE THE OVERRIDES (Very Important!)
        string overrides = InputSystem.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputOverrides", overrides);
        PlayerPrefs.Save();
    }

    private void UpdateDisplay()
    {
        if (action != null)
        {
            // this will show only "W" instead of the whole string!
            displayAttribute.text = action.GetBindingDisplayString(bindingIndex);
        }
    }
}
