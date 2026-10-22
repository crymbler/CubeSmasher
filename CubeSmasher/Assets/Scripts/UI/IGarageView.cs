using System;
using CubeSmasher.Core.Interfaces;
using UnityEngine;

/// <summary>
/// Контракт окна гаража. Презентер работает только с ним.
/// </summary>
public interface IGarageView
{
    /// <summary>Игрок выбрал машину в списке.</summary>
    event Action<int> CarSelected;

    /// <summary>Построить ленту: иконки и стартовые состояния каждой машины.</summary>
    void SetCarCount(UnityEngine.Sprite[] icons, GarageCarState[] states);

    /// <summary>Обновить состояние одной иконки: закрытая приглушается, доступная получает кружок.</summary>
    void SetCarState(int index, GarageCarState state);

    /// <summary>Игрок нажал "Открыть" у доступной машины.</summary>
    event Action<int> CarOpenRequested;

    /// <summary>Показать машину в одном из трёх состояний. См. GarageCarState.</summary>
    void ShowCar(int index, string name, string description, string characteristics,
                 double incomePerSecond, GarageCarState state, int unlockStage,
                 GameObject modelPrefab);

    /// <summary>Отыграть эффект открытия: партиклы и звук. Вызывается после CarOpenRequested.</summary>
    void PlayUnlockEffect();
}
