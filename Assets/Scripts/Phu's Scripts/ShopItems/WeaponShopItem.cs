using UnityEngine;

namespace PhuScene
{
    public class WeaponShopItem : BaseShopItem
    {
        [Header("Weapon Configuration")]
        [SerializeField] private string weaponId = "Pistol";

        private bool isOwned = false;

        protected override void Start()
        {
            isOwned = PlayerPrefs.GetInt("UnlockedWeapon_" + weaponId, 0) == 1;
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
            PlayerPrefs.SetInt("UnlockedWeapon_" + weaponId, 1);
            PlayerPrefs.Save();
            
            // Trigger gameplay event or notify weapon manager to equip/unlock
            Debug.Log($"[Shop] Weapon unlocked permanently: {weaponId}");
        }

        public override void UpdateUIState()
        {
            base.UpdateUIState();
            if (statusText != null)
            {
                statusText.text = isOwned ? "Owned" : "";
            }
        }
    }
}
