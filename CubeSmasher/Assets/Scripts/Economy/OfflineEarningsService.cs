using System;
using CubeSmasher.Core.Interfaces;
using UnityEngine;
using YG;

/// <summary>
/// Начисляет доход за время, которое игрок отсутствовал.
/// Логика перенесена из Bootstrapper без изменений в расчётах.
/// </summary>
public class OfflineEarningsService
{
    /// <summary>Больше 4 часов отсутствия не учитываем. Решение владельца проекта (PLAN_V2, T18).</summary>
    private const long MaxOfflineSeconds = 14400;
    private const double OfflineIncomeFactor = 0.1;

    private readonly IPassiveIncomeService _passiveIncome;

    public OfflineEarningsService(IPassiveIncomeService passiveIncome)
    {
        _passiveIncome = passiveIncome;
    }

    /// <summary>Считает оффлайн-доход без начисления. Возвращает 0, если начислять нечего.</summary>
    public double Calculate()
    {
        long lastSaveTime = YG2.saves.lastSaveTime;
        if (lastSaveTime <= 0) return 0;

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long offlineSeconds = currentTime - lastSaveTime;

        if (offlineSeconds > MaxOfflineSeconds)
        {
            offlineSeconds = MaxOfflineSeconds;
        }

        if (offlineSeconds <= 0) return 0;
        if (_passiveIncome.CurrentIncomePerSecond <= 0) return 0;

        return offlineSeconds * _passiveIncome.CurrentIncomePerSecond * OfflineIncomeFactor;
    }

    public double GetPendingReward()
    {
        if (YG2.saves.pendingOfflineReward > 0) return YG2.saves.pendingOfflineReward;

        double amount = Calculate();
        YG2.saves.pendingOfflineReward = amount;
        YG2.saves.lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return amount;
    }
}
