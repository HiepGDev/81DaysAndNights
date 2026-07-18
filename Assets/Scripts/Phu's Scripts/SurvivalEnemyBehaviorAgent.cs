using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SurvivalEnemyBehaviorAgent : MonoBehaviour
{
    public enum EnemyMode { Wander, Ambush }

    private NavMeshAgent agent;
    private EnemyDetection detection;
    private SurvivalEnemyShooting shooting;
    private EnemyCover cover;
    private SurvivalEnemyTacticalPeek peek;
    private Animator animator;
    [SerializeField] private EnemySO enemyData;

    [Header("Ambush Settings")]
    public EnemyMode currentMode = EnemyMode.Wander;
    [SerializeField] private float ambushShootRange = 15f; 
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float idleTime = 2f;

    [Header("Squad Spacing")]
    [SerializeField] private float minEngagementDist = 8.0f; 
    [SerializeField] private float rangeSpread = 3.0f;       
    [SerializeField] private float destinationSpread = 2.0f;
    
    private Transform playerTransform;
    private Vector3 spawnPoint;
    private float idleTimer;
    private bool isIdle = false;
    private bool isInCover = false;
    private Vector3 currentTarget; 
    private EnemyCover.CoverPoint activeCover;

    // Spacing and Targeting
    private float personalRangeOffset;
    private float personalFlankingAngle;
    private float targetSearchTimer = 0f;

    // Squad Distribution
    private static System.Collections.Generic.Dictionary<int, int> targetAttackers = new System.Collections.Generic.Dictionary<int, int>();
    private int myCurrentTargetID = -1;

    public bool IsReadyToShoot { get; private set; }
    public float CurrentEngagementDist { get; private set; } 
    public Transform CurrentAmbushTarget { get; private set; }
    public Transform PlayerTransform => playerTransform;

    public bool IsMovingToCover => isInCover && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
    public bool IsInCover => isInCover;
    public EnemyCover.CoverPoint ActiveCover => activeCover;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        shooting = GetComponent<SurvivalEnemyShooting>();
        cover = GetComponent<EnemyCover>();
        peek = GetComponent<SurvivalEnemyTacticalPeek>();
        animator = GetComponentInChildren<Animator>();

        if (enemyData != null)
        {
            currentMode = (EnemyMode)enemyData.defaultMode;
            ambushShootRange = enemyData.ambushShootRange;
            wanderRadius = enemyData.wanderRadius;
            idleTime = enemyData.idleTime;
            minEngagementDist = enemyData.minEngagementDist;
            rangeSpread = enemyData.rangeSpread;
            destinationSpread = enemyData.destinationSpread;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        if (animator != null) {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        agent.updatePosition = false; 
        agent.updateRotation = true;
        agent.speed = 4.0f;
        agent.acceleration = 30f; 
        agent.angularSpeed = 600f; 
        agent.stoppingDistance = 0.5f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        personalRangeOffset = Random.Range(-rangeSpread, rangeSpread);
        personalFlankingAngle = Random.Range(-destinationSpread * 10f, destinationSpread * 10f);
    }

    private void Start()
    {
        spawnPoint = transform.position;
        if (agent.isOnNavMesh) agent.Warp(transform.position);
        StartIdle();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;
        transform.position = agent.nextPosition;

        IsReadyToShoot = false;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        bool isAmbushing = (currentMode == EnemyMode.Ambush);
        if (isAmbushing)
        {
            targetSearchTimer -= Time.deltaTime;
            if (targetSearchTimer <= 0 || CurrentAmbushTarget == null || !IsAlive(CurrentAmbushTarget.gameObject))
            {
                CurrentAmbushTarget = GetClosestTarget();
                targetSearchTimer = 0.5f; 
            }
        }

        bool isDetected = (isAmbushing && CurrentAmbushTarget != null) || (detection != null && detection.IsTargetDetected);
        Transform target = isAmbushing ? CurrentAmbushTarget : (detection != null ? detection.CurrentTarget : null);

        float distToTarget = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
        float maxGunRange = (shooting != null ? shooting.FireDistance : 25f);
        float baseRange = isAmbushing ? ambushShootRange : maxGunRange;
        
        CurrentEngagementDist = Mathf.Clamp(baseRange + personalRangeOffset, minEngagementDist, maxGunRange - 2.0f);
        bool inShootingRange = isDetected && distToTarget <= CurrentEngagementDist;

        if (animator != null)
        {
            float intentSpeed = agent.isStopped ? 0f : agent.desiredVelocity.magnitude;
            bool isActuallyReloading = (shooting != null && shooting.IsReloading);
            bool isMovingToCover = IsMovingToCover;

            if (isActuallyReloading && !isMovingToCover)
            {
                if (!agent.isStopped) StopAgent();
            }

            if (!isActuallyReloading || isMovingToCover)
            {
                bool shouldBeRunning = intentSpeed > 2.0f;
                if (HasParameter("isRunning", animator))
                    animator.SetBool("isRunning", shouldBeRunning);

                if (HasParameter("Speed", animator))
                {
                    float finalSpeed = shouldBeRunning ? 0.5f : intentSpeed; 
                    animator.SetFloat("Speed", finalSpeed);
                }
            }
            else
            {
                if (HasParameter("isRunning", animator)) animator.SetBool("isRunning", false);
                if (HasParameter("Speed", animator)) animator.SetFloat("Speed", 0f);
            }
        }

        if (shooting != null && shooting.IsOutOfAmmo)
        {
            if (!isInCover) FindAndGoToCover();
            else HandleCoverLogic(inShootingRange);
            return;
        }

        if (isInCover)
        {
            HandleCoverLogic(inShootingRange);
            return;
        }

        if (isDetected && target != null)
        {
            if (!inShootingRange)
            {
                if (agent.isStopped) agent.isStopped = false;
                
                Vector3 dirFromTarget = (transform.position - target.position).normalized;
                if (dirFromTarget == Vector3.zero) dirFromTarget = Vector3.forward;
                Vector3 flankingDir = Quaternion.Euler(0, personalFlankingAngle, 0) * dirFromTarget;
                Vector3 tacticalPos = target.position + flankingDir * (CurrentEngagementDist - 1.0f); 

                NavMeshHit hit;
                if (NavMesh.SamplePosition(tacticalPos, out hit, 4.0f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(target.position);
                
                FaceTarget();
            }
            else
            {
                if (!agent.isStopped) StopAgent();
                FaceTarget();
                IsReadyToShoot = true;
            }
            return;
        }

        if (isIdle) HandleIdle();
        else HandleWandering();
    }

    private void FindAndGoToCover()
    {
        bool isAmbushing = (currentMode == EnemyMode.Ambush && CurrentAmbushTarget != null);
        if (cover == null || (detection.CurrentTarget == null && !isAmbushing)) 
        {
            shooting.TriggerReload();
            return;
        }

        Vector3 targetPos = isAmbushing ? CurrentAmbushTarget.position : detection.CurrentTarget.position;
        activeCover = cover.FindNearestCover(targetPos);
        
        if (activeCover.found)
        {
            isInCover = true;
            isIdle = false;
            agent.isStopped = false;
            agent.SetDestination(activeCover.position);
        }
        else
        {
            shooting.TriggerReload();
        }
    }

    private void HandleCoverLogic(bool inShootingRange)
    {
        bool isPeeking = (peek != null && peek.IsPeeking);
        float distToCover = Vector3.Distance(transform.position, activeCover.position);

        if (isPeeking || distToCover <= 0.2f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            bool isReloading = (shooting != null && shooting.IsReloading);
            bool shouldCrouch = (shooting != null && shooting.IsOutOfAmmo);

            if (isPeeking && !isReloading)
            {
                if (!inShootingRange)
                {
                    ExitCover();
                    return;
                }

                IsReadyToShoot = true;
                FaceTarget();
            }
            else if (shouldCrouch || isReloading)
            {
                StopAgent();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(activeCover.lookDirection), Time.deltaTime * 10f);
            }

            if (!isReloading)
            {
                if (shouldCrouch)
                {
                    if (animator != null && HasParameter("isCovering", animator))
                    {
                        animator.SetBool("isCovering", true);
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Cover_Crouching"))
                            animator.CrossFade("Cover_Crouching", 0.1f);
                    }
                }
                else if (!shooting.IsOutOfAmmo && !isPeeking)
                {
                    if (!inShootingRange) ExitCover();
                }
            }

            if (shooting != null && shooting.IsOutOfAmmo && !isReloading)
                shooting.TriggerReload();
        }
    }

    private void ExitCover()
    {
        if (cover != null) cover.ReleaseCover();
        isInCover = false;
        if (animator != null) animator.SetBool("isCovering", false);
        agent.isStopped = false;
        if (activeCover.found)
        {
            Vector3 pushOutDir = (transform.position - activeCover.position).normalized;
            agent.Warp(transform.position + pushOutDir * 0.5f);
        }
    }

    public void FaceTarget()
    {
        bool isAmbushing = (currentMode == EnemyMode.Ambush && CurrentAmbushTarget != null);
        Transform t = isAmbushing ? CurrentAmbushTarget : detection.CurrentTarget;
        if (t != null)
        {
            Vector3 dir = (t.position - transform.position);
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    private void HandleIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0)
        {
            isIdle = false;
            SetNewWanderDestination();
        }
    }

    private void HandleWandering()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            StartIdle();
    }

    private void StartIdle()
    {
        isIdle = true;
        idleTimer = Random.Range(idleTime * 0.5f, idleTime * 1.5f);
        StopAgent();
    }

    private void SetNewWanderDestination()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += spawnPoint;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randomDir, out navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(navHit.position);
        }
    }

    private void StopAgent()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private Transform GetClosestTarget()
    {
        float bestScore = float.MaxValue;
        Transform bestTarget = null;
        string[] tags = (detection != null) ? detection.TargetTags : new string[] { "Player", "Teammate" };

        foreach (string tag in tags)
        {
            GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in potentialTargets)
            {
                if (!IsAlive(obj)) continue;

                float dist = Vector3.Distance(transform.position, obj.transform.position);
                int targetID = obj.GetInstanceID();

                int attackers = targetAttackers.ContainsKey(targetID) ? targetAttackers[targetID] : 0;
                float score = dist + (attackers * 10f);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = obj.transform;
                }
            }
        }

        UpdateAttackerCount(bestTarget);
        return bestTarget;
    }

    private void UpdateAttackerCount(Transform newTarget)
    {
        if (myCurrentTargetID != -1 && targetAttackers.ContainsKey(myCurrentTargetID))
            targetAttackers[myCurrentTargetID]--;

        if (newTarget != null)
        {
            myCurrentTargetID = newTarget.gameObject.GetInstanceID();
            if (!targetAttackers.ContainsKey(myCurrentTargetID)) targetAttackers[myCurrentTargetID] = 0;
            targetAttackers[myCurrentTargetID]++;
        }
        else myCurrentTargetID = -1;
    }

    private void OnDisable()
    {
        if (cover != null) cover.ReleaseCover();
        UpdateAttackerCount(null);
    }

    private void OnDestroy() 
    { 
        if (cover != null) cover.ReleaseCover(); 
        UpdateAttackerCount(null); 
    }

    private bool IsAlive(GameObject obj)
    {
        if (obj == null) return false;
        GameObject root = obj.transform.root.gameObject;
        
        var netHealth = root.GetComponentInChildren<SurvivalPlayerHealth>();
        if (netHealth != null) return !netHealth.IsDead;

        var ph = root.GetComponentInChildren<PlayerHealth>();
        if (ph != null) return !ph.IsDead; 

        var th = root.GetComponentInChildren<TeammateHealth>();
        if (th != null) return root.CompareTag("Teammate");
        return true; 
    }

    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
            if (param.name == paramName) return true;
        return false;
    }
}
