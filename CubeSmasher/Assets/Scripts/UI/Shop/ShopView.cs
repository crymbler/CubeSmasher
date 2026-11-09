using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отображение магазина. Не знает о моделях: получает готовые строки через IShopView
/// и сообщает о нажатиях "Купить" через BuyRequested.
/// </summary>
public class ShopView : MonoBehaviour, IShopView
{
    [SerializeField] private UpgradeItemView _itemPrefab;
    [SerializeField] private Transform _itemsContainer;

    [Header("Кнопки управления")]
    [SerializeField] private Button _closeButton; // Крестик внутри окна магазина

    [Header("Скрывать на время магазина")]
    [Tooltip("HUD и нижнюю панель: назначить в инспекторе. Они прячутся при открытии и возвращаются при закрытии.")]
    [SerializeField] private GameObject[] _hideWhileOpen;

    // События открытия/закрытия окна: на них подписывается UIWiringInitializer (пауза, курсор)
    public event Action OnOpened;
    public event Action OnClosed;

    // IShopView: нажатие "Купить" по улучшению с данным id
    public event Action<string> BuyRequested;

    private readonly Dictionary<string, UpgradeItemView> _items = new Dictionary<string, UpgradeItemView>();

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        SetHidden(true);
        OnOpened?.Invoke();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        SetHidden(false);
        OnClosed?.Invoke();
    }

    /// <summary>Прячет HUD и нижнюю панель, пока открыт магазин, и возвращает как было.</summary>
    private void SetHidden(bool hide)
    {
        if (_hideWhileOpen != null && _hideWhileOpen.Length > 0)
        {
            foreach (GameObject go in _hideWhileOpen)
            {
                if (go != null) go.SetActive(!hide);
            }
            return;
        }

        // Явный список не заполнен: скрываем верхнюю панель и нижнее меню.
        // Счётчик внутри не даст двум открытым окнам показать HUD раньше времени.
        CubeSmasher.UI.HudVisibility.Toggle(hide);
    }

    public void Render(string id, UpgradeRowData row)
    {
        UpgradeItemView item = GetOrCreateItem(id);
        item.UpdateData(row.Icon, row.Name, row.Level, row.Price,
                        row.CanAfford, row.IsMaxLevel, row.IsLocked, row.UnlockStage);
        item.SetLevelProgress(row.MaxLevel > 0 ? (float)row.Level / row.MaxLevel : 0f);
        item.SetDelta(row.Delta);

        // Порядок задаёт презентер: карточки выстраиваются по стадии открытия,
        // а не в том порядке, в каком их перебрал foreach
        item.transform.SetSiblingIndex(row.Order);
    }

    /// <summary>
    /// Апгрейд прокачан до максимума - карточка больше не нужна, убираем совсем.
    /// Оставлять её в списке незачем: купить нельзя, а место занимает.
    /// </summary>
    public void Hide(string id)
    {
        if (!_items.TryGetValue(id, out UpgradeItemView item)) return;

        if (item != null)
        {
            item.OnBuyClicked -= RaiseBuyRequested;
            Destroy(item.gameObject);
        }

        _items.Remove(id);
    }

    private UpgradeItemView GetOrCreateItem(string id)
    {
        if (!_items.TryGetValue(id, out UpgradeItemView item))
        {
            item = Instantiate(_itemPrefab, _itemsContainer);
            item.Initialize(id);
            item.OnBuyClicked += RaiseBuyRequested;
            _items.Add(id, item);
        }
        return item;
    }

    private void RaiseBuyRequested(string id)
    {
        BuyRequested?.Invoke(id);
    }

    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
    }
}
