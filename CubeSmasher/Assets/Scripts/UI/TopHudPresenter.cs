using System;
using CubeSmasher.Core.Interfaces;

public class TopHudPresenter : IDisposable
{
    private readonly IWallet _wallet;
    private readonly TopHudView _view;

    public TopHudPresenter(IWallet wallet, TopHudView view)
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
        _view.UpdateBalance(newBalance);
    }

    // Этот метод мы будем вызывать из системы сборки машины (позже)
    public void UpdateMachineProgress(int current, int max, string name)
    {
        _view.UpdateProgress(current, max, name);
    }

    public void Dispose()
    {
        // Обязательная отписка при уничтожении класса
        _wallet.OnBalanceChanged -= HandleBalanceChanged;
    }
}