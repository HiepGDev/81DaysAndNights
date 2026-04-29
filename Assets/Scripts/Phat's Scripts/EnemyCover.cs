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

        // 1. Find the point on the wall surface closest to the player
        // THE FIX: Use visualBounds to support non-convex Mesh Colliders
        Vector3 wallHitPoint = visualBounds.ClosestPoint(playerPos);
        
        // 2. Get the actual Face Normal using a Raycast
        RaycastHit faceHit;
        Vector3 rayDir = (wallHitPoint - playerPos).normalized;
        if (rayDir == Vector3.zero) rayDir = transform.forward; // Safety
        
        Vector3 faceNormal = rayDir; // Fallback
        
        if (Physics.Raycast(playerPos, rayDir, out faceHit, Vector3.Distance(playerPos, wallHitPoint) + 5f, coverLayer))
        {
            faceNormal = faceHit.normal;
        }
        faceNormal.y = 0;
        if (faceNormal == Vector3.zero) faceNormal = rayDir;
        faceNormal.Normalize();

        // 3. DEPTH is always AGAINST the normal
        Vector3 depthDir = -faceNormal;
        
        // 4. TANGENT is along the wall face
        Vector3 wallTangent = Vector3.Cross(Vector3.up, faceNormal).normalized;
        if (wallTangent == Vector3.zero) wallTangent = transform.right; // Safety for zero vector

        // 4. PROBE: Find the actual physical corners (Raycast walk along the surface)
        Vector3 edgeR = ProbeForPhysicalEdge(wallHitPoint, wallTangent, faceNormal);
        Vector3 edgeL = ProbeForPhysicalEdge(wallHitPoint, -wallTangent, faceNormal);

        float dR = Vector3.Distance(transform.position, edgeR);
        float dL = Vector3.Distance(transform.position, edgeL);
        bool useRight = dR < dL;
        
        Vector3 chosenCorner = useRight ? edgeR : edgeL;
        Vector3 edgeDir = useRight ? wallTangent : -wallTangent;

        // 5. CALCULATE POSITION (Shadow Projection)
        float agentRadius = (agent != null ? agent.radius : 0.5f);
        
        // A. Shadow Direction: The straight line from player to the corner
        Vector3 shadowDir = (chosenCorner - playerPos).normalized;
        shadowDir.y = 0;

        // B. Depth: Push into the shadow (Radius + 0.4m buffer)
        // This ensures the back and shoulders are well behind the line of sight
        Vector3 depthOffset = shadowDir * (agentRadius + 0.4f);
        
        // C. Tuck: Move slightly back along the wall to be extra safe
        Vector3 tuckOffset = -edgeDir * 0.3f; 

        Vector3 safePos = chosenCorner + depthOffset + tuckOffset;
        safePos.y = transform.position.y;

        // 6. SNAP TO NAVMESH
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(safePos, out navHit, 3.0f, NavMesh.AllAreas))
        {
            cp.position = navHit.position;
            // Face along the wall towards the peek side
            cp.lookDirection = edgeDir; 
            cp.isRightSide = useRight;
            cp.found = true;
        }
        
        return cp;
    }

    private Vector3 ProbeForPhysicalEdge(Vector3 start, Vector3 moveDir, Vector3 faceNormal)
    {
        Vector3 current = start;
        // Step 10cm at a time along the wall face
        for (int i = 0; i < 400; i++)
        {
            Vector3 next = current + (moveDir * 0.1f);
            
            // THE FIX: Instead of ClosestPoint, fire a ray from "outside" the wall back into it.
            // If the ray misses, we've gone past the corner.
            Vector3 rayOrigin = next + (faceNormal * 0.5f);
            if (!Physics.Raycast(rayOrigin, -faceNormal, 1.0f, coverLayer))
            {
                return current; 
            }

            current = next;
        }
        return current;
    }
}
