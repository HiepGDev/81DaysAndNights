using Unity.Cinemachine;
using UnityEngine;

public class FOVLoader : MonoBehaviour
{
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private PlayerGun playerGun;

    void Start()
    {
        // Pull the setting from memory
        float savedFOV = PlayerPrefs.GetFloat("PlayerFOV", 75f);

        // Apply it to the cameras
        if (normalCamera != null) normalCamera.Lens.FieldOfView = savedFOV;
        if (weaponCamera != null) weaponCamera.fieldOfView = savedFOV;

        // Tell the Gun script what the "Hipfire" FOV is
        if (playerGun != null)
        {
            playerGun.UpdateDefaultFOV(savedFOV);
        }
        
        Debug.Log($"Player spawned. FOV set to {savedFOV}");
    }
}
