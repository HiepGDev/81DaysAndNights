using UnityEngine;

public class SimpleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    
    [Tooltip("Where should it spawn? Leave this empty to spawn it exactly where this script is located.")]
    [SerializeField] private Transform spawnLocation;

    [Header("Options")]
    [Tooltip("Check this to spawn the prefab immediately when the game starts.")]
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            Spawn();
        }
    }

    // call this method from a UI Button, a Unity Event, or another script!
    public void Spawn()
    {
        if (prefabToSpawn != null)
        {
            // If a specific spawn location is assigned, use its position and rotation
            // Otherwise, default to the position of the GameObject holding this script
            Vector3 position = spawnLocation != null ? spawnLocation.position : transform.position;
            Quaternion rotation = spawnLocation != null ? spawnLocation.rotation : transform.rotation;
            // Create the prefab in the scene
            Instantiate(prefabToSpawn, position, rotation);
        }
        else
        {
            Debug.LogWarning("[SimpleSpawner] Cannot spawn! No prefab is assigned in the Inspector.");
        }
    }
}
