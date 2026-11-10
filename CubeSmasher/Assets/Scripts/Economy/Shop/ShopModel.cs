using System;
using System.Collections.Generic;
using System.Linq;
using CubeSmasher.Core.Interfaces;
using YG;

public class ShopModel : IShopService
{
    private readonly IWallet _wallet;
    private readonly ISaveService _saveService;
    private readonly List<UpgradeConfig> _availableUpgrades;

    // Событие для UI и игровых систем: апгрейд куплен, обновите картинку и статы
    public event Action<string> OnUpgradePurchased;

    public IReadOnlyList<UpgradeConfig> AllConfigs => _availableUpgrades;

    public ShopModel(IWallet wallet, List<UpgradeConfig> availableUpgrades, ISaveService saveService)
    {
        _wallet = wallet;
        _availableUpgrades = availableUpgrades;
        _saveService = saveService;

        InitializeSaves();
    }

    private void InitializeSaves()
    {
        bool isDataChanged = false;

        // Уровень 0 означает "ничего не куплено". Покупка поднимает его до 1,
        // поэтому при MaxLevel 10 игрок делает ровно 10 покупок.
        foreach (UpgradeConfig config in _availableUpgrades)
        {
            if (config == null) continue;

            UpgradeData data = YG2.saves.upgrades.FirstOrDefault(u => u.id == config.Id);

            if (data == null)
            {
                data = new UpgradeData
                {
                    id = config.Id,
                    level = 0,
                    price = config.CalculatePrice(0),
                    value = config.CalculateValue(0)
                };

                YG2.saves.upgrades.Add(data);
                isDataChanged = true;
                continue;
            }

            // Старые сохранения считали базу с 1: уровень 1 значил "ничего не куплено".
            // Сдвигаем один раз, иначе у игрока появится лишняя покупка.
            if (!YG2.saves.upgradeLevelMigrated && data.level > 0)
            {
                data.level--;
                isDataChanged = true;
            }

            if (data.level < 0) data.level = 0;

            // Цена и сила всегда берутся из конфига, а не из сохранения:
            // сохранённые значения могли остаться от прошлой формулы
            data.price = config.CalculatePrice(data.level);
            data.value = config.CalculateValue(data.level);
        }

        if (!YG2.saves.upgradeLevelMigrated)
        {
            YG2.saves.upgradeLevelMigrated = true;
            isDataChanged = true;
        }

        if (isDataChanged) _saveService.Save();
    }

    public UpgradeData GetUpgradeData(string id)
    {
        return YG2.saves.upgrades.FirstOrDefault(u => u.id == id);
    }

    public UpgradeConfig GetConfig(string id)
    {
        return _availableUpgrades.FirstOrDefault(c => c.Id == id);
    }

    public bool TryPurchaseUpgrade(string id)
    {
        UpgradeData data = GetUpgradeData(id);
        UpgradeConfig config = GetConfig(id);

        if (data == null || config == null) return false;

        // Стадию проверяем при каждой покупке. Раньше здесь был флаг isLocked,
        // записанный один раз при создании: апгрейд, закрытый на старте, так и
        // оставался заблокированным, даже когда стадия уже пройдена.
        int currentStage = YG2.saves.level < 0 ? 0 : YG2.saves.level;
        if (currentStage < config.UnlockStageLevel) return false;
        if (data.level >= config.MaxLevel) return false;

        if (!_wallet.TrySpend(data.price)) return false;

        data.level++;
        data.price = config.CalculatePrice(data.level);
        data.value = config.CalculateValue(data.level);

        _saveService.Save();
        OnUpgradePurchased?.Invoke(id);
        return true;
    }
}
