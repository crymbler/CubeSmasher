using System;
using CubeSmasher.Data.Configs;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Гараж: какие машины открыты, какая показана и какая выбрана для игры.
    /// Показать можно любую машину, выбрать для игры — только открытую.
    /// Доход и бонусы даёт выбранная машина (см. PassiveIncome и StatService).
    /// </summary>
    public interface IGarageService
    {
        /// <summary>Все машины из конфига, в порядке конфига.</summary>
        GarageCarData[] AllCars { get; }

        /// <summary>Стадия пройдена: машину можно открыть. Ещё не значит "открыта".</summary>
        bool IsAvailable(int index);

        /// <summary>Открыл ли игрок машину сам.</summary>
        bool IsOpened(int index);

        /// <summary>Состояние машины для интерфейса.</summary>
        GarageCarState GetState(int index);

        /// <summary>Открыть доступную машину. Выбранной для игры она не становится.</summary>
        bool Open(int index);

        /// <summary>Индекс машины, выбранной для игры. Её доход и бонусы действуют.</summary>
        int SelectedIndex { get; }

        /// <summary>Индекс машины, показанной в гараже. Может отличаться от выбранной.</summary>
        int ViewedIndex { get; }

        /// <summary>Показать машину в гараже: любую, включая закрытую. Доход и бонусы не меняет.</summary>
        bool View(int index);

        /// <summary>Выбрать машину для игры. Возвращает false, если она не открыта.</summary>
        bool TrySelect(int index);

        /// <summary>Пересчитать открытые машины по текущей стадии.</summary>
        void RefreshForStage(int stage);

        /// <summary>Игрок показал другую машину (только интерфейс).</summary>
        event Action<int> ViewChanged;

        /// <summary>Стадия пройдена, машину можно открыть.</summary>
        event Action<int> CarAvailable;

        /// <summary>Игрок открыл машину.</summary>
        event Action<int> CarOpened;

        /// <summary>Игрок выбрал другую машину для игры: доход и бонусы меняются.</summary>
        event Action<int> SelectionChanged;
    }
}
