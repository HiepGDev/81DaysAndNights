using System.Collections.Generic;
using UnityEngine;

namespace PhuScene
{
    [System.Serializable]
    public struct ShopItemConfig
    {
        public string name;
        [TextArea(2, 4)] public string description;
        public int price;
        public Sprite icon;
        public string[] specs;

        public enum ShopItemType { Weapon, Ammo, Ally }
        public ShopItemType itemType;

        [Header("Weapon Configurations")]
        public string weaponId;

        [Header("Ally Configurations")]
        public GameObject allyPrefab;
        public int maxAlliesCount;
    }

    public class ShopInitializer : MonoBehaviour
    {
        [SerializeField] private ShopUI shopUI;

        [Header("Containers Item Lists")]
        [SerializeField] private List<ShopItemConfig> container1Items;
        [SerializeField] private List<ShopItemConfig> container2Items;
        [SerializeField] private List<ShopItemConfig> container3Items;

        private void Start()
        {
            if (shopUI == null)
            {
                shopUI = GetComponent<ShopUI>();
            }

            if (shopUI != null)
            {
                InitializeShop();
            }
        }

        private void InitializeShop()
        {
            ClearContainer(shopUI.Container1);
            ClearContainer(shopUI.Container2);
            ClearContainer(shopUI.Container3);

            SpawnItems(shopUI.Container1, container1Items);
            SpawnItems(shopUI.Container2, container2Items);
            SpawnItems(shopUI.Container3, container3Items);
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void SpawnItems(Transform parent, List<ShopItemConfig> configs)
        {
            if (parent == null || configs == null) return;

            foreach (var config in configs)
            {
                switch (config.itemType)
                {
                    case ShopItemConfig.ShopItemType.Weapon:
                        shopUI.CreateWeaponCell(parent, config.name, config.description, config.price, config.icon, config.specs, config.weaponId);
                        break;
                    case ShopItemConfig.ShopItemType.Ammo:
                        shopUI.CreateAmmoCell(parent, config.name, config.description, config.price, config.icon, config.specs);
                        break;
                    case ShopItemConfig.ShopItemType.Ally:
                        shopUI.CreateAllyCell(parent, config.name, config.description, config.price, config.icon, config.specs, config.allyPrefab, config.maxAlliesCount);
                        break;
                }
            }
        }
    }
}
