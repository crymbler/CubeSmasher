using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Каталог косметики: группы молотов и тарелок.
    ///
    /// Покупка за монеты идёт через IWallet, покупка за яны — реальный платёж
    /// Яндекс.Игр по PurchaseId. Пока платёж не подтверждён, предмет не выдаётся:
    /// выдача происходит в обработчике YG2.onPurchaseSuccess.
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticConfig", menuName = "CubeSmasher/Cosmetic Config")]
    public class CosmeticConfig : ScriptableObject
    {
        [Header("Группы")]
        [Tooltip("Молоты и тарелки. Порядок важен: в этом порядке группы идут в магазине")]
        public CosmeticGroup[] Groups;

        [Header("Донат")]
        [Tooltip("Выключено — премиальные слоты показываются, но купить их нельзя. " +
                 "Нужно, пока товары не созданы в консоли Яндекс.Игр")]
        public bool PaymentsEnabled = true;

        [Tooltip("Id товара для пополнения баланса ян, если используется валюта-кошелёк. " +
                 "Пусто — каждый премиальный предмет покупается напрямую через свой PurchaseId")]
        public string YanRechargePurchaseId;
    }
}
