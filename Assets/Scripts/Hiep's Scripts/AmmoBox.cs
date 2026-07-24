using UnityEngine;
using UnityEngine.Localization;
public class AmmoBox : MonoBehaviour, IInteractable
{
    [Header("Ammo Settings")]
    [SerializeField] private int ammoToGive = 30;
    [SerializeField] private LocalizedString localizedInteractPrompt;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    public string GetInteractText()
    {
       return localizedInteractPrompt.GetLocalizedString();
    }

    // This is called by PlayerInteraction.cs when the player presses E
    public void Interact()
    {
        // Find the player's gun in the scene
        PlayerGun playerGun = FindFirstObjectByType<PlayerGun>();

        if (playerGun != null)
        {
            //  Give the ammo to the gun
            playerGun.AddAmmo(ammoToGive);

            // Play a sound effect at the box's location
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            //  Destroy the box so it can't be spammed (or disable it)
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogWarning("AmmoBox could not find PlayerGun in the scene!");
        }
    }
}
