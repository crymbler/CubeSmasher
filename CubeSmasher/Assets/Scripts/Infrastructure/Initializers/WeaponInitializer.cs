using CubeSmasher.Core.Interfaces;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Ставит молоту урон из сохранённого апгрейда и обновляет его при покупке.
    /// </summary>
    public class WeaponInitializer : IGameInitializer
    {
        private readonly HammerCaster _hammerCaster;

        public WeaponInitializer(HammerCaster hammerCaster)
        {
            _hammerCaster = hammerCaster;
        }

        public void Initialize()
        {
            if (_hammerCaster == null) return;
            if (!ServiceLocator.Instance.TryGet(out IShopService shopService)) return;

            ApplyDamage(shopService);
            ApplyRadius(shopService);

            shopService.OnUpgradePurchased += upgradeId =>
            {
                if (upgradeId == "hammer_damage") ApplyDamage(shopService);
                if (upgradeId == "hammer_radius") ApplyRadius(shopService);
            };
        }

        private void ApplyRadius(IShopService shopService)
        {
            var radiusData = shopService.GetUpgradeData("hammer_radius");
            if (radiusData != null) _hammerCaster.SetHitRadius((float)radiusData.value);
        }

        private void ApplyDamage(IShopService shopService)
        {
            var damageData = shopService.GetUpgradeData("hammer_damage");
            if (damageData != null)
            {
                _hammerCaster.SetDamage((float)damageData.value);
            }
        }
    }
}
