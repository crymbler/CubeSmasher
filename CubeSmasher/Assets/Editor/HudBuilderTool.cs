using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Собирает экран меню в префаб Assets/Prefabs/UI/HUD_Screen.prefab.
/// Запуск: Tools → CubeSmasher → Build HUD Screen.
///
/// Структура повторяет рабочий гараж из CubeScene: гараж это НЕ полноэкранная панель,
/// а узкая полоса с текстами внизу — 3D-витрина (showcase) остаётся видимой.
///
///   Canvas (Scale With Screen Size, 1080x1920, Match 0.5)
///     SafeArea
///       TopHud      — баланс, доход, стадия, полоса прогресса
///       BottomNav   — четыре кнопки с подписями
///       ShopWindow  — полноэкранное окно магазина (скрыто)
///       GarageStrip — нижняя полоса гаража (скрыта)
/// </summary>
public static class HudBuilderTool
{
    private const string UIPrefabFolder = "Assets/Prefabs/UI";
    private const string OutPath = UIPrefabFolder + "/HUD_Screen.prefab";

    // Шрифт с кириллицей. LiberationSans её не содержит: Ж превращается в квадрат.
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";

    private const float Margin = 24f;
    private const float TopBarHeight = 184f;
    private const float BottomBarHeight = 230f;
    private const float NavCell = 120f;   // размер ячейки кнопки в reference-пикселях 1080x1920
    private const float NavGap = 24f;     // расстояние между кнопками
    private const int NavCount = 4;
    private const float GarageStripHeight = 380f;

    private static TMP_FontAsset _font;

    [MenuItem("Tools/CubeSmasher/Build HUD Screen")]
    public static void BuildHud()
    {
        if (!AssetDatabase.IsValidFolder(UIPrefabFolder))
        {
            Debug.LogError("[HUD] Нет папки " + UIPrefabFolder + ". Сначала Build UI Prefabs.");
            return;
        }

        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
        {
            Debug.LogError("[HUD] Не найден шрифт " + FontPath +
                           ". Текст с кириллицей не отрисуется. Проверьте путь.");
            return;
        }

        GameObject windowPrefab = Load("Panel_Window");
        GameObject cardPrefab = Load("UpgradeCard");
        GameObject badgePrefab = Load("CurrencyBadge");
        GameObject iconButtonPrefab = Load("Button_Icon");
        GameObject progressPrefab = Load("ProgressBar");

        if (windowPrefab == null || cardPrefab == null || badgePrefab == null
            || iconButtonPrefab == null || progressPrefab == null)
        {
            Debug.LogError("[HUD] Не хватает префабов в " + UIPrefabFolder + ". Запустите Build UI Prefabs.");
            return;
        }

        if (cardPrefab.GetComponent<UpgradeItemView>() == null)
        {
            Debug.LogError("[HUD] На префабе UpgradeCard нет UpgradeItemView. Пересоберите UI: " +
                           "Tools → CubeSmasher → Reset UI Assets.");
            return;
        }

        GameObject root = BuildCanvas();

        GameObject safe = NewUI("SafeArea", root.transform);
        Stretch(safe.GetComponent<RectTransform>());

        BuildTopHud(safe.transform, badgePrefab, progressPrefab);
        BuildBottomNav(safe.transform, iconButtonPrefab);
        GameObject shop = BuildShopWindow(safe.transform, windowPrefab, cardPrefab);
        GameObject garage = BuildGarageStrip(safe.transform, badgePrefab);

        shop.SetActive(false);
        garage.SetActive(false);

        BindTopHud(root);
        BindShop(shop, cardPrefab);
        BindGarage(garage);

        PrefabUtility.SaveAsPrefabAsset(root, OutPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[HUD] Готово: " + OutPath);
        Debug.Log("[HUD] ЧТО СДЕЛАТЬ В СЦЕНЕ после перетаскивания префаба:");
        Debug.Log("[HUD] 1. Bootstrapper: _topHudView → TopHud, _shopView → ShopWindow, " +
                  "_garageView → GarageStrip");
        Debug.Log("[HUD] 2. Bootstrapper: _openShopButton и _openGarageButton → кнопки в BottomNav");
        Debug.Log("[HUD] 2б. Bootstrapper: _garageDot → BottomNav/Row/BtnGarage/DotGarage, " +
                  "_shopDot → BottomNav/Row/BtnShop/DotShop");
        Debug.Log("[HUD] 3. GarageStrip: _garageCamera → GarageCamera, _showcase → showcase " +
                  "(эти два объекта живут в сцене, префаб на них ссылаться не может)");
    }

    // ------------------------------------------------------------------
    // Каркас
    // ------------------------------------------------------------------

    private static GameObject Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(UIPrefabFolder + "/" + name + ".prefab");
    }

    private static GameObject BuildCanvas()
    {
        var root = new GameObject("HUD_Screen", typeof(RectTransform), typeof(Canvas),
                                  typeof(CanvasScaler), typeof(GraphicRaycaster));

        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return root;
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        int uiLayer = LayerMask.NameToLayer("UI");
        go.layer = uiLayer >= 0 ? uiLayer : 0;
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }

    /// <summary>Полоса на всю ширину: растянута по X, высота фиксирована, прижата к верху или низу.</summary>
    private static void Band(RectTransform rt, bool top, float height)
    {
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.offsetMin = new Vector2(0f, top ? -height : 0f);
        rt.offsetMax = new Vector2(0f, top ? 0f : height);
        rt.localScale = Vector3.one;
    }

    /// <summary>Точка в углу родителя: anchor задаёт угол, pivot — куда растёт элемент.</summary>
    private static RectTransform Point(GameObject go, Vector2 anchor, Vector2 pivot,
                                       Vector2 offset, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static GameObject PlacePrefab(GameObject prefab, Transform parent, string name)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static TextMeshProUGUI Label(GameObject parent, string name, string text, float size,
                                         Color color, TextAlignmentOptions align)
    {
        GameObject go = NewUI(name, parent.transform);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;
        tmp.font = _font;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        Stretch(go.GetComponent<RectTransform>());
        return tmp;
    }

    // ------------------------------------------------------------------
    // Верхняя панель
    // ------------------------------------------------------------------

    private static void BuildTopHud(Transform parent, GameObject badgePrefab, GameObject progressPrefab)
    {
        GameObject top = NewUI("TopHud", parent);
        Band(top.GetComponent<RectTransform>(), top: true, TopBarHeight);

        // Баланс слева
        GameObject badge = PlacePrefab(badgePrefab, top.transform, "BalanceBadge");
        Point(badge, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Margin, -Margin), new Vector2(300f, 80f));

        // Доход справа
        GameObject incomeBox = NewUI("Income", top.transform);
        Point(incomeBox, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-Margin, -Margin), new Vector2(320f, 80f));
        Label(incomeBox, "IncomeValue", "+0/сек", 36f,
            CubeSmasher.UI.UITheme.NeonGreen, TextAlignmentOptions.Right);

        // Вторая строка: подпись стадии слева, полоса справа
        const float row2 = Margin + 80f + 16f;

        GameObject stageBox = NewUI("Stage", top.transform);
        Point(stageBox, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Margin, -row2), new Vector2(460f, 44f));
        Label(stageBox, "StageLabel", "Машина 1", 34f,
            CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Left);

        GameObject bar = PlacePrefab(progressPrefab, top.transform, "StageProgress");
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.offsetMin = new Vector2(Margin + 484f, -row2 - 44f);
        barRt.offsetMax = new Vector2(-Margin, -row2);
        barRt.localScale = Vector3.one;

        Slider slider = bar.GetComponent<Slider>();
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
        }
    }

    // ------------------------------------------------------------------
    // Нижняя панель
    // ------------------------------------------------------------------

    private static void BuildBottomNav(Transform parent, GameObject iconButtonPrefab)
    {
        GameObject nav = NewUI("BottomNav", parent);
        Band(nav.GetComponent<RectTransform>(), top: false, BottomBarHeight);

        GameObject plate = NewUI("Plate", nav.transform);
        Band(plate.GetComponent<RectTransform>(), top: false, BottomBarHeight);
        var plateImg = plate.AddComponent<Image>();
        plateImg.color = new Color(0.10f, 0.14f, 0.22f, 0.82f);
        plateImg.raycastTarget = false;

        // Ряд кнопок: центрированный блок. Сетка даёт каждой кнопке одинаковую ячейку,
        // ContentSizeFitter подгоняет ширину ряда под кнопки, поэтому они не растягиваются
        // на всю панель. Одинаково на ПК и на телефоне: ряд всегда по центру.
        GameObject row = NewUI("Row", nav.transform);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0f);
        rowRt.anchorMax = new Vector2(0.5f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, 24f);

        var grid = row.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(NavCell, NavCell);
        grid.spacing = new Vector2(NavGap, 0f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = NavCount;

        var fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        string[] names = { "BtnGarage", "BtnShop", "BtnSkins", "BtnSettings" };
        string[] labels = { "Гараж", "Магазин", "Скины", "Настройки" };

        for (int i = 0; i < NavCount; i++)
        {
            GameObject btn = PlacePrefab(iconButtonPrefab, row.transform, names[i]);

            // Подпись под кнопкой: pivot сверху, позиция чуть ниже нижнего края кнопки
            GameObject caption = NewUI("Caption", btn.transform);
            Point(caption, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(NavCell + 40f, 30f));
            Label(caption, "CaptionText", labels[i], 24f,
                CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Center);

            // Кружок-уведомление в правом верхнем углу кнопки.
            // Гараж горит, когда есть машина к открытию; магазин — когда есть доступная покупка.
            if (names[i] == "BtnGarage" || names[i] == "BtnShop")
            {
                BuildNotificationDot(btn.transform, names[i] == "BtnGarage" ? "DotGarage" : "DotShop");
            }
        }
    }

    /// <summary>Маленький красный кружок в углу кнопки. Выключен: включает его NotificationDots.</summary>
    private static GameObject BuildNotificationDot(Transform parent, string name)
    {
        GameObject dot = NewUI(name, parent);
        Point(dot, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(-6f, -6f), new Vector2(34f, 34f));

        var img = dot.AddComponent<Image>();
        img.color = CubeSmasher.UI.UITheme.Notification;
        img.raycastTarget = false; // клик должен проходить в кнопку под кружком

        // Тёмная окантовка, чтобы кружок читался на любом фоне
        var outline = dot.AddComponent<Outline>();
        outline.effectColor = CubeSmasher.UI.UITheme.NotificationRim;
        outline.effectDistance = new Vector2(3f, -3f);

        dot.SetActive(false);
        return dot;
    }

    // ------------------------------------------------------------------
    // Окно магазина: полноэкранное, со списком карточек
    // ------------------------------------------------------------------

    private static GameObject BuildShopWindow(Transform parent, GameObject windowPrefab, GameObject cardPrefab)
    {
        GameObject win = PlacePrefab(windowPrefab, parent, "ShopWindow");
        RectTransform rt = win.GetComponent<RectTransform>();
        Stretch(rt, Margin, Margin + BottomBarHeight, Margin, TopBarHeight);

        SetTitle(win, "МАГАЗИН");

        Transform content = win.transform.Find("Content");
        if (content == null)
        {
            Debug.LogError("[HUD] В Panel_Window нет объекта Content");
            return win;
        }

        // Прокрутка
        GameObject scroll = NewUI("Scroll", content);
        Stretch(scroll.GetComponent<RectTransform>());

        var scrollRect = scroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;

        // Viewport обрезает список. RectMask2D не требует спрайта и материала.
        GameObject viewport = NewUI("Viewport", scroll.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();

        // Список: карточки идут сверху вниз, высота считается сама
        GameObject list = NewUI("List", viewport.transform);
        RectTransform listRt = list.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.offsetMin = Vector2.zero;
        listRt.offsetMax = Vector2.zero;
        listRt.localScale = Vector3.one;

        var layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = list.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = listRt;

        // Одна карточка-образец для наглядности. Реальные создаёт ShopPresenter в _itemsContainer.
        GameObject sample = PlacePrefab(cardPrefab, list.transform, "UpgradeCard_Sample");
        var le = sample.AddComponent<LayoutElement>();
        le.preferredHeight = 250f;
        le.flexibleWidth = 1f;

        return win;
    }

    // ------------------------------------------------------------------
    // Гараж: узкая полоса внизу. НЕ перекрывает 3D-витрину с машиной.
    // ------------------------------------------------------------------

    private static GameObject BuildGarageStrip(Transform parent, GameObject badgePrefab)
    {
        GameObject strip = NewUI("GarageStrip", parent);
        RectTransform stripRt = strip.GetComponent<RectTransform>();
        // Прижата к низу, над нижней панелью с кнопками
        stripRt.anchorMin = new Vector2(0f, 0f);
        stripRt.anchorMax = new Vector2(1f, 0f);
        stripRt.pivot = new Vector2(0.5f, 0f);
        stripRt.offsetMin = new Vector2(Margin, BottomBarHeight);
        stripRt.offsetMax = new Vector2(-Margin, BottomBarHeight + GarageStripHeight);
        stripRt.localScale = Vector3.one;

        // Тёмная подложка, чтобы текст читался на любом фоне
        var bg = strip.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.14f, 0.22f, 0.88f);
        bg.raycastTarget = true;

        // Кнопка закрытия справа сверху
        GameObject close = NewUI("CloseButton", strip.transform);
        Point(close, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -16f), new Vector2(64f, 64f));
        var closeImg = close.AddComponent<Image>();
        closeImg.color = CubeSmasher.UI.UITheme.NeonPink;
        var closeBtn = close.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        Label(close, "Text", "×", 40f, CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);

        // Название машины
        GameObject name = NewUI("Name", strip.transform);
        Point(name, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -20f), new Vector2(700f, 56f));
        Label(name, "Text", "Название машины", 42f,
            CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Left);

        // Описание
        GameObject desc = NewUI("description", strip.transform);
        Point(desc, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -84f), new Vector2(880f, 92f));
        Label(desc, "Text", "Описание машины", 28f,
            CubeSmasher.UI.UITheme.TextSecondary, TextAlignmentOptions.TopLeft);

        // Доход
        GameObject income = NewUI("income", strip.transform);
        Point(income, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 84f), new Vector2(420f, 44f));
        Label(income, "Text", "Доход: —", 32f,
            CubeSmasher.UI.UITheme.NeonGreen, TextAlignmentOptions.Left);

        // Статус: открыта машина или нет
        GameObject status = NewUI("StageText", strip.transform);
        Point(status, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 32f), new Vector2(420f, 44f));
        Label(status, "Text", "Открыто", 30f,
            CubeSmasher.UI.UITheme.TextSecondary, TextAlignmentOptions.Left);

        // Стрелки переключения машин
        GameObject prev = NewUI("_prevButton", strip.transform);
        Point(prev, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-176f, 24f), new Vector2(88f, 88f));
        var prevImg = prev.AddComponent<Image>();
        prevImg.color = CubeSmasher.UI.UITheme.MetalLight;
        prev.AddComponent<Button>().targetGraphic = prevImg;
        Label(prev, "Text", "‹", 52f, CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);

        GameObject next = NewUI("_nextButton", strip.transform);
        Point(next, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-72f, 24f), new Vector2(88f, 88f));
        var nextImg = next.AddComponent<Image>();
        nextImg.color = CubeSmasher.UI.UITheme.MetalLight;
        next.AddComponent<Button>().targetGraphic = nextImg;
        Label(next, "Text", "›", 52f, CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);

        return strip;
    }

    private static void SetTitle(GameObject window, string title)
    {
        Transform tr = window.transform.Find("Title");
        if (tr == null) return;
        var tmp = tr.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = title;
            tmp.font = _font;
        }
    }

    // ------------------------------------------------------------------
    // Привязка: компоненты вешаются здесь, поля назначаются по имени
    // ------------------------------------------------------------------

    private static void SetRef(Object target, string field, Object value)
    {
        if (target == null) return;

        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[HUD] У " + target.GetType().Name + " нет поля " + field + ". Проверьте имя в скрипте.");
            return;
        }

        if (value == null)
            Debug.LogWarning("[HUD] " + target.GetType().Name + "." + field + " осталось пустым: объект не найден.");

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Component Child(GameObject root, string path, System.Type type)
    {
        Transform tr = root.transform.Find(path);
        if (tr == null)
        {
            Debug.LogError("[HUD] Не найден путь " + path + " в " + root.name);
            return null;
        }
        return tr.GetComponent(type);
    }

    private static void BindTopHud(GameObject root)
    {
        Transform top = root.transform.Find("SafeArea/TopHud");
        if (top == null) { Debug.LogError("[HUD] Нет SafeArea/TopHud"); return; }

        var view = top.gameObject.AddComponent<TopHudView>();

        SetRef(view, "_balanceText", Child(top.gameObject, "BalanceBadge/Value", typeof(TextMeshProUGUI)));
        SetRef(view, "_incomeText",  Child(top.gameObject, "Income/IncomeValue", typeof(TextMeshProUGUI)));
        SetRef(view, "_progressText", Child(top.gameObject, "Stage/StageLabel", typeof(TextMeshProUGUI)));
        SetRef(view, "_progressBar", Child(top.gameObject, "StageProgress", typeof(Slider)));
    }

    private static void BindShop(GameObject shop, GameObject cardPrefab)
    {
        var view = shop.AddComponent<ShopView>();

        SetRef(view, "_closeButton", Child(shop, "Close", typeof(Button)));
        SetRef(view, "_itemsContainer", Child(shop, "Content/Scroll/Viewport/List", typeof(Transform)));
        SetRef(view, "_itemPrefab", cardPrefab.GetComponent<UpgradeItemView>());
    }

    private static void BindGarage(GameObject strip)
    {
        var view = strip.AddComponent<GarageView>();

        // _window — сам объект-полоса: Open() включает его, Close() выключает
        SetRef(view, "_window", strip);
        SetRef(view, "_closeButton", Child(strip, "CloseButton", typeof(Button)));
        SetRef(view, "_nameText", Child(strip, "Name/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_descriptionText", Child(strip, "description/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_incomeText", Child(strip, "income/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_statusText", Child(strip, "StageText/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_prevButton", Child(strip, "_prevButton", typeof(Button)));
        SetRef(view, "_nextButton", Child(strip, "_nextButton", typeof(Button)));

        // Эти три объекта живут в сцене, а не в префабе: назначить вручную после перетаскивания
        Debug.LogWarning("[HUD] GarageStrip: вручную назначить _arenaCamera, _garageCamera, _showcase " +
                         "из сцены CubeScene.");
    }
}
