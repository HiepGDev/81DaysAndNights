using UnityEngine;

namespace PhuScene
{
    public class AllyShopItem : BaseShopItem
    {
        [Header("Ally Configuration")]
        [SerializeField] private GameObject allyPrefab;
        [SerializeField] private int maxAllies = 3;
        [SerializeField] private float spawnOffsetDistance = 2f;

        public void SetupAlly(string name, string desc, int price, Sprite icon, string[] specs, GameObject prefab, int maxCount)
        {
            SetupItem(name, desc, price, icon, specs);
            this.allyPrefab = prefab;
            this.maxAllies = maxCount;
        }

        private int GetActiveAllyCount()
        {
            TeammateShooting[] teammates = FindObjectsByType<TeammateShooting>(FindObjectsSortMode.None);
            return teammates != null ? teammates.Length : 0;
        }

        protected override bool IsPurchaseable()
        {
            if (allyPrefab == null) return false;
            return GetActiveAllyCount() < maxAllies;
        }

        protected override void OnPurchaseSuccess()
        {
            if (allyPrefab == null) return;

            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            var player = FindFirstObjectByType<SurvivalPlayerHealth>();
            if (player != null)
            {
                spawnPos = player.transform.position + player.transform.forward * spawnOffsetDistance + Vector3.up * 0.5f;
                spawnRot = player.transform.rotation;
            }
            else
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    spawnPos = mainCam.transform.position + mainCam.transform.forward * spawnOffsetDistance;
                    spawnPos.y = 0f;
                }
            }

            GameObject spawnedAlly = Instantiate(allyPrefab, spawnPos, spawnRot);
            spawnedAlly.SetActive(true);
            
            Debug.Log($"[Shop] Spawned new ally. Active allies: {GetActiveAllyCount()}/{maxAllies}");
        }

        protected override void OnPurchaseFailed()
        {
            base.TriggerFailureShake();
        }

        public override void UpdateUIState()
        {
            base.UpdateUIState();

            int activeCount = GetActiveAllyCount();
            if (statusText != null)
            {
                if (activeCount >= maxAllies)
                {
                    statusText.text = "Full";
                }
                else
                {
                    statusText.text = $"Active: {activeCount}/{maxAllies}";
                }
            }
        }
    }
}
