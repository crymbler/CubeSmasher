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
        OnOpened?.Invoke();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        OnClosed?.Invoke();
    }

    public void Render(string id, UpgradeRowData row)
    {
        UpgradeItemView item = GetOrCreateItem(id);
        item.UpdateData(row.Icon, row.Name, row.Level, row.Price,
                        row.CanAfford, row.IsMaxLevel, row.IsLocked, row.UnlockStage);
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
