using UnityEngine;

public class OfflineEconomyCalculator
{
    private const float PenaltyCoefficient = 0.5f; // Игрок получает только 50%
    private const int MaxOfflineSeconds = 14400; // Лимит 4 часа

    // Метод принимает: доход в секунду, время выхода, время входа
    public double CalculateOfflineReward(double incomePerSecond, long lastSaveTimestamp, long currentTimestamp)
    {
        if (incomePerSecond <= 0 || lastSaveTimestamp <= 0) return 0;

        // Разница в секундах
        long awaySeconds = currentTimestamp - lastSaveTimestamp;

        // Ранний выход, если разница отрицательная или нулевая
        if (awaySeconds <= 0) return 0;

        // Применяем Hard Cap (максимум 4 часа)
        if (awaySeconds > MaxOfflineSeconds)
        {
            awaySeconds = MaxOfflineSeconds;
        }

        // Формула из GDD: OfflineReward = TI * Time * Penalty
        double reward = incomePerSecond * awaySeconds * PenaltyCoefficient;

        return reward;
    }
}