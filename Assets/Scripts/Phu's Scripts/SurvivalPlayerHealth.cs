using UnityEngine;

public class SurvivalPlayerHealth : PlayerHealth
{
    private SurvivalPlayerMovement survivalMovement;
    private bool handledSurvivalDeath = false;

    private void Start()
    {
        survivalMovement = GetComponent<SurvivalPlayerMovement>();
    }

    private void Update()
    {
        if (IsDead && !handledSurvivalDeath)
        {
            handledSurvivalDeath = true;
            if (survivalMovement != null)
            {
                survivalMovement.enabled = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }
}
