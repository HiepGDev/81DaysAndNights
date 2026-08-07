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
    [Tooltip("If the player's physical speed is above this number, the crosshair expands to sprint size.")]
    [SerializeField] private float sprintSpeedThreshold = 6f;
    private float currentSize;
    private InputAction moveAction;
    [Header("Player References")]
    // [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerGun gun;
    [SerializeField] private CharacterController controller;
    // Added to manually track speed
    private Vector3 lastPosition;
    void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }
    void Start()
    {
        // if (movement == null) movement = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (gun == null) gun = FindFirstObjectByType<PlayerGun>(FindObjectsInactive.Include);
        if (controller == null) controller = FindFirstObjectByType<CharacterController>();
        if ( gun == null || controller == null)
        {
            Debug.LogWarning($"[Crosshair] Missing Player references in {gameObject.name}!");
        }
        currentSize = normalSize;
        if (controller != null)
        {
            lastPosition = controller.transform.position;
        }
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isInputtingMovement = input.sqrMagnitude > 0.01f;
        // Calculate the target size based on movement only
        float targetSize = normalSize;
        if (isInputtingMovement && controller != null)
        {
            Vector3 currentPos = controller.transform.position;
            Vector3 actualVelocity = (currentPos - lastPosition) / Time.deltaTime;
            
            // Flatten the Y axis so jumping/falling doesn't trigger the sprint crosshair
            Vector3 flatVelocity = new Vector3(actualVelocity.x, 0, actualVelocity.z);
            float currentSpeed = flatVelocity.magnitude;

            // Determine target size
            targetSize = (currentSpeed >= sprintSpeedThreshold) ? sprintSize : walkSize;
        }
        if (controller != null)
        {
            lastPosition = controller.transform.position;
        }
        
        PlayerGun activeGun = GetActiveGun();
        // Hide crosshair if aiming (ADS) or if the player is holding the rice arm
        bool isAiming = activeGun != null && activeGun.isAiming;
        bool isUnarmed = activeGun == null;
        float targetAlpha = (isAiming ||  isUnarmed) ? 0f : 1f;
        UpdateAlpha(targetAlpha);

        //  Smoothly move toward the target size
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * lerpSpeed);
        ApplyPositions();



        top.gameObject.SetActive(Cursor.lockState == CursorLockMode.Locked);
        bottom.gameObject.SetActive(Cursor.lockState == CursorLockMode.Locked);
        left.gameObject.SetActive(Cursor.lockState == CursorLockMode.Locked);
        right.gameObject.SetActive(Cursor.lockState == CursorLockMode.Locked);
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
    private PlayerGun GetActiveGun()
    {
        if (gun == null || !gun.gameObject.activeInHierarchy)
        {
            // Find the active gun in the scene
            gun = Object.FindFirstObjectByType<PlayerGun>(); 
        }
        return gun;
    }
}
