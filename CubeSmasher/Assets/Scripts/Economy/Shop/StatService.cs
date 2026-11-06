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

    public StatService(HammerConfig hammer, PhysicsConfig physics,
                       IShopService shop, IGarageService garage)
    {
        _hammer = hammer;
        _physics = physics;
        _shop = shop;
        _garage = garage;

        if (_shop != null) _shop.OnUpgradePurchased += _ => Changed?.Invoke();
        if (_garage != null) _garage.SelectionChanged += _ => Changed?.Invoke();
    }

    /// <summary>Итоговое значение стата: апгрейд плюс бонус выбранной машины.</summary>
    public float Get(StatType stat) => UpgradeOrBase(stat) + CarBonus(stat);

    private float UpgradeOrBase(StatType stat)
    {
        if (_shop != null)
        {
            var data = _shop.GetUpgradeData(stat.ToString());
            if (data != null) return (float)data.value;
        }
        return BaseValue(stat);
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
