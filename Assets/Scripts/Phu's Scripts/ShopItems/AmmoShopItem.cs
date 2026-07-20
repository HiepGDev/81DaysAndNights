using UnityEngine;

namespace PhuScene
{
    public class AmmoShopItem : BaseShopItem
    {
        // [Header("Ammo Configuration")]
        // [SerializeField] private int ammoPerPurchase = 30;

        public void SetupAmmo(string name, string desc, int price, Sprite icon, string[] specs)
        {
            SetupItem(name, desc, price, icon, specs);
        }

        private WeaponSO GetActiveWeaponData()
        {
            // Find active networked gun first
            var survivalGun = FindAnyObjectByType<SurvivalPlayerGun>();
            if (survivalGun != null && survivalGun.enabled && survivalGun.WeaponData != null)
            {
                return survivalGun.WeaponData;
            }

            // Fallback to offline gun
            var playerGun = FindAnyObjectByType<PlayerGun>();
            if (playerGun != null && playerGun.enabled && playerGun.WeaponData != null)
            {
                return playerGun.WeaponData;
            }

            return null;
        }

        protected override bool IsPurchaseable()
        {
            WeaponSO activeWeapon = GetActiveWeaponData();
            if (activeWeapon == null) return false;

            return activeWeapon.reserveAmmo < activeWeapon.maxReserveAmmo;
        }

        protected override void OnPurchaseSuccess()
        {
            WeaponSO activeWeapon = GetActiveWeaponData();
            if (activeWeapon != null)
            {
                activeWeapon.reserveAmmo = activeWeapon.maxReserveAmmo;
                Debug.Log($"[Shop] Replenished ammo. Current reserve: {activeWeapon.reserveAmmo}/{activeWeapon.maxReserveAmmo}");
            }
        }

        protected override void OnPurchaseFailed()
        {
            base.TriggerFailureShake();
        }

        public override void UpdateUIState()
        {
            base.UpdateUIState();

            WeaponSO activeWeapon = GetActiveWeaponData();
            if (statusText != null)
            {
                if (activeWeapon == null)
                {
                    statusText.text = "No Weapon";
                }
                else if (activeWeapon.reserveAmmo >= activeWeapon.maxReserveAmmo)
                {
                    statusText.text = "Full";
                }
                else
                {
                    statusText.text = $"{activeWeapon.reserveAmmo}/{activeWeapon.maxReserveAmmo}";
                }
            }
        }
    }
}
