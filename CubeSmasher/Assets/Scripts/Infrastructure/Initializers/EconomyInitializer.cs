using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using YG;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Создаёт экономические сервисы (кошелёк, стадия, пассивный доход, магазин,
    /// оффлайн-доход), регистрирует их в контейнере и связывает события между ними.
    /// </summary>
    public class EconomyInitializer : IGameInitializer
    {
        private readonly GameSettings _settings;
        private readonly ISaveService _saveService;
        private readonly List<UpgradeConfig> _upgradeConfigs;

        /// <summary>Имя лидерборда по стадии. Должно совпадать с таблицей в консоли Яндекс.Игр (см. MANUAL_TODO).</summary>
        public const string LeaderboardName = "stage";

        public EconomyInitializer(GameSettings settings, ISaveService saveService, List<UpgradeConfig> upgradeConfigs)
        {
            _settings = settings;
            _saveService = saveService;
            _upgradeConfigs = upgradeConfigs;
        }

        public void Initialize()
        {
            ServiceLocator services = ServiceLocator.Instance;
            services.TryGet(out Subscriptions subs);

            var wallet = new Wallet(YG2.saves.balance);
            services.Register<IWallet>(wallet);

            // Если игра запущена впервые, в сейве level = 0, а стадии начинаются с 1
            int savedStage = YG2.saves.level < 1 ? 1 : YG2.saves.level;
            var stageModel = new StageModel(savedStage, YG2.saves.stageDetails);
            services.Register(stageModel);

            // Гараж создаём до дохода: доход считает по открытым машинам
            var garage = new GarageService(_settings.Garage, _saveService);
            services.Register<IGarageService>(garage);

            var passiveIncome = new PassiveIncome(_settings.Garage, garage);
            services.Register<IPassiveIncomeService>(passiveIncome);

            var shopModel = new ShopModel(wallet, _upgradeConfigs, _saveService);
            services.Register<IShopService>(shopModel);
            services.Register(shopModel); // презентеры магазина работают с конкретной моделью

            var offlineEarnings = new OfflineEarningsService(wallet, passiveIncome);
            services.Register(offlineEarnings);

            // Пассивный доход идёт в кошелёк
            passiveIncome.OnIncomeGenerated += wallet.Add;

            // На новой стадии пересчитываем доход и сохраняем прогресс
            // Открытие машин и пересчёт дохода по стадии: при старте (сохранённая стадия)
            // и при каждом переходе на новую. Раньше при старте доход оставался нулевым.
            garage.RefreshForStage(stageModel.CurrentStage);
            passiveIncome.RecalculateIncome(stageModel.CurrentStage);

            // Игрок открыл машину -> она начинает приносить доход, пересчитываем сразу.
            // Делегат в переменной: отписка лямбдой создаёт новый делегат и не работает.
            Action<int> onCarOpened = _ =>
            {
                passiveIncome.RecalculateIncome(stageModel.CurrentStage);
            };

            if (subs != null)
            {
                subs.Track(() => garage.CarOpened += onCarOpened,
                           () => garage.CarOpened -= onCarOpened);
            }

            // Все подписки — через Subscriptions, чтобы снимались при уничтожении Bootstrapper.
            // Делегаты хранятся в переменных: отписка работает только с тем же объектом.
            Action<int> onStageSaved = newStage =>
            {
                YG2.saves.level = newStage;
                YG2.saves.stageDetails = 0;
                _saveService.Save();
            };
            Action<int> onDetailsSaved = details => YG2.saves.stageDetails = details;

            // Межстраничная реклама: плагин сам держит таймер (interAdvInterval).
            // Здесь только просим показ, если игрок не купил отключение рекламы.
            Action<int> onStageAd = _ =>
            {
                if (YG2.saves.isAdsRemoved) return;
                YG2.InterstitialAdvShow();
            };

            if (subs != null)
            {
                subs.Track(() => stageModel.OnStageCompleted += garage.RefreshForStage,
                           () => stageModel.OnStageCompleted -= garage.RefreshForStage);
                subs.Track(() => stageModel.OnStageCompleted += passiveIncome.RecalculateIncome,
                           () => stageModel.OnStageCompleted -= passiveIncome.RecalculateIncome);
                subs.Track(() => stageModel.OnStageCompleted += onStageSaved,
                           () => stageModel.OnStageCompleted -= onStageSaved);
                subs.Track(() => stageModel.OnStageCompleted += onStageAd,
                           () => stageModel.OnStageCompleted -= onStageAd);

                // Лидерборд по максимальной стадии. Название таблицы — как в консоли Яндекс.Игр.
                // Плагин сам отправит результат только при включённых лидербордах и авторизации.
                Action<int> onStageLeaderboard = stage => YG2.SetLeaderboard(LeaderboardName, stage);
                subs.Track(() => stageModel.OnStageCompleted += onStageLeaderboard,
                           () => stageModel.OnStageCompleted -= onStageLeaderboard);
                subs.Track(() => stageModel.OnDetailsChanged += onDetailsSaved,
                           () => stageModel.OnDetailsChanged -= onDetailsSaved);
            }
        }
    }
}
