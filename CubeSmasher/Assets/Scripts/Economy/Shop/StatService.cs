using System;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;

/// <summary>
/// Единственное место, где считается итоговое значение стата:
/// значение апгрейда (оно уже включает базу из ассета апгрейда) плюс бонус выбранной машины.
/// Если апгрейда для стата нет, берётся база из HammerConfig или PhysicsConfig.
/// </summary>
public class StatService
{
    /// <summary>Статы изменились: купили апгрейд или сменили машину. Перечитайте значения.</summary>
    public event Action Changed;

    private readonly HammerConfig _hammer;
    private readonly PhysicsConfig _physics;
    private readonly IShopService _shop;
    private readonly IGarageService _garage;
    private readonly StageModel _stage;

    public StatService(HammerConfig hammer, PhysicsConfig physics,
                       IShopService shop, IGarageService garage, StageModel stage = null)
    {
        _hammer = hammer;
        _physics = physics;
        _shop = shop;
        _garage = garage;
        _stage = stage;

        if (_shop != null) _shop.OnUpgradePurchased += _ => Changed?.Invoke();
        if (_garage != null) _garage.SelectionChanged += _ => Changed?.Invoke();
    }

    /// <summary>
    /// Итоговое значение стата: база из конфига + вклад ВСЕХ открытых тиров этого стата
    /// + бонус выбранной машины. Тиров у одного стата несколько, поэтому складываем.
    /// </summary>
    public float Get(StatType stat) => BaseValue(stat) + UpgradeSum(stat) + CarBonus(stat);

    /// <summary>Итог без одного конкретного тира: для карточки магазина.</summary>
    public float GetContributionExcept(StatType stat, UpgradeConfig except)
        => BaseValue(stat) + UpgradeSum(stat, except) + CarBonus(stat);

    private float UpgradeSum(StatType stat, UpgradeConfig except = null)
    {
        if (_shop == null || _shop.AllConfigs == null) return 0f;

        int stage = _stage != null ? _stage.CurrentStage : 0;
        float sum = 0f;

        for (int i = 0; i < _shop.AllConfigs.Count; i++)
        {
            UpgradeConfig cfg = _shop.AllConfigs[i];
            if (cfg == null || cfg.Stat != stat || cfg == except) continue;
            if (stage < cfg.UnlockStageLevel) continue;

            var data = _shop.GetUpgradeData(cfg.Id);
            if (data != null) sum += (float)data.value;
        }
        return sum;
    }

    private float BaseValue(StatType stat)
    {
        switch (stat)
        {
            case StatType.HammerDamage: return _hammer != null ? _hammer.BaseDamage : 10f;
            case StatType.HammerRadius: return _hammer != null ? _hammer.BaseHitRadius : 1.5f;
            case StatType.AttackSpeed:  return _hammer != null ? _hammer.BaseAttackSpeed : 1f;
            case StatType.CritChance:   return _hammer != null ? _hammer.BaseCritChance : 0f;
            case StatType.CritDamage:   return _hammer != null ? _hammer.BaseCritDamage : 2f;
            case StatType.SpawnParts:   return _physics != null ? _physics.StartPartsPerWave : 2f;
            case StatType.RespawnDelay: return _physics != null ? _physics.RespawnDelay : 2f;
            default: return 0f;
        }
    }

    /// <summary>Сумма бонусов выбранной машины по этому стату.</summary>
    private float CarBonus(StatType stat)
    {
        if (_garage == null) return 0f;

        GarageCarData[] cars = _garage.AllCars;
        int i = _garage.SelectedIndex;
        if (cars == null || i < 0 || i >= cars.Length) return 0f;

        CarBonus bonus = cars[i].Bonus;
        if (bonus == null || bonus.Bonuses == null) return 0f;

        float sum = 0f;
        for (int k = 0; k < bonus.Bonuses.Count; k++)
            if (bonus.Bonuses[k].Stat == stat) sum += bonus.Bonuses[k].Value;
        return sum;
    }
}
