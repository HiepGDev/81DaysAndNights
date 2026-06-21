using UnityEngine;

public class FriendlyNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject rice;
    [SerializeField] private AudioSource voiceSource; // for dialogue
    [SerializeField] private AudioClip thankYouClip;

    [SerializeField] private Transform headBone;      
    [SerializeField] private float maxLookAngle = 60f; // Prevent turning past a natural shoulder line
    [SerializeField] private float lookSpeed = 4f;

    private Transform playerTransform;
    private float currentLookWeight = 0f; // Blends between animation and look-at target
    private bool hasReceivedFood = false;
    private bool isReceivingFoodState = false;
    private PlayerMovement playerMovement;
    void Start()
    {
        // Ensure the rice is hidden when the game starts
        if (rice != null) rice.SetActive(false);
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
        }
    }
    void LateUpdate()
    {
        if (headBone == null || playerTransform == null) return;

        //  Calculate direction vector to the player's eye level
        Vector3 targetEyeLevel = playerTransform.position + new Vector3(0, 1.5f, 0); 
        Vector3 directionToPlayer = targetEyeLevel - headBone.position;

        // Calculate the flat horizontal angle relative to the NPC's body forward direction
        Vector3 flatForward = transform.forward;
        Vector3 flatTargetDir = directionToPlayer;
        flatForward.y = 0;
        flatTargetDir.y = 0;
        
        float angleToPlayer = Vector3.Angle(flatForward, flatTargetDir);

        //  track the player if the angle is safe AND the NPC is actively in the food-receiving state
        bool shouldLook = isReceivingFoodState && (angleToPlayer <= maxLookAngle);

        // Smoothly blend the tracking weight up or down to avoid sudden neck snapping
        currentLookWeight = Mathf.Lerp(currentLookWeight, shouldLook ? 1f : 0f, Time.deltaTime * lookSpeed);

        if (currentLookWeight > 0.01f)
        {
            // Store the keyframe animation pose as our starting position
            Quaternion animatedRotation = headBone.rotation;

            // Calculate the look rotation facing the player
            Quaternion targetLookRotation = Quaternion.LookRotation(directionToPlayer, transform.up);

            // Blend smoothly between the animated frame and the procedural look calculation
            headBone.rotation = Quaternion.Slerp(animatedRotation, targetLookRotation, currentLookWeight);
        }
    }
    public string GetInteractText()
    {
        return hasReceivedFood ? "" : "Give Rice";
    }

    public void Interact()
    {
        if (hasReceivedFood) return;

        if (animator != null)
        {
            animator.SetTrigger("ReceiveFood");
        }

        hasReceivedFood = true;
        isReceivingFoodState = true;
        if (playerMovement != null) playerMovement.canMove = false;
        Debug.Log("Soldier received rice.");
    }
    public void SetRiceVisible(int visible)
    {
        if (rice != null)
        {
            rice.SetActive(visible == 1);
        }
    }

    public void PlayThankYouVoice()
    {
        if (voiceSource != null && thankYouClip != null)
        {
            voiceSource.PlayOneShot(thankYouClip);
        }
    }
    public void StopLookingAtPlayer()
    {
        isReceivingFoodState = false;
        if (playerMovement != null) playerMovement.canMove = true;
        Debug.Log("Food animation finished. Returning to normal idle.");
    }
}
