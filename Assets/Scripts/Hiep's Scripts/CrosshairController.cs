using UnityEngine;
using UnityEngine.InputSystem;

public class CrosshairController : MonoBehaviour
{
    [Header("UI RectTransforms")]
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;

    [Header("Size Settings")]
    [SerializeField] private float normalSize = 15f;
    [SerializeField] private float walkSize = 30f;
    [SerializeField] private float sprintSize = 60f;
    [SerializeField] private float lerpSpeed = 15f;
    private float currentSize;
    private InputAction moveAction;
    [Header("Player References")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerGun gun;
    [SerializeField] private CharacterController controller;
    [SerializeField] private WeaponSwitchManager weaponManager;
    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }
    void Start()
    {
        if (movement == null) movement = FindFirstObjectByType<PlayerMovement>();
        if (gun == null) gun = FindFirstObjectByType<PlayerGun>();
        if (controller == null) controller = FindFirstObjectByType<CharacterController>();
        if (weaponManager == null) weaponManager = FindFirstObjectByType<WeaponSwitchManager>();
        if (movement == null || gun == null || controller == null)
        {
            Debug.LogWarning($"[Crosshair] Missing Player references in {gameObject.name}!");
        }
        currentSize = normalSize;
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isMoving = input.sqrMagnitude > 0.01f;
        // Calculate the target size based on movement only
        float targetSize = normalSize;
        if (isMoving)
        {
            targetSize = movement.isSprinting ? sprintSize : walkSize;
        }

        // Hide crosshair if aiming (ADS) or if the player is holding the rice arm
        bool isAiming = gun != null && gun.isAiming;
        bool holdingRice = weaponManager != null && (weaponManager.IsHoldingRice || weaponManager.IsUnarmed);
        float targetAlpha = (isAiming || holdingRice) ? 0f : 1f;
        UpdateAlpha(targetAlpha);

        //  Smoothly move toward the target size
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * lerpSpeed);
        ApplyPositions();
    }

    // Call this from PlayerGun.cs inside HandleShoot()
    public void FireKick(float amount)
    {
        currentSize += amount;
    }

    private void ApplyPositions()
    {
        top.localPosition = new Vector2(0, currentSize);
        bottom.localPosition = new Vector2(0, -currentSize);
        left.localPosition = new Vector2(-currentSize, 0);
        right.localPosition = new Vector2(currentSize, 0);
    }

    private void UpdateAlpha(float alpha)
    {
        // Smoothly fade the images in/out
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = Mathf.Lerp(group.alpha, alpha, Time.deltaTime * lerpSpeed);
        }
    }
}
