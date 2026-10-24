using CubeSmasher.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopHudView : MonoBehaviour, ITopHudView
{
    [Header("Экономика")]
    [SerializeField] private TextMeshProUGUI _balanceText;

    [Header("Прогресс сборки")]
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _progressText;

    [Header("Доход")]
    [SerializeField] private TextMeshProUGUI _incomeText;

    // Обновляет текст денег. ToString("N0") добавит пробелы между тысячами (1 000 000).
    // Если у тебя остался твой NumberFormatter, можешь использовать его здесь.
    public void UpdateBalance(double balance)
    {
        if (_balanceText != null)
        {
            _balanceText.text = balance.ToString("N0");
        }
    }

    // Обновляет ползунок и текст внутри него (например, "Двигатель: 45 / 100")
    public void UpdateProgress(int currentDetails, int maxDetails, int stage)
    {
        if (_progressBar != null && maxDetails > 0)
        {
            // Плавное наполнение с подсветкой; новая стадия (сброс к 0) ставится сразу
            AnimatedProgressBar.For(_progressBar).SetValue((float)currentDetails / maxDetails);
        }

        if (_progressText != null)
        {
            _progressText.text = $"{Localization.Get("stage.machine", stage)}: {currentDetails} / {maxDetails}";
        }
    }

    // Доход в секунду, например "+12/сек". Пусто, пока открытых машин нет.
    public void UpdateIncome(double incomePerSecond)
    {
        if (_incomeText == null) return;
        _incomeText.text = incomePerSecond > 0
            ? Localization.Get("hud.income", incomePerSecond.ToString("N0"))
            : string.Empty;
    }
}