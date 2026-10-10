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

    // Событие для сохранения: прогресс стадии изменился (текущие детали)
    public event Action<int> OnDetailsChanged;

    public int CurrentStage => _currentStage;

    /// <summary>Сколько деталей разбито на текущей стадии (для сохранения).</summary>
    public int CurrentDetails => _currentDetails;

    /// <summary>Сколько деталей нужно разбить на текущей стадии.</summary>
    public int RequiredDetails => _requiredDetails;

    public StageModel(int initialStage, int initialDetails = 0)
    {
        _currentStage = initialStage < 1 ? 1 : initialStage;
        CalculateRequiredDetails();

        // Незавершённый прогресс стадии из сохранения, но не больше требуемого
        _currentDetails = initialDetails > 0 ? Math.Min(initialDetails, _requiredDetails - 1) : 0;
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
        OnDetailsChanged?.Invoke(_currentDetails);

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
        OnDetailsChanged?.Invoke(_currentDetails);
    }

    private void CalculateRequiredDetails()
    {
        // Простая математика: 1 уровень = 50 осколков, каждый следующий на 25 больше
        // Можно вынести эту формулу в EconomyConfig, если захочешь
        _requiredDetails = 50 + (_currentStage - 1) * 25;
    }
}