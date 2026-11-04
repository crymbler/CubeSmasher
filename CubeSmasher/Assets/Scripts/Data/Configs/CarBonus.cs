using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Один бонус: какой стат и на сколько меняет.</summary>
[Serializable]
public class StatBonus
{
    public StatType Stat;
    public float Value;
}

/// <summary>Набор бонусов машины. Например: +1 SpawnParts, -0.3 RespawnDelay, +20 HammerDamage.</summary>
[CreateAssetMenu(fileName = "New Car Bonus", menuName = "Tycoon/Car Bonus")]
public class CarBonus : ScriptableObject
{
    public List<StatBonus> Bonuses = new List<StatBonus>();
}
