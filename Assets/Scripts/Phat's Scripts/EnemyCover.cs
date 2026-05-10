using UnityEngine;
using UnityEngine.AI;

public class EnemyCover : MonoBehaviour
{
    public struct CoverPoint { public Vector3 position; public Vector3 lookDirection; public bool isRightSide; public bool found; }

    [Header("Cover Settings")]
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private float searchRadius = 25f;

    private NavMeshAgent agent;

    private void Awake() { agent = GetComponent<NavMeshAgent>(); }

    // THE SQUAD COORDINATOR: Track all spots being moved to or occupied
    private static System.Collections.Generic.Dictionary<int, Vector3> claimedSpots = new System.Collections.Generic.Dictionary<int, Vector3>();

    public CoverPoint FindNearestCover(Vector3 playerPos)
    {
        CoverPoint cp = new CoverPoint { found = false };
        Collider[] potentialCovers = Physics.OverlapSphere(transform.position, searchRadius, coverLayer);
        
        if (potentialCovers.Length == 0) return cp;

        System.Array.Sort(potentialCovers, (a, b) => 
            Vector3.Distance(transform.position, a.transform.position).CompareTo(
            Vector3.Distance(transform.position, b.transform.position)));

        foreach (var col in potentialCovers)
        {
            Renderer rend = col.GetComponentInChildren<Renderer>();
            Bounds visualBounds = (rend != null) ? rend.bounds : col.bounds;
            Vector3 wallHitPoint = visualBounds.ClosestPoint(playerPos);
            
            RaycastHit faceHit;
            Vector3 rayDir = (wallHitPoint - playerPos).normalized;
            if (rayDir == Vector3.zero) rayDir = transform.forward;
            Vector3 faceNormal = rayDir; 
            if (Physics.Raycast(playerPos, rayDir, out faceHit, Vector3.Distance(playerPos, wallHitPoint) + 5f, coverLayer))
                faceNormal = faceHit.normal;

            faceNormal.y = 0;
            faceNormal.Normalize();

            Vector3 wallTangent = Vector3.Cross(Vector3.up, faceNormal).normalized;
            Vector3 edgeR = ProbeForPhysicalEdge(wallHitPoint, wallTangent, faceNormal);
            Vector3 edgeL = ProbeForPhysicalEdge(wallHitPoint, -wallTangent, faceNormal);

            foreach (bool tryRight in new bool[] { true, false })
            {
                Vector3 chosenCorner = tryRight ? edgeR : edgeL;
                Vector3 edgeDir = tryRight ? wallTangent : -wallTangent;

                float agentRadius = (agent != null ? agent.radius : 0.5f);
                Vector3 shadowDir = (chosenCorner - playerPos).normalized;
                shadowDir.y = 0;

                Vector3 depthOffset = shadowDir * (agentRadius + 0.4f);
                Vector3 tuckOffset = -edgeDir * 0.3f; 
                Vector3 safePos = chosenCorner + depthOffset + tuckOffset;
                safePos.y = transform.position.y;

                // THE SQUAD FIX 1: Check if anyone has RESERVED this spot already
                bool isReserved = false;
                foreach (var claim in claimedSpots)
                {
                    if (claim.Key != gameObject.GetInstanceID() && Vector3.Distance(claim.Value, safePos) < 1.5f)
                    {
                        isReserved = true;
                        break;
                    }
                }
                if (isReserved) continue;

                // THE SQUAD FIX 2: Check if someone is PHYSICALLY there (Chest Height)
                if (Physics.CheckSphere(safePos + Vector3.up * 1f, 1.0f, LayerMask.GetMask("Enemy")))
                    continue; 

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(safePos, out navHit, 3.0f, NavMesh.AllAreas))
                {
                    cp.position = navHit.position;
                    cp.lookDirection = edgeDir; 
                    cp.isRightSide = tryRight;
                    cp.found = true;
                    
                    // Claim the spot
                    claimedSpots[gameObject.GetInstanceID()] = cp.position;
                    return cp;
                }
            }
        }
        return cp;
    }

    // Call this when enemy leaves cover or dies
    public void ReleaseCover()
    {
        int id = gameObject.GetInstanceID();
        if (claimedSpots.ContainsKey(id)) claimedSpots.Remove(id);
    }

    private void OnDestroy() { ReleaseCover(); }

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
