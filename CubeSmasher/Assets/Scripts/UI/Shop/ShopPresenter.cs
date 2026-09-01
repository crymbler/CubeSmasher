using System;
using System.Collections.Generic;

public class ShopPresenter : IDisposable
{
    private readonly ShopModel _model;
    private readonly ShopView _view;
    private readonly Wallet _wallet;
    private readonly List<UpgradeConfig> _configs;

    public ShopPresenter(ShopModel model, ShopView view, Wallet wallet, List<UpgradeConfig> configs)
    {
        if (model == null || view == null || wallet == null || configs == null) return;

        _model = model;
        _view = view;
        _wallet = wallet;
        _configs = configs;

        // Подписываемся на изменения
        _model.OnUpgradeChanged += UpdateSingleItemUI;
        _wallet.OnBalanceChanged += CheckAffordability;

        InitializeUI();
    }

    // Создаем кнопки при старте
    private void InitializeUI()
    {
        foreach (var config in _configs)
        {
            UpgradeItemView itemView = _view.GetOrCreateItem(config.Id);
            itemView.OnBuyClicked += HandleBuyClicked;

            UpdateSingleItemUI(config.Id);
        }
    }

    // Игрок кликнул "Купить"
    private void HandleBuyClicked(string id)
    {
        _model.TryBuyUpgrade(id);
    }

    // Обновляем конкретную кнопку
    private void UpdateSingleItemUI(string id)
    {
        var data = _model.GetUpgradeData(id);
        var config = _model.GetConfig(id);

        if (data == null || config == null) return;

        UpgradeItemView itemView = _view.GetOrCreateItem(id);

        bool isMaxLevel = data.level >= config.MaxLevel;
        bool canAfford = !isMaxLevel && _wallet.Balance >= data.price;

        itemView.UpdateData(config.Icon, config.UpgradeName, data.level, data.price, canAfford, isMaxLevel);
    }

    // Обновляем доступность кнопок, когда меняется баланс монет
    private void CheckAffordability(double currentBalance)
    {
        foreach (var config in _configs)
        {
            UpdateSingleItemUI(config.Id);
        }
    }

    // Отписка для предотвращения утечек
    public void Dispose()
    {
        _model.OnUpgradeChanged -= UpdateSingleItemUI;
        _wallet.OnBalanceChanged -= CheckAffordability;
    }
}