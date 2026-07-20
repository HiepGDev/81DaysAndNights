using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PurrNet;
using PhuScene;

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

    private void Awake()
    {
        playerController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>(); 
        if (staminaSystem == null) staminaSystem = GetComponent<PlayerStamina>();
        if (playerCamera == null)
        {
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

        if (playerCamera != null) originalCamPos = playerCamera.localPosition;
        lookSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 10f);
    }

    void Update()
    {
        if (isSpawned && !isOwner) return;

        // Toggle cursor lock state when pressing Alt
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
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
        Vector2 moveValue = canMove ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool hasEnergy = staminaSystem != null && staminaSystem.HasStamina();
        if (sprintAction.IsPressed() && isCrouching)
        {
            isCrouching = false;
        }
        isSprinting = sprintAction.IsPressed() && moveValue.magnitude > 0.1f && moveValue.y > 0.1f 
        && hasEnergy && !isCrouching && canSprint; 
        float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        Vector3 move = transform.right * moveValue.x + transform.forward * moveValue.y;
        playerController.Move(move * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        playerController.Move(velocity * Time.deltaTime);

        if (playerController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        bool isWalking = moveValue.magnitude > 0.1f && !isSprinting;
        bool isRunning = isSprinting;

        // Check aiming state from local player inventory
        bool isAiming = false;
        var inventory = GetComponent<SurvivalInventory>();
        if (inventory != null && inventory.ActiveGun != null)
        {
            isAiming = inventory.ActiveGun.isAiming;
        }

        bool Walk = isWalking && !isAiming;
        bool Run = isRunning && !isAiming;
        
        if (animator != null)
        {
            animator.SetBool("isWalk", Walk);
            animator.SetBool("isRun", Run);
        }
    }

    void HandleLook()
    {
        // Prevent rotating camera when unlocking mouse
        if (Cursor.lockState == CursorLockMode.None)
            return;

        Vector2 lookValue = lookAction.ReadValue<Vector2>();
        float mouseX = lookValue.x * lookSensitivity * Time.deltaTime;
        float mouseY = lookValue.y * lookSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleJump()
    {
        if (jumpAction.triggered && canMove)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (playerController.isGrounded && jumpBufferCounter > 0f && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
        }
    }

    private void HandleCrouch()
    {
        if (canMove && crouchAction.triggered)
        {
            isCrouching = !isCrouching;
        } 
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(playerController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        playerController.height = newHeight;

        playerController.center = new Vector3(0, newHeight / 2f, 0);
        float heightDifference = standingHeight - newHeight;

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
