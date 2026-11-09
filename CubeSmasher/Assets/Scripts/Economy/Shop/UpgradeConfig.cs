using UnityEngine;
using UnityEngine.Localization;

public enum GrowthType { Linear, Exponential }

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Tycoon/Upgrade Config")]
public class UpgradeConfig : ScriptableObject
{
    [Header("Идентификация (Связь с SavesYG)")]
    public StatType Stat;   // Что улучшает апгрейд (выбирается из списка)

    /// <summary>
    /// Ключ для сохранения. Собирается из стата И стадии открытия, поэтому уникален:
    /// у одного стата может быть несколько тиров, и они не перезапишут друг друга.
    /// ВНИМАНИЕ: менять UnlockStageLevel у выпущенного апгрейда нельзя - слетит прогресс.
    /// </summary>
    public string Id => Stat + "_" + UnlockStageLevel;

    [Header("Визуал UI")]
    public string UpgradeName;

    [Header("Название (локализация)")]
    [Tooltip("Ключ из String Table UI. Если пусто, берётся UpgradeName")]
    public LocalizedString LocalizedName;

    /// <summary>Название на текущем языке. Используется в магазине.</summary>
    public string DisplayName => LocalizedName != null && !LocalizedName.IsEmpty
        ? LocalizedName.GetLocalizedString()
        : UpgradeName;
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
    /// Строка "было -> станет" для карточки магазина. Пусто на максимальном уровне.
    /// otherContribution - вклад базы, остальных тиров и бонуса машины, чтобы показывать
    /// итоговую характеристику, а не прибавку одного тира.
    /// </summary>
    public string FormatDelta(int currentLevel, float otherContribution = 0f)
    {
        if (currentLevel >= MaxLevel) return string.Empty;

        float from = otherContribution + CalculateValue(currentLevel);
        float to   = otherContribution + CalculateValue(currentLevel + 1);

        string fmt  = "F" + Mathf.Clamp(Decimals, 0, 3);
        string stat = string.IsNullOrEmpty(StatNameKey) ? string.Empty : UiText.Get(StatNameKey) + " ";
        string unit = string.IsNullOrEmpty(UnitKey) ? string.Empty : " " + UiText.Get(UnitKey);

        return stat + from.ToString(fmt) + unit + " \u2192 " + to.ToString(fmt) + unit;
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