using UnityEngine;

namespace PhuScene
{
    public class AllyShopItem : BaseShopItem
    {
        [Header("Ally Configuration")]
        [SerializeField] private GameObject allyPrefab;
        [SerializeField] private int maxAllies = 10;
        [SerializeField] private float spawnOffsetDistance = 2f;

        public void SetupAlly(string name, string desc, int price, Sprite icon, string[] specs, GameObject prefab, int maxCount)
        {
            SetupItem(name, desc, price, icon, specs);
            this.allyPrefab = prefab;
            this.maxAllies = maxCount;
        }

        private int GetActiveAllyCount()
        {
            TeammateHealth[] allies = FindObjectsByType<TeammateHealth>(FindObjectsSortMode.None);
            if (allies != null && allies.Length > 0)
            {
                int activeCount = 0;
                foreach (var ally in allies)
                {
                    if (ally != null && ally.gameObject.activeInHierarchy && !ally.IsDead)
                    {
                        activeCount++;
                    }
                }
                return activeCount;
            }

            // Fallback for objects that only have TeammateShooting component enabled
            TeammateShooting[] teammates = FindObjectsByType<TeammateShooting>(FindObjectsSortMode.None);
            if (teammates != null)
            {
                int activeCount = 0;
                foreach (var teammate in teammates)
                {
                    if (teammate != null && teammate.enabled && teammate.gameObject.activeInHierarchy)
                    {
                        activeCount++;
                    }
                }
                return activeCount;
            }

            return 0;
        }

        protected override bool IsPurchaseable()
        {
            if (allyPrefab == null) return false;
            return GetActiveAllyCount() < maxAllies;
        }

        protected override void OnPurchaseSuccess()
        {
            if (allyPrefab == null) return;

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.SpawnAlly(allyPrefab);
                Debug.Log($"[Shop] Requested WaveManager to spawn new ally. Active allies: {GetActiveAllyCount()}/{maxAllies}");
            }
        }

        protected override void OnPurchaseFailed()
        {
            base.TriggerFailureShake();
        }

        public override void UpdateUIState()
        {
            base.UpdateUIState();

            int activeCount = GetActiveAllyCount();
            if (displayCountText != null)
            {
                displayCount.gameObject.SetActive(activeCount > 0);
                displayCountText.text = $"{activeCount}/{maxAllies}";
            }
        }
    }
}
