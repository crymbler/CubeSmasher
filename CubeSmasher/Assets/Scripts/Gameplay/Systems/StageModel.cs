using System;

public class StageModel
{
    private int _currentStage;
    private int _currentDetails;
    private int _requiredDetails;

    // Событие для UI: (Текущие детали, Требуемые детали, Название уровня)
    public event Action<int, int, string> OnProgressChanged;

    // Событие для Ядра игры: "Уровень пройден, вот номер нового!"
    public event Action<int> OnStageCompleted;

    public int CurrentStage => _currentStage;

    public StageModel(int initialStage)
    {
        _currentStage = initialStage < 1 ? 1 : initialStage;
        CalculateRequiredDetails();
    }

    // Инициализация UI при старте
    public void ForceUpdateUI()
    {
        OnProgressChanged?.Invoke(_currentDetails, _requiredDetails, $"Машина {_currentStage}");
    }

    // Добавляем прогресс (вызывается при уничтожении мелкого кубика)
    public void AddProgress(int amount = 1)
    {
        _currentDetails += amount;

        OnProgressChanged?.Invoke(_currentDetails, _requiredDetails, $"Машина {_currentStage}");

        if (_currentDetails >= _requiredDetails)
        {
            CompleteStage();
        }
    }

    private void CompleteStage()
    {
        _currentStage++;
        _currentDetails = 0;
        CalculateRequiredDetails();

        // Уведомляем игру, что пора спавнить новую машину
        OnStageCompleted?.Invoke(_currentStage);
        OnProgressChanged?.Invoke(_currentDetails, _requiredDetails, $"Машина {_currentStage}");
    }

    private void CalculateRequiredDetails()
    {
        // Простая математика: 1 уровень = 50 осколков, каждый следующий на 25 больше
        // Можно вынести эту формулу в GameConfig, если захочешь
        _requiredDetails = 50 + (_currentStage - 1) * 25;
    }
}