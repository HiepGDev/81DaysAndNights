using UnityEngine;

public class EnemyCover : MonoBehaviour
{
    public struct CoverPoint
    {
        public Vector3 position;
        public Vector3 lookDirection;
        public bool isRightSide;
        public bool found;
    }

    [Header("Cover Detection")]
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private float searchRadius = 15f;
    [SerializeField] private float coverOffset = 0.6f; 
    [SerializeField] private float edgeBuffer = 0.4f;  

    public CoverPoint FindNearestCover(Vector3 playerPos)
    {
        CoverPoint cp = new CoverPoint { found = false };
        
        Collider[] potentialCovers = Physics.OverlapSphere(transform.position, searchRadius, coverLayer);
        Collider bestCollider = null;
        float closestDist = float.MaxValue;

        foreach (var col in potentialCovers)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist) { closestDist = dist; bestCollider = col; }
        }

        if (bestCollider == null) return cp;

        // 1. Get the wall surface normal
        Vector3 dirToWall = (bestCollider.bounds.center - transform.position).normalized;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dirToWall, out hit, searchRadius, coverLayer))
        {
            Vector3 wallNormal = hit.normal;
            Vector3 wallTangent = Vector3.Cross(wallNormal, Vector3.up).normalized;

            // 2. Scan for edges
            Vector3 edgeR = ScanForEdge(hit.point, wallTangent, bestCollider);
            Vector3 edgeL = ScanForEdge(hit.point, -wallTangent, bestCollider);

            float dR = Vector3.Distance(transform.position, edgeR);
            float dL = Vector3.Distance(transform.position, edgeL);

            bool useRight = dR < dL;
            Vector3 bestEdge = useRight ? edgeR : edgeL;
            Vector3 sideDir = useRight ? wallTangent : -wallTangent;

            // 3. Final Position & Direction
            // Step back from normal, and slightly back from the edge to hide
            cp.position = bestEdge + (wallNormal * coverOffset) - (sideDir * edgeBuffer);
            cp.position.y = transform.position.y;
            
            // Facing "outwards" towards the edge
            cp.lookDirection = sideDir;
            cp.isRightSide = useRight;
            cp.found = true;
            
            Debug.Log($"[Cover] Found {(useRight ? "Right" : "Left")} edge of {bestCollider.gameObject.name}");
        }

        return cp;
    }

    private Vector3 ScanForEdge(Vector3 start, Vector3 dir, Collider wall)
    {
        Vector3 current = start;
        for (int i = 0; i < 40; i++)
        {
            Vector3 next = current + (dir * 0.25f);
            // If the next step is outside the wall bounds, we found the edge
            if (!wall.bounds.Contains(next)) return current;
            current = next;
        }
        return current;
    }
}
