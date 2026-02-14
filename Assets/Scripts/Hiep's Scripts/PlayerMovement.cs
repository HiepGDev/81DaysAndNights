using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class PlayerMovement : MonoBehaviour
{
    public Transform playerCamera;
    private CharacterController playerController;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction lookAction;
    InputAction sprintAction;
    [Header("Movement setting")] 
    // Movement setting
    [SerializeField] float walkSpeed = 0f;
    [SerializeField] float sprintSpeed = 0f;
    [SerializeField] float jumpHeight = 0f;
    [SerializeField] float gravity = 0f;
    // Mouse setiing 
    [SerializeField] float lookSensitivity = 0f;
    private float xRotation = 0f;
    // JumpBuffer settings
    public float jumpBufferTime = 0.2f;  // How long to remember a jump input (in seconds)
    private float jumpBufferCounter = 0f;
    // Gravity 
    private Vector3 velocity;
    [HideInInspector]
    public bool isSprinting;
    
    [Header("Stamina Setting")]
    // stamina 
    [SerializeField] private Image staminaBar;
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float drainRate = 1f;
    [SerializeField] private float regenRate = 0.5f;
    [SerializeField] private float regenDelay = 2f;
    private float currentStamina; 
    private float regenTimer = 0f;

    [Header("Headbob Settings")]
    // Headbob settings
    [SerializeField] private float bobAmplitudeWalk = 0.08f;
    [SerializeField] private float bobAmplitudeSprint = 0.12f;
    [SerializeField] private float bobSpeedWalk = 8f;
    [SerializeField] private float bobSpeedSprint = 12f;
    private Vector3 originalCamPos;
    private float bobTimer = 0f;

    private void Awake()
    {
        DisableCursor();
        playerController = GetComponent<CharacterController>();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
    }
    void Start()
    {
        currentStamina = maxStamina;
        originalCamPos = playerCamera.localPosition;
    }

    void Update()
    {
        HandleMove();
        HandleLook();
        HandleJump();
        HandleStamina();
        HandleHeadbob();
    }

    void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void HandleMove()
    {
        // get vector from input 
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        // sprint is held or not 
        isSprinting = sprintAction.IsPressed() && moveValue.magnitude > 0.1f && moveValue.y > 0.1f && currentStamina > 0f;
        // Convert input to to world-space directions, movement logic.
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 move = transform.right * moveValue.x + transform.forward * moveValue.y;
        playerController.Move(move * currentSpeed * Time.deltaTime);

        // Apply gravity continuously
        velocity.y += gravity * Time.deltaTime;
        playerController.Move(velocity * Time.deltaTime);

        // If grounded, reset vertical velocity (ensuring don't accumulate gravity)
        if (playerController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }
    void HandleLook()
    {
        // Read the mouse delta
        Vector2 lookValue = lookAction.ReadValue<Vector2>();

        // Rotate camera up/down
        xRotation -= lookValue.y * lookSensitivity * Time.deltaTime;
        xRotation = Math.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotate player left/right
        transform.Rotate(Vector3.up * (lookValue.x * lookSensitivity * Time.deltaTime));
    }
    void HandleJump()
    {
        // If the jump button is pressed this frame, reset the jump buffer counter
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // If grounded and a jump was buffered, perform the jump
        if (playerController.isGrounded && jumpBufferCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f; // reset the buffer once the jump is triggered
        }
    }

    void HandleStamina()
    {
        if (isSprinting)
        {
            //drain
            currentStamina -= drainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            regenTimer = 0f; // reset regen delay
        }
        else
        {
            // count up until regenDelay
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
    void HandleHeadbob()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        bool isMoving = moveValue.magnitude > 0.1f && playerController.isGrounded;

        if (isMoving)
        {
            float amplitude = isSprinting ? bobAmplitudeSprint : bobAmplitudeWalk;
            float bobSpeed = isSprinting ? bobSpeedSprint : bobSpeedWalk;
            bobTimer += Time.deltaTime * bobSpeed;
            float bobY = Mathf.Sin(bobTimer) * amplitude;
            playerCamera.localPosition = originalCamPos + new Vector3(0, bobY, 0);
        }
        else
        {
            // Smoothly lerp back to original position when not moving
            playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, originalCamPos, Time.deltaTime * 10f);
        }
    }
}
