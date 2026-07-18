using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PurrNet;

public class SurvivalPlayerMovement : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    public Transform playerCamera; 
    private CharacterController playerController; 
    InputAction moveAction; 
    InputAction jumpAction; 
    InputAction lookAction; 
    InputAction sprintAction; 
    private InputAction crouchAction; 
    [Header("Movement setting")]
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
    private PlayerStamina staminaSystem;
    private SurvivalPlayerGun playerGun;
    public bool isSprinting;
    public bool canSprint = true;
    public bool canMove = true;
    [Header("Crouch Settings")] 
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float standingHeight = 2.0f; 
    [SerializeField] private float crouchSpeed = 2.0f; 
    [SerializeField] private float crouchTransitionSpeed = 8f; 
    
    public bool isCrouching = false;
    private Vector3 originalCamPos; 
    // center lerping to stop sinking/stutter
    private void Awake()
    {
        playerController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>(); 
        if (staminaSystem == null) staminaSystem = GetComponent<PlayerStamina>();
        if (playerCamera == null)
        {
            // Look for a child named...
            Transform camPivot = transform.Find("Player_Camera"); 
            playerCamera = camPivot;
        }
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
    }
    void Start()
    {
        if (isSpawned && !isOwner)
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            // Disable actions
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            sprintAction?.Disable();
            crouchAction?.Disable();
            enabled = false;
            return;
        }

        DisableCursor();

        if (playerGun == null) playerGun = GetComponentInChildren<SurvivalPlayerGun>();
        if (playerCamera != null) originalCamPos = playerCamera.localPosition;
        lookSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 10f);
    }

    void Update()
    {
        if (isSpawned && !isOwner) return;

        // Toggle cursor lock state when pressing Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        HandleMove();
        HandleLook();
        HandleJump();
        HandleCrouch();
        if (staminaSystem != null)
        {
            staminaSystem.HandleStamina(isSprinting);
        }
    }

    void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void HandleMove()
    {
        // get vector from input 
        Vector2 moveValue = canMove ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        // Check the stamina system to see if we are allowed to sprint
        bool hasEnergy = staminaSystem != null && staminaSystem.HasStamina();
        // sprint is held or not 
        if (sprintAction.IsPressed() && isCrouching)
        {
            isCrouching = false;
        }
        isSprinting = sprintAction.IsPressed() && moveValue.magnitude > 0.1f && moveValue.y > 0.1f 
        && hasEnergy && !isCrouching && canSprint; 
        // Speed selection
        float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

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
        bool isMoving = moveValue.magnitude > 0.1f;
        bool isWalking = isMoving && !isSprinting;
        bool isRunning = isMoving && isSprinting;

        // only set walk/run to true if NOT aiming.
        // if aiming, the animator will naturally fall back to "Idle".
        bool Walk = isWalking && (playerGun == null || !playerGun.isAiming);
        bool Run = isRunning && (playerGun == null || !playerGun.isAiming);
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetBool("isWalk", Walk);
            animator.SetBool("isRun", Run);
        }
    }
    
    void HandleLook()
    {
        // If the cursor is not locked, do not process look input to prevent camera swinging when clicking UI
        if (Cursor.lockState != CursorLockMode.Locked) return;

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
        if (!canMove) return;
        // If the jump button is pressed this frame, reset the jump buffer counter
        if (jumpAction.triggered)
        {
            if (isCrouching)
            {
                isCrouching = false;
                jumpBufferCounter = 0f; // Clear the buffer so they don't jump immediately
                return; // Stop here; the player just stood up

            }
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // If grounded and a jump was buffered, perform the jump
        if (playerController.isGrounded && jumpBufferCounter > 0f && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f; // reset the buffer once the jump is triggered
        }
    }
    private void HandleCrouch()
    {
        if (canMove && crouchAction.triggered)
        {
            isCrouching = !isCrouching;
        } 
        // Smoothly adjust CharacterController height
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(playerController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        playerController.height = newHeight;

        playerController.center = new Vector3(0, newHeight / 2f, 0);
        float heightDifference = standingHeight - newHeight;

        // Adjust camera position relative to crouch
        Vector3 targetCamPos = new Vector3(originalCamPos.x, originalCamPos.y - heightDifference, originalCamPos.z);
        playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetCamPos, Time.deltaTime * crouchTransitionSpeed);
    }
    public void UpdateSensitivity(float newSensitivity)
    {
        lookSensitivity = newSensitivity;
    }
    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;
    }
}
