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
        public int bestScore = 0;
        public bool isAdsRemoved = false;

        public int level = 0;

        public double balance = 1;
        public double clickPerSecond = 1;
        public double tapPower = 1;

        public float soundVolume = 0.5f;
        public float musicVolume = 0.5f;

        // Твои списки. Мы будем использовать tapUpgrades для молота, 
        // а passiveUpgrades оставим для Гаража и деталей!
        public List<UpgradeData> passiveUpgrades = new List<UpgradeData>();
        public List<UpgradeData> tapUpgrades = new List<UpgradeData>();
    }
}