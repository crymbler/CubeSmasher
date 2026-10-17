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

    private readonly IWallet _wallet;
    private readonly IPassiveIncomeService _passiveIncome;

    public OfflineEarningsService(IWallet wallet, IPassiveIncomeService passiveIncome)
    {
        _wallet = wallet;
        _passiveIncome = passiveIncome;
    }

    /// <summary>Начисляет оффлайн-доход. Возвращает 0, если начислять нечего.</summary>
    public double Apply()
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

        double earned = offlineSeconds * _passiveIncome.CurrentIncomePerSecond;
        _wallet.Add(earned);

        Debug.Log($"[Оффлайн] Игрок отсутствовал {offlineSeconds} сек. Заработано: {earned} монет!");
        return earned;
    }
}
