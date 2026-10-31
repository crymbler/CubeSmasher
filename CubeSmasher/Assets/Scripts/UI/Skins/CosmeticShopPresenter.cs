using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using YG;
using YG.Utils.Pay;

namespace CubeSmasher.UI.Skins
{
    /// <summary>
    /// Логика экрана косметики. Знает, что показать в каждом слоте и что делать
    /// по нажатию, но ничего не знает о вёрстке: работает через ICosmeticShopView.
    ///
    /// Нажатие на слот значит разное в зависимости от состояния:
    ///  - ForCoins / ForYan — купить;
    ///  - Owned — надеть;
    ///  - Equipped — ничего;
    ///  - Locked — ничего, кнопка показывает нужную стадию.
    /// </summary>
    public class CosmeticShopPresenter : IDisposable
    {
        private readonly ICosmeticService _cosmetics;
        private readonly ICosmeticShopView _view;
        private readonly IWallet _wallet;
        private readonly StageModel _stage;

        public CosmeticShopPresenter(ICosmeticService cosmetics, ICosmeticShopView view,
                                     IWallet wallet, StageModel stage)
        {
            _cosmetics = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));

            _view.SlotClicked += HandleSlotClicked;
            _view.Closed += HandleClosed;

            _cosmetics.Equipped += HandleEquipped;
            _cosmetics.Purchased += HandlePurchased;
            _wallet.OnBalanceChanged += HandleBalanceChanged;

            YG2.onGetPayments += HandlePaymentsReady;

            RefreshAll();
        }

        public void Dispose()
        {
            _view.SlotClicked -= HandleSlotClicked;
            _view.Closed -= HandleClosed;

            _cosmetics.Equipped -= HandleEquipped;
            _cosmetics.Purchased -= HandlePurchased;
            _wallet.OnBalanceChanged -= HandleBalanceChanged;

            YG2.onGetPayments -= HandlePaymentsReady;
        }

        /// <summary>Открыть: перерисовать всё, цены в янах могли обновиться.</summary>
        public void RefreshAll()
        {
            CosmeticGroup[] groups = _cosmetics.Groups;

            for (int g = 0; g < groups.Length; g++)
            {
                _view.RenderGroup(BuildGroup(groups[g]));
            }
        }

        private CosmeticGroupData BuildGroup(CosmeticGroup group)
        {
            int count = group.Items != null ? group.Items.Length : 0;
            var slots = new CosmeticSlotData[count];

            for (int i = 0; i < count; i++)
            {
                slots[i] = BuildSlot(group.Id, group.Items[i]);
            }

            return new CosmeticGroupData
            {
                GroupId = group.Id,
                Title = group.GetTitle(),
                Slots = slots
            };
        }

        private CosmeticSlotData BuildSlot(string groupId, CosmeticItem item)
        {
            int stage = _stage.CurrentStage;
            bool owned = _cosmetics.IsOwned(groupId, item.Id);
            bool available = _cosmetics.IsAvailable(groupId, item.Id, stage);
            bool equipped = _cosmetics.GetEquipped(groupId) == item.Id;

            CosmeticSlotState state;

            if (owned)
            {
                state = equipped ? CosmeticSlotState.Equipped : CosmeticSlotState.Owned;
            }
            else if (stage < item.UnlockStage)
            {
                state = CosmeticSlotState.Locked;
            }
            else if (item.IsPremium || !item.ForCoins)
            {
                // Премиальный слот без PurchaseId купить нельзя: товар не заведён
                // в консоли Яндекс.Игр. Показываем явно, а не молча.
                state = string.IsNullOrEmpty(item.PurchaseId) || !available
                    ? CosmeticSlotState.Unavailable
                    : CosmeticSlotState.ForYan;
            }
            else
            {
                state = CosmeticSlotState.ForCoins;
            }

            return new CosmeticSlotData
            {
                GroupId = groupId,
                ItemId = item.Id,
                Name = item.GetName(),
                State = state,
                CoinPrice = item.SoftPrice,
                YanPrice = GetYanPrice(item.PurchaseId),
                UnlockStage = item.UnlockStage,
                IsPremium = item.IsPremium
            };
        }

        /// <summary>Цена в янах из данных платформы: "25 YAN". Пусто, если товар не загружен.</summary>
        private static string GetYanPrice(string purchaseId)
        {
            if (string.IsNullOrEmpty(purchaseId)) return string.Empty;

            Purchase purchase = YG2.PurchaseByID(purchaseId);
            return purchase != null ? purchase.price : string.Empty;
        }

        private void HandleSlotClicked(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            string[] parts = key.Split('/');
            if (parts.Length != 2) return;

            string groupId = parts[0];
            string itemId = parts[1];
            int stage = _stage.CurrentStage;

            if (_cosmetics.IsOwned(groupId, itemId))
            {
                _cosmetics.TryEquip(groupId, itemId);
                return;
            }

            // TryBuy для монет списывает и выдаёт сразу, для ян — отправляет платёж.
            // В обоих случаях перерисовка придёт по событиям сервиса.
            _cosmetics.TryBuy(groupId, itemId, stage);
        }

        private void HandlePurchased(string key) => RefreshGroupOf(key);

        private void HandleEquipped(string groupId)
        {
            CosmeticGroup[] groups = _cosmetics.Groups;

            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g].Id != groupId) continue;
                _view.RenderGroup(BuildGroup(groups[g]));
                return;
            }
        }

        /// <summary>
        /// Баланс изменился: обновить только слоты за монеты, чтобы не дёргать
        /// перерисовку всех групп на каждое начисление пассивного дохода.
        /// </summary>
        private void HandleBalanceChanged(double balance) => RefreshCoinSlots();

        private void HandlePaymentsReady() => RefreshAll();

        private void HandleClosed() => RefreshAll();

        private void RefreshCoinSlots()
        {
            CosmeticGroup[] groups = _cosmetics.Groups;

            for (int g = 0; g < groups.Length; g++)
            {
                CosmeticGroup group = groups[g];
                if (group.Items == null) continue;

                for (int i = 0; i < group.Items.Length; i++)
                {
                    CosmeticItem item = group.Items[i];
                    if (item.IsPremium || !item.ForCoins) continue;

                    _view.UpdateSlot(BuildSlot(group.Id, item));
                }
            }
        }

        private void RefreshGroupOf(string key)
        {
            string[] parts = key.Split('/');
            if (parts.Length != 2) return;

            CosmeticGroup[] groups = _cosmetics.Groups;
            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g].Id != parts[0]) continue;
                _view.RenderGroup(BuildGroup(groups[g]));
                return;
            }
        }
    }
}
