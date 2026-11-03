using System;
using CubeSmasher.Core.Interfaces;

public class TopHudPresenter : IDisposable
{
    private readonly IWallet _wallet;
    private readonly ITopHudView _view;

    // Последние переданные значения. Нужны для перерисовки после смены языка:
    // данные не изменились, но подписи вокруг них (hud.income, stage.machine)
    // должны перечитаться из таблицы нового языка.
    private double _lastBalance;
    private int _lastCurrent;
    private int _lastMax;
    private int _lastStage;
    private double _lastIncome;
    private bool _hasProgress;
    private bool _hasIncome;

    public TopHudPresenter(IWallet wallet, ITopHudView view)
    {
        if (wallet == null) throw new ArgumentNullException(nameof(wallet));
        if (view == null) throw new ArgumentNullException(nameof(view));

        _wallet = wallet;
        _view = view;

        // Подписываемся на событие изменения денег
        _wallet.OnBalanceChanged += HandleBalanceChanged;

        // Обновляем UI актуальными данными при старте
        HandleBalanceChanged(_wallet.Balance);
    }

    private void HandleBalanceChanged(double newBalance)
    {
        _lastBalance = newBalance;
        _view.UpdateBalance(newBalance);
    }

    // Этот метод мы будем вызывать из системы сборки машины (позже)
    public void UpdateMachineProgress(int current, int max, int stage)
    {
        _lastCurrent = current;
        _lastMax = max;
        _lastStage = stage;
        _hasProgress = true;

        _view.UpdateProgress(current, max, stage);
    }

    /// <summary>Вызывается при пересчёте пассивного дохода (стадия, открытие машины).</summary>
    public void UpdateIncome(double incomePerSecond)
    {
        _lastIncome = incomePerSecond;
        _hasIncome = true;

        _view.UpdateIncome(incomePerSecond);
    }

    /// <summary>
    /// Перерисовать HUD на текущем языке. Данные те же, но подпись «Доход»
    /// и «Машин: N» должны перечитаться из таблицы нового языка.
    /// </summary>
    public void Refresh()
    {
        _view.UpdateBalance(_lastBalance);

        if (_hasProgress) _view.UpdateProgress(_lastCurrent, _lastMax, _lastStage);
        if (_hasIncome) _view.UpdateIncome(_lastIncome);
    }

    public void Dispose()
    {
        // Обязательная отписка при уничтожении класса
        _wallet.OnBalanceChanged -= HandleBalanceChanged;
    }
}