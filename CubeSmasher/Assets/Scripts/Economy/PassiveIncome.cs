using System;

public class PassiveIncome
{
    public event Action<double> OnIncomeGenerated;

    // Событие, если захочешь потом вывести цифру "Доход: Х/сек" в UI
    public event Action<double> OnIncomeRecalculated;

    public double CurrentIncomePerSecond { get; private set; }

    private float _timer = 0f;
    private readonly GameConfig _config;

    // Теперь он получает конфиг при рождении
    public PassiveIncome(GameConfig config)
    {
        _config = config;
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
        if (_config.GarageCars == null) return;

        // Проходимся по всем машинам и суммируем доход тех, которые открыты
        foreach (var car in _config.GarageCars)
        {
            if (currentStage >= car.UnlockStage)
            {
                CurrentIncomePerSecond += car.IncomePerSecond;
            }
        }

        OnIncomeRecalculated?.Invoke(CurrentIncomePerSecond);
    }
}