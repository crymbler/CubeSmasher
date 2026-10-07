using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using YG;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Создаёт экономические сервисы (кошелёк, стадия, пассивный доход, магазин,
    /// оффлайн-доход), регистрирует их в контейнере и связывает события между ними.
    /// </summary>
    public class EconomyInitializer : IGameInitializer
    {
        private readonly GameConfig _config;
        private readonly ISaveService _saveService;
        private readonly List<UpgradeConfig> _upgradeConfigs;

        public EconomyInitializer(GameConfig config, ISaveService saveService, List<UpgradeConfig> upgradeConfigs)
        {
            _config = config;
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

            var passiveIncome = new PassiveIncome(_config);
            services.Register<IPassiveIncomeService>(passiveIncome);

            var shopModel = new ShopModel(wallet, _upgradeConfigs, _saveService);
            services.Register<IShopService>(shopModel);
            services.Register(shopModel); // презентеры магазина работают с конкретной моделью

            var offlineEarnings = new OfflineEarningsService(wallet, passiveIncome);
            services.Register(offlineEarnings);

            // Пассивный доход идёт в кошелёк
            passiveIncome.OnIncomeGenerated += wallet.Add;

            // На новой стадии пересчитываем доход и сохраняем прогресс
            stageModel.OnStageCompleted += passiveIncome.RecalculateIncome;
            stageModel.OnStageCompleted += newStage =>
            {
                YG2.saves.level = newStage;
                _saveService.Save();
            };
        }
    }
}
