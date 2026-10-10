# Архитектура CubeSmasher

## Слои

- **Core/Interfaces** — контракты сервисов: `ISaveService`, `IWallet`, `IShopService`, `IPassiveIncomeService`, `IGarageService`, `IPoolable`, `IDestructible`.
- **Infrastructure** — сборка приложения:
  - `ServiceLocator` — контейнер сервисов по типу интерфейса.
  - `Subscriptions` — список подписок на события, снимаются разом при уничтожении.
  - `YandexSaveService` — единственная точка работы с YG2 для сохранения.
  - `Initializers/` — четыре шага сборки в явном порядке: Economy → Gameplay → Weapon → UIWiring.
- **Economy** — `Wallet`, `PassiveIncome`, `GarageService`, `OfflineEarningsService`, `Shop/ShopModel`.
- **Gameplay** — `PartLifecycleService` (спавн, награда, раскол, респавн), `FractureSystem`, `StageModel`, `DecaySystem`.
- **UI** — View и Presenter. Презентеры зависят от интерфейсов `IShopView`, `ITopHudView`, `IGarageView`, а не от конкретных View.
- **Data/Configs** — ScriptableObject-конфиги: `GameSettings` держит `PhysicsConfig`, `EconomyConfig`, `GarageConfig`.

## Жизненный цикл

1. `Bootstrapper.Awake` создаёт `ServiceLocator`, регистрирует `ISaveService` и `Subscriptions`.
2. Инициализаторы регистрируют сервисы и связывают события через `Subscriptions`.
3. `Bootstrapper.Start` — пересчёт гаража и дохода по сохранённой стадии, первая волна деталей, автосохранение.
4. `Bootstrapper.OnDestroy` — `Subscriptions.Dispose`, затем презентеры, затем очистка контейнера.

## Ключевые правила

- Презентер не содержит правил покупки или открытия. Правила живут в моделях/сервисах.
- Все подписки на события добавляются через `Subscriptions.Track(subscribe, unsubscribe)`.
- Деталь, вернувшаяся в пул, очищает свои подписки (`CarPart.ReturnToPool`).
- Учёт деталей на столе — список живых объектов, не счётчик. Деталь, упавшая за край, заменяется.

## Тесты

EditMode-тесты в `Assets/Tests/Editor/`: `SubscriptionsTests`, `ServiceLocatorTests`, `StageModelTests`.
Запуск: Window → General → Test Runner → EditMode → Run All.
Тесты покрывают только чистые C#-классы. Сцена, префабы и `MonoBehaviour` проверяются вручную.

## Конфиги и редактор

- `GameSettings.asset` ссылается на четыре субконфига. Сцена ссылается на `GameSettings`.
- Машины гаража: `GarageConfig.GarageCars`, каждая с `ModelPrefab`.
- Новые поля конфигов добавлять через `[Tooltip]` и `[Header]`, значения по умолчанию совпадают с ассетами.
