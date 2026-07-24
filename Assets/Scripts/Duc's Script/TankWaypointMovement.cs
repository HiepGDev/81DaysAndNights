using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TankWaypointMovement : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;

    [Header("Waypoint Settings")]
    [SerializeField, Min(0.1f)]
    private float waypointReachDistance = 1f;

    [SerializeField, Min(0.1f)]
    private float waypointSampleRadius = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private bool hasFinishedPath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                $"{name}: Tank is not positioned on a NavMesh.",
                this
            );
            enabled = false;
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning(
                $"{name}: No waypoints assigned.",
                this
            );
            enabled = false;
            return;
        }

        currentWaypointIndex = 0;
        MoveToCurrentWaypoint();
    }

    private void Update()
    {
        if (hasFinishedPath || agent.pathPending)
            return;

        if (!agent.hasPath)
            return;

        float requiredDistance = Mathf.Max(
            agent.stoppingDistance,
            waypointReachDistance
        );

        if (agent.remainingDistance > requiredDistance)
            return;

        currentWaypointIndex++;

        if (currentWaypointIndex >= waypoints.Length)
        {
            StopAtFinalWaypoint();
            return;
        }

        MoveToCurrentWaypoint();
    }

    private void MoveToCurrentWaypoint()
    {
        Transform waypoint = waypoints[currentWaypointIndex];

        if (waypoint == null)
        {
            Debug.LogError(
                $"{name}: Waypoint {currentWaypointIndex} is missing.",
                this
            );
            StopAtFinalWaypoint();
            return;
        }

        // Bắt waypoint phải khớp với NavMesh gần đó.
        if (!NavMesh.SamplePosition(
                waypoint.position,
                out NavMeshHit navMeshHit,
                waypointSampleRadius,
                agent.areaMask))
        {
            Debug.LogError(
                $"{name}: Waypoint {currentWaypointIndex} " +
                $"is not near the NavMesh.",
                waypoint
            );

            StopAtFinalWaypoint();
            return;
        }

        agent.isStopped = false;

        bool destinationAccepted =
            agent.SetDestination(navMeshHit.position);

        if (!destinationAccepted)
        {
            Debug.LogError(
                $"{name}: Cannot create a path to " +
                $"waypoint {currentWaypointIndex}.",
                waypoint
            );
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                $"{name}: Moving to waypoint " +
                $"{currentWaypointIndex}.",
                waypoint
            );
        }
    }

    private void StopAtFinalWaypoint()
    {
        hasFinishedPath = true;
        agent.isStopped = true;
        agent.ResetPath();

        if (showDebugLog)
            Debug.Log($"{name}: Finished tank path.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.DrawLine(transform.position, waypoints[0].position);

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawWireSphere(
                waypoints[i].position,
                waypointReachDistance
            );

            if (i < waypoints.Length - 1 &&
                waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[i + 1].position
                );
            }
        }
    }
}