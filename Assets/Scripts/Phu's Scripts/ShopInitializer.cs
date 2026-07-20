using System.Collections.Generic;
using UnityEngine;

namespace PhuScene
{
    public class ShopInitializer : MonoBehaviour
    {
        [SerializeField] private ShopUI shopUI;

        [Header("Containers Item Lists")]
        [SerializeField] private List<ShopSO> container1Items;
        [SerializeField] private List<ShopSO> container2Items;
        [SerializeField] private List<ShopSO> container3Items;

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

        private void SpawnItems(Transform parent, List<ShopSO> configs)
        {
            if (parent == null || configs == null) return;

            foreach (var config in configs)
            {
                if (config == null) continue;

                switch (config.itemType)
                {
                    case ShopSO.ShopItemType.Weapon:
                        shopUI.CreateWeaponCell(parent, config.itemName, config.itemDescription, config.price, config.icon, config.specs, config.ItemId);
                        break;
                    case ShopSO.ShopItemType.Ammo:
                        shopUI.CreateAmmoCell(parent, config.itemName, config.itemDescription, config.price, config.icon, config.specs);
                        break;
                    case ShopSO.ShopItemType.Ally:
                        shopUI.CreateAllyCell(parent, config.itemName, config.itemDescription, config.price, config.icon, config.specs, config.allyPrefab, config.maxAlliesCount);
                        break;
                }
            }
        }
    }
}
