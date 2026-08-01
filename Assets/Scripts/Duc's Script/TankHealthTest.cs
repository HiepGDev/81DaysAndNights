using UnityEngine;

public class TankHealthTest : MonoBehaviour
{
    [SerializeField] private TankHealth tankHealth;
    [SerializeField] private int testDamage = 100;

    private void Awake()
    {
        if (tankHealth == null)
            tankHealth = GetComponent<TankHealth>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            tankHealth?.TakeDamage(testDamage);
        }
    }
}