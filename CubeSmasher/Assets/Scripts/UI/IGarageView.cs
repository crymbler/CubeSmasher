using System;
using UnityEngine;

/// <summary>
/// Контракт окна гаража. Презентер работает только с ним.
/// </summary>
public interface IGarageView
{
    /// <summary>Игрок выбрал машину в списке.</summary>
    event Action<int> CarSelected;

    /// <summary>Сколько машин всего, чтобы кнопки знали границы.</summary>
    void SetCarCount(int count);

    /// <summary>Показать выбранную машину: её модель, название, доход, статус.</summary>
    void ShowCar(int index, string name, string description, double incomePerSecond,
                 bool isUnlocked, int unlockStage, GameObject modelPrefab);
}
