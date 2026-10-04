using System;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для управления балансом игрока
    /// </summary>
    public interface IWallet
    {
        /// <summary>
        /// Текущий баланс
        /// </summary>
        double Balance { get; }

        /// <summary>
        /// Событие изменения баланса (передает новое значение)
        /// </summary>
        event Action<double> OnBalanceChanged;

        /// <summary>
        /// Попытка потратить указанную сумму
        /// </summary>
        /// <returns>true если хватило денег, false если недостаточно</returns>
        bool TrySpend(double amount);

        /// <summary>
        /// Добавить деньги на баланс
        /// </summary>
        void Add(double amount);
    }
}
