using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Один слот магазина косметики: картинка модели, название, цена, кнопка.
    ///
    /// РАЗДЕЛЕНИЕ ОТВЕТСТВЕННОСТИ:
    ///  - префаб хранит ВИД: цвета состояний, размеры, шрифты, отступы;
    ///  - код только выбирает, какое состояние сейчас, и подставляет
    ///    цвета из полей этого компонента.
    ///
    /// Раньше цвета были захардкожены в Render() через UITheme, и правки
    /// в префабе затирались при каждом старте. Теперь всё настраивается
    /// в инспекторе: что поставили, то и будет в игре.
    ///
    /// Значения по умолчанию берутся из UITheme, но это только стартовые
    /// значения полей. После создания префаба они становятся обычными
    /// сериализованными данными.
    /// </summary>
    public class CosmeticSlotView : MonoBehaviour
    {
        [Header("Тексты")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _stateText;

        [Header("Графика")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _previewImage;
        [SerializeField] private Image _frame;
        [SerializeField] private Button _button;

        [Header("Акценты")]
        [Tooltip("Полоска-акцент сверху карточки или рамка премиального слота")]
        [SerializeField] private Image _accent;

        [Header("Цвета: за монеты")]
        [SerializeField] private Color _coinBackground = UITheme.MetalDark;
        [SerializeField] private Color _coinAccent = UITheme.NeonGreen;

        [Header("Цвета: премиум")]
        [SerializeField] private Color _premiumBackground = UITheme.MetalDark;
        [SerializeField] private Color _premiumAccent = UITheme.NeonPurple;

        [Header("Цвета: куплено")]
        [SerializeField] private Color _ownedBackground = UITheme.MetalDarkest;
        [SerializeField] private Color _ownedAccent = UITheme.MetalLight;

        [Header("Цвета: надето")]
        [SerializeField] private Color _equippedBackground = UITheme.MetalDarkest;
        [SerializeField] private Color _equippedAccent = UITheme.MetalSheen;

        [Header("Цвета: закрыто")]
        [SerializeField] private Color _lockedBackground = UITheme.MetalDarkest;
        [SerializeField] private Color _lockedAccent = UITheme.TextMuted;

        [Header("Цвета текста")]
        [SerializeField] private Color _nameColor = UITheme.TextPrimary;
        [SerializeField] private Color _priceColor = UITheme.TextPrimary;
        [SerializeField] private Color _stateColor = UITheme.TextSecondary;
        [SerializeField] private Color _stateColorEquipped = UITheme.NeonGreen;

        [Header("Звук")]
        [Tooltip("Громкость клика по карточке")]
        [SerializeField, Range(0f, 1f)] private float _clickVolume = 0.8f;

        /// <summary>Нажатие. Передаёт ключ "groupId/itemId".</summary>
        public event Action<string> Clicked;

        private string _key;

        /// <summary>
        /// Заполнить ссылки на дочерние элементы. Нужно, когда слот собирается
        /// кодом, а не берётся из префаба. Цвета при этом НЕ трогаются:
        /// они остаются теми, что стоят в полях компонента.
        /// </summary>
        public void Bind(TextMeshProUGUI nameText, TextMeshProUGUI priceText,
                         TextMeshProUGUI stateText, Button button, Image background,
                         Image previewImage = null, Image accent = null, Image frame = null)
        {
            _nameText = nameText;
            _priceText = priceText;
            _stateText = stateText;
            _button = button;
            _background = background;
            _previewImage = previewImage;
            _accent = accent;
            _frame = frame;

            if (_button != null)
            {
                _button.onClick.RemoveListener(RaiseClicked);
                _button.onClick.AddListener(RaiseClicked);
            }
        }

        private void Awake()
        {
            if (_button != null) _button.onClick.AddListener(RaiseClicked);
        }

        public void Initialize(string key) => _key = key;

        public void Render(CosmeticSlotData slot)
        {
            _key = slot.GroupId + "/" + slot.ItemId;

            if (_nameText != null) _nameText.text = slot.Name;

            // Картинка модели. Если её нет (префаб не задан), остаётся пустой
            // прямоугольник — но текст и кнопка работают
            if (_previewImage != null)
            {
                _previewImage.sprite = slot.Preview;
                _previewImage.enabled = slot.Preview != null;
            }

            Color background;
            Color accent;
            string price = string.Empty;
            string state = string.Empty;

            switch (slot.State)
            {
                case CosmeticSlotState.ForCoins:
                    background = _coinBackground;
                    accent = _coinAccent;
                    price = slot.CoinPrice.ToString("N0");
                    state = UiText.Get("shop.buy_coins", price);
                    break;

                case CosmeticSlotState.ForYan:
                    background = _premiumBackground;
                    accent = _premiumAccent;
                    price = slot.YanPrice ?? string.Empty;
                    state = string.IsNullOrEmpty(price)
                        ? UiText.Get("ui.buy")
                        : UiText.Get("shop.buy_yan", price);
                    break;

                case CosmeticSlotState.Owned:
                    background = _ownedBackground;
                    accent = _ownedAccent;
                    state = UiText.Get("skins.equip");
                    break;

                case CosmeticSlotState.Equipped:
                    background = _equippedBackground;
                    accent = _equippedAccent;
                    state = UiText.Get("skins.equipped");
                    break;

                case CosmeticSlotState.Locked:
                    background = _lockedBackground;
                    accent = _lockedAccent;
                    state = UiText.Get("shop.unlock", slot.UnlockStage);
                    break;

                case CosmeticSlotState.Unavailable:
                    background = _lockedBackground;
                    accent = _lockedAccent;
                    state = UiText.Get("skins.unavailable");
                    break;

                default:
                    background = _lockedBackground;
                    accent = _lockedAccent;
                    break;
            }

            if (_background != null) _background.color = background;
            if (_accent != null) _accent.color = accent;
            if (_frame != null) _frame.color = accent;
            if (_nameText != null) _nameText.color = _nameColor;
            if (_priceText != null) _priceText.color = _priceColor;

            if (_stateText != null)
            {
                _stateText.text = state;
                _stateText.color = slot.State == CosmeticSlotState.Equipped
                    ? _stateColorEquipped
                    : _stateColor;
            }

            if (_priceText != null)
            {
                // Для купленных и недоступных цена не нужна: состояние уже всё сказало
                bool showPrice = slot.State == CosmeticSlotState.ForCoins
                              || slot.State == CosmeticSlotState.ForYan;
                _priceText.text = showPrice ? price : string.Empty;
            }
        }

        private void RaiseClicked()
        {
            PlayClick();
            Clicked?.Invoke(_key);
        }

        /// <summary>
        /// Короткий клик через AudioService: слоты создаются кодом,
        /// поэтому ButtonClickSound на них не висит.
        /// </summary>
        private void PlayClick()
        {
            if (!CubeSmasher.Infrastructure.ServiceLocator.Instance.TryGet(
                    out CubeSmasher.Infrastructure.AudioService audio)) return;

            audio.PlaySfxVaried(CubeSmasher.UI.AudioClipLibrary.Click, _clickVolume);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(RaiseClicked);
        }
    }
}
