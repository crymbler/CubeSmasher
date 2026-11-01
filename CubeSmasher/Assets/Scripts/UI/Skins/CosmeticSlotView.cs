using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Один слот магазина косметики: картинка модели, название, цена, кнопка.
    ///
    /// Цвета берутся из UITheme, а не из полей инспектора: раньше цвета были
    /// захардкожены прямо в компоненте и расходились с палитрой игры —
    /// отсюда зелёные и синие прямоугольники вместо неона.
    ///
    /// Состояния различаются не только текстом, но и цветом подложки:
    /// монеты — зелёный, премиум — фиолетовый, надето — синий металл,
    /// закрыто — тёмный. Так карточку можно понять, не читая подпись.
    ///
    /// Звук клика играет здесь же через AudioService: на кнопке магазина нет
    /// ButtonClickSound, потому что слоты создаются кодом, и вешать компонент
    /// на каждый было бы лишней работой.
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

        [Header("Звук")]
        [Tooltip("Громкость клика по карточке")]
        [SerializeField, Range(0f, 1f)] private float _clickVolume = 0.8f;

        /// <summary>Нажатие. Передаёт ключ "groupId/itemId".</summary>
        public event Action<string> Clicked;

        private string _key;

        /// <summary>
        /// Заполнить ссылки на дочерние элементы. Нужно, когда слот собирается
        /// кодом, а не берётся из префаба.
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
                    background = UITheme.MetalDark;
                    accent = UITheme.NeonGreen;
                    price = slot.CoinPrice.ToString("N0");
                    state = UiText.Get("shop.buy_coins", price);
                    break;

                case CosmeticSlotState.ForYan:
                    background = UITheme.MetalDark;
                    accent = UITheme.NeonPurple;
                    price = slot.YanPrice ?? string.Empty;
                    state = string.IsNullOrEmpty(price)
                        ? UiText.Get("ui.buy")
                        : UiText.Get("shop.buy_yan", price);
                    break;

                case CosmeticSlotState.Owned:
                    background = UITheme.MetalDarkest;
                    accent = UITheme.MetalLight;
                    state = UiText.Get("skins.equip");
                    break;

                case CosmeticSlotState.Equipped:
                    background = UITheme.MetalDarkest;
                    accent = UITheme.MetalSheen;
                    state = UiText.Get("skins.equipped");
                    break;

                case CosmeticSlotState.Locked:
                    background = UITheme.MetalDarkest;
                    accent = UITheme.TextMuted;
                    state = UiText.Get("shop.unlock", slot.UnlockStage);
                    break;

                case CosmeticSlotState.Unavailable:
                    background = UITheme.MetalDarkest;
                    accent = UITheme.TextMuted;
                    state = UiText.Get("skins.unavailable");
                    break;

                default:
                    background = UITheme.MetalDark;
                    accent = UITheme.MetalLight;
                    break;
            }

            if (_background != null) _background.color = background;
            if (_accent != null) _accent.color = accent;
            if (_frame != null) _frame.color = accent;
            if (_stateText != null) _stateText.text = state;

            // Надетое состояние выделяем цветом текста, чтобы его было видно сразу
            if (_stateText != null)
            {
                _stateText.color = slot.State == CosmeticSlotState.Equipped
                    ? UITheme.NeonGreen
                    : UITheme.TextSecondary;
            }

            if (_nameText != null) _nameText.color = UITheme.TextPrimary;

            if (_priceText != null)
            {
                bool showPrice = slot.State == CosmeticSlotState.ForCoins
                              || slot.State == CosmeticSlotState.ForYan;
                _priceText.text = showPrice ? price : string.Empty;
                _priceText.color = UITheme.TextPrimary;
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
