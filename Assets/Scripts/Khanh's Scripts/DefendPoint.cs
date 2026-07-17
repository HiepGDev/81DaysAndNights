using UnityEngine;

public class DefendPoint : MonoBehaviour
{
    public bool isOccupied = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}