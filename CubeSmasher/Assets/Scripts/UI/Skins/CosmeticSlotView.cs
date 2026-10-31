using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Один слот магазина косметики. Показывает название, цену и состояние,
    /// сообщает наружу о нажатии. Цвета берёт из UITheme по состоянию:
    /// покупка — зелёная, премиум — розовая, надето — синяя, недоступно — серая.
    ///
    /// Кнопка всегда кликабельна, даже если предмет купить нельзя:
    /// презентер сам решает, что делать, а игрок получает обратную связь
    /// через текст (нужная стадия, недоступно), а не через мёртвую кнопку.
    /// </summary>
    public class CosmeticSlotView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private GameObject _premiumBadge;

        [Header("Цвета состояний")]
        [SerializeField] private Color _coinColor = new Color(0.36f, 0.55f, 0.22f);
        [SerializeField] private Color _premiumColor = new Color(0.63f, 0.20f, 0.63f);
        [SerializeField] private Color _ownedColor = new Color(0.24f, 0.28f, 0.44f);
        [SerializeField] private Color _equippedColor = new Color(0.30f, 0.42f, 0.66f);
        [SerializeField] private Color _lockedColor = new Color(0.20f, 0.22f, 0.30f);

        /// <summary>Нажатие. Передаёт ключ "groupId/itemId".</summary>
        public event Action<string> Clicked;

        private string _key;

        private void Awake()
        {
            if (_button != null) _button.onClick.AddListener(RaiseClicked);
        }

        public void Initialize(string key) => _key = key;

        public void Render(CosmeticSlotData slot)
        {
            _key = slot.GroupId + "/" + slot.ItemId;

            if (_nameText != null) _nameText.text = slot.Name;
            if (_premiumBadge != null) _premiumBadge.SetActive(slot.IsPremium);

            string price = string.Empty;
            string state = string.Empty;
            Color color = _coinColor;

            switch (slot.State)
            {
                case CosmeticSlotState.ForCoins:
                    price = slot.CoinPrice.ToString("N0");
                    state = UiText.Get("shop.buy_coins", price);
                    color = _coinColor;
                    break;

                case CosmeticSlotState.ForYan:
                    // Цена в янах приходит строкой из платформы: "25 YAN".
                    // Пусто — товар ещё не загрузился, показываем название кнопки.
                    price = string.IsNullOrEmpty(slot.YanPrice) ? string.Empty : slot.YanPrice;
                    state = string.IsNullOrEmpty(price)
                        ? UiText.Get("ui.buy")
                        : UiText.Get("shop.buy_yan", price);
                    color = _premiumColor;
                    break;

                case CosmeticSlotState.Owned:
                    state = UiText.Get("skins.equip");
                    color = _ownedColor;
                    break;

                case CosmeticSlotState.Equipped:
                    state = UiText.Get("skins.equipped");
                    color = _equippedColor;
                    break;

                case CosmeticSlotState.Locked:
                    state = UiText.Get("shop.unlock", slot.UnlockStage);
                    color = _lockedColor;
                    break;

                case CosmeticSlotState.Unavailable:
                    state = UiText.Get("skins.unavailable");
                    color = _lockedColor;
                    break;
            }

            if (_stateText != null) _stateText.text = state;
            if (_background != null) _background.color = color;

            if (_priceText != null)
            {
                // Для купленных и недоступных цена не нужна: состояние уже всё сказало
                bool showPrice = slot.State == CosmeticSlotState.ForCoins
                              || slot.State == CosmeticSlotState.ForYan;
                _priceText.text = showPrice ? price : string.Empty;
            }
        }

        private void RaiseClicked() => Clicked?.Invoke(_key);

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveAllListeners();
        }
    }
}
