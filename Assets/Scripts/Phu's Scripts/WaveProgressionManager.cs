using UnityEngine;
using System.Collections.Generic;

namespace PhuScene
{
    [System.Serializable]
    public struct EnemySpawnRule
    {
        [Tooltip("The enemy prefab to spawn.")]
        public GameObject enemyPrefab;

        [Tooltip("The wave number at which this enemy starts spawning (inclusive).")]
        public int startWave;

        [Tooltip("Relative weight weight of spawning this enemy type when active.")]
        [Range(0f, 100f)] public float spawnChanceWeight;
    }

    public class WaveProgressionManager : MonoBehaviour
    {
        [Header("Progression Setup")]
        [SerializeField] private List<EnemySpawnRule> enemyRules = new List<EnemySpawnRule>();

        /// <summary>
        /// Selects an enemy prefab dynamically matching the active progression rules for the given wave.
        /// </summary>
        /// <param name="wave">The current wave round number.</param>
        /// <returns>Selected enemy prefab, or null if no active rules match.</returns>
        public GameObject ChooseEnemyPrefabForWave(int wave)
        {
            List<EnemySpawnRule> activeRules = new List<EnemySpawnRule>();
            float totalWeight = 0f;

            foreach (var rule in enemyRules)
            {
                if (rule.enemyPrefab != null && wave >= rule.startWave && rule.spawnChanceWeight > 0f)
                {
                    activeRules.Add(rule);
                    totalWeight += rule.spawnChanceWeight;
                }
            }

            if (activeRules.Count == 0)
            {
                return null;
            }

            // Weighted random selection
            float randomValue = Random.Range(0f, totalWeight);
            float currentSum = 0f;

            foreach (var rule in activeRules)
            {
                currentSum += rule.spawnChanceWeight;
                if (randomValue <= currentSum)
                {
                    return rule.enemyPrefab;
                }
            }

            return activeRules[0].enemyPrefab;
        }
    }
}
