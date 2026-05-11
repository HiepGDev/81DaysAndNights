using UnityEngine;

public class FriendlyNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject rice;
    [SerializeField] private AudioSource voiceSource; // for dialogue
    [SerializeField] private AudioClip thankYouClip;
    private bool hasReceivedFood = false;
    void Start()
    {
        // Ensure the rice is hidden when the game starts
        if (rice != null) rice.SetActive(false);
    }
    public string GetInteractText()
    {
        return hasReceivedFood ? "" : "Press E to Give Rice";
    }

    public void Interact()
    {
        if (hasReceivedFood) return;

        if (animator != null)
        {
            animator.SetTrigger("ReceiveFood");
        }

        hasReceivedFood = true;
        Debug.Log("Soldier received rice.");
    }
    public void SetRiceVisible(int visible)
    {
        if (rice != null)
        {
            rice.SetActive(visible == 1);
        }
    }

    public void PlayThankYouVoice()
    {
        if (voiceSource != null && thankYouClip != null)
        {
            voiceSource.PlayOneShot(thankYouClip);
        }
    }
}
