using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Награды за разрушение: сколько монет приносит разбитая деталь
    /// и как быстро награда растёт с поколением осколка.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "CubeSmasher/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Награда за детали")]
        [Tooltip("Базовая награда за деталь поколения 0")]
        public double BasePartReward = 5;

        [Tooltip("Множитель награды за каждое следующее поколение осколка")]
        public float PartRewardMultiplier = 1.25f;

        [Tooltip("Множитель награды за каждую следующую стадию. Растёт вместе с HP деталей, иначе монет на единицу HP падает")]
        public float StageRewardMultiplier = 1.14f;

        [Header("ХП деталей")]
        [Tooltip("ХП детали по умолчанию (поколение 0 и любые неописанные)")]
        public float BaseHp = 50f;

        [Tooltip("ХП осколка поколения 1")]
        public float BaseHpGen1 = 75f;

        [Tooltip("ХП осколка поколения 2")]
        public float BaseHpGen2 = 100f;

        [Tooltip("Множитель ХП за каждую следующую стадию")]
        public float HpGrowthPerStage = 1.13f;
    }
}
