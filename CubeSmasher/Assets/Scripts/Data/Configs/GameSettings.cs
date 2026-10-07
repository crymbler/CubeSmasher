using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Корневой конфиг игры: держит ссылки на тематические конфиги, чтобы
    /// каждая система видела только свои настройки. Заменяет прежний
    /// монолитный GameConfig, где лежали и оружие, и физика, и гараж.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "CubeSmasher/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Тематические конфиги")]
        [Tooltip("Награды за разбитые детали")]
        public EconomyConfig Economy;

        [Tooltip("Спавн, пул объектов и раскол деталей")]
        public PhysicsConfig Physics;

        [Tooltip("Машины гаража и их пассивный доход")]
        public GarageConfig Garage;
    }
}
