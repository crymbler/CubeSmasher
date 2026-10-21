using System;
using CubeSmasher.Data.Configs;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Гараж: какие машины открыты и какая сейчас выбрана. Доход машин считает PassiveIncome.
    /// </summary>
    public interface IGarageService
    {
        /// <summary>Все машины из конфига, в порядке конфига.</summary>
        GarageCarData[] AllCars { get; }

        /// <summary>Стадия пройдена: машину можно открыть. Ещё не значит "открыта".</summary>
        bool IsAvailable(int index);

        /// <summary>Открыл ли игрок машину сам. Только такие машины дают доход.</summary>
        bool IsOpened(int index);

        /// <summary>Состояние машины для интерфейса.</summary>
        GarageCarState GetState(int index);

        /// <summary>Открыть доступную машину. Возвращает false, если она закрыта или уже открыта.</summary>
        bool Open(int index);

        /// <summary>Индекс выбранной машины (то, что показано в гараже).</summary>
        int SelectedIndex { get; }

        /// <summary>Выбрать машину. Возвращает false, если она закрыта или индекс некорректен.</summary>
        bool TrySelect(int index);

        /// <summary>Пересчитать открытые машины по текущей стадии.</summary>
        void RefreshForStage(int stage);

        /// <summary>Стадия пройдена: машина стала доступна для открытия (но ещё не открыта).</summary>
        event Action<int> CarAvailable;

        /// <summary>Игрок открыл машину. С этого момента она даёт доход.</summary>
        event Action<int> CarOpened;

        /// <summary>Вызывается при смене выбранной машины.</summary>
        event Action<int> SelectionChanged;
    }
}
