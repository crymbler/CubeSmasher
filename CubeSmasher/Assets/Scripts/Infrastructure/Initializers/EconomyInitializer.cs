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

        public EconomyInitializer(GameSettings settings, ISaveService saveService, List<UpgradeConfig> upgradeConfigs)
        {
            _settings = settings;
            _saveService = saveService;
            _upgradeConfigs = upgradeConfigs;
        }

        public void Initialize()
        {
            ServiceLocator services = ServiceLocator.Instance;

            var wallet = new Wallet(YG2.saves.balance);
            services.Register<IWallet>(wallet);

            // Если игра запущена впервые, в сейве level = 0, а стадии начинаются с 1
            int savedStage = YG2.saves.level < 1 ? 1 : YG2.saves.level;
            var stageModel = new StageModel(savedStage);
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

            stageModel.OnStageCompleted += garage.RefreshForStage;
            stageModel.OnStageCompleted += passiveIncome.RecalculateIncome;
            stageModel.OnStageCompleted += newStage =>
            {
                YG2.saves.level = newStage;
                _saveService.Save();
            };
        }
    }
}
