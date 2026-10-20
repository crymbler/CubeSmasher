# DEV_PLAN — технический план

Для исполнителя. Владельцу читать не обязательно, его план в `PLAN_V2.md`.
Устройство кода — `ARCHITECTURE.md`.

Обновлён 11.10.2026. HEAD на момент записи: `62ff565`.

---

## Состояние на сейчас

Из десяти задач кода выполнена **одна**.

| Код | Задача | Статус | Файлы |
|---|---|---|---|
| K1 | Палитра + генератор префабов | **готово** | `UI/UITheme.cs`, `Assets/Editor/UIBuilderTool.cs` |
| K2 | HUD: верх, низ, окна | не начато | — |
| K3 | Окна скрывают HUD | не начато | `UI/GarageView.cs`, `UI/Shop/ShopView.cs` |
| K4 | Прокачка кубов и спавна | не начато | `Data/Configs/PhysicsConfig.cs`, `Gameplay/Systems/PartLifecycleService.cs`, `Economy/Shop/UpgradeConfig.cs`, `UI/Shop/UpgradeItemView.cs` |
| K5 | Локализация с аргументами | не начато | `Settings/Localizer.cs`, `Settings/Localization.cs`, `Economy/Shop/UpgradeConfig.cs`, `Data/Configs/GarageCarData.cs` |
| K6 | SafeArea | не начато | `UI/SafeAreaFitter.cs` (нет) |
| K7 | Звук | не начато | `Infrastructure/AudioService.cs` (нет) |
| K8 | Скины и донат-валюта | не начато | `Data/Configs/SkinConfig.cs` (нет) |
| K9 | Частицы | не начато | `UI/ProgressBarShine.cs` (нет) |
| K10 | Экран загрузки | не начато | `Core/BootScreen.cs` |

Проверено по дереву `origin/main`: `SafeAreaFitter`, `AudioService`, `SkinConfig`, `SkinService`, `SkinShopView`, `BottomNavView`, `ProgressBarShine` отсутствуют. В `PhysicsConfig` нет `RespawnDelay` и `MaxPartsPerWave`. В `UpgradeConfig` нет `StatNameKey`. В `Localizer` нет `SetArgs`. В `BootScreen` нет окраски кубов.

## Что сделано в K1

`UIBuilderTool.cs`, 902 строки. Меню:

- `Tools/CubeSmasher/Build UI Prefabs` — спрайты и 7 префабов.
- `Tools/CubeSmasher/Rebuild UI Sprites Only` — только спрайты.
- `Tools/CubeSmasher/Reset UI Assets (delete and rebuild)` — удалить папки и собрать заново.
- `Tools/CubeSmasher/Verify UI Prefabs` — проверка спрайтов, материалов, PPU.

Спрайты (рисуются кодом, внешних ассетов нет): `voxel_btn_green`, `voxel_btn_blue`, `voxel_btn_pink`, `voxel_panel`, `voxel_panel_header`, `solid_dark`, `solid_green`, `circle_white`.

Префабы: `Button_Primary`, `Button_Secondary`, `Button_Icon`, `Panel_Window`, `ProgressBar`, `CurrencyBadge`, `UpgradeCard`.

Палитра — `UITheme`, 52 константы. Тёмный металл гаража и неон.

## Следующая задача: K2 — генератор HUD

Причина: владелец не должен расставлять десятки RectTransform руками.

Файлы: `Assets/Editor/HudBuilderTool.cs` (новый), `Scripts/UI/BottomNavView.cs` (новый).

Меню `Tools/CubeSmasher/Build HUD Screen`. Генерирует `Assets/Prefabs/UI/HUD_Screen.prefab`:

1. Корневой `Canvas`: `Screen Space - Overlay`, `CanvasScaler` = `Scale With Screen Size`, `1080×1920`, `matchWidthOrHeight 0.5`, `GraphicRaycaster`, `EventSystem` не создавать (он в сцене).
2. `SafeArea` — пустой RectTransform-контейнер под `SafeAreaFitter` (K6).
3. `TopHud` — привязка `top-stretch`, высота 160:
   - `CurrencyBadge` слева (префаб) — баланс
   - `StageLabel` по центру + `ProgressBar` под ним
   - `IncomeLabel` справа
4. `BottomNav` — привязка `bottom-stretch`, высота 180:
   - 4 × `Button_Icon` (префаб): гараж, магазин, скины, настройки
   - поле `_icon` у каждой, спрайт пустой (заполнит владелец из Kenney)
5. `ShopWindow` — `Panel_Window` (префаб), скрыт (`SetActive(false)`), внутри `ScrollRect` + `VerticalLayoutGroup` + `ContentSizeFitter` под карточки `UpgradeCard`.
6. `GarageWindow` — `Panel_Window`, скрыт, внутри площадка под `Showcase` и тексты.

Требования к вёрстке:

- Все отступы через `offsetMin`/`offsetMax`, не через `anchoredPosition`. Позиция ломается при смене разрешения.
- Списки — `VerticalLayoutGroup` + `ContentSizeFitter`, чтобы карточки растягивались по ширине.
- Никаких абсолютных координат от центра экрана.

Приёмка: префаб собирается без ошибок, на 1080×1920, 1920×1080 и 1170×2532 ничего не перекрывается и не уезжает за край.

## Порядок дальше

1. **K2** генератор HUD → владелец смотрит меню.
2. **K3** `_hideWhileOpen` в `GarageView` и `ShopView`: запомнить активные объекты, выключить, вернуть при закрытии.
3. **K6** `SafeAreaFitter`: подгонка RectTransform под `Screen.safeArea` в `OnEnable` и при повороте.
4. **K4** прокачка. `PhysicsConfig`: `RespawnDelay` (float, 2f), `MaxPartsPerWave` (int, 4). `PartLifecycleService`: читать из конфига вместо `const float 2f`. `UpgradeConfig`: `StatNameKey`, `UnitKey`, `Decimals`, `ValueDecreases`. `UpgradeItemView`: строка «было → станет (дельта)».
5. **K5** локализация. `Localizer.SetArgs`, `Localization.Pick(ru,en,tr)`, поля `NameRu/NameEn/NameTr` в `UpgradeConfig`, `DescriptionRu/En/Tr` в `GarageCarData`.
6. **K10** `BootScreen`: HSV-цвет каждому кубу, размер 0.6–1.4, спавн в объёме, фон-градиент.
7. **K7** `AudioService` + `AudioInitializer`: музыка (loop) и SFX (`PlayOneShot`), громкость из `YG2.saves.musicVolume` / `soundVolume`.
8. **K9** вызовы частиц из `CarPart.TakeDamage` / `CarPart.Die`, `ProgressBarShine`.
9. **K8** `SkinConfig`, `SkinService`, `SkinShopView`, флаг `_hardCurrencyEnabled`.

## Известные ограничения

- Unity в песочнице нет. Компиляция не проверяется, только баланс скобок, наличие `using`, существование констант палитры.
- `YandexSaveService`: плагин не сообщает об успехе облачного сохранения, поэтому `hasPendingLocalSave` ставится только при исключении и не снимается автоматически.
- Interstitial: таймер живёт в плагине (`InterAdv_info.interAdvInterval`). Свой `AdsService` не нужен, это даст два расходящихся таймера.
- Лидерборд: таблица должна называться `stage` в консоли Яндекс.Игр (`EconomyInitializer.LeaderboardName`).

## Ловушки, на которые уже наступали

- `go.layer = -1` бросает исключение, если слоя `UI` нет. Проверять через `LayerMask.NameToLayer` и `>= 0`.
- Срез 9-slice должен быть больше радиуса, иначе при смене радиуса углы едут.
- `Subscriptions.Track` с лямбдой в отписке создаёт новый делегат. Делегат хранить в переменной.
- Правка `UIBuilderTool` поиском «до следующей функции» вырезала код. Заменять по подсчёту баланса скобок.
