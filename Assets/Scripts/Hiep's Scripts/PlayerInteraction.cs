using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private TextMeshProUGUI interactUI; // "Press E" text
    // [SerializeField] private GameObject riceInHand;
    [SerializeField] private Animator riceAnimator;
    private InputAction interactAction;
    private string currentKeyName = "E";
    void Awake()
    {
        // Find the action exactly as named in Action Map
        interactAction = InputSystem.actions.FindAction("Interact");
        interactAction.Enable();
        currentKeyName = interactAction.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions); 
    }

    void Update()
    {
        //  Check if player look at something interactable
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                // Only show UI if the NPC hasn't been fed yet
                string text = interactable.GetInteractText();
                if (!string.IsNullOrEmpty(text))
                {
                    interactUI.text = $"Press {currentKeyName} to {text}";
                    interactUI.gameObject.SetActive(true);

                    if (interactAction != null && interactAction.WasPressedThisFrame())
                    {
                        //  NPC to play "Receive" animation
                        interactable.Interact();
                        //  Tell the Player's hand to play the "Give" animation
                        if (riceAnimator != null)
                        {
                            riceAnimator.SetTrigger("GiveFood");
                        }
                    }
                }
                else
                {
                    interactUI.gameObject.SetActive(false);
                }
                return;
            }
        }
        interactUI.gameObject.SetActive(false);
    }
}
