using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopView : MonoBehaviour
{
    [SerializeField] private UpgradeItemView _itemPrefab;
    [SerializeField] private Transform _itemsContainer;

    [Header("Кнопки управления")]
    [SerializeField] private Button _closeButton; // Крестик внутри окна магазина

    // События, на которые подписывается Bootstrapper
    public event Action OnOpened;
    public event Action OnClosed;

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
        OnOpened?.Invoke(); // Кричим "Я открылся!"
    }

    public void Close()
    {
        gameObject.SetActive(false);
        OnClosed?.Invoke(); // Кричим "Я закрылся!"
    }

    public UpgradeItemView GetOrCreateItem(string id)
    {
        if (!_items.ContainsKey(id))
        {
            UpgradeItemView newItem = Instantiate(_itemPrefab, _itemsContainer);
            newItem.Initialize(id);
            _items.Add(id, newItem);
        }
        return _items[id];
    }

    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
    }
}