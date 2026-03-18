using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFootstep : MonoBehaviour
{
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepSound;
    [SerializeField] private float walkStepInterval = 0.5f; // Time between steps when walking
    [SerializeField] private float sprintStepInterval = 0.3f; // Time between steps when sprinting (shorter = faster)
    [SerializeField] private float crouchStepInterval = 0.7f;
    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;
    private float stepTimer = 0f;
    private float lastStepTime = 0f;
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
                if (Time.time > lastStepTime + 0.2f)
                {
                    PlayFootstep();
                    lastStepTime = Time.time; // Reset timer
                }
                stepTimer = interval;
            }
        }
        else
        {
            stepTimer = 0.1f; // Reset when not moving
        }
    }

    private void PlayFootstep()
    {
        if (footstepSource != null && footstepSound.Length > 0)
        {
            footstepSource.pitch = Random.Range(pitchMin, pitchMax);
            AudioClip clip = footstepSound[Random.Range(0, footstepSound.Length)];
            footstepSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogError("PlayerFootstep: Cannot play footstep - AudioSource or clips missing!");
        }
    }
} 


