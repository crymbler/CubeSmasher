using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;

/// <summary>
/// Презентер магазина: связывает IShopView с IShopService и IWallet.
/// Правил покупки и цен здесь нет, только перевод состояния модели в строки для View.
/// </summary>
public class ShopPresenter : IDisposable
{
    private readonly IShopService _shop;
    private readonly IShopView _view;
    private readonly IWallet _wallet;
    private readonly StageModel _stage;
    private readonly List<UpgradeConfig> _configs;

    public ShopPresenter(IShopService shop, IShopView view, IWallet wallet,
                         List<UpgradeConfig> configs, StageModel stage)
    {
        _shop = shop ?? throw new ArgumentNullException(nameof(shop));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _configs = configs;

        _view.BuyRequested += HandleBuyRequested;
        _shop.OnUpgradePurchased += RefreshUpgrade;
        _wallet.OnBalanceChanged += HandleBalanceChanged;
        _stage.OnStageCompleted += HandleStageCompleted;

        RefreshAll();
    }

    private void HandleBuyRequested(string id)
    {
        _shop.TryPurchaseUpgrade(id);
    }

    private void HandleBalanceChanged(double balance)
    {
        RefreshAll();
    }

    private void HandleStageCompleted(int stage)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (_configs == null) return;

        foreach (UpgradeConfig config in _configs)
        {
            RefreshUpgrade(config.Id);
        }
    }

    private void RefreshUpgrade(string id)
    {
        var data = _shop.GetUpgradeData(id);
        UpgradeConfig config = _shop.GetConfig(id);
        if (data == null || config == null) return;

        bool isMaxLevel = data.level >= config.MaxLevel;
        bool isLocked = _stage.CurrentStage < config.UnlockStageLevel;
        bool canAfford = !isMaxLevel && !isLocked && _wallet.Balance >= data.price;

        _view.Render(id, new UpgradeRowData
        {
            Name = config.UpgradeName,
            Icon = config.Icon,
            Level = data.level,
            Price = data.price,
            UnlockStage = config.UnlockStageLevel,
            IsLocked = isLocked,
            IsMaxLevel = isMaxLevel,
            CanAfford = canAfford
        });
    }

    public void Dispose()
    {
        _view.BuyRequested -= HandleBuyRequested;
        _shop.OnUpgradePurchased -= RefreshUpgrade;
        _wallet.OnBalanceChanged -= HandleBalanceChanged;
        _stage.OnStageCompleted -= HandleStageCompleted;
    }
}
