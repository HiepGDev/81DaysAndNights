using UnityEngine;

public class KillBarrier : MonoBehaviour
{
    public CombatAreaManager combatAreaManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("Player entered combat boundary");

        combatAreaManager.ShowWarning();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("Player returned to combat area");

        combatAreaManager.HideWarning();
    }
}