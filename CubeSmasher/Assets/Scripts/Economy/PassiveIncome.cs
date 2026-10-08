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

    // Этот метод будет вызываться каждый раз, когда игрок переходит на новую стадию
    public void RecalculateIncome(int currentStage)
    {
        CurrentIncomePerSecond = 0;

        // Если машин в конфиге нет — выходим
        if (_config == null || _config.GarageCars == null) return;

        // Суммируем доход открытых машин; открытость берём из GarageService
        for (int i = 0; i < _config.GarageCars.Length; i++)
        {
            if (_garage != null ? _garage.IsUnlocked(i) : currentStage >= _config.GarageCars[i].UnlockStage)
            {
                CurrentIncomePerSecond += _config.GarageCars[i].IncomePerSecond;
            }
        }
    }
}