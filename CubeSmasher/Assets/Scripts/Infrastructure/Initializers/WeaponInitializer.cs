using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using UnityEngine;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Передаёт молоту статы из StatService и обновляет их при покупке апгрейда или смене машины.
    /// </summary>
    public class WeaponInitializer : IGameInitializer
    {
        private readonly HammerCaster _hammerCaster;
        private readonly StatService _stats;
        private readonly HammerConfig _hammerConfig;

        public WeaponInitializer(HammerCaster hammerCaster, StatService stats, HammerConfig hammerConfig)
        {
            _hammerCaster = hammerCaster;
            _stats = stats;
            _hammerConfig = hammerConfig;
        }

        public void Initialize()
        {
            if (_hammerCaster == null || _stats == null) return;

            Apply();
            _stats.Changed += Apply;
        }

        private void Apply()
        {
            if (_hammerCaster == null) return;

            _hammerCaster.SetCombatStats(
                _stats.Get(StatType.HammerDamage),
                _stats.Get(StatType.HammerRadius),
                _stats.Get(StatType.AttackSpeed),
                _stats.Get(StatType.CritChance),
                _stats.Get(StatType.CritDamage));

            if (_hammerConfig != null)
                _hammerCaster.SetDamageColors(_hammerConfig.NormalDamageColor, _hammerConfig.CritDamageColor);
        }
    }
}
