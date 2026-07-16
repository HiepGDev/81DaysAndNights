using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponSO : ScriptableObject
{
    [Header("Stats")]
    public int  Damage;
    public float MaxDistance;
    public int currentAmmo;
    public int reserveAmmo;
    public int maxReserveAmmo;
    public int  magazineSize;
    public float fireRate;
    public float reloadTime;
    public bool reloading;
    public bool isAutomatic;
    public bool canZoom;
    public float zoomAmount;
    public bool useScopeOverlay;       // TICK THIS BOX IF THE GUN IS A SNIPER
    public float scopeDelay = 0.15f;

    [Header("Asset References")]
    public AudioClip GunSound; 
    public AudioClip reloadSound;
    // public GameObject MuzzleFlashPrefab;   // Prefab 
    public GameObject HitVfxPrefab;     // Prefab 
    
}
