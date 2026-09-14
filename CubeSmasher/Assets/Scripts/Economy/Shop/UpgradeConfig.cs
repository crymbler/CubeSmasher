using UnityEngine;

public enum GrowthType { Linear, Exponential }
public enum UpgradeCategory { Tap, Passive } // Чтобы знать, в какой List сохранять

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Tycoon/Upgrade Config")]
public class UpgradeConfig : ScriptableObject
{
    [Header("Идентификация (Связь с SavesYG)")]
    public string Id;                  // Должен быть уникальным (например: "hammer_damage")
    public UpgradeCategory Category;   // Tap (молот) или Passive (гараж/детали)

    [Header("Визуал UI")]
    public string UpgradeName;
    public Sprite Icon;

    [Header("Лимиты")]
    public int MaxLevel = 50;

    [Header("Разблокировка")]
    [Tooltip("На какой стадии сборки машины откроется это улучшение")]
    public int UnlockStageLevel = 1;

    [Header("Формула Цены (Экспонента)")]
    public double BasePrice = 50;
    public float PriceMultiplier = 1.35f;

    [Header("Формула Характеристики (Урон/Радиус и т.д.)")]
    public GrowthType ValueGrowthType = GrowthType.Exponential;
    public float BaseValue = 10f;
    public float ValueStep = 0.15f;       // Для линейного роста
    public float ValueMultiplier = 1.20f; // Для экспоненциального

    // Математика для расчета СЛЕДУЮЩЕГО уровня
    public double CalculatePrice(int nextLevel)
    {
        if (nextLevel > MaxLevel) return double.MaxValue;
        return BasePrice * Mathf.Pow(PriceMultiplier, nextLevel - 1);
    }

    public float CalculateValue(int nextLevel)
    {
        if (ValueGrowthType == GrowthType.Linear)
        {
            return BaseValue + (ValueStep * (nextLevel - 1));
        }
        else
        {
            return BaseValue * Mathf.Pow(ValueMultiplier, nextLevel - 1);
        }
    }
}