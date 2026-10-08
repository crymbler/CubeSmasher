using System;
using YG;

namespace CubeSmasher.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса магазина улучшений
    /// </summary>
    public interface IShopService
    {
        /// <summary>
        /// Получить данные улучшения (уровень, цена, значение)
        /// </summary>
        UpgradeData GetUpgradeData(string id);

        /// <summary>Конфиг улучшения: название, иконка, лимиты, стадия разблокировки.</summary>
        UpgradeConfig GetConfig(string id);

        /// <summary>
        /// Попытка купить улучшение
        /// </summary>
        /// <returns>true если покупка успешна</returns>
        bool TryPurchaseUpgrade(string id);

        /// <summary>
        /// Событие покупки улучшения (передает id купленного улучшения)
        /// </summary>
        event Action<string> OnUpgradePurchased;
    }
}
