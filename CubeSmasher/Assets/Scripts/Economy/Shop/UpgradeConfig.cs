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

    [Header("Название по языкам (K5). Пусто — берётся UpgradeName")]
    public string NameRu;
    public string NameEn;
    public string NameTr;

    /// <summary>Название на текущем языке. Используется в магазине.</summary>
    public string DisplayName => CubeSmasher.Settings.Localization.Pick(NameRu, NameEn, NameTr, UpgradeName);
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

    [Header("Отображение")]
    [Tooltip("Ключ локализации названия характеристики (используется в K5)")]
    public string StatNameKey;
    [Tooltip("Единица измерения, например с, %, м. Пусто - без единицы")]
    public string UnitKey;
    [Tooltip("Знаков после запятой в строке 'было -> станет'")]
    [Range(0, 3)] public int Decimals = 1;
    [Tooltip("Характеристика уменьшается при улучшении. Пока только хранится, для K5")]
    public bool ValueDecreases;

    /// <summary>
    /// Строка 'было -> станет' для карточки. Пусто на максимальном уровне.
    /// Принято: уровень 0 даёт значение 0, уровень N даёт CalculateValue(N).
    /// </summary>
    public string FormatDelta(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return string.Empty;

        float from = currentLevel <= 0 ? 0f : CalculateValue(currentLevel);
        float to = CalculateValue(currentLevel + 1);
        string fmt = "F" + Mathf.Clamp(Decimals, 0, 3);
        string unit = string.IsNullOrEmpty(UnitKey) ? string.Empty : " " + UnitKey;
        return from.ToString(fmt) + unit + " \u2192 " + to.ToString(fmt) + unit;
    }

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