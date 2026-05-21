using UnityEngine;

public class RiceModel : MonoBehaviour
{
    [SerializeField] private GameObject riceModel; 
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogueClip; 
    [SerializeField] private AudioClip handoutSFX;

    //  the function the Animator will find
    public void SetRiceVisible(int visible)
    {
        if (riceModel != null)
        {
            // If the Animator sends 1, the rice turns on. If 0, it turns off 
            riceModel.SetActive(visible == 1);
        }
    }
    public void PlayHandoutSounds()
    {
        if (audioSource != null)
        {
            // PlayOneShot layers the sounds so they play together
            if (dialogueClip != null) audioSource.PlayOneShot(dialogueClip);
            if (handoutSFX != null) audioSource.PlayOneShot(handoutSFX);
        }
    }
}
