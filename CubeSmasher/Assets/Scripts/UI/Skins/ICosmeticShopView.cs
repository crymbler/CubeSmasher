using System;

namespace CubeSmasher.UI.Skins
{
    /// <summary>Состояние одного слота в магазине косметики.</summary>
    public enum CosmeticSlotState
    {
        /// <summary>Можно купить за монеты.</summary>
        ForCoins,

        /// <summary>Можно купить за яны.</summary>
        ForYan,

        /// <summary>Куплено, не надето.</summary>
        Owned,

        /// <summary>Куплено и надето.</summary>
        Equipped,

        /// <summary>Стадия ещё не пройдена.</summary>
        Locked,

        /// <summary>Покупка за яны недоступна: товар не заведён в консоли или донат выключен.</summary>
        Unavailable
    }

    /// <summary>Данные одного слота для отображения. Без ссылок на модели и сервисы.</summary>
    public struct CosmeticSlotData
    {
        public string GroupId;
        public string ItemId;
        public string Name;
        public CosmeticSlotState State;

        /// <summary>Цена в монетах. Имеет смысл только для ForCoins.</summary>
        public double CoinPrice;

        /// <summary>Цена в янах строкой из платформы: "25 YAN". Пусто — не задана.</summary>
        public string YanPrice;

        public int UnlockStage;
        public bool IsPremium;
    }

    /// <summary>Одна группа в магазине: заголовок и три слота.</summary>
    public struct CosmeticGroupData
    {
        public string GroupId;
        public string Title;
        public CosmeticSlotData[] Slots;
    }

    /// <summary>
    /// Контракт экрана косметики. Презентер работает только с ним,
    /// поэтому экран можно переделать, не трогая логику покупки.
    /// </summary>
    public interface ICosmeticShopView
    {
        /// <summary>Игрок нажал на слот. id — "groupId/itemId".</summary>
        event Action<string> SlotClicked;

        /// <summary>Игрок закрыл магазин.</summary>
        event Action Closed;

        /// <summary>Перерисовать группу целиком (создаёт её, если ещё нет).</summary>
        void RenderGroup(CosmeticGroupData group);

        /// <summary>Обновить один слот, если группа уже создана.</summary>
        void UpdateSlot(CosmeticSlotData slot);
    }
}
