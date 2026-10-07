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
    }
}
