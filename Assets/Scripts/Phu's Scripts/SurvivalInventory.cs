using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Cinemachine;

namespace PhuScene
{
    public class SurvivalInventory : MonoBehaviour
    {
        public static SurvivalInventory Instance { get; private set; }

        [Header("Weapon Inventory & Configurations")]
        [SerializeField] private List<ShopSO> defaultUnlockeds;
        [SerializeField] private List<ShopSO> equipSlots;
        [SerializeField] private Transform weaponHolder;
        private int currentWeaponIndex = -1;

        [Header("Workaround References")]
        [SerializeField] private TMP_Text ammoText;

        private HashSet<string> unlockedWeapons = new HashSet<string>();
        private Dictionary<string, GameObject> weaponPrefabsDict = new Dictionary<string, GameObject>();
        private PlayerGun activePlayerGun;

        public PlayerGun ActiveGun => activePlayerGun;

        private void Awake()
        {
            Instance = this;
            LoadInventory();
            InitializePrefabsDictionary();
        }

        private void Start()
        {
            TrySwitchWeapon(0);
        }

        private void Update()
        {
            // Weapon switching numeric keys based on unlocked order sequence
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToUnlockedWeaponNumber(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToUnlockedWeaponNumber(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToUnlockedWeaponNumber(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToUnlockedWeaponNumber(3);

            // Weapon switching via mouse scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                HandleScrollWeaponSwitch(scroll);
            }
        }

        private List<int> GetUnlockedSlotIndices()
        {
            List<int> unlockedIndices = new List<int>();
            if (equipSlots != null)
            {
                for (int i = 0; i < equipSlots.Count; i++)
                {
                    if (equipSlots[i] != null)
                    {
                        string weaponId = equipSlots[i].ItemId;
                        if (IsWeaponUnlocked(weaponId))
                        {
                            unlockedIndices.Add(i);
                        }
                    }
                }
            }
            return unlockedIndices;
        }

        private int currentSelectedSlotIndex = -1;

        private void HandleScrollWeaponSwitch(float scroll)
        {
            List<int> unlockedSlotIndices = GetUnlockedSlotIndices();
            if (unlockedSlotIndices.Count <= 1) return;

            int activeIndex = currentSelectedSlotIndex >= 0 ? currentSelectedSlotIndex : currentWeaponIndex;
            int currentUnlockedPos = unlockedSlotIndices.IndexOf(activeIndex);
            if (currentUnlockedPos == -1) currentUnlockedPos = 0;

            if (scroll > 0f) // Scroll Up -> Next weapon
            {
                currentUnlockedPos = (currentUnlockedPos + 1) % unlockedSlotIndices.Count;
            }
            else if (scroll < 0f) // Scroll Down -> Previous weapon
            {
                currentUnlockedPos = (currentUnlockedPos - 1 + unlockedSlotIndices.Count) % unlockedSlotIndices.Count;
            }

            int nextSlotIndex = unlockedSlotIndices[currentUnlockedPos];
            TrySwitchWeapon(nextSlotIndex);
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
                        TrySwitchWeapon(i, ignoreCooldown: true);
                        break;
                    }
                }
            }
        }

        [Header("Weapon Switching Cooldown")]
        [SerializeField] private float switchCooldown = 0.1f;
        private float lastSwitchTime = -100f;

        public void TrySwitchWeapon(int index, bool ignoreCooldown = false)
        {
            if (equipSlots == null || index < 0 || index >= equipSlots.Count) return;

            if (!ignoreCooldown && Time.time < lastSwitchTime + switchCooldown)
            {
                return;
            }

            string weaponId = GetWeaponIdAt(index);
            if (string.IsNullOrEmpty(weaponId)) return;

            // Verify if unlocked in inventory
            if (!IsWeaponUnlocked(weaponId))
            {
                Debug.Log($"[SurvivalInventory] Cannot switch: weapon {weaponId} is locked.");
                return;
            }

            lastSwitchTime = Time.time;

            currentWeaponIndex = index;
            currentSelectedSlotIndex = index;
            HandleWeaponIndexChanged(index);
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
            if (string.IsNullOrEmpty(weaponId)) return;

            if (weaponHolder == null)
            {
                Debug.LogWarning("[SurvivalInventory] weaponHolder reference is missing.");
                return;
            }

            activePlayerGun = null;
            Transform targetChild = null;

            ShopSO shopSO = equipSlots[index];
            string itemName = shopSO != null ? shopSO.itemName : "";
            GameObject weaponPrefab = shopSO != null ? shopSO.weaponPrefab : null;
            string prefabName = weaponPrefab != null ? weaponPrefab.name : "";

            int childIndex = 0;
            // Pass 1: Find target weapon child and disable all non-matching preloaded weapon GameObjects.
            // Disabling non-matching guns FIRST prevents their OnDisable() from accidentally disabling
            // the shared InputSystem actions after the target weapon enables them.
            foreach (Transform child in weaponHolder)
            {
                PlayerGun gunComp = child.GetComponent<PlayerGun>();
                
                bool isMatch = child.name.Equals(weaponId, System.StringComparison.OrdinalIgnoreCase) ||
                               child.name.Contains(weaponId, System.StringComparison.OrdinalIgnoreCase) ||
                               (!string.IsNullOrEmpty(itemName) && child.name.Contains(itemName, System.StringComparison.OrdinalIgnoreCase)) ||
                               (!string.IsNullOrEmpty(prefabName) && child.name.Contains(prefabName, System.StringComparison.OrdinalIgnoreCase)) ||
                               (gunComp != null && gunComp.WeaponData != null && (
                                   gunComp.WeaponData.name.Equals(weaponId, System.StringComparison.OrdinalIgnoreCase) ||
                                   gunComp.WeaponData.name.Contains(weaponId, System.StringComparison.OrdinalIgnoreCase)
                               )) ||
                               (childIndex == index);

                if (isMatch)
                {
                    targetChild = child;
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
                childIndex++;
            }

            // Pass 2: Enable the target matching weapon LAST so its OnEnable() enables shoot/reload/aim inputs
            if (targetChild != null)
            {
                // Force a toggle if it was already active to re-trigger OnEnable() and refresh input actions
                if (targetChild.gameObject.activeSelf)
                {
                    targetChild.gameObject.SetActive(false);
                }
                targetChild.gameObject.SetActive(true);

                activePlayerGun = targetChild.GetComponent<PlayerGun>();

                // Sync weapon animator with movement script to restore walk/run sway
                SurvivalPlayerMovement survivalMovement = GetComponentInParent<SurvivalPlayerMovement>();
                if (survivalMovement != null)
                {
                    Animator newAnim = targetChild.GetComponentInChildren<Animator>(true);
                    if (newAnim != null)
                    {
                        survivalMovement.SetAnimator(newAnim);
                    }
                }

                currentlyEquippedWeaponId = weaponId;
                currentSelectedSlotIndex = index;
                UpdateActiveWeaponAmmoUI();
                Debug.Log($"[SurvivalInventory] Activated preloaded weapon model for index: {index} ({weaponId})");
            }
            else
            {
                Debug.LogWarning($"[SurvivalInventory] Preloaded weapon GameObject matching '{weaponId}' was not found under weaponHolder.");
            }
        }

        public void UpdateActiveWeaponAmmoUI()
        {
            if (activePlayerGun != null && activePlayerGun.WeaponData != null)
            {
                if (ammoText == null) ammoText = FindFirstObjectByType<TMP_Text>();
                if (ammoText != null)
                {
                    ammoText.text = $"{activePlayerGun.WeaponData.currentAmmo:D2} / {activePlayerGun.WeaponData.reserveAmmo:D3}";
                }
            }
        }

        public bool IsAnyWeaponMissingAmmo()
        {
            if (weaponHolder == null) return false;

            foreach (Transform child in weaponHolder)
            {
                PlayerGun gun = child.GetComponent<PlayerGun>();
                if (gun != null && gun.WeaponData != null)
                {
                    string id = gun.WeaponData.name;
                    if (IsWeaponUnlocked(id) || IsWeaponUnlocked(child.name))
                    {
                        if (gun.WeaponData.currentAmmo < gun.WeaponData.magazineSize ||
                            gun.WeaponData.reserveAmmo < gun.WeaponData.maxReserveAmmo)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void RefillAllWeaponsAmmo()
        {
            if (weaponHolder == null) return;

            foreach (Transform child in weaponHolder)
            {
                PlayerGun gun = child.GetComponent<PlayerGun>();
                if (gun != null && gun.WeaponData != null)
                {
                    string id = gun.WeaponData.name;
                    if (IsWeaponUnlocked(id) || IsWeaponUnlocked(child.name))
                    {
                        gun.WeaponData.currentAmmo = gun.WeaponData.magazineSize;
                        gun.WeaponData.reserveAmmo = gun.WeaponData.maxReserveAmmo;
                    }
                }
            }

            UpdateActiveWeaponAmmoUI();
        }
    }
}
