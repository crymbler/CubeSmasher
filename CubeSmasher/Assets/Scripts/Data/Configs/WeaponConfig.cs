using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Базовые параметры молота и формулы роста по уровням.
    ///
    /// ВАЖНО: сейчас эти значения не читаются кодом игры. Фактический урон
    /// берётся из ассетов UpgradeConfig магазина (HammerDamage, HammerRadius)
    /// и применяется через WeaponInitializer. Конфиг хранит базовые значения
    /// и формулы для будущего баланса.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "CubeSmasher/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Урон молота")]
        public float BaseDamage = 10f;
        public float DamageMultiplier = 1.20f; // рост за уровень, 20%
        public double BaseDamageCost = 50;
        public float DamageCostMultiplier = 1.35f; // рост цены, 35%

        [Header("Радиус сплеша")]
        public float BaseRadius = 1.0f;
        public float RadiusStep = 0.15f;
        public double BaseRadiusCost = 150;
        public float RadiusCostMultiplier = 1.50f;

        [Header("Критический удар")]
        public float BaseCritChance = 0.01f;
        public float CritChanceStep = 0.01f;
        public float MaxCritChance = 0.35f;
        public float CritMultiplier = 5f;

        public float GetDamage(int level) => BaseDamage * Mathf.Pow(DamageMultiplier, level - 1);
        public double GetDamageCost(int level) => BaseDamageCost * Mathf.Pow(DamageCostMultiplier, level - 1);
        public float GetRadius(int level) => BaseRadius + RadiusStep * (level - 1);
        public double GetRadiusCost(int level) => BaseRadiusCost * Mathf.Pow(RadiusCostMultiplier, level - 1);
        public float GetCritChance(int level) => Mathf.Clamp(BaseCritChance + CritChanceStep * (level - 1), 0, MaxCritChance);
    }
}
