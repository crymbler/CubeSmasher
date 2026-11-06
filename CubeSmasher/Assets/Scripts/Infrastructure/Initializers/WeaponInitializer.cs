using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Передаёт молоту статы из StatService и обновляет их при покупке апгрейда или смене машины.
    /// </summary>
    public class WeaponInitializer : IGameInitializer
    {
        private readonly HammerCaster _hammerCaster;
        private readonly HammerConfig _hammerConfig;

        public WeaponInitializer(HammerCaster hammerCaster, HammerConfig hammerConfig)
        {
            _hammerCaster = hammerCaster;
            _hammerConfig = hammerConfig;
        }

        public void Initialize()
        {
            if (_hammerCaster == null) return;
            if (!ServiceLocator.Instance.TryGet(out StatService stats)) return;

            Apply(stats);
            stats.Changed += () => Apply(stats);
        }

        private void Apply(StatService stats)
        {
            if (_hammerCaster == null) return;

            _hammerCaster.SetCombatStats(
                stats.Get(StatType.HammerDamage),
                stats.Get(StatType.HammerRadius),
                stats.Get(StatType.AttackSpeed),
                stats.Get(StatType.CritChance),
                stats.Get(StatType.CritDamage));

            // Цвета цифр урона задаются в HammerConfig
            if (_hammerConfig != null)
                _hammerCaster.SetDamageColors(_hammerConfig.NormalDamageColor, _hammerConfig.CritDamageColor);
        }
    }
}
