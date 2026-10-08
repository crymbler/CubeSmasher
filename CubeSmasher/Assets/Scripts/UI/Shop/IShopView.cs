using System;
using UnityEngine;

/// <summary>
/// Данные одной строки магазина для отображения. Без ссылок на модели.
/// </summary>
public struct UpgradeRowData
{
    public string Name;
    public Sprite Icon;
    public int Level;
    public double Price;
    public int UnlockStage;
    public bool IsLocked;
    public bool IsMaxLevel;
    public bool CanAfford;
}

/// <summary>
/// Контракт View магазина. Презентер работает только с ним.
/// </summary>
public interface IShopView
{
    /// <summary>Игрок нажал "Купить" у улучшения с данным id.</summary>
    event Action<string> BuyRequested;

    /// <summary>Обновить строку улучшения (создаёт строку, если её ещё нет).</summary>
    void Render(string id, UpgradeRowData row);
}
