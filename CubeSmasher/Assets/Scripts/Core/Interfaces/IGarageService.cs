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

        /// <summary>Открыта ли машина с данным индексом.</summary>
        bool IsUnlocked(int index);

        /// <summary>Индекс выбранной машины (то, что показано в гараже).</summary>
        int SelectedIndex { get; }

        /// <summary>Выбрать машину. Возвращает false, если она закрыта или индекс некорректен.</summary>
        bool TrySelect(int index);

        /// <summary>Пересчитать открытые машины по текущей стадии.</summary>
        void RefreshForStage(int stage);

        /// <summary>Вызывается, когда открылась новая машина.</summary>
        event Action<int> CarUnlocked;

        /// <summary>Вызывается при смене выбранной машины.</summary>
        event Action<int> SelectionChanged;
    }
}
