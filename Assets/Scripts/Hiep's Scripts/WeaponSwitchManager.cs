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

    // keep this property exactly like this so CrosshairController script DOES NOT break
    public bool IsHoldingRice => CurrentState == HandState.Rice;
    
    // new property in case need to check if the player is unarmed
    public bool IsUnarmed => CurrentState == HandState.NormalArm;
    void Start()
    {
        // Set the initial state based on preference
        SetHandState(startingState);
    }

    public void SetHandState(HandState newState)
    {
        CurrentState = newState;

        // Turn EVERYTHING off first to guarantee no overlapping models
        if (gunObject != null) gunObject.SetActive(false);
        if (riceObject != null) riceObject.SetActive(false);
        if (normalArm != null) normalArm.SetActive(false);

        // Turn on ONLY the requested model
        switch (newState)
        {
            case HandState.Gun:
                if (gunObject != null) gunObject.SetActive(true);
                Debug.Log("Holding Gun");
                break;
            case HandState.Rice:
                if (riceObject != null) riceObject.SetActive(true);
                Debug.Log("Holding Rice");
                break;
            case HandState.NormalArm:
                if (normalArm != null) normalArm.SetActive(true);
                Debug.Log("Holding Normal Arm");
                break;
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
