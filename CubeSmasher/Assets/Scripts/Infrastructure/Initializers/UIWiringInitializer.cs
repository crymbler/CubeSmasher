using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace CubeSmasher.Infrastructure.Initializers
{
    /// <summary>
    /// Создаёт презентеры и связывает UI с моделями: баланс, прогресс сборки,
    /// кнопки магазина и гаража, пауза и показ курсора при открытых окнах.
    /// </summary>
    public class UIWiringInitializer : IGameInitializer
    {
        private readonly TopHudView _topHudView;
        private readonly ShopView _shopView;
        private readonly GarageView _garageView;
        private readonly Button _openShopButton;
        private readonly Button _openGarageButton;
        private readonly List<UpgradeConfig> _upgradeConfigs;

        public UIWiringInitializer(TopHudView topHudView, ShopView shopView, GarageView garageView,
                                   Button openShopButton, Button openGarageButton,
                                   List<UpgradeConfig> upgradeConfigs)
        {
            _topHudView = topHudView;
            _shopView = shopView;
            _garageView = garageView;
            _openShopButton = openShopButton;
            _openGarageButton = openGarageButton;
            _upgradeConfigs = upgradeConfigs;
        }

        public void Initialize()
        {
            ServiceLocator services = ServiceLocator.Instance;

            if (!services.TryGet(out IWallet wallet)) return;
            if (!services.TryGet(out StageModel stageModel)) return;
            if (!services.TryGet(out ShopModel shopModel)) return;

            var gamePause = new GamePause();
            var cursorHider = new CursorHider();
            cursorHider.Hide();

            // HUD: баланс монет и прогресс сборки машины
            var topHudPresenter = new TopHudPresenter(wallet, _topHudView);
            services.Register(topHudPresenter);
            stageModel.OnProgressChanged += topHudPresenter.UpdateMachineProgress;

            // Гараж: окно выбора машин
            if (services.TryGet(out IGarageService garage) && _garageView != null)
            {
                var garagePresenter = new GaragePresenter(garage, _garageView);
                services.Register(garagePresenter);
            }

            // Магазин
            var shopPresenter = new ShopPresenter(shopModel, _shopView, wallet, _upgradeConfigs, stageModel);
            services.Register(shopPresenter);

            if (_openShopButton != null && _shopView != null)
            {
                _openShopButton.onClick.AddListener(_shopView.Open);
            }

            if (_openGarageButton != null && _garageView != null)
            {
                _openGarageButton.onClick.AddListener(_garageView.Open);
            }

            // На время открытых окон ставим паузу и показываем курсор
            if (_shopView != null)
            {
                _shopView.OnOpened += gamePause.Enable;
                _shopView.OnOpened += cursorHider.Show;
                _shopView.OnClosed += gamePause.Disable;
                _shopView.OnClosed += cursorHider.Hide;
            }

            if (_garageView != null)
            {
                _garageView.OnOpened += gamePause.Enable;
                _garageView.OnOpened += cursorHider.Show;
                _garageView.OnClosed += gamePause.Disable;
                _garageView.OnClosed += cursorHider.Hide;
            }
        }
    }
}
