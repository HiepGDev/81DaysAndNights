using UnityEngine;
using UnityEngine.AI;

public class EnemyTacticalPeek : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyShooting shooting;
    private EnemyDetection detection;
    private EnemyBehaviorAgent behaviorAgent;
    
    [Header("Peek Settings")]
    [SerializeField] private float peekDistance = 0.7f; 
    [SerializeField] private float arrivalThreshold = 0.4f;

    private Vector3 originalCoverPos;
    private Vector3 peekPos;
    private bool isCurrentlyPeeking = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        shooting = GetComponent<EnemyShooting>();
        detection = GetComponent<EnemyDetection>();
        behaviorAgent = GetComponent<EnemyBehaviorAgent>();
    }

    private void Update()
    {
        if (behaviorAgent == null || !behaviorAgent.IsInCover) 
        {
            if (isCurrentlyPeeking) isCurrentlyPeeking = false;
            return;
        }

        if (shooting == null || agent == null) return;

        // TRIGGER PEEK: Ammo full and ready
        if (!shooting.IsOutOfAmmo && !shooting.IsReloading && !isCurrentlyPeeking)
        {
            // If the Agent is stopped at the hide spot, start the peek
            if (agent.isStopped || agent.remainingDistance < 0.4f)
            {
                StartPeek();
            }
        }

        // TRIGGER STOP: Empty or target lost
        if ((shooting.IsOutOfAmmo || (detection != null && !detection.IsTargetDetected)) && isCurrentlyPeeking)
        {
            ReturnToCover();
        }

        // SHOOT LOCK: Only fire when arrived
        if (isCurrentlyPeeking)
        {
            float distToPeek = Vector3.Distance(transform.position, peekPos);
            shooting.enabled = (distToPeek < arrivalThreshold);
        }
    }

    private void StartPeek()
    {
        // Use the master safe position from the behavior agent
        Vector3 safeHome = behaviorAgent.ActiveCover.position;
        Vector3 edgeDirection = behaviorAgent.ActiveCover.lookDirection;
        
        peekPos = safeHome + (edgeDirection * peekDistance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(peekPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            peekPos = hit.position;
            isCurrentlyPeeking = true;
            agent.isStopped = false;
            agent.SetDestination(peekPos);
            Debug.Log("[Tactical] Peeking out...");
        }
    }

    private void ReturnToCover()
    {
        isCurrentlyPeeking = false;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            // Always return to the EXACT master safe spot
            agent.SetDestination(behaviorAgent.ActiveCover.position);
            Debug.Log("[Tactical] Pulling back to master safe spot.");
        }
    }
}
