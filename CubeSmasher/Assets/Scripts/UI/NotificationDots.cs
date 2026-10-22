using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using YG;

/// <summary>
/// Красные кружки-уведомления на кнопках нижней панели.
///
/// Гараж: горит, когда есть машина, которую можно открыть, и гаснет, когда игрок
/// открыл окно гаража. Кружок на самой машине (в ленте) остаётся, пока её не откроют.
///
/// Магазин: горит, когда хотя бы одну прокачку можно купить прямо сейчас —
/// хватает денег, стадия позволяет, уровень не максимальный.
///
/// Правила здесь, а не во View: View только включает и выключает кружок.
/// </summary>
public class NotificationDots : IDisposable
{
    private readonly GameObject _garageDot;
    private readonly GameObject _shopDot;
    private readonly IGarageService _garage;
    private readonly IShopService _shop;
    private readonly IWallet _wallet;
    private readonly StageModel _stage;
    private readonly List<UpgradeConfig> _configs;

    private readonly Action<double> _onBalanceChanged;
    private readonly Action<string> _onUpgradePurchased;
    private readonly Action<int> _onStageCompleted;
    private readonly Action<int> _onCarAvailable;
    private readonly Action<int> _onCarOpened;
    private readonly Action _onGarageOpened;

    /// <summary>
    /// Кружки создаются кодом у переданных кнопок (NotificationDot.Ensure),
    /// поэтому ни префаб HUD, ни сцена не требуют назначений.
    /// </summary>
    public NotificationDots(Button garageButton, Button shopButton, IGarageService garage,
                            IShopService shop, IWallet wallet, StageModel stage,
                            List<UpgradeConfig> configs, Subscriptions subs)
    {
        _garageDot = NotificationDot.Ensure(garageButton != null ? garageButton.transform : null, "DotGarage");
        _shopDot = NotificationDot.Ensure(shopButton != null ? shopButton.transform : null, "DotShop");
        _garage = garage;
        _shop = shop;
        _wallet = wallet;
        _stage = stage;
        _configs = configs;

        _onBalanceChanged = _ => RecalculateShop();
        _onUpgradePurchased = _ => RecalculateShop();
        _onStageCompleted = _ => { RecalculateShop(); RecalculateGarage(); };
        _onCarAvailable = _ => RecalculateGarage();
        _onCarOpened = _ => RecalculateGarage();

        // Открыли гараж — уведомление на кнопке больше не нужно
        _onGarageOpened = HideGarage;

        if (subs != null)
        {
            subs.Track(() => _wallet.OnBalanceChanged += _onBalanceChanged,
                       () => _wallet.OnBalanceChanged -= _onBalanceChanged);
            subs.Track(() => _shop.OnUpgradePurchased += _onUpgradePurchased,
                       () => _shop.OnUpgradePurchased -= _onUpgradePurchased);
            subs.Track(() => _stage.OnStageCompleted += _onStageCompleted,
                       () => _stage.OnStageCompleted -= _onStageCompleted);
            subs.Track(() => _garage.CarAvailable += _onCarAvailable,
                       () => _garage.CarAvailable -= _onCarAvailable);
            subs.Track(() => _garage.CarOpened += _onCarOpened,
                       () => _garage.CarOpened -= _onCarOpened);
        }

        RecalculateShop();
        RecalculateGarage();
    }

    /// <summary>Подписка на открытие окна гаража: там кружок гаснет.</summary>
    public void BindGarageView(IGarageView view, Subscriptions subs)
    {
        if (view is GarageView garageView && subs != null)
        {
            subs.Track(() => garageView.OnOpened += _onGarageOpened,
                       () => garageView.OnOpened -= _onGarageOpened);
        }
    }

    /// <summary>Есть машина, которую можно открыть, и игрок ещё не заходил в гараж.</summary>
    public void RecalculateGarage()
    {
        if (_garageDot == null || _garage == null) return;

        GarageCarData[] cars = _garage.AllCars;
        for (int i = 0; i < cars.Length; i++)
        {
            if (_garage.GetState(i) == GarageCarState.Available)
            {
                _garageDot.SetActive(true);
                return;
            }
        }

        _garageDot.SetActive(false);
    }

    /// <summary>Хотя бы одну прокачку можно купить прямо сейчас.</summary>
    public void RecalculateShop()
    {
        if (_shopDot == null || _shop == null) return;
        _shopDot.SetActive(CanBuySomething());
    }

    private bool CanBuySomething()
    {
        if (_configs == null) return false;

        int stage = _stage != null ? _stage.CurrentStage : 0;

        foreach (UpgradeConfig config in _configs)
        {
            if (config == null) continue;
            if (stage < config.UnlockStageLevel) continue; // ещё закрыта по стадии

            UpgradeData data = _shop.GetUpgradeData(config.Id);
            if (data == null) continue;
            if (data.level >= config.MaxLevel) continue;   // уже максимум

            if (_wallet.Balance >= data.price) return true;
        }

        return false;
    }

    private void HideGarage()
    {
        if (_garageDot != null) _garageDot.SetActive(false);
    }

    public void Dispose()
    {
        // Отписки идут через Subscriptions, здесь только состояние
        HideGarage();
        if (_shopDot != null) _shopDot.SetActive(false);
    }
}
