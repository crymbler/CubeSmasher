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
    /// два маленьких за монеты и один крупный премиальный. Раскладка внутри
    /// группы: премиальный слот с одной стороны на всю высоту, два обычных
    /// друг под другом с другой. Сторона премиального задаётся флагом,
    /// поэтому группу можно зеркалить как на картинке.
    ///
    /// Слоты создаются кодом из двух префабов: обычного и премиального.
    /// Пока макета нет, размеры и цвета берутся из UITheme.
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
        [Tooltip("Заголовок группы. Можно оставить пустым — создастся кодом")]
        [SerializeField] private TextMeshProUGUI _groupTitlePrefab;

        [Header("Раскладка")]
        [Tooltip("Расстояние между группами")]
        [SerializeField] private float _groupSpacing = 24f;
        [Tooltip("Расстояние между слотами внутри группы")]
        [SerializeField] private float _slotSpacing = 12f;
        [Tooltip("Доля ширины и высоты, которую занимает премиальный слот")]
        [SerializeField, Range(0.4f, 0.7f)] private float _premiumShare = 0.55f;
        [Tooltip("С какой стороны крупный премиальный слот: true — справа")]
        [SerializeField] private bool _premiumOnRight = true;

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

            // Слотов ровно три по макету, но вёрстка не должна падать,
            // если в конфиге окажется другое число
            int count = Mathf.Min(ui.Slots.Length, group.Slots != null ? group.Slots.Length : 0);
            for (int i = 0; i < count; i++)
            {
                ui.Slots[i].Render(group.Slots[i]);
            }
        }

        public void UpdateSlot(CosmeticSlotData slot)
        {
            if (!_slots.TryGetValue(SlotKey(slot), out CosmeticSlotView view)) return;
            view.Render(slot);
        }

        private GroupUi CreateGroup(CosmeticGroupData group)
        {
            var root = new GameObject("Group_" + group.GroupId, typeof(RectTransform));
            root.transform.SetParent(_groupsContainer != null ? _groupsContainer : transform, false);

            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = _slotSpacing;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (_groupTitlePrefab != null)
            {
                TextMeshProUGUI title = Instantiate(_groupTitlePrefab, root.transform);
                title.text = group.Title;
            }

            // Ряд со слотами: премиальный крупный и колонка из двух обычных
            var row = new GameObject("Slots", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);

            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = _slotSpacing;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            var rowFitter = row.AddComponent<ContentSizeFitter>();
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CosmeticSlotData[] slots = group.Slots ?? new CosmeticSlotData[0];

            // Разделяем: премиальные и обычные. По макету премиальный один и крупный
            var coins = new List<CosmeticSlotData>();
            var premium = new List<CosmeticSlotData>();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsPremium || slots[i].State == CosmeticSlotState.ForYan
                    || slots[i].State == CosmeticSlotState.Unavailable)
                {
                    premium.Add(slots[i]);
                }
                else
                {
                    coins.Add(slots[i]);
                }
            }

            GameObject coinColumn = CreateColumn(row.transform, "CoinColumn");
            GameObject premiumColumn = CreateColumn(row.transform, "PremiumColumn");

            // Порядок колонок в ряду задаёт сторону премиального слота
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

            SetFlexible(premiumColumn, _premiumShare);
            SetFlexible(coinColumn, 1f - _premiumShare);

            var created = new List<CosmeticSlotView>();

            for (int i = 0; i < coins.Count; i++)
            {
                created.Add(CreateSlot(coins[i], _coinSlotPrefab, coinColumn.transform));
            }

            for (int i = 0; i < premium.Count; i++)
            {
                created.Add(CreateSlot(premium[i], _premiumSlotPrefab != null ? _premiumSlotPrefab : _coinSlotPrefab,
                                       premiumColumn.transform));
            }

            return new GroupUi { Root = root, Slots = created.ToArray() };
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
            view.Render(data);

            _slots[SlotKey(data)] = view;
            return view;
        }

        private static GameObject CreateColumn(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            return go;
        }

        private static void SetFlexible(GameObject go, float width)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = width;
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
