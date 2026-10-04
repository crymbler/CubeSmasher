using System;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса гаража (управление машинами)
    /// </summary>
    public interface IGarageService
    {
        /// <summary>
        /// Получить данные машины по индексу
        /// </summary>
        GarageCarData GetCarData(int index);

        /// <summary>
        /// Получить все разблокированные машины
        /// </summary>
        GarageCarData[] GetUnlockedCars();

        /// <summary>
        /// Проверить разблокирована ли машина
        /// </summary>
        bool IsCarUnlocked(int index);

        /// <summary>
        /// Разблокировать машину при достижении уровня
        /// </summary>
        void UnlockCar(int stageLevel);

        /// <summary>
        /// Событие разблокировки машины
        /// </summary>
        event Action<GarageCarData> OnCarUnlocked;
    }
}
