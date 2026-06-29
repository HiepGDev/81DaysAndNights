using System.Collections;
using UnityEngine;

public class ScriptedTreeFall : MonoBehaviour
{
    [Header("Event Targets")]
    [SerializeField] private Transform treeToFall;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fallSound; 

    [Header("Fall Settings")]
    [Tooltip("How much should the tree rotate? (Usually 90 on the X or Z axis)")]
    [SerializeField] private Vector3 fallRotation = new Vector3(90f, 0f, 0f); 
    [Tooltip("How many seconds does it take to hit the ground?")]
    [SerializeField] private float fallDuration = 2.5f;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player walked into the trigger, and ensure it only happens ONCE
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(FallTreeSequence());
        }
    }

    private IEnumerator FallTreeSequence()
    {
        if (audioSource != null && fallSound != null)
        {
            audioSource.PlayOneShot(fallSound);
        }

        //  Animate the fall
        if (treeToFall != null)
        {
            Quaternion startRotation = treeToFall.rotation;
            // Calculate the final resting rotation
            Quaternion endRotation = startRotation * Quaternion.Euler(fallRotation);

            float timer = 0f;
            while (timer < fallDuration)
            {
                timer += Time.deltaTime;
                
                // Calculate percentage of completion (0.0 to 1.0)
                float t = timer / fallDuration;
                float accelerationCurve = t * t; 

                treeToFall.rotation = Quaternion.Slerp(startRotation, endRotation, accelerationCurve);
                yield return null;
            }

            // Ensure it snaps perfectly to the final rotation at the very end
            treeToFall.rotation = endRotation;
            
            Debug.Log("Tree has fallen!");
        }
    }
}
