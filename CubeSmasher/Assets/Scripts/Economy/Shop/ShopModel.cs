using System;
using System.Collections.Generic;
using System.Linq;
using YG; // Пространство имен плагина Яндекса

public class ShopModel
{
    private readonly Wallet _wallet;
    private readonly List<UpgradeConfig> _availableUpgrades;

    // Событие для UI и игровых систем: "Апгрейд куплен, обновите картинку и статы!"
    public event Action<string> OnUpgradeChanged;

    public ShopModel(Wallet wallet, List<UpgradeConfig> availableUpgrades)
    {
        _wallet = wallet;
        _availableUpgrades = availableUpgrades;

        InitializeSaves();
    }

    // Синхронизация наших конфигов с сохранениями YG2
    private void InitializeSaves()
    {
        bool isDataChanged = false;

        // Получаем текущую стадию игрока (если 0, считаем как 1)
        int currentStage = YG2.saves.level < 1 ? 1 : YG2.saves.level;

        foreach (var config in _availableUpgrades)
        {
            // Выбираем нужный список на основе категории (Tap или Passive)
            List<UpgradeData> targetList = config.Category == UpgradeCategory.Tap
                ? YG2.saves.tapUpgrades
                : YG2.saves.passiveUpgrades;

            // Ищем, есть ли уже сохранение для этого апгрейда
            UpgradeData data = targetList.FirstOrDefault(u => u.id == config.Id);

            // Если сохранения нет, создаем его (игрок новенький или мы добавили апгрейд)
            if (data == null)
            {
                data = new UpgradeData
                {
                    id = config.Id,
                    level = 1,
                    // Теперь проверяем блокировку по текущей стадии игрока
                    isLocked = currentStage < config.UnlockStageLevel
                };

                // Считаем стартовые значения по формулам из конфига
                data.price = config.CalculatePrice(data.level);
                data.value = config.CalculateValue(data.level);

                targetList.Add(data);
                isDataChanged = true;
            }
        }

        // Если мы добавили новые апгрейды в список, сразу сохраняем их в облако
        //if (isDataChanged)
        //{
        //    YG2.SaveProgress();
        //}
    }


    // Метод для получения текущих данных апгрейда (используем в UI и для получения Урона/Радиуса)
    public UpgradeData GetUpgradeData(string id)
    {
        return YG2.saves.tapUpgrades.FirstOrDefault(u => u.id == id)
            ?? YG2.saves.passiveUpgrades.FirstOrDefault(u => u.id == id);
    }

    // Метод для получения базовых настроек (имя, иконка, макс. уровень)
    public UpgradeConfig GetConfig(string id)
    {
        return _availableUpgrades.FirstOrDefault(c => c.Id == id);
    }

    // Логика покупки (вызывается из UI)
    public bool TryBuyUpgrade(string id)
    {
        UpgradeData data = GetUpgradeData(id);
        UpgradeConfig config = GetConfig(id);

        if (data == null || config == null) return false;
        if (data.isLocked) return false;
        if (data.level >= config.MaxLevel) return false; // Достигнут лимит

        // Пытаемся списать деньги из Кошелька
        if (_wallet.TrySpend(data.price))
        {
            // Повышаем уровень
            data.level++;

            // Пересчитываем новую цену и силу по формулам из Конфига
            data.price = config.CalculatePrice(data.level);
            data.value = config.CalculateValue(data.level);

            // Сохраняем прогресс в облако Яндекса
            YG2.SaveProgress();

            // Кричим всем системам, что апгрейд обновился
            OnUpgradeChanged?.Invoke(id);

            return true;
        }

        return false;
    }
}