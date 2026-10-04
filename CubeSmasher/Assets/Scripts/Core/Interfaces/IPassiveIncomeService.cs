using System;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса пассивного дохода
    /// </summary>
    public interface IPassiveIncomeService
    {
        /// <summary>
        /// Текущий доход в секунду
        /// </summary>
        double CurrentIncomePerSecond { get; }

        /// <summary>
        /// Пересчитать доход на основе текущего этажа
        /// </summary>
        void RecalculateIncome(int currentStage);

        /// <summary>
        /// Обновление каждый кадр (начисление дохода)
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Событие генерации дохода (передает сумму)
        /// </summary>
        event Action<double> OnIncomeGenerated;
    }
}
