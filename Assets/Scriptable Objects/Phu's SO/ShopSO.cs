using UnityEngine;

namespace PhuScene
{
    [CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item")]
    public class ShopSO : ScriptableObject
    {
        [Header("Shop Display Information")]
        public string itemName = "Item Name";
        [TextArea(2, 4)] public string itemDescription = "Item Description";
        public int price = 100;
        public Sprite icon;
        public string[] specs;

        public enum ShopItemType { Weapon, Ammo, Ally }
        [Header("Item Configuration")]
        public ShopItemType itemType;

        [Header("Weapon Settings")]
        [Tooltip("The weapon prefab instantiated on the player.")]
        public GameObject weaponPrefab;

        [Header("Ally Settings")]
        [Tooltip("The teammate prefab spawned in the scene.")]
        public GameObject allyPrefab;
        [Tooltip("The maximum active teammates allowed.")]
        public int maxAlliesCount = 3;

        /// <summary>
        /// Resolves the unique item or weapon ID dynamically from the asset's name.
        /// </summary>
        public string ItemId => this.name;
    }
}
