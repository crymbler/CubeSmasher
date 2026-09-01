using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItemView : MonoBehaviour
{
    [Header("UI Ёлементы")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Image _buttonBackground;

    [Header("÷вета по GDD")]
    [SerializeField] private Color _affordableColor = new Color(0.2f, 0.8f, 0.2f); // «еленый
    [SerializeField] private Color _expensiveColor = new Color(0.5f, 0.5f, 0.5f);  // —ерый

    private string _id;
    public event Action<string> OnBuyClicked;

    public void Initialize(string id)
    {
        _id = id;
        _buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(_id));
    }

    public void UpdateData(Sprite icon, string name, int level, double price, bool canAfford, bool isMaxLevel)
    {
        if (_iconImage != null) _iconImage.sprite = icon;
        if (_nameText != null) _nameText.text = name;
        if (_levelText != null) _levelText.text = "”р. " + level;

        if (isMaxLevel)
        {
            _priceText.text = "ћј —";
            _buttonBackground.color = _expensiveColor;
            _buyButton.interactable = false;
        }
        else
        {
            _priceText.text = price.ToString("N0"); // N0 раздел€ет тыс€чи пробелами
            _buttonBackground.color = canAfford ? _affordableColor : _expensiveColor;
            _buyButton.interactable = canAfford;
        }
    }

    private void OnDestroy()
    {
        _buyButton.onClick.RemoveAllListeners();
    }
}