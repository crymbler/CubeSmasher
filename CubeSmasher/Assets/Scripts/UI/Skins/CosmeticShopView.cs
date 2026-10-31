using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Экран магазина косметики.
    ///
    /// Вёрстка по макету: группы идут списком, в каждой три слота —
    /// два маленьких за монеты и один крупный премиальный. Внутри группы:
    /// премиальный слот занимает колонку на всю высоту, два обычных делят
    /// вторую колонку пополам.
    ///
    /// Высоты заданы явно через LayoutElement, ContentSizeFitter стоит только
    /// на контейнере групп. Причина: когда VerticalLayoutGroup с
    /// childControlHeight управляет детьми, а на ребёнке ещё и ContentSizeFitter,
    /// они пересчитывают друг друга и высота уходит в ноль. Здесь каждый уровень
    /// объявляет свою высоту числом, и спорить не о чем.
    /// </summary>
    public class CosmeticShopView : MonoBehaviour, ICosmeticShopView
    {
        [Header("Каркас")]
        [SerializeField] private Transform _groupsContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject[] _hideWhileOpen;

        [Header("Префабы слотов")]
        [Tooltip("Маленький слот за монеты. Пусто — слот соберётся кодом")]
        [SerializeField] private CosmeticSlotView _coinSlotPrefab;
        [Tooltip("Крупный премиальный слот за яны. Пусто — используется слот за монеты")]
        [SerializeField] private CosmeticSlotView _premiumSlotPrefab;

        [Header("Шрифт")]
        [Tooltip("Шрифт с кириллицей. Пусто — берётся Franklin из References, " +
                 "а если его нет, TMP сам подставит свой")]
        [SerializeField] private TMP_FontAsset _font;

        [Header("Раскладка")]
        [Tooltip("Высота крупного премиального слота. Она же высота ряда со слотами")]
        [SerializeField, Min(120f)] private float _premiumSlotHeight = 420f;
        [Tooltip("Высота заголовка группы. 0 — без заголовков")]
        [SerializeField, Min(0f)] private float _titleHeight = 64f;
        [Tooltip("Расстояние между группами")]
        [SerializeField] private float _groupSpacing = 32f;
        [Tooltip("Расстояние между заголовком и слотами, и между слотами в колонке")]
        [SerializeField] private float _slotSpacing = 16f;
        [Tooltip("Доля ширины ряда, которую занимает премиальная колонка")]
        [SerializeField, Range(0.3f, 0.7f)] private float _premiumShare = 0.55f;
        [Tooltip("С какой стороны крупный премиальный слот: true — справа")]
        [SerializeField] private bool _premiumOnRight = true;

        [Header("Заголовок группы")]
        [SerializeField] private float _titleFontSize = 44f;

        public event System.Action<string> SlotClicked;
        public event System.Action Closed;

        private readonly Dictionary<string, GroupUi> _groups = new Dictionary<string, GroupUi>();
        private readonly Dictionary<string, CosmeticSlotView> _slots = new Dictionary<string, CosmeticSlotView>();

        private class GroupUi
        {
            public GameObject Root;
            public CosmeticSlotView[] Slots;
        }

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// Шрифт для текста, созданного кодом. В инспекторе задаётся шрифт
        /// с кириллицей (Franklin из References). Если поле пустое, берётся
        /// дефолтный шрифт TMP: он без кириллицы, и русский текст превратится
        /// в квадраты — поэтому шрифт лучше назначать.
        /// </summary>
        private TMP_FontAsset ResolveFont()
        {
            if (_font != null) return _font;

            if (_fontWarned) return null;
            _fontWarned = true;

            Debug.LogWarning("[CosmeticShop] Не назначен шрифт с кириллицей (_font). " +
                             "Русские названия групп и слотов не отрисуются. " +
                             "Назначьте Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset");
            return null;
        }

        private bool _fontWarned;

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
            float rowHeight = Mathf.Max(120f, _premiumSlotHeight);

            float groupHeight = rowHeight + _slotSpacing;
            if (_titleHeight > 0f) groupHeight += _titleHeight;

            // --- Группа ---
            var root = new GameObject("Group_" + group.GroupId, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = _slotSpacing;
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childAlignment = TextAnchor.UpperCenter;

            // Высота объявлена числом: родитель (контейнер групп) её прочитает
            var rootElement = root.AddComponent<LayoutElement>();
            rootElement.preferredHeight = groupHeight;
            rootElement.minHeight = groupHeight;
            rootElement.flexibleWidth = 1f;

            if (_titleHeight > 0f) CreateTitle(group.Title, root.transform);

            // --- Ряд со слотами ---
            var row = new GameObject("Slots", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = _slotSpacing;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;

            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = rowHeight;
            rowElement.minHeight = rowHeight;
            rowElement.flexibleWidth = 1f;

            CosmeticSlotData[] slots = group.Slots ?? new CosmeticSlotData[0];

            // Премиальный слот определяем по флагу из конфига, а не по состоянию:
            // купленный премиальный предмет остаётся крупным, меняется только подпись
            var coins = new List<CosmeticSlotData>();
            var premium = new List<CosmeticSlotData>();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsPremium) premium.Add(slots[i]);
                else coins.Add(slots[i]);
            }

            GameObject coinColumn = CreateColumn(row.transform, "CoinColumn", rowHeight, 1f - _premiumShare);
            GameObject premiumColumn = CreateColumn(row.transform, "PremiumColumn", rowHeight, _premiumShare);

            // Порядок в ряду задаёт сторону премиального слота
            if (_premiumOnRight)
            {
                coinColumn.transform.SetSiblingIndex(0);
                premiumColumn.transform.SetSiblingIndex(1);
            }
            else
            {
                premiumColumn.transform.SetSiblingIndex(0);
                coinColumn.transform.SetSiblingIndex(1);
            }

            var created = new List<CosmeticSlotView>();

            for (int i = 0; i < coins.Count; i++)
            {
                CosmeticSlotView view = CreateSlot(coins[i], _coinSlotPrefab, coinColumn.transform);
                if (view != null) created.Add(view);
            }

            CosmeticSlotView premiumPrefab = _premiumSlotPrefab != null ? _premiumSlotPrefab : _coinSlotPrefab;
            for (int i = 0; i < premium.Count; i++)
            {
                CosmeticSlotView view = CreateSlot(premium[i], premiumPrefab, premiumColumn.transform);
                if (view != null) created.Add(view);
            }

            return new GroupUi { Root = root, Slots = created.ToArray() };
        }

        private void CreateTitle(string text, Transform parent)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = _titleFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = UITheme.NeonGreen;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;

            TMP_FontAsset font = ResolveFont();
            if (font != null) label.font = font;

            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = _titleHeight;
            element.minHeight = _titleHeight;
            element.flexibleWidth = 1f;
        }

        private CosmeticSlotView CreateSlot(CosmeticSlotData data, CosmeticSlotView prefab, Transform parent)
        {
            CosmeticSlotView view;

            if (prefab != null)
            {
                view = Instantiate(prefab, parent);
            }
            else
            {
                // Префаб не назначен: собираем слот кодом, чтобы магазин всё равно работал
                view = BuildSlotInCode(parent);
            }

            if (view == null) return null;

            view.Initialize(SlotKey(data));
            view.Clicked += HandleSlotClicked;

            // Слот делит высоту колонки: своей preferredHeight у него нет
            var element = view.GetComponent<LayoutElement>();
            if (element == null) element = view.gameObject.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.flexibleWidth = 1f;
            element.minHeight = 80f;

            view.Render(data);

            _slots[SlotKey(data)] = view;
            return view;
        }

        /// <summary>Запасной слот: используется, если префаб не назначен в инспекторе.</summary>
        private CosmeticSlotView BuildSlotInCode(Transform parent)
        {
            var go = new GameObject("Slot", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var background = go.AddComponent<Image>();
            background.color = new Color(0.36f, 0.55f, 0.22f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI name = BuildLabel(go.transform, "Name", 30f, TextAlignmentOptions.Top);
            TextMeshProUGUI price = BuildLabel(go.transform, "Price", 36f, TextAlignmentOptions.Center);
            TextMeshProUGUI state = BuildLabel(go.transform, "State", 28f, TextAlignmentOptions.Bottom);

            var nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0f, 0.62f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(12f, 0f);
            nameRect.offsetMax = new Vector2(-12f, -8f);

            var priceRect = (RectTransform)price.transform;
            priceRect.anchorMin = new Vector2(0f, 0.3f);
            priceRect.anchorMax = new Vector2(1f, 0.62f);
            priceRect.offsetMin = new Vector2(12f, 0f);
            priceRect.offsetMax = new Vector2(-12f, 0f);

            var stateRect = (RectTransform)state.transform;
            stateRect.anchorMin = new Vector2(0f, 0f);
            stateRect.anchorMax = new Vector2(1f, 0.3f);
            stateRect.offsetMin = new Vector2(12f, 8f);
            stateRect.offsetMax = new Vector2(-12f, 0f);

            var view = go.AddComponent<CosmeticSlotView>();
            view.Bind(name, price, state, button, background);
            return view;
        }

        private TextMeshProUGUI BuildLabel(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = align;
            label.raycastTarget = false;

            TMP_FontAsset font = ResolveFont();
            if (font != null) label.font = font;

            return label;
        }

        private static GameObject CreateColumn(Transform parent, string name, float height, float widthShare)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 1f;
            element.flexibleWidth = widthShare;

            return go;
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
