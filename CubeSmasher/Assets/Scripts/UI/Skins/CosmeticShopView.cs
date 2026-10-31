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
    /// вторую колонку пополам. Сторона премиального задаётся флагом,
    /// поэтому группу можно зеркалить как на картинке.
    ///
    /// Высоты задаются здесь числами, а не берутся из префабов. Причина:
    /// когда высотой управляет VerticalLayoutGroup с childControlHeight,
    /// он спрашивает у ребёнка preferredHeight, а у слота без LayoutElement
    /// он равен нулю — слоты схлопывались в пиксель. Теперь размеры
    /// выставляются явно через LayoutElement на каждом уровне.
    /// </summary>
    public class CosmeticShopView : MonoBehaviour, ICosmeticShopView
    {
        [Header("Каркас")]
        [SerializeField] private Transform _groupsContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject[] _hideWhileOpen;

        [Header("Префабы слотов")]
        [Tooltip("Маленький слот за монеты")]
        [SerializeField] private CosmeticSlotView _coinSlotPrefab;
        [Tooltip("Крупный премиальный слот за яны")]
        [SerializeField] private CosmeticSlotView _premiumSlotPrefab;

        [Header("Раскладка")]
        [Tooltip("Высота крупного премиального слота. Она же высота всей группы со слотами")]
        [SerializeField, Min(120f)] private float _premiumSlotHeight = 420f;
        [Tooltip("Высота заголовка группы")]
        [SerializeField, Min(0f)] private float _titleHeight = 64f;
        [Tooltip("Расстояние между группами")]
        [SerializeField] private float _groupSpacing = 32f;
        [Tooltip("Расстояние между слотами внутри группы")]
        [SerializeField] private float _slotSpacing = 16f;
        [Tooltip("Доля ширины ряда, которую занимает премиальная колонка")]
        [SerializeField, Range(0.3f, 0.7f)] private float _premiumShare = 0.55f;
        [Tooltip("С какой стороны крупный премиальный слот: true — справа")]
        [SerializeField] private bool _premiumOnRight = true;

        [Header("Заголовок группы")]
        [Tooltip("Создать заголовок кодом, если префаб не назначен")]
        [SerializeField] private bool _buildTitleInCode = true;
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

            var root = new GameObject("Group_" + group.GroupId, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);

            var rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = _groupSpacing;
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childAlignment = TextAnchor.UpperCenter;

            float rowHeight = Mathf.Max(120f, _premiumSlotHeight);
            float groupHeight = rowHeight + (_titleHeight > 0f ? _titleHeight + _groupSpacing : 0f);

            var rootElement = root.AddComponent<LayoutElement>();
            rootElement.preferredHeight = groupHeight;
            rootElement.minHeight = groupHeight;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Заголовок группы
            if (_titleHeight > 0f)
            {
                CreateTitle(group.Title, root.transform);
            }

            // Ряд со слотами
            var row = new GameObject("Slots", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = _slotSpacing;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment = TextAnchor.UpperCenter;

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

            GameObject coinColumn = CreateColumn(row.transform, "CoinColumn");
            GameObject premiumColumn = CreateColumn(row.transform, "PremiumColumn");

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

            SetColumnWidth(premiumColumn, _premiumShare);
            SetColumnWidth(coinColumn, 1f - _premiumShare);

            SetColumnHeight(coinColumn, rowHeight);
            SetColumnHeight(premiumColumn, rowHeight);

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

        /// <summary>Заголовок группы: готовый префаб или простой текст, созданный кодом.</summary>
        private void CreateTitle(string text, Transform parent)
        {
            if (!_buildTitleInCode) return;

            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = _titleFontSize;
            label.color = UITheme.NeonGreen;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;

            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = _titleHeight;
            element.minHeight = _titleHeight;
            element.flexibleWidth = 1f;
        }

        private CosmeticSlotView CreateSlot(CosmeticSlotData data, CosmeticSlotView prefab, Transform parent)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[CosmeticShop] Не назначен префаб слота: магазин будет пустым.");
                return null;
            }

            CosmeticSlotView view = Instantiate(prefab, parent);
            view.Initialize(SlotKey(data));
            view.Clicked += HandleSlotClicked;

            // Слоты делят высоту колонки: у слота нет своей preferredHeight,
            // он растягивается на всё, что дал родитель
            var element = view.GetComponent<LayoutElement>();
            if (element == null) element = view.gameObject.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.flexibleWidth = 1f;
            element.minHeight = 80f;

            view.Render(data);

            _slots[SlotKey(data)] = view;
            return view;
        }

        private static GameObject CreateColumn(Transform parent, string name)
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

            go.AddComponent<LayoutElement>();
            return go;
        }

        private static void SetColumnWidth(GameObject column, float share)
        {
            var element = column.GetComponent<LayoutElement>();
            if (element == null) element = column.AddComponent<LayoutElement>();
            element.flexibleWidth = share;
            element.minWidth = 0f;
        }

        private static void SetColumnHeight(GameObject column, float height)
        {
            var element = column.GetComponent<LayoutElement>();
            if (element == null) element = column.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 1f;
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
