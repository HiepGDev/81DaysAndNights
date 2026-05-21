using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Scriptable Objects/EnemySO")]
public class EnemySO : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 100;

    [Header("Wander Settings")]
    public EnemyBehaviorAgent.EnemyMode defaultMode = EnemyBehaviorAgent.EnemyMode.Wander;
    public float wanderRadius = 15f;
    public float idleTime = 2f;

    [Header("Ambush Settings")]
    public float ambushShootRange = 15f;

    [Header("Squad Spacing")]
    public float minEngagementDist = 8.0f; 
    public float rangeSpread = 3.0f;       
    public float destinationSpread = 2.0f;

    [Header("Weapon Stats")]
    public float fireRate = 0.1f;
    public float fireDistance = 25.0f;
    public int damagePerShot = 5;
    public int magazineSize = 30;
    public float reloadTime = 3.0f;

    [Header("Bloom (Recoil) Settings")]
    public float minSpread = 0.01f;
    public float maxSpread = 0.08f;
    public float bloomIncrease = 0.01f;

    [Header("Cover Settings")]
    public float coverSearchRadius = 25f;

    [Header("Peek Settings")]
    public float peekDistance = 0.7f;

    [Header("Asset References")]
    public GameObject enemyPrefab;
    public GameObject impactVfxPrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject tracerPrefab;
    public AudioClip shootSound;
    [Range(0, 1)] public float shootVolume = 0.4f;
}
