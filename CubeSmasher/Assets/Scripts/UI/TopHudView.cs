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
    public void UpdateProgress(int currentDetails, int maxDetails, string stageName)
    {
        if (_progressBar != null && maxDetails > 0)
        {
            _progressBar.value = (float)currentDetails / maxDetails;
        }

        if (_progressText != null)
        {
            _progressText.text = $"{stageName}: {currentDetails} / {maxDetails}";
        }
    }
}