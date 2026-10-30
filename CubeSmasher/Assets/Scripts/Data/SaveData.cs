using System;
using System.Collections.Generic;

namespace YG
{
    [Serializable]
    public class UpgradeData
    {
        public string id;
        public int level;        // Добавили уровень (важно для формул)
        public double price;
        public double value;     // Заменили profit на value (так как это может быть Урон, Радиус или Шанс)
        public bool isLocked;
    }

    public partial class SavesYG
    {
        public long lastSaveTime;
        public int bestScore = 0;
        public bool isAdsRemoved = false;

        public int level = 0;

        /// <summary>Сколько деталей уже разбито на текущей стадии. Иначе прогресс обнулялся при перезапуске.</summary>
        public int stageDetails = 0;

        /// <summary>Облако не приняло последнее сохранение: локальная копия ждёт отправки.</summary>
        public bool hasPendingLocalSave = false;

        public int lastGarageCarIndex = 0;

        /// <summary>Индексы машин, которые игрок открыл сам (нажал "Открыть").</summary>
        public List<int> openedGarageCars = new List<int>();

        /// <summary>Однократный перенос: у старых сохранений открытые по стадии машины
        /// считались открытыми. Флаг не даёт потерять доход при обновлении.</summary>
        public bool garageOpenMigrated = false;

        public double balance = 1;
        public double clickPerSecond = 1;
        public double tapPower = 1;

        public float soundVolume = 0.5f;
        public float musicVolume = 0.5f;

        /// <summary>Твёрдая валюта (донат). Тратится только при включённом флаге в SkinConfig.</summary>
        public int hardCurrency = 0;

        /// <summary>Id выбранного скина. Пустая строка — скин не выбран.</summary>
        public string selectedSkinId = "";

        /// <summary>Купленные скины. Стартовый сюда не попадает, он доступен по флагу IsDefault.</summary>
        public List<string> ownedSkins = new List<string>();

        /// <summary>Стартовый скин и валюта выданы. Не даёт выдать повторно.</summary>
        public bool skinGranted = false;

        // Твои списки. Мы будем использовать tapUpgrades для молота, 
        // а passiveUpgrades оставим для Гаража и деталей!
        public List<UpgradeData> passiveUpgrades = new List<UpgradeData>();
        public List<UpgradeData> tapUpgrades = new List<UpgradeData>();
    }
}