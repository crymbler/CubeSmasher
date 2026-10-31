using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using YG;

/// <summary>
/// Косметика: покупка, владение, экипировка.
///
/// Два вида оплаты:
///  - монеты игры: TryBuy списывает через IWallet и сразу выдаёт предмет;
///  - яны Яндекс.Игр: TryBuy вызывает YG2.BuyPayments(PurchaseId) и НЕ выдаёт
///    предмет. Выдача происходит в HandlePurchaseSuccess, когда платформа
///    подтвердила оплату. Иначе игрок получал бы товар, не заплатив.
///
/// Ожидающие покупки хранятся в словаре purchaseId -> itemId: платформа
/// возвращает в onPurchaseSuccess только id товара, а нам нужен предмет.
/// </summary>
public class CosmeticService : ICosmeticService
{
    private readonly CosmeticConfig _config;
    private readonly IWallet _wallet;
    private readonly ISaveService _saveService;
    private readonly List<string> _owned = new List<string>();

    /// <summary>PurchaseId -> "groupId/itemId". Заполняется при запросе покупки за яны.</summary>
    private readonly Dictionary<string, string> _pendingPurchases = new Dictionary<string, string>();

    public event Action<string> Equipped;
    public event Action<string> Purchased;

    public CosmeticService(CosmeticConfig config, IWallet wallet, ISaveService saveService)
    {
        _config = config;
        _wallet = wallet;
        _saveService = saveService;

        LoadOwned();
        EnsureDefaults();

        YG2.onPurchaseSuccess += HandlePurchaseSuccess;
        YG2.onPurchaseFailed += HandlePurchaseFailed;
    }

    /// <summary>
    /// Отписка от событий плагина. Вызывается при уничтожении Bootstrapper:
    /// статические события YG2 переживают сцену, и без отписки сервис
    /// получал бы чужие покупки после перезапуска.
    /// </summary>
    public void Dispose()
    {
        YG2.onPurchaseSuccess -= HandlePurchaseSuccess;
        YG2.onPurchaseFailed -= HandlePurchaseFailed;
    }

    public CosmeticGroup[] Groups =>
        _config != null && _config.Groups != null ? _config.Groups : Array.Empty<CosmeticGroup>();

    public string GetEquipped(string groupId) =>
        groupId switch
        {
            "hammer" => YG2.saves.equippedHammer,
            "dish" => YG2.saves.equippedDish,
            _ => string.Empty
        };

    public bool IsOwned(string groupId, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        CosmeticItem? item = Find(groupId, itemId);
        if (item.HasValue && item.Value.IsDefault) return true;

        return _owned.Contains(Key(groupId, itemId));
    }

    public bool IsAvailable(string groupId, string itemId, int stage)
    {
        CosmeticItem? found = Find(groupId, itemId);
        if (!found.HasValue) return false;

        CosmeticItem item = found.Value;
        if (item.IsPremium && (_config == null || !_config.PaymentsEnabled)) return false;

        return stage >= item.UnlockStage;
    }

    public bool TryBuy(string groupId, string itemId, int stage)
    {
        if (IsOwned(groupId, itemId)) return false;
        if (!IsAvailable(groupId, itemId, stage)) return false;

        CosmeticItem item = Find(groupId, itemId).Value;

        if (item.IsPremium || !item.ForCoins)
        {
            if (string.IsNullOrEmpty(item.PurchaseId))
            {
                UnityEngine.Debug.LogWarning(
                    $"[Cosmetic] У {groupId}/{itemId} нет PurchaseId: покупка за яны невозможна. " +
                    "Создайте товар в консоли разработчика Яндекс.Игр и впишите его id.");
                return false;
            }

            _pendingPurchases[item.PurchaseId] = Key(groupId, itemId);
            YG2.BuyPayments(item.PurchaseId);
            return true; // запрос ушёл, выдача придёт в onPurchaseSuccess
        }

        if (!_wallet.TrySpend(item.SoftPrice)) return false;

        Grant(groupId, itemId);
        return true;
    }

    public bool TryEquip(string groupId, string itemId)
    {
        if (!IsOwned(groupId, itemId)) return false;
        if (GetEquipped(groupId) == itemId) return true;

        switch (groupId)
        {
            case "hammer": YG2.saves.equippedHammer = itemId; break;
            case "dish": YG2.saves.equippedDish = itemId; break;
            default:
                UnityEngine.Debug.LogWarning($"[Cosmetic] Неизвестная группа: {groupId}");
                return false;
        }

        _saveService.Save();
        Equipped?.Invoke(groupId);
        return true;
    }

    /// <summary>Первый предмет группы, помеченный IsDefault, надевается и считается доступным.</summary>
    private void EnsureDefaults()
    {
        CosmeticGroup[] groups = Groups;

        for (int g = 0; g < groups.Length; g++)
        {
            CosmeticGroup group = groups[g];
            if (group.Items == null || group.Items.Length == 0) continue;

            string equipped = GetEquipped(group.Id);
            if (!string.IsNullOrEmpty(equipped)) continue;

            for (int i = 0; i < group.Items.Length; i++)
            {
                if (!group.Items[i].IsDefault) continue;
                TryEquip(group.Id, group.Items[i].Id);
                break;
            }
        }
    }

    private void Grant(string groupId, string itemId)
    {
        string key = Key(groupId, itemId);
        if (_owned.Contains(key)) return;

        _owned.Add(key);
        SaveOwned();
        Purchased?.Invoke(key);
    }

    private void HandlePurchaseSuccess(string purchaseId)
    {
        if (string.IsNullOrEmpty(purchaseId)) return;
        if (!_pendingPurchases.TryGetValue(purchaseId, out string key)) return;

        _pendingPurchases.Remove(purchaseId);

        string[] parts = key.Split('/');
        if (parts.Length != 2) return;

        Grant(parts[0], parts[1]);
    }

    private void HandlePurchaseFailed(string purchaseId)
    {
        if (string.IsNullOrEmpty(purchaseId)) return;

        // Запрос не удался: убираем ожидание, иначе следующая успешная покупка
        // того же товара выдала бы предмет дважды.
        _pendingPurchases.Remove(purchaseId);
    }

    private CosmeticItem? Find(string groupId, string itemId)
    {
        CosmeticGroup[] groups = Groups;

        for (int g = 0; g < groups.Length; g++)
        {
            if (groups[g].Id != groupId || groups[g].Items == null) continue;

            for (int i = 0; i < groups[g].Items.Length; i++)
            {
                if (groups[g].Items[i].Id == itemId) return groups[g].Items[i];
            }
        }
        return null;
    }

    private static string Key(string groupId, string itemId) => groupId + "/" + itemId;

    private void LoadOwned()
    {
        _owned.Clear();
        if (YG2.saves.ownedCosmetics != null) _owned.AddRange(YG2.saves.ownedCosmetics);
    }

    private void SaveOwned()
    {
        YG2.saves.ownedCosmetics = new List<string>(_owned);
        _saveService.Save();
    }
}
