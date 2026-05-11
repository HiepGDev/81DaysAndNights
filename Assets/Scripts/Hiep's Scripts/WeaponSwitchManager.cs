using UnityEngine;

public class WeaponSwitchManager : MonoBehaviour
{
    [SerializeField] private GameObject gunObject;
    [SerializeField] private GameObject riceObject;
    [SerializeField] private bool startWithRice = true;

    void Start()
    {
        // Set the initial state based on preference
        SetHoldingRice(startWithRice);
    }

    public void SetHoldingRice(bool isHoldingRice)
    {
        if (isHoldingRice)
        {
            if (gunObject != null) gunObject.SetActive(false);
            if (riceObject != null) riceObject.SetActive(true);
        }
        else
        {
            if (gunObject != null) gunObject.SetActive(true);
            if (riceObject != null) riceObject.SetActive(false);
        }
        
        Debug.Log(isHoldingRice ? "Holding Rice" : "Holding Gun");
    }

    // A simple helper method to switch to the gun later
    public void SwitchToGun()
    {
        SetHoldingRice(false);
    }
}
