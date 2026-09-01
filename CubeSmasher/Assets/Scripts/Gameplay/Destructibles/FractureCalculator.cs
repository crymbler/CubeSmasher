using UnityEngine;

public class FractureCalculator
{
    // Теперь кубы делятся почти всегда, создавая хаос
    public bool TrySplit(int generation, float baseSplitChance)
    {
        // Самые мелкие осколки (Gen 2) не делятся никогда
        if (generation >= 2) return false;

        // Поколение 0 делится в 100% случаев. Поколение 1 - с шансом 80-90%
        float chance = generation == 0 ? 1f : baseSplitChance;

        return Random.value <= chance;
    }

    // Инвертированная логика ХП: мелкие кубики прочнее!
    public float CalculateHP(int generation, int stage)
    {
        // Базовые значения для каждого поколения
        float baseHp = 50f;
        if (generation == 1) baseHp = 75f;
        if (generation == 2) baseHp = 100f;

        // Умножаем на сложность стадии (каждая стадия делает детали толще)
        if (stage < 1) stage = 1;
        return baseHp * Mathf.Pow(1.45f, stage - 1);
    }
}