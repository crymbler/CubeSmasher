using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItemView : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Image _buttonBackground;

    [Header("Цвета по GDD")]
    [SerializeField] private Color _affordableColor = new Color(0.2f, 0.8f, 0.2f); // Зеленый
    [SerializeField] private Color _expensiveColor = new Color(0.5f, 0.5f, 0.5f);  // Серый

    private string _id;
    public event Action<string> OnBuyClicked;

    public void Initialize(string id)
    {
        _id = id;
        _buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(_id));
    }

    // Добавили два параметра в конец: isLocked и unlockStage
    public void UpdateData(Sprite icon, string name, int level, double price, bool canAfford, bool isMaxLevel, bool isLocked, int unlockStage)
    {
        if (_iconImage != null) _iconImage.sprite = icon;
        if (_nameText != null) _nameText.text = name;
        if (_levelText != null) _levelText.text = "Ур. " + level;

        if (isLocked)
        {
            _priceText.text = $"Сцена {unlockStage}"; // Пишем условие разблокировки
            _buttonBackground.color = _expensiveColor;
            _buyButton.interactable = false;
        }
        else if (isMaxLevel)
        {
            _priceText.text = "МАКС";
            _buttonBackground.color = _expensiveColor;
            _buyButton.interactable = false;
        }
        else
        {
            _priceText.text = price.ToString("N0");
            _buttonBackground.color = canAfford ? _affordableColor : _expensiveColor;
            _buyButton.interactable = canAfford;
        }
    }

    private void OnDestroy()
    {
        _buyButton.onClick.RemoveAllListeners();
    }
}