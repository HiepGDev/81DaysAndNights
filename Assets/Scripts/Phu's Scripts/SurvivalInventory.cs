using System.Collections.Generic;
using UnityEngine;
using PurrNet;
using TMPro;
using Unity.Cinemachine;

namespace PhuScene
{
    public class SurvivalInventory : NetworkBehaviour
    {
        public static SurvivalInventory Instance { get; private set; }

        [Header("Weapon Inventory & Configurations")]
        [SerializeField] private List<ShopSO> defaultUnlockeds;
        [SerializeField] private List<ShopSO> equipSlots;
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private SyncVar<int> currentWeaponIndex = new(-1);

        [Header("Injected Weapon References")]
        [SerializeField] private GunRecoil recoil;
        [SerializeField] private CrosshairController crosshair;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private Camera weaponCamera;
        [SerializeField] private GameObject scopeOverlayUI;

        private HashSet<string> unlockedWeapons = new HashSet<string>();
        private Dictionary<string, GameObject> weaponPrefabsDict = new Dictionary<string, GameObject>();
        private SurvivalPlayerGun activePlayerGun;

        public SurvivalPlayerGun ActiveGun => activePlayerGun;

        private void Awake()
        {
            LoadInventory();
            InitializePrefabsDictionary();
        }

        private void Start()
        {
            // Fallback for offline mode or local execution before Spawned
            if (!isSpawned)
            {
                Instance = this;
                TrySwitchWeapon(0);
            }
        }

        protected override void OnSpawned()
        {
            if (isOwner)
            {
                Instance = this;
                currentWeaponIndex.value = 0; // Default to first weapon
            }
            else
            {
                // Clients sync initial weapon
                if (currentWeaponIndex.value != -1)
                {
                    HandleWeaponIndexChanged(currentWeaponIndex.value);
                }
            }

            currentWeaponIndex.onChanged += HandleWeaponIndexChanged;
        }

        private void Update()
        {
            if (isSpawned && !isOwner) return;

            // Weapon switching numeric keys based on unlocked order sequence
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToUnlockedWeaponNumber(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToUnlockedWeaponNumber(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToUnlockedWeaponNumber(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToUnlockedWeaponNumber(3);
        }

        private int GetSlotIndexForUnlockedWeaponNumber(int unlockedNumber)
        {
            if (equipSlots == null) return -1;
            
            int unlockedCount = 0;
            for (int i = 0; i < equipSlots.Count; i++)
            {
                if (equipSlots[i] != null)
                {
                    string weaponId = equipSlots[i].ItemId;
                    if (IsWeaponUnlocked(weaponId))
                    {
                        if (unlockedCount == unlockedNumber)
                        {
                            return i;
                        }
                        unlockedCount++;
                    }
                }
            }
            return -1;
        }

        private void SwitchToUnlockedWeaponNumber(int number)
        {
            int slotIndex = GetSlotIndexForUnlockedWeaponNumber(number);
            if (slotIndex != -1)
            {
                TrySwitchWeapon(slotIndex);
            }
        }

        private void InitializePrefabsDictionary()
        {
            if (equipSlots != null)
            {
                foreach (var mapping in equipSlots)
                {
                    if (mapping == null) continue;
                    if (mapping.itemType == ShopSO.ShopItemType.Weapon)
                    {
                        string id = mapping.ItemId;
                        if (!string.IsNullOrEmpty(id) && !weaponPrefabsDict.ContainsKey(id))
                        {
                            weaponPrefabsDict.Add(id, mapping.weaponPrefab);
                        }
                    }
                }
            }
        }

        private void LoadInventory()
        {
            // Default starting weapons are always unlocked
            if (defaultUnlockeds != null)
            {
                foreach (var shopItem in defaultUnlockeds)
                {
                    if (shopItem != null)
                    {
                        unlockedWeapons.Add(shopItem.ItemId);
                    }
                }
            }
        }

        public void UnlockWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            weaponId = weaponId.Trim();

            if (!unlockedWeapons.Contains(weaponId))
            {
                unlockedWeapons.Add(weaponId);
                Debug.Log($"[SurvivalInventory] Weapon unlocked globally: {weaponId}");
            }
        }

        public bool IsWeaponUnlocked(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return false;
            return unlockedWeapons.Contains(weaponId.Trim());
        }

        public void ResetInventory()
        {
            unlockedWeapons.Clear();
            if (defaultUnlockeds != null)
            {
                foreach (var shopItem in defaultUnlockeds)
                {
                    if (shopItem != null)
                    {
                        unlockedWeapons.Add(shopItem.ItemId);
                    }
                }
            }
        }

        public string GetWeaponIdAt(int index)
        {
            if (equipSlots == null || index < 0 || index >= equipSlots.Count) return null;
            if (equipSlots[index] == null) return null;
            return equipSlots[index].ItemId;
        }

        public GameObject GetWeaponPrefab(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return null;
            weaponPrefabsDict.TryGetValue(weaponId, out GameObject prefab);
            return prefab;
        }

        public void EquipWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            weaponId = weaponId.Trim();

            if (equipSlots != null)
            {
                for (int i = 0; i < equipSlots.Count; i++)
                {
                    if (equipSlots[i] != null && equipSlots[i].ItemId == weaponId)
                    {
                        TrySwitchWeapon(i);
                        break;
                    }
                }
            }
        }

        public void TrySwitchWeapon(int index)
        {
            if (equipSlots == null || index < 0 || index >= equipSlots.Count) return;
            string weaponId = GetWeaponIdAt(index);
            if (string.IsNullOrEmpty(weaponId)) return;

            // Verify if unlocked in inventory
            if (!IsWeaponUnlocked(weaponId))
            {
                Debug.Log($"[SurvivalInventory] Cannot switch: weapon {weaponId} is locked.");
                return;
            }

            if (isSpawned)
            {
                currentWeaponIndex.value = index;
            }
            else
            {
                HandleWeaponIndexChanged(index);
            }
        }

        private class WeaponAmmoState
        {
            public int currentAmmo;
            public int reserveAmmo;
        }

        private Dictionary<string, WeaponAmmoState> weaponAmmoStates = new Dictionary<string, WeaponAmmoState>();
        private string currentlyEquippedWeaponId = "";

        private void SaveWeaponAmmoState(string weaponId, int current, int reserve)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            if (weaponAmmoStates.TryGetValue(weaponId, out WeaponAmmoState state))
            {
                state.currentAmmo = current;
                state.reserveAmmo = reserve;
            }
            else
            {
                weaponAmmoStates.Add(weaponId, new WeaponAmmoState { currentAmmo = current, reserveAmmo = reserve });
            }
        }

        private void HandleWeaponIndexChanged(int index)
        {
            if (equipSlots == null || index < 0 || index >= equipSlots.Count) return;
            string weaponId = GetWeaponIdAt(index);
            GameObject prefab = GetWeaponPrefab(weaponId);

            // Save old weapon's ammo count before destroying it
            if (activePlayerGun != null)
            {
                if (!string.IsNullOrEmpty(currentlyEquippedWeaponId))
                {
                    SaveWeaponAmmoState(currentlyEquippedWeaponId, activePlayerGun.CurrentAmmo, activePlayerGun.ReserveAmmo);
                }
                Destroy(activePlayerGun.gameObject);
                activePlayerGun = null;
            }
            else
            {
                if (weaponHolder != null)
                {
                    foreach (Transform child in weaponHolder)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            // Instantiate new gun
            if (prefab != null && weaponHolder != null)
            {
                GameObject newGunObj = Instantiate(prefab, weaponHolder);
                // newGunObj.transform.localPosition = Vector3.zero;
                // newGunObj.transform.localRotation = Quaternion.identity;
                // newGunObj.transform.localScale = Vector3.one;

                activePlayerGun = newGunObj.GetComponent<SurvivalPlayerGun>();
                if (activePlayerGun != null)
                {
                    activePlayerGun.InitializeGunReferences(recoil, crosshair, ammoText, virtualCamera, weaponCamera, scopeOverlayUI);

                    // Load saved ammo state if exists; otherwise save default values as initial state
                    if (weaponAmmoStates.TryGetValue(weaponId, out WeaponAmmoState savedState))
                    {
                        activePlayerGun.SetAmmo(savedState.currentAmmo, savedState.reserveAmmo);
                    }
                    else
                    {
                        SaveWeaponAmmoState(weaponId, activePlayerGun.CurrentAmmo, activePlayerGun.ReserveAmmo);
                    }
                }

                // Sync weapon animator with movement script to restore walk/run sway
                SurvivalPlayerMovement survivalMovement = GetComponentInParent<SurvivalPlayerMovement>();
                if (survivalMovement != null)
                {
                    Animator newAnim = newGunObj.GetComponentInChildren<Animator>(true);
                    if (newAnim != null)
                    {
                        survivalMovement.SetAnimator(newAnim);
                    }
                }

                currentlyEquippedWeaponId = weaponId;
                
                Debug.Log($"[SurvivalInventory] Switched active weapon model locally to index: {index} ({weaponId})");
            }
        }

        public bool IsAnyWeaponMissingAmmo()
        {
            if (equipSlots == null) return false;

            for (int i = 0; i < equipSlots.Count; i++)
            {
                if (equipSlots[i] == null) continue;
                string weaponId = equipSlots[i].ItemId;
                
                if (IsWeaponUnlocked(weaponId))
                {
                    GameObject prefab = GetWeaponPrefab(weaponId);
                    if (prefab != null)
                    {
                        SurvivalPlayerGun gunComponent = prefab.GetComponent<SurvivalPlayerGun>();
                        if (gunComponent != null && gunComponent.WeaponData != null)
                        {
                            int maxMag = gunComponent.WeaponData.magazineSize;
                            int maxRes = gunComponent.WeaponData.maxReserveAmmo;

                            int curClip = maxMag;
                            int curRes = maxRes;

                            if (activePlayerGun != null && currentlyEquippedWeaponId == weaponId)
                            {
                                curClip = activePlayerGun.CurrentAmmo;
                                curRes = activePlayerGun.ReserveAmmo;
                            }
                            else if (weaponAmmoStates.TryGetValue(weaponId, out WeaponAmmoState state))
                            {
                                curClip = state.currentAmmo;
                                curRes = state.reserveAmmo;
                            }

                            if (curClip < maxMag || curRes < maxRes)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void RefillAllWeaponsAmmo()
        {
            if (equipSlots == null) return;

            for (int i = 0; i < equipSlots.Count; i++)
            {
                if (equipSlots[i] == null) continue;
                string weaponId = equipSlots[i].ItemId;

                if (IsWeaponUnlocked(weaponId))
                {
                    GameObject prefab = GetWeaponPrefab(weaponId);
                    if (prefab != null)
                    {
                        SurvivalPlayerGun gunComponent = prefab.GetComponent<SurvivalPlayerGun>();
                        if (gunComponent != null && gunComponent.WeaponData != null)
                        {
                            int maxMag = gunComponent.WeaponData.magazineSize;
                            int maxRes = gunComponent.WeaponData.maxReserveAmmo;

                            if (activePlayerGun != null && currentlyEquippedWeaponId == weaponId)
                            {
                                activePlayerGun.SetAmmo(maxMag, maxRes);
                            }

                            if (weaponAmmoStates.TryGetValue(weaponId, out WeaponAmmoState state))
                            {
                                state.currentAmmo = maxMag;
                                state.reserveAmmo = maxRes;
                            }
                            else
                            {
                                weaponAmmoStates.Add(weaponId, new WeaponAmmoState { currentAmmo = maxMag, reserveAmmo = maxRes });
                            }
                        }
                    }
                }
            }
        }
    }
}
