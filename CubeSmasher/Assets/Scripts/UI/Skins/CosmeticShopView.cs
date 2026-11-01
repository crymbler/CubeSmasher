using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Экран магазина косметики.
    ///
    /// Вёрстка по макету: группы идут списком, в каждой три слота — два маленьких
    /// за монеты и один крупный премиальный.
    ///
    /// Позиции и размеры считаются здесь числами и ставятся прямо в RectTransform.
    /// Никаких LayoutGroup и ContentSizeFitter внутри экрана: при вложенных
    /// группах с childControlHeight они пересчитывают друг друга, и высота детей
    /// уходит в ноль — слоты получаются размером в пиксель. Ручная раскладка
    /// предсказуема: что посчитали, то и стоит.
    /// </summary>
    public class CosmeticShopView : MonoBehaviour, ICosmeticShopView
    {
        [Header("Каркас")]
        [SerializeField] private RectTransform _groupsContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject[] _hideWhileOpen;

        [Header("Префабы слотов")]
        [Tooltip("Маленький слот за монеты. Пусто — слот соберётся кодом")]
        [SerializeField] private CosmeticSlotView _coinSlotPrefab;
        [Tooltip("Крупный премиальный слот за яны. Пусто — используется слот за монеты")]
        [SerializeField] private CosmeticSlotView _premiumSlotPrefab;

        [Header("Шрифт")]
        [Tooltip("Шрифт с кириллицей: Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset")]
        [SerializeField] private TMP_FontAsset _font;

        [Header("Размеры")]
        [Tooltip("Высота крупного премиального слота. Она же высота ряда со слотами")]
        [SerializeField, Min(160f)] private float _premiumSlotHeight = 420f;
        [Tooltip("Высота заголовка группы. 0 — без заголовков")]
        [SerializeField, Min(0f)] private float _titleHeight = 72f;
        [Tooltip("Расстояние между группами")]
        [SerializeField] private float _groupSpacing = 40f;
        [Tooltip("Зазор между слотами и между заголовком и слотами")]
        [SerializeField] private float _slotSpacing = 16f;
        [Tooltip("Доля ширины ряда, которую занимает премиальная колонка")]
        [SerializeField, Range(0.3f, 0.7f)] private float _premiumShare = 0.55f;
        [Tooltip("С какой стороны крупный премиальный слот: true — справа")]
        [SerializeField] private bool _premiumOnRight = true;
        [Tooltip("Отступ по краям контента")]
        [SerializeField] private float _contentPadding = 8f;

        [Header("Заголовок группы")]
        [SerializeField] private float _titleFontSize = 44f;

        public event System.Action<string> SlotClicked;
        public event System.Action Closed;

        private readonly Dictionary<string, GroupUi> _groups = new Dictionary<string, GroupUi>();
        private readonly Dictionary<string, CosmeticSlotView> _slots = new Dictionary<string, CosmeticSlotView>();

        private float _contentHeight;
        private bool _fontWarned;

        private class GroupUi
        {
            public GameObject Root;
            public CosmeticSlotView[] Slots;
        }

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            SetHidden(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            SetHidden(false);
            Closed?.Invoke();
        }

        public void RenderGroup(CosmeticGroupData group)
        {
            if (!_groups.TryGetValue(group.GroupId, out GroupUi ui))
            {
                ui = CreateGroup(group);
                _groups[group.GroupId] = ui;
                UpdateContentHeight();
            }

            if (ui.Slots == null) return;

            int count = Mathf.Min(ui.Slots.Length, group.Slots != null ? group.Slots.Length : 0);
            for (int i = 0; i < count; i++)
            {
                if (ui.Slots[i] == null) continue;
                ui.Slots[i].Render(group.Slots[i]);
            }
        }

        public void UpdateSlot(CosmeticSlotData slot)
        {
            if (!_slots.TryGetValue(SlotKey(slot), out CosmeticSlotView view) || view == null) return;
            view.Render(slot);
        }

        private GroupUi CreateGroup(CosmeticGroupData group)
        {
            Transform parent = _groupsContainer != null ? _groupsContainer : transform;

            float rowHeight = Mathf.Max(160f, _premiumSlotHeight);
            float titleHeight = Mathf.Max(0f, _titleHeight);
            float titleBlock = titleHeight > 0f ? titleHeight + _slotSpacing : 0f;
            float groupHeight = titleBlock + rowHeight;

            RectTransform root = NewRect("Group_" + group.GroupId, parent);
            SetTopStretch(root, -_contentHeight, groupHeight);

            if (titleHeight > 0f)
            {
                CreateTitle(group.Title, root, titleHeight);
            }

            RectTransform row = NewRect("Slots", root);
            SetTopStretch(row, -titleBlock, rowHeight);

            List<CosmeticSlotData> coins = new List<CosmeticSlotData>();
            List<CosmeticSlotData> premium = new List<CosmeticSlotData>();

            // Премиальный определяем по флагу конфига, а не по состоянию:
            // купленный премиальный предмет остаётся крупной карточкой
            CosmeticSlotData[] slots = group.Slots ?? new CosmeticSlotData[0];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsPremium) premium.Add(slots[i]);
                else coins.Add(slots[i]);
            }

            float coinShare = 1f - _premiumShare;
            float half = _slotSpacing * 0.5f;

            // Колонки задаются долями ширины ряда
            RectTransform coinColumn = NewRect("CoinColumn", row);
            RectTransform premiumColumn = NewRect("PremiumColumn", row);

            if (_premiumOnRight)
            {
                SetColumn(coinColumn, 0f, coinShare, -half);
                SetColumn(premiumColumn, coinShare, 1f, half);
            }
            else
            {
                SetColumn(premiumColumn, 0f, _premiumShare, -half);
                SetColumn(coinColumn, _premiumShare, 0f, half);
            }

            var created = new List<CosmeticSlotView>();

            // Обычные слоты: делят высоту колонки поровну
            int coinCount = coins.Count;
            float coinHeight = coinCount > 0
                ? (rowHeight - _slotSpacing * (coinCount - 1)) / coinCount
                : rowHeight;

            for (int i = 0; i < coinCount; i++)
            {
                float y = -i * (coinHeight + _slotSpacing);
                CosmeticSlotView view = CreateSlot(coins[i], _coinSlotPrefab, coinColumn, y, coinHeight);
                if (view != null) created.Add(view);
            }

            CosmeticSlotView premiumPrefab = _premiumSlotPrefab != null ? _premiumSlotPrefab : _coinSlotPrefab;
            for (int i = 0; i < premium.Count; i++)
            {
                float y = -i * (rowHeight + _slotSpacing);
                CosmeticSlotView view = CreateSlot(premium[i], premiumPrefab, premiumColumn, y, rowHeight);
                if (view != null) created.Add(view);
            }

            _contentHeight += groupHeight + _groupSpacing;
            return new GroupUi { Root = root.gameObject, Slots = created.ToArray() };
        }

        private void CreateTitle(string text, RectTransform parent, float height)
        {
            RectTransform rect = NewRect("Title", parent);
            SetTopStretch(rect, 0f, height);

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = _titleFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = UITheme.NeonGreen;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            // Мягкая тень под заголовком: без неё текст сливается с тёмным фоном
            label.fontMaterial.EnableKeyword("OUTLINE_ON");
            label.outlineWidth = 0.18f;
            label.outlineColor = UITheme.MetalDarkest;

            TMP_FontAsset font = ResolveFont();
            if (font != null) label.font = font;
        }

        private CosmeticSlotView CreateSlot(CosmeticSlotData data, CosmeticSlotView prefab,
                                            RectTransform parent, float top, float height)
        {
            CosmeticSlotView view;

            if (prefab != null) view = Instantiate(prefab, parent);
            else view = BuildSlotInCode(parent);

            if (view == null) return null;

            var rect = (RectTransform)view.transform;
            SetTopStretch(rect, top, height);

            view.Initialize(SlotKey(data));
            view.Clicked += HandleSlotClicked;
            view.Render(data);

            _slots[SlotKey(data)] = view;
            return view;
        }

        /// <summary>Запасной слот: используется, если префаб не назначен в инспекторе.</summary>
        private CosmeticSlotView BuildSlotInCode(RectTransform parent)
        {
            RectTransform root = NewRect("Slot", parent);

            var background = root.gameObject.AddComponent<Image>();
            background.color = UITheme.MetalDark;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            // Картинка модели. Лежит в верхней половине карточки, с отступом,
            // чтобы воксельный молот не прилипал к краям
            RectTransform previewRect = NewRect("Preview", root);
            previewRect.anchorMin = new Vector2(0.12f, 0.42f);
            previewRect.anchorMax = new Vector2(0.88f, 0.94f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;

            var preview = previewRect.gameObject.AddComponent<Image>();
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            preview.enabled = false;

            // Акцентная полоска сверху: цвет говорит о состоянии карточки
            RectTransform accentRect = NewRect("Accent", root);
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 6f);

            var accent = accentRect.gameObject.AddComponent<Image>();
            accent.color = UITheme.NeonGreen;
            accent.raycastTarget = false;

            TextMeshProUGUI name = BuildLabel(root, "Name", 30f, TextAlignmentOptions.Center, 0.30f, 0.42f);
            TextMeshProUGUI price = BuildLabel(root, "Price", 34f, TextAlignmentOptions.Center, 0.12f, 0.30f);
            TextMeshProUGUI state = BuildLabel(root, "State", 26f, TextAlignmentOptions.Center, 0f, 0.12f);

            var view = root.gameObject.AddComponent<CosmeticSlotView>();
            view.Bind(name, price, state, button, background, preview, accent, null);
            return view;
        }

        /// <summary>
        /// Подпись внутри слота. Полоса задаётся парой долей по вертикали:
        /// 0 — низ слота, 1 — верх. Название занимает верх, цена середину,
        /// состояние низ.
        /// </summary>
        private TextMeshProUGUI BuildLabel(RectTransform parent, string name, float size,
                                          TextAlignmentOptions align, float fromY, float toY)
        {
            RectTransform rect = NewRect(name, parent);

            rect.anchorMin = new Vector2(0f, fromY);
            rect.anchorMax = new Vector2(1f, toY);
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = align;
            label.raycastTarget = false;

            TMP_FontAsset font = ResolveFont();
            if (font != null) label.font = font;

            return label;
        }

        private TMP_FontAsset ResolveFont()
        {
            if (_font != null) return _font;

            if (_fontWarned) return null;
            _fontWarned = true;

            Debug.LogWarning("[CosmeticShop] Не назначен шрифт с кириллицей (_font). " +
                             "Русский текст не отрисуется. Назначьте " +
                             "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset");
            return null;
        }

        /// <summary>Высота контента: по ней ScrollRect понимает, куда можно прокрутить.</summary>
        private void UpdateContentHeight()
        {
            if (_groupsContainer == null) return;

            _groupsContainer.sizeDelta = new Vector2(_groupsContainer.sizeDelta.x,
                                                     Mathf.Max(0f, _contentHeight));
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return (RectTransform)go.transform;
        }

        /// <summary>Растянуть по ширине родителя и поставить сверху с отступом top и высотой height.</summary>
        private static void SetTopStretch(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(0f, height);
        }

        /// <summary>Колонка внутри ряда: доля ширины от from до to, вся высота ряда.</summary>
        private static void SetColumn(RectTransform rect, float from, float to, float sideOffset)
        {
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(sideOffset, 0f);
            rect.offsetMax = new Vector2(sideOffset, 0f);
        }

        private void HandleSlotClicked(string key) => SlotClicked?.Invoke(key);

        private static string SlotKey(CosmeticSlotData slot) => slot.GroupId + "/" + slot.ItemId;

        private void SetHidden(bool hide)
        {
            if (_hideWhileOpen == null) return;

            foreach (GameObject go in _hideWhileOpen)
            {
                if (go != null) go.SetActive(!hide);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();

            foreach (KeyValuePair<string, CosmeticSlotView> pair in _slots)
            {
                if (pair.Value != null) pair.Value.Clicked -= HandleSlotClicked;
            }
        }
    }
}
