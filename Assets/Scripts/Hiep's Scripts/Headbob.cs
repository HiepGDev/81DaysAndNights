using UnityEngine;

public class Headbob : MonoBehaviour
{
    [SerializeField] private PlayerMovement campaignMovement;
    [SerializeField] private CharacterController characterController;

    [Header("Headbob Settings")]
    [SerializeField] private float walkAmplitude = 0.08f; // Vertical bob height for walking
    [SerializeField] private float sprintAmplitude = 0.12f; // Vertical bob height for sprinting 
    [SerializeField] private float walkFrequency = 8f;
    [SerializeField] private float sprintFrequency = 12f;
    [SerializeField] private float returnSpeed = 10f;
    [SerializeField] private float moveThreshold = 0.1f;

    private Vector3 originalPosition;
    private float timer;
    private Vector3 previousPosition;

    private void Start()
    {
        characterController = GetComponentInParent<CharacterController>();
        campaignMovement = GetComponentInParent<PlayerMovement>();
        characterController = GetComponentInParent<CharacterController>();
        // Store original local position of this camera (for resetting bob)
        originalPosition = transform.localPosition;
        // Initialize previous position for velocity calculation
        previousPosition = characterController.transform.position;
    }
    private bool IsSprinting()
    {
        if (campaignMovement != null) return campaignMovement.isSprinting;
        // if (survivalMovement != null) return survivalMovement.isSprinting;
        return false; 
    }

    private void LateUpdate()
    {
        // Calculate horizontal velocity
        Vector3 currentPosition = characterController.transform.position;
        Vector3 deltaPosition = currentPosition - previousPosition;
        float horizontalVelocity = new Vector2(deltaPosition.x, deltaPosition.z).magnitude / Time.deltaTime;
        previousPosition = currentPosition;
        // Check if player is grounded AND moving fast enough
        bool isGrounded = characterController.isGrounded;
        bool isMoving = isGrounded && horizontalVelocity > moveThreshold;

        if (isMoving)
        {
            bool isSprinting = IsSprinting();
            float amplitude = isSprinting ? sprintAmplitude : walkAmplitude;
            float frequency = isSprinting ? sprintFrequency : walkFrequency;

            // Accumulate timer and calculate sine-based vertical bob
            timer += Time.deltaTime * frequency;
            float bobY = Mathf.Sin(timer) * amplitude;
            // Apply bob only offset Y
            transform.localPosition = originalPosition + new Vector3(0f, bobY, 0f);
        }
        else
        {
            // Smoothly interpolate back to original position (no snap)
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, returnSpeed * Time.deltaTime);
        }
    }
}
