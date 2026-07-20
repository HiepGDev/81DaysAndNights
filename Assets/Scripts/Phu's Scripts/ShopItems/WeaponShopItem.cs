using UnityEngine;
using System.Collections.Generic;

namespace PhuScene
{
    public class WeaponShopItem : BaseShopItem
    {
        [Header("Weapon Configuration")]
        [SerializeField] private string weaponId = "Pistol";

        private bool isOwned = false;

        // Static runtime HashSet to keep track of unlocks in memory during tests without persistence
        private static readonly HashSet<string> runtimeUnlockedWeapons = new HashSet<string>();

        protected override void Start()
        {
            base.Start();
        }

        public void SetupWeapon(string name, string desc, int price, Sprite icon, string[] specs, string id)
        {
            SetupItem(name, desc, price, icon, specs);
            this.weaponId = id;
        }

        protected override bool IsPurchaseable()
        {
            return !isOwned;
        }

        protected override void OnPurchaseSuccess()
        {
            isOwned = true;
            runtimeUnlockedWeapons.Add(weaponId);

            if (SurvivalInventory.Instance != null)
            {
                SurvivalInventory.Instance.UnlockWeapon(weaponId);
                SurvivalInventory.Instance.EquipWeapon(weaponId);
            }
            
            Debug.Log($"[Shop] Weapon unlocked and equipped: {weaponId}");

            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.OnWeaponPurchased(weaponId);
            }
        }

        protected override void OnPurchaseFailed()
        {
            if (!isOwned)
                base.TriggerFailureShake();
        }


        public override void UpdateUIState()
        {
            if (SurvivalInventory.Instance != null)
            {
                isOwned = SurvivalInventory.Instance.IsWeaponUnlocked(weaponId);
            }
            else
            {
                isOwned = runtimeUnlockedWeapons.Contains(weaponId);
            }

            base.UpdateUIState();
            
            if (displayTick != null)
            {
                displayTick.SetActive(isOwned);
            }
        }
    }
}
