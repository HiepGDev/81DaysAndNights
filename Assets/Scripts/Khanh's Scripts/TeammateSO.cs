using UnityEngine;

[CreateAssetMenu(fileName = "TeammateSO", menuName = "Scriptable Objects/TeammateSO")]
public class TeammateSO : ScriptableObject
{
    [Header("Base Stats")]
    public float maxHealth = 100f; 

    [Header("AI & Detection Settings")]
    public float detectionRadius = 15.0f;
    public float rotationSpeed = 8f;
    [Header("Line of Sight (Vật cản)")]
    public LayerMask obstacleLayer;
    //public string[] obstacleTags = { "Wall", "Obstacle" };
    public Transform idleLookTarget;

    [Header("Follow Settings")]
    public float followTriggerDistance = 10f;
    public float stopFollowDistance = 2.5f;

    [Header("Patrol Settings")]
    public float waypointStopDistance = 0.5f;
    public float waypointWaitTime = 1.5f;
    public bool loopPatrol = false;

    [Header("Weapon Stats")]
    public TeammateShooting.FireMode fireMode = TeammateShooting.FireMode.SemiAuto;
    public float autoFireRate = 0.3f;
    public float semiFireRateMin = 0.5f;
    public float semiFireRateMax = 1f;
    public float fireDistance = 25.0f;
    public int damagePerShot = 4;

    [Header("Ammo Settings")]
    public int magazineSize = 30;
    public float reloadTime = 2.5f;

    [Header("Bloom (Recoil) Settings")]
    public float minSpread = 0.03f;
    public float maxSpread = 0.15f;
    public float bloomIncrease = 0.025f;

    //[Header("Cover Settings")]
    //public float coverSearchRadius = 25f;

    [Header("Asset References")]
    public GameObject teammatePrefab; 
    public GameObject impactVfxPrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject tracerPrefab;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    [Range(0f, 1f)] public float shootVolume = 0.8f;
}