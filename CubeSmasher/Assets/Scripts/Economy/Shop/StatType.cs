/// <summary>
/// Все статы игры. Выбираются из списка в инспекторе — опечаток быть не может.
/// Используется в апгрейдах магазина (UpgradeConfig.Stat) и в бонусах машин (CarBonus).
/// </summary>
public enum StatType
{
    HammerDamage,
    HammerRadius,
    CritChance,
    CritDamage,
    SpawnParts,
    RespawnDelay,
    BigPartsChance,
}
