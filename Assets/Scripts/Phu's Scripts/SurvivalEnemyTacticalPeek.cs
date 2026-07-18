using UnityEngine;
using UnityEngine.AI;

public class SurvivalEnemyTacticalPeek : MonoBehaviour
{
    private NavMeshAgent agent;
    private SurvivalEnemyShooting shooting;
    private EnemyDetection detection;
    private SurvivalEnemyBehaviorAgent behaviorAgent;
    
    [SerializeField] private EnemySO enemyData;

    [Header("Peek Settings")]
    [SerializeField] private float peekDistance = 0.7f; 

    private Vector3 originalCoverPos;
    private Vector3 peekPos;
    private bool isCurrentlyPeeking = false;

    public bool IsPeeking => isCurrentlyPeeking;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        shooting = GetComponent<SurvivalEnemyShooting>();
        detection = GetComponent<EnemyDetection>();
        behaviorAgent = GetComponent<SurvivalEnemyBehaviorAgent>();

        if (agent != null) agent.stoppingDistance = 0.1f;

        if (enemyData != null)
        {
            peekDistance = enemyData.peekDistance;
        }
    }

    private void Update()
    {
        if (behaviorAgent == null || !behaviorAgent.IsInCover) 
        {
            if (isCurrentlyPeeking) isCurrentlyPeeking = false;
            if (shooting != null) shooting.allowFiring = true;
            return;
        }

        if (shooting == null || agent == null) return;

        bool hasTarget = (detection != null && detection.IsTargetDetected) || 
                          (behaviorAgent.currentMode == SurvivalEnemyBehaviorAgent.EnemyMode.Ambush && behaviorAgent.CurrentAmbushTarget != null);

        if (!shooting.IsOutOfAmmo && !shooting.IsReloading && !isCurrentlyPeeking && hasTarget)
        {
            if (agent.isStopped || agent.remainingDistance < 0.4f)
            {
                StartPeek();
            }
        }

        if ((shooting.IsOutOfAmmo || !hasTarget) && isCurrentlyPeeking)
        {
            ReturnToCover();
        }

        if (isCurrentlyPeeking)
        {
            Vector3 flatSelf = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatTarget = new Vector3(peekPos.x, 0, peekPos.z);
            float distToPeek = Vector3.Distance(flatSelf, flatTarget);

            shooting.allowFiring = (distToPeek <= 0.6f);
        }
        else
        {
            shooting.allowFiring = false;
        }
    }

    private void StartPeek()
    {
        Vector3 safeHome = behaviorAgent.ActiveCover.position;
        Vector3 edgeDirection = behaviorAgent.ActiveCover.lookDirection;
        
        peekPos = safeHome + (edgeDirection * peekDistance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(peekPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            peekPos = hit.position;
            isCurrentlyPeeking = true;
            
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(peekPos);
            }
        }
    }

    private void ReturnToCover()
    {
        isCurrentlyPeeking = false;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(behaviorAgent.ActiveCover.position);
        }
    }
}
