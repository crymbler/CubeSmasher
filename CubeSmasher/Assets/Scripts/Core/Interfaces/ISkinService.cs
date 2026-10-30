using System;
using CubeSmasher.Data.Configs;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Скины: какие куплены, какой выбран и сколько твёрдой валюты у игрока.
    ///
    /// Мягкая валюта (монеты) списывается через IWallet, твёрдая хранится
    /// в сохранении отдельным счётчиком. Пока SkinConfig.HardCurrencyEnabled
    /// выключен, премиальные скины недоступны и твёрдая валюта не тратится.
    /// </summary>
    public interface ISkinService
    {
        /// <summary>Все скины из конфига, в порядке конфига.</summary>
        SkinData[] AllSkins { get; }

        /// <summary>Включена ли твёрдая валюта. Управляется флагом в SkinConfig.</summary>
        bool HardCurrencyEnabled { get; }

        /// <summary>Сколько твёрдой валюты у игрока.</summary>
        int HardCurrency { get; }

        /// <summary>Id выбранного скина. Пустая строка — скин не выбран.</summary>
        string SelectedId { get; }

        /// <summary>Куплен ли скин или доступен по умолчанию.</summary>
        bool IsOwned(string id);

        /// <summary>Скин виден в магазине: стадия пройдена и скин не премиальный при выключенной твёрдой валюте.</summary>
        bool IsAvailable(string id, int stage);

        /// <summary>Купить скин. Возвращает false, если уже куплен, недоступен или не хватает валюты.</summary>
        bool TryBuy(string id, int stage);

        /// <summary>Выбрать купленный скин. Возвращает false, если скин не куплен.</summary>
        bool TrySelect(string id);

        /// <summary>Выбран другой скин.</summary>
        event Action<string> SelectionChanged;

        /// <summary>Скин куплен.</summary>
        event Action<string> SkinPurchased;
    }
}
