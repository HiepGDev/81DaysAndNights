using UnityEngine;
using UnityEngine.AI;

public class TeammateCover : MonoBehaviour
{
    public struct CoverPoint { public Vector3 position; public Vector3 lookDirection; public bool isRightSide; public bool found; }

    [Header("Cover Settings")]
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private float searchRadius = 25f;
    [SerializeField] private TeammateSO teammateData;

    private NavMeshAgent agent;

    private void Awake() 
    {
        if (teammateData != null)
        {
            searchRadius = teammateData.coverSearchRadius;
        }
        agent = GetComponent<NavMeshAgent>();       
    }

    public CoverPoint FindNearestCover(Vector3 threatPos)
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

        Vector3 wallHitPoint = visualBounds.ClosestPoint(threatPos);

        RaycastHit faceHit;
        Vector3 rayDir = (wallHitPoint - threatPos).normalized;
        if (rayDir == Vector3.zero) rayDir = transform.forward;

        Vector3 faceNormal = rayDir;
        if (Physics.Raycast(threatPos, rayDir, out faceHit, Vector3.Distance(threatPos, wallHitPoint) + 5f, coverLayer))
        {
            faceNormal = faceHit.normal;
        }
        faceNormal.y = 0;
        if (faceNormal == Vector3.zero) faceNormal = rayDir;
        faceNormal.Normalize();

        Vector3 wallTangent = Vector3.Cross(Vector3.up, faceNormal).normalized;
        if (wallTangent == Vector3.zero) wallTangent = transform.right;

        Vector3 edgeR = ProbeForPhysicalEdge(wallHitPoint, wallTangent, faceNormal);
        Vector3 edgeL = ProbeForPhysicalEdge(wallHitPoint, -wallTangent, faceNormal);

        float dR = Vector3.Distance(transform.position, edgeR);
        float dL = Vector3.Distance(transform.position, edgeL);
        bool useRight = dR < dL;

        Vector3 chosenCorner = useRight ? edgeR : edgeL;
        Vector3 edgeDir = useRight ? wallTangent : -wallTangent;

        float agentRadius = (agent != null ? agent.radius : 0.5f);
        Vector3 shadowDir = (chosenCorner - threatPos).normalized;
        shadowDir.y = 0;

        Vector3 depthOffset = shadowDir * (agentRadius + 0.4f);
        Vector3 tuckOffset = -edgeDir * 0.3f;

        Vector3 safePos = chosenCorner + depthOffset + tuckOffset;
        safePos.y = transform.position.y;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(safePos, out navHit, 3.0f, NavMesh.AllAreas))
        {
            cp.position = navHit.position;
            cp.lookDirection = edgeDir;
            cp.isRightSide = useRight;
            cp.found = true;
        }

        return cp;
    }

    private Vector3 ProbeForPhysicalEdge(Vector3 start, Vector3 moveDir, Vector3 faceNormal)
    {
        Vector3 current = start;
        for (int i = 0; i < 400; i++)
        {
            Vector3 next = current + (moveDir * 0.1f);
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