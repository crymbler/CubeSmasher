using CubeSmasher.Core.Interfaces;
using UnityEngine;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Собирает игровые системы: пул деталей, раскол, гниение осколков
    /// и сервис жизненного цикла деталей, который владеет спавном и наградой.
    /// </summary>
    public class GameplayInitializer : IGameInitializer
    {
        private readonly GameConfig _config;
        private readonly CarPart _carPartPrefab;
        private readonly Transform _poolContainer;

        public GameplayInitializer(GameConfig config, CarPart carPartPrefab, Transform poolContainer)
        {
            _config = config;
            _carPartPrefab = carPartPrefab;
            _poolContainer = poolContainer;
        }

        public void Initialize()
        {
            ServiceLocator services = ServiceLocator.Instance;

            var fractureCalculator = new FractureCalculator();
            var decaySystem = new DecaySystem();

            services.TryGet(out IWallet wallet);
            services.TryGet(out StageModel stageModel);

            var lifecycle = new PartLifecycleService(
                _config, _carPartPrefab, _poolContainer, fractureCalculator, decaySystem, wallet, stageModel);

            services.Register(lifecycle);
        }
    }
}
