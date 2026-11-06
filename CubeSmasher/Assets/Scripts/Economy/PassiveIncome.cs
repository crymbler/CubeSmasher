using System;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;

public class PassiveIncome : IPassiveIncomeService
{
    public event Action<double> OnIncomeGenerated;


    public double CurrentIncomePerSecond { get; private set; }

    private float _timer = 0f;
    private readonly GarageConfig _config;

    // Теперь он получает конфиг при рождении
    private readonly IGarageService _garage;

    public PassiveIncome(GarageConfig config, IGarageService garage = null)
    {
        _config = config;
        _garage = garage;
    }

    public void Tick(float deltaTime)
    {
        if (CurrentIncomePerSecond <= 0) return;

        _timer += deltaTime;

        if (_timer >= 1f)
        {
            OnIncomeGenerated?.Invoke(CurrentIncomePerSecond);
            _timer -= 1f;
        }
    }

    /// <summary>
    /// Доход даёт только выбранная машина, и только если она открыта.
    /// Сумму по всем открытым машинам убрали: теперь игрок выбирает одну.
    /// </summary>
    public void RecalculateIncome(int currentStage)
    {
        CurrentIncomePerSecond = 0;

        if (_config == null || _config.GarageCars == null || _config.GarageCars.Length == 0) return;

        int selected = _garage != null ? _garage.SelectedIndex : 0;
        if (selected < 0 || selected >= _config.GarageCars.Length) return;

        bool opened = _garage != null
            ? _garage.IsOpened(selected)
            : currentStage >= _config.GarageCars[selected].UnlockStage;

        if (opened) CurrentIncomePerSecond = _config.GarageCars[selected].IncomePerSecond;
    }
}