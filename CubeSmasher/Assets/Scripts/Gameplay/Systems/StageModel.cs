using System;

public class StageModel
{
    private int _currentStage;
    private int _currentDetails;
    private int _requiredDetails;

    // —обытие дл€ UI: (“екущие детали, “ребуемые детали, Ќазвание уровн€)
    public event Action<int, int, int> OnProgressChanged; // (детали, нужно, номер стадии)

    // —обытие дл€ ядра игры: "”ровень пройден, вот номер нового!"
    public event Action<int> OnStageCompleted;

    // —обытие дл€ сохранени€: прогресс стадии изменилс€ (текущие детали)
    public event Action<int> OnDetailsChanged;

    public int CurrentStage => _currentStage;

    /// <summary>—колько деталей разбито на текущей стадии (дл€ сохранени€).</summary>
    public int CurrentDetails => _currentDetails;

    /// <summary>—колько деталей нужно разбить на текущей стадии.</summary>
    public int RequiredDetails => _requiredDetails;

    public StageModel(int initialStage, int initialDetails = 0)
    {
        // —тади€ 0 Ч стартова€: до первой машины нужно собрать 10 деталей
        _currentStage = initialStage < 0 ? 0 : initialStage;
        CalculateRequiredDetails();

        // ЌезавершЄнный прогресс стадии из сохранени€, но не больше требуемого
        _currentDetails = initialDetails > 0 ? Math.Min(initialDetails, _requiredDetails - 1) : 0;
    }

    // »нициализаци€ UI при старте
    public void ForceUpdateUI()
    {
        OnProgressChanged?.Invoke(_currentDetails, _requiredDetails, _currentStage);
    }

    // ƒобавл€ем прогресс (вызываетс€ при уничтожении мелкого кубика)
    public void AddProgress(int amount = 1)
    {
        _currentDetails += amount;

        OnProgressChanged?.Invoke(_currentDetails, _requiredDetails, _currentStage);
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

        // ”ведомл€ем игру, что пора спавнить новую машину
        OnStageCompleted?.Invoke(_currentStage);
        OnProgressChanged?.Invoke(_currentDetails, _requiredDetails, _currentStage);
        OnDetailsChanged?.Invoke(_currentDetails);
    }

    private void CalculateRequiredDetails()
    {
        // ѕроста€ математика: 1 уровень = 50 осколков, каждый следующий на 25 больше
        // ћожно вынести эту формулу в EconomyConfig, если захочешь
        _requiredDetails = _currentStage == 0 ? 10 : 50 + (_currentStage - 1) * 25;
    }
}