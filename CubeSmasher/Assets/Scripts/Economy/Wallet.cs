using System;

public class Wallet
{
    private double _balance;

    // Событие, которое кричит: "Баланс изменился, вот новое значение!"
    public event Action<double> OnBalanceChanged;

    public double Balance => _balance;

    // Инициализация при загрузке (из облака YG)
    public Wallet(double initialBalance)
    {
        _balance = initialBalance < 0 ? 0 : initialBalance;
    }

    public void AddMoney(double amount)
    {
        if (amount <= 0) return;

        _balance += amount;
        OnBalanceChanged?.Invoke(_balance);
    }

    // Возвращает true, если покупка успешна
    public bool TrySpend(double amount)
    {
        if (amount <= 0 || _balance < amount)
            return false;

        _balance -= amount;
        OnBalanceChanged?.Invoke(_balance);
        return true;
    }
}