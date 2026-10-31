using System;
using CubeSmasher.Data.Configs;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Косметика: какие молоты и тарелки куплены, что надето сейчас.
    ///
    /// Две валюты: монеты игры (IWallet) и яны Яндекс.Игр. Покупка за яны —
    /// не списание со счёта, а реальный платёж по PurchaseId: предмет выдаётся
    /// в TryBuy, только когда платформа подтвердила покупку.
    /// </summary>
    public interface ICosmeticService
    {
        /// <summary>Группы из конфига: молоты, тарелки.</summary>
        CosmeticGroup[] Groups { get; }

        /// <summary>Id предмета, надетого в группе. Пусто — предмет по умолчанию.</summary>
        string GetEquipped(string groupId);

        /// <summary>Куплен ли предмет или доступен по умолчанию.</summary>
        bool IsOwned(string groupId, string itemId);

        /// <summary>Виден ли предмет в магазине на текущей стадии.</summary>
        bool IsAvailable(string groupId, string itemId, int stage);

        /// <summary>
        /// Купить. Для монет списывает сразу и возвращает true.
        /// Для ян отправляет платёж и возвращает true, если запрос ушёл:
        /// предмет выдаётся позже, в onPurchaseSuccess.
        /// </summary>
        bool TryBuy(string groupId, string itemId, int stage);

        /// <summary>Надеть купленный предмет.</summary>
        bool TryEquip(string groupId, string itemId);

        /// <summary>Надет другой предмет.</summary>
        event Action<string> Equipped;

        /// <summary>Предмет куплен (после подтверждения платежа или списания монет).</summary>
        event Action<string> Purchased;
    }
}
