using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponSO : ScriptableObject
{
    public string weaponID;
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

    [Header("Asset References")]
    public AudioClip GunSound; 
    public AudioClip reloadSound;
    // public GameObject MuzzleFlashPrefab;   // Prefab 
    public GameObject HitVfxPrefab;     // Prefab 
    
}
