using UnityEngine;

namespace PhuScene
{
    public enum EnemyType
    {
        Basic,
        Elite,
        Boss
    }

    public class EnemyReward : MonoBehaviour
    {
        public EnemyType enemyType;
        public int pointsAwarded;
        public int moneyAwarded;
    }
}
