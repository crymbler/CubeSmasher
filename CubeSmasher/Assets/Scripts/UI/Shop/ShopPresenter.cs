using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Infrastructure;

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
        _shop.OnUpgradePurchased += HandleUpgradePurchased;
        _wallet.OnBalanceChanged += HandleBalanceChanged;
        _stage.OnStageCompleted += HandleStageCompleted;

        RefreshAll();
    }

    /// <summary>
    /// Апгрейд куплен. Перерисовываем весь список, а не одну карточку:
    /// порядок зависит от стадии открытия, и одна карточка с order=0 прыгнула бы наверх.
    /// </summary>
    private void HandleUpgradePurchased(string id) => RefreshAll();

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

    /// <summary>
    /// Перерисовать магазин. Вызывается после смены языка и при открытии окна:
    /// цены и уровни те же, но «Ур. N» и «Откроется на стадии N» перечитываются.
    /// Карточки создаются один раз и живут между открытиями, поэтому без
    /// повторного Render остаются на языке прошлого показа.
    /// </summary>
    public void Refresh()
    {
        RefreshAll();
    }

    /// <summary>Порядок карточек: открытые раньше - выше, внутри стадии по стату и цене.</summary>
    private sealed class ConfigOrder : System.Collections.Generic.IComparer<UpgradeConfig>
    {
        public int Compare(UpgradeConfig a, UpgradeConfig b)
        {
            if (a == null || b == null) return 0;

            int byStage = a.UnlockStageLevel.CompareTo(b.UnlockStageLevel);
            if (byStage != 0) return byStage;

            int byStat = a.Stat.CompareTo(b.Stat);
            if (byStat != 0) return byStat;

            return a.BasePrice.CompareTo(b.BasePrice);
        }
    }

    private void RefreshAll()
    {
        if (_configs == null) return;

        // Сортируем копию: порядок нужен для отображения, сам список не трогаем
        var ordered = new List<UpgradeConfig>(_configs);
        ordered.Sort(new ConfigOrder());

        int order = 0;
        foreach (UpgradeConfig config in ordered)
        {
            if (config == null) continue;
            RefreshUpgrade(config.Id, order++);
        }
    }

    private void RefreshUpgrade(string id, int order)
    {
        var data = _shop.GetUpgradeData(id);
        UpgradeConfig config = _shop.GetConfig(id);
        if (data == null || config == null) return;

        bool isMaxLevel = data.level >= config.MaxLevel;

        // Прокачан до максимума - убираем карточку совсем, покупать больше нечего
        if (isMaxLevel)
        {
            _view.Hide(id);
            return;
        }

        bool isLocked = _stage.CurrentStage < config.UnlockStageLevel;
        bool canAfford = !isLocked && _wallet.Balance >= data.price;

        _view.Render(id, new UpgradeRowData
        {
            Order = order,
            Name = config.DisplayName,
            Icon = config.Icon,
            Level = data.level,
            MaxLevel = config.MaxLevel,
            Price = data.price,
            UnlockStage = config.UnlockStageLevel,
            IsLocked = isLocked,
            IsMaxLevel = isMaxLevel,
            CanAfford = canAfford,
            Delta = config.FormatDelta(data.level, StatContribution(config))
        });
    }

    /// <summary>
    /// Вклад базы, остальных тиров этого стата и бонуса машины.
    /// Нужен, чтобы карточка показывала итоговую характеристику, а не прибавку одного тира:
    /// у урона пять тиров, и «0.00 -> 6.00» без этой поправки сбивает с толку.
    /// </summary>
    private float StatContribution(UpgradeConfig config)
    {
        if (!ServiceLocator.Instance.TryGet(out StatService stats)) return 0f;
        return stats.GetContributionExcept(config.Stat, config);
    }

    public void Dispose()
    {
        _view.BuyRequested -= HandleBuyRequested;
        _shop.OnUpgradePurchased -= HandleUpgradePurchased;
        _wallet.OnBalanceChanged -= HandleBalanceChanged;
        _stage.OnStageCompleted -= HandleStageCompleted;
    }
}
