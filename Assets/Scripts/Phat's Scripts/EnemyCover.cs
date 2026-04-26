using UnityEngine;
using UnityEngine.AI;

public class EnemyCover : MonoBehaviour
{
    public struct CoverPoint { public Vector3 position; public Vector3 lookDirection; public bool isRightSide; public bool found; }

    [Header("Cover Settings")]
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private float hugDistance = 0.2f; 

    private NavMeshAgent agent;

    private void Awake() { agent = GetComponent<NavMeshAgent>(); }

    public CoverPoint FindNearestCover(Vector3 playerPos)
    {
        CoverPoint cp = new CoverPoint { found = false };
        Collider[] potentialCovers = Physics.OverlapSphere(transform.position, searchRadius, coverLayer);
        Collider bestCol = null;
        float closestDist = float.MaxValue;

        foreach (var col in potentialCovers)
        {
            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < closestDist) { closestDist = d; bestCol = col; }
        }

        if (bestCol == null) return cp;

        Renderer rend = bestCol.GetComponentInChildren<Renderer>();
        Bounds visualBounds = (rend != null) ? rend.bounds : bestCol.bounds;

        Vector3 wallHitPoint = visualBounds.ClosestPoint(playerPos);
        Vector3 dirFromPlayer = (wallHitPoint - playerPos).normalized;
        dirFromPlayer.y = 0;

        Vector3 wallTangent = Vector3.Cross(Vector3.up, dirFromPlayer).normalized;

        Vector3 edgeR = ProbeForVisualEdge(wallHitPoint, wallTangent, visualBounds);
        Vector3 edgeL = ProbeForVisualEdge(wallHitPoint, -wallTangent, visualBounds);

        float dR = Vector3.Distance(transform.position, edgeR);
        float dL = Vector3.Distance(transform.position, edgeL);
        bool useRight = dR < dL;
        
        Vector3 chosenCorner = useRight ? edgeR : edgeL;
        Vector3 edgeDir = useRight ? wallTangent : -wallTangent;

        // Shadow Positioning logic
        Vector3 pushBehindWallDir = (chosenCorner - playerPos).normalized;
        pushBehindWallDir.y = 0;
        float radius = (agent != null ? agent.radius : 0.5f);
        
        Vector3 safePos = chosenCorner + (pushBehindWallDir * (radius + hugDistance)) - (edgeDir * 0.4f);
        safePos.y = transform.position.y;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(safePos, out navHit, 2.0f, NavMesh.AllAreas))
        {
            cp.position = navHit.position;
            cp.lookDirection = edgeDir; 
            cp.isRightSide = useRight;
            cp.found = true;
        }
        
        return cp;
    }

    private Vector3 ProbeForVisualEdge(Vector3 start, Vector3 moveDir, Bounds bounds)
    {
        Vector3 current = start;
        for (int i = 0; i < 400; i++)
        {
            Vector3 next = current + (moveDir * 0.1f);
            if (next.x < bounds.min.x || next.x > bounds.max.x || next.z < bounds.min.z || next.z > bounds.max.z)
                return current; 
            current = next;
        }
        return current;
    }
}
