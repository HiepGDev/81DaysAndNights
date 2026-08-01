using UnityEngine;

[ExecuteAlways]
public class EnemySniperSpawner : MonoBehaviour
{
    [Header("Spawn References")]
    [SerializeField] private GameObject sniperPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform designatedPoint;

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool spawnOnStart = true;

    private GameObject spawnedSniper;

    public Transform SpawnPoint => spawnPoint;
    public Transform DesignatedPoint => designatedPoint;
    public void SetDesignatedPoint(Transform point) { designatedPoint = point; }

    private void Start()
    {
        if (spawnOnStart && Application.isPlaying)
        {
            SpawnSniper();
        }
    }

    public void SpawnSniper()
    {
        if (sniperPrefab == null)
        {
            Debug.LogError("[Sniper Spawner] No Sniper Prefab assigned!");
            return;
        }

        if (designatedPoint == null)
        {
            Debug.LogError("[Sniper Spawner] No Designated Point assigned!");
            return;
        }

        // 1. Determine spawn location (fallback to spawner's own transform if spawnPoint is null)
        Transform finalSpawnPoint = (spawnPoint != null) ? spawnPoint : this.transform;
        spawnedSniper = Instantiate(sniperPrefab, finalSpawnPoint.position, finalSpawnPoint.rotation);
        
        // 2. Ensure the newly instantiated GameObject is active (in case the prefab source is disabled in the hierarchy master)
        spawnedSniper.SetActive(true);

        // 3. Configure it as a Sniper and set its designated destination waypoint
        EnemyBehaviorAgent agent = spawnedSniper.GetComponent<EnemyBehaviorAgent>();
        if (agent != null)
        {
            agent.currentMode = EnemyBehaviorAgent.EnemyMode.Sniper;
            agent.SetDesignatedSniperPoint(designatedPoint);
        }
        else
        {
            Debug.LogWarning("[Sniper Spawner] Spawned enemy prefab does not have an EnemyBehaviorAgent component!");
        }

        Debug.Log($"[Sniper Spawner] Successfully spawned Sniper '{spawnedSniper.name}' heading to designated point: {designatedPoint.position}");
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 1. Snap the spawner itself (acting as spawn point) to the ground
            SnapToGround(this.transform);

            // 2. Snap the designated waypoint target to the ground
            if (designatedPoint != null)
            {
                SnapToGround(designatedPoint);
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void SnapToGround(Transform point)
    {
        if (point == null) return;

        // Start raycast 50m above the current point to catch high towers/platforms
        Vector3 origin = point.position;
        origin.y += 50f;

        RaycastHit hit;
        // Raycast down, ignoring triggers
        if (Physics.Raycast(origin, Vector3.down, out hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 targetPos = point.position;
            targetPos.y = hit.point.y;
            point.position = targetPos;
        }
        else
        {
            // Fallback clamp Y so it does not fall below the base ground plane (0)
            if (point.position.y < 0f)
            {
                Vector3 targetPos = point.position;
                targetPos.y = 0f;
                point.position = targetPos;
            }
        }
    }
#endif
}
