using System;
using CubeSmasher.Core.Interfaces;
using YG;

public class Wallet : IWallet
{
    public double Balance { get; private set; }

    // Возвращаем Action<double>, чтобы презентеры получали сумму
    public event Action<double> OnBalanceChanged;

    public Wallet(double initialBalance)
    {
        Balance = initialBalance;
    }

    public void Add(double amount)
    {
        Balance += amount;
        UpdateSaveData();
        OnBalanceChanged?.Invoke(Balance); // Передаем новую сумму всем, кто подписан
    }

    // Возвращаем TrySpend, который ждет ShopModel
    public bool TrySpend(double amount)
    {
        if (Balance >= amount)
        {
            Balance -= amount;
            UpdateSaveData();
            OnBalanceChanged?.Invoke(Balance); // Передаем новую сумму
            return true; // Успешно потратили
        }
        return false; // Не хватает денег
    }

    // Тихо обновляем данные в памяти
    private void UpdateSaveData()
    {
        YG2.saves.balance = Balance;
    }
}