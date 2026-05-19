using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFootstep : MonoBehaviour
{
    [SerializeField] private AudioSource footstepSource;
    [Header("Surface Sounds")]
    [SerializeField] private AudioClip[] dirtSteps;
    [SerializeField] private AudioClip[] stoneSteps;
    [SerializeField] private AudioClip[] woodSteps;
    [SerializeField] private float walkStepInterval = 0.5f; // Time between steps when walking
    [SerializeField] private float sprintStepInterval = 0.3f; // Time between steps when sprinting (shorter = faster)
    [SerializeField] private float crouchStepInterval = 0.7f;
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private LayerMask groundLayers;
    // [SerializeField] private float pitchMin = 0.9f;
    // [SerializeField] private float pitchMax = 1.1f;
    private float stepTimer = 0f;
    // private float lastStepTime = 0f;
    private CharacterController controller;
    private PlayerMovement movement;
    private InputAction moveAction;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        moveAction = InputSystem.actions.FindAction("Move");
        moveAction.Enable();
        if (controller == null) Debug.LogError("PlayerFootstep: CharacterController not found on this GameObject!");
        if (movement == null) Debug.LogError("PlayerFootstep: PlayerMovement not found on this GameObject!");
    }

    void Update()
    {
        HandleFootsteps();
    }
    private void HandleFootsteps()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        bool isMoving = moveValue.magnitude > 0.1f && controller.isGrounded;

        if (isMoving)
        {
            float interval;
            if (movement.isSprinting) interval = sprintStepInterval;
            else if (movement.isCrouching) interval = crouchStepInterval;
            else interval = walkStepInterval;
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                CheckSurfaceAndPlay();
                stepTimer = interval;
            }
        }
        else
        {
            stepTimer = 0.1f; // Reset when not moving
        }
    }

    private void CheckSurfaceAndPlay()
    {
        RaycastHit hit;
        // Raycast straight down from the player's center
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance, groundLayers))
        {
            string surfaceTag = hit.collider.tag;
            AudioClip[] currentClips;
            // Select array based on tag
            switch (surfaceTag)
            {
                case "Stone":
                    currentClips = stoneSteps;
                    break;
                case "Wood":
                    currentClips = woodSteps;
                    break;
                default: // Default to Dirt 
                    currentClips = dirtSteps;
                    break;
            }
            if (currentClips != null && currentClips.Length > 0)
            {
                footstepSource.pitch = Random.Range(0.94f, 1.1f);
                footstepSource.PlayOneShot(currentClips[Random.Range(0, currentClips.Length)]);
            }
        }
    }
}



