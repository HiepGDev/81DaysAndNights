using UnityEngine;

public class NpcDigger : MonoBehaviour
{
    [Header("Audio Setup")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip digSound;

    // The Animator will trigger this specific function
    public void PlayDigSound()
    {
        if (audioSource != null && digSound != null)
        {
            // Randomize the pitch slightly each time so the digging doesn't sound like a robot
            audioSource.pitch = Random.Range(0.96f, 1.0f); 
            audioSource.PlayOneShot(digSound);
        }
    }
}
