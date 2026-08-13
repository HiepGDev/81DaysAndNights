using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private TextMeshProUGUI interactUI; // "Press E" text
    // [SerializeField] private GameObject riceInHand;
    [SerializeField] private Animator riceAnimator;
    [SerializeField] private LocalizedString localizedPressTemplate;
    [Header("Player Voice")]
    [SerializeField] private AudioSource playerVoiceSource; 
    [SerializeField] private AudioClip[] giveFoodClips;
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
                string actionText = interactable.GetInteractText();
                if (!string.IsNullOrEmpty(actionText))
                {
                    interactUI.text = localizedPressTemplate.GetLocalizedString(currentKeyName, actionText);
                    interactUI.gameObject.SetActive(true);

                    if (interactAction != null && interactAction.WasPressedThisFrame())
                    {
                        //  NPC to play "Receive" animation
                        interactable.Interact();
                        //  Tell the Player's hand to play the "Give" animation
                        if (riceAnimator != null && !hit.collider.TryGetComponent(out AmmoBox _))
                        {
                            riceAnimator.SetTrigger("GiveFood");
                            PlayRandomGiveFoodVoice();
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
    private void PlayRandomGiveFoodVoice()
    {
        // Ensure AudioSource exist and at least one clip assigned
        if (playerVoiceSource != null && giveFoodClips != null && giveFoodClips.Length > 0)
        {
            // Pick a random number between 0 and the total number of clips
            int randomIndex = Random.Range(0, giveFoodClips.Length);
            // Play the randomly selected clip
            playerVoiceSource.PlayOneShot(giveFoodClips[randomIndex]);
        }
    }
}
