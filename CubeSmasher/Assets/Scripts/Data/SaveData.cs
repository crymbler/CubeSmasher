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

        /// <summary>
        /// Уровни апгрейдов переехали с базы 1 на базу 0: первая покупка даёт уровень 1.
        /// Флаг не даёт сдвинуть уровни повторно.
        /// </summary>
        public bool upgradeLevelMigrated;

        public double balance = 1;
        public double clickPerSecond = 1;
        public double tapPower = 1;

        public float soundVolume = 0.5f;
        public float musicVolume = 0.5f;

        /// <summary>Купленная косметика в виде "группа/предмет": "hammer/hammer_1".</summary>
        public List<string> ownedCosmetics = new List<string>();

        /// <summary>Надетый молот. Пусто — берётся предмет по умолчанию.</summary>
        public string equippedHammer = "";

        /// <summary>Надетая тарелка. Пусто — берётся предмет по умолчанию.</summary>
        public string equippedDish = "";

        public List<UpgradeData> upgrades = new List<UpgradeData>();
    }
}