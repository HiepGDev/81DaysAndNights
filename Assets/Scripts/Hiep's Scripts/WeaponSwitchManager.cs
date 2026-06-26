using UnityEngine;

public class WeaponSwitchManager : MonoBehaviour
{
    public enum HandState
    {
        Gun,
        Rice,
        NormalArm
    }
    [SerializeField] private GameObject gunObject;
    [SerializeField] private GameObject riceObject;
    [SerializeField] private GameObject normalArm;
    [SerializeField] private HandState startingState = HandState.Rice;
    public HandState CurrentState { get; private set; }
    public bool IsHoldingRice => CurrentState == HandState.Rice;
    public bool IsUnarmed => CurrentState == HandState.NormalArm;
    private bool hasInitialized = false;
    private PlayerMovement playerMovement;
    void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();

        // Set the initial state based on preference
        if (!hasInitialized)
        {
            SetHandState(startingState);
        }
    }

    public void SetHandState(HandState newState)
    {
        hasInitialized = true;
        CurrentState = newState;

        // Turn EVERYTHING off first to guarantee no overlapping models
        if (gunObject != null) gunObject.SetActive(false);
        if (riceObject != null) riceObject.SetActive(false);
        if (normalArm != null) normalArm.SetActive(false);

        // Turn on ONLY the requested model and store it temporarily
        GameObject activeModel = null;
        switch (newState)
        {
            case HandState.Gun:
                if (gunObject != null) { gunObject.SetActive(true); activeModel = gunObject; }
                Debug.Log("Holding Gun");
                break;
            case HandState.Rice:
                if (riceObject != null) { riceObject.SetActive(true); activeModel = riceObject; }
                Debug.Log("Holding Rice");
                break;
            case HandState.NormalArm:
                if (normalArm != null) { normalArm.SetActive(true); activeModel = normalArm; }
                Debug.Log("Holding Normal Arm");
                break;
        }
        if (playerMovement != null && activeModel != null)
        {
            Animator newAnim = activeModel.GetComponentInChildren<Animator>(true); 
            
            if (newAnim != null)
            {
                playerMovement.SetAnimator(newAnim);
                // Automatically tell the animator which weapon is held 
                newAnim.SetInteger("WeaponState", (int)newState);
            }
            else
            {
                Debug.LogWarning("[WeaponSwitchManager] Could not find an Animator on " + activeModel.name);
            }
        }
    }

    // --- Helper methods to call easily from Cutscene Events or UI Buttons ---
    public void SwitchToGun()
    {
        SetHandState(HandState.Gun);
    }

    public void SwitchToRice()
    {
        SetHandState(HandState.Rice);
    }

    public void SwitchToNormalArm()
    {
        SetHandState(HandState.NormalArm);
    }
}
