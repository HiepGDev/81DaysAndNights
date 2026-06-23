using UnityEngine;

public class KillBarrier : MonoBehaviour
{
    public CombatAreaManager combatAreaManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        combatAreaManager.PlayerLeftCombatArea();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        combatAreaManager.PlayerReturnedCombatArea();
    }
}