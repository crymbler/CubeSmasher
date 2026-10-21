using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Собирает готовый экран меню в префаб Assets/Prefabs/UI/HUD_Screen.prefab.
/// Запуск: Tools → CubeSmasher → Build HUD Screen.
///
/// Что создаётся:
///   Canvas (Scale With Screen Size, 1080x1920, Match 0.5)
///     SafeArea
///       TopHud  — баланс, доход, стадия, полоса прогресса
///       BottomNav — четыре кнопки: гараж, магазин, скины, настройки
///       ShopWindow — окно магазина со списком карточек (скрыто)
///       GarageWindow — окно гаража (скрыто)
///
/// Все позиции задаются якорями и отступами, не абсолютными координатами,
/// поэтому экран не разъезжается на разных разрешениях.
/// </summary>
public static class HudBuilderTool
{
    private const string UIPrefabFolder = "Assets/Prefabs/UI";
    private const string OutPath = UIPrefabFolder + "/HUD_Screen.prefab";

    // Размеры посчитаны для 1080x1920
    private const float Margin = 24f;
    private const float TopBarHeight = 184f;
    private const float BottomBarHeight = 230f;
    private const float NavButtonSize = 128f;
    private const float NavGap = 24f;

    [MenuItem("Tools/CubeSmasher/Build HUD Screen")]
    public static void BuildHud()
    {
        if (!AssetDatabase.IsValidFolder(UIPrefabFolder))
        {
            Debug.LogError("[HUD] Нет папки " + UIPrefabFolder + ". Сначала Build UI Prefabs.");
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

        GameObject root = BuildCanvas();

        GameObject safe = NewUI("SafeArea", root.transform);
        Stretch(safe.GetComponent<RectTransform>());

        BuildTopHud(safe.transform, badgePrefab, progressPrefab);
        BuildBottomNav(safe.transform, iconButtonPrefab);
        GameObject shop = BuildShopWindow(safe.transform, windowPrefab, cardPrefab);
        GameObject garage = BuildGarageWindow(safe.transform, windowPrefab);

        // Окна закрыты на старте
        shop.SetActive(false);
        garage.SetActive(false);

        // Компоненты окон и назначение их полей — здесь, а не руками в инспекторе
        BindTopHud(root);
        BindShop(root, shop);
        BindGarage(root, garage);

        PrefabUtility.SaveAsPrefabAsset(root, OutPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[HUD] Готово: " + OutPath);
        Debug.Log("[HUD] Дальше: перетащите префаб в сцену CubeScene, " +
                  "удалите старые объекты TopHud и BottomHud из старого Canvas.");
    }

    // ------------------------------------------------------------------
    // Хелперы
    // ------------------------------------------------------------------

    private static GameObject Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(UIPrefabFolder + "/" + name + ".prefab");
    }

    private static GameObject BuildCanvas()
    {
        var root = new GameObject("HUD_Screen", typeof(RectTransform), typeof(Canvas),
                                  typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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
        return go;
    }

    private static Image Flat(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Text(GameObject parent, string name, string text, float size,
                                        Color color, TextAlignmentOptions align, bool bold = true)
    {
        GameObject go = NewUI(name, parent.transform);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        Stretch(go.GetComponent<RectTransform>());
        return tmp;
    }

    private static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }

    /// <summary>Пояс, прижатый к верху: растянут по ширине, высота фиксирована.</summary>
    private static void AnchorTop(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>Пояс, прижатый к низу.</summary>
    private static void AnchorBottom(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, height);
        rt.localScale = Vector3.one;
    }

    /// <summary>Элемент с фиксированным размером в заданной точке якоря.</summary>
    private static void PlaceAt(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    /// <summary>Горизонтальная растяжка внутри родителя с отступами по краям.</summary>
    private static void WideRow(RectTransform rt, float y, float height, float left, float right)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -y - height);
        rt.offsetMax = new Vector2(-right, -y);
        rt.localScale = Vector3.one;
    }

    private static GameObject PlacePrefab(GameObject prefab, Transform parent, string name)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }

    // ------------------------------------------------------------------
    // Верхняя панель
    // ------------------------------------------------------------------

    private static void BuildTopHud(Transform parent, GameObject badgePrefab, GameObject progressPrefab)
    {
        GameObject top = NewUI("TopHud", parent);
        AnchorTop(top.GetComponent<RectTransform>(), TopBarHeight);

        // --- Строка 1: баланс слева, доход справа ---
        GameObject badge = PlacePrefab(badgePrefab, top.transform, "BalanceBadge");
        PlaceAt(badge.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Margin, -Margin), new Vector2(300f, 80f));

        GameObject incomeBox = NewUI("Income", top.transform);
        PlaceAt(incomeBox.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-Margin, -Margin), new Vector2(300f, 80f));

        TextMeshProUGUI income = Text(incomeBox, "Value", "+0/сек", 36f,
            CubeSmasher.UI.UITheme.NeonGreen, TextAlignmentOptions.Right);
        income.name = "IncomeValue";

        // --- Строка 2: подпись стадии слева, полоса справа ---
        float row2 = Margin + 80f + 16f;

        GameObject stageBox = NewUI("Stage", top.transform);
        PlaceAt(stageBox.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Margin, -row2), new Vector2(300f, 44f));

        TextMeshProUGUI stage = Text(stageBox, "Label", "Машина 1", 34f,
            CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Left);
        stage.name = "StageLabel";
        stage.raycastTarget = false;

        GameObject bar = PlacePrefab(progressPrefab, top.transform, "StageProgress");
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.offsetMin = new Vector2(Margin + 316f, -row2 - 44f);
        barRt.offsetMax = new Vector2(-Margin, -row2);
        barRt.sizeDelta = new Vector2(barRt.sizeDelta.x, 44f);
    }

    // ------------------------------------------------------------------
    // Нижняя панель
    // ------------------------------------------------------------------

    private static void BuildBottomNav(Transform parent, GameObject iconButtonPrefab)
    {
        GameObject nav = NewUI("BottomNav", parent);
        AnchorBottom(nav.GetComponent<RectTransform>(), BottomBarHeight);

        // Тёмная подложка, чтобы кнопки читались на любом фоне
        GameObject plate = NewUI("Plate", nav.transform);
        AnchorBottom(plate.GetComponent<RectTransform>(), BottomBarHeight);
        Flat(plate, new Color(0.10f, 0.14f, 0.22f, 0.85f));  // raycastTarget = false, клики проходят к кнопкам

        string[] names = { "BtnGarage", "BtnShop", "BtnSkins", "BtnSettings" };
        string[] labels = { "Гараж", "Магазин", "Скины", "Настройки" };

        float totalWidth = names.Length * NavButtonSize + (names.Length - 1) * NavGap;
        float startX = -totalWidth * 0.5f + NavButtonSize * 0.5f;

        for (int i = 0; i < names.Length; i++)
        {
            GameObject btn = PlacePrefab(iconButtonPrefab, nav.transform, names[i]);
            PlaceAt(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(startX + i * (NavButtonSize + NavGap), Margin),
                new Vector2(NavButtonSize, NavButtonSize));

            // Подпись под иконкой, чтобы кнопки были понятны без картинок
            // Подпись сидит под кнопкой: pivot внизу, позиция ниже нижнего края кнопки
            GameObject caption = NewUI("Caption", nav.transform);
            PlaceAt(caption.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(startX + i * (NavButtonSize + NavGap), Margin - 4f),
                new Vector2(NavButtonSize + 20f, 28f));

            TextMeshProUGUI captionText = Text(caption, "Text", labels[i], 24f,
                CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Center);
            captionText.name = "CaptionText";
        }
    }

    // ------------------------------------------------------------------
    // Окно магазина
    // ------------------------------------------------------------------

    private static GameObject BuildShopWindow(Transform parent, GameObject windowPrefab, GameObject cardPrefab)
    {
        GameObject win = PlacePrefab(windowPrefab, parent, "ShopWindow");
        RectTransform winRt = win.GetComponent<RectTransform>();
        Stretch(winRt, Margin, Margin + BottomBarHeight, Margin, TopBarHeight);

        SetTitle(win, "МАГАЗИН УЛУЧШЕНИЙ");

        Transform content = win.transform.Find("Content");
        if (content == null)
        {
            Debug.LogError("[HUD] В Panel_Window нет дочернего объекта Content");
            return win;
        }

        GameObject scroll = NewUI("Scroll", content);
        Stretch(scroll.GetComponent<RectTransform>());

        var scrollRect = scroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;

        // Viewport обрезает содержимое. RectMask2D не требует материала и спрайта.
        GameObject viewport = NewUI("Viewport", scroll.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();

        // Список: карточки выстраиваются сверху вниз и сами задают высоту
        GameObject list = NewUI("List", viewport.transform);
        RectTransform listRt = list.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.offsetMin = Vector2.zero;
        listRt.offsetMax = Vector2.zero;

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

        // Две карточки-образца, чтобы увидеть вид. Реальные создаёт ShopPresenter.
        for (int i = 0; i < 2; i++)
        {
            GameObject card = PlacePrefab(cardPrefab, list.transform, "UpgradeCard" + (i + 1));
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 250f;
            le.flexibleWidth = 1f;
        }

        return win;
    }

    // ------------------------------------------------------------------
    // Окно гаража
    // ------------------------------------------------------------------

    private static GameObject BuildGarageWindow(Transform parent, GameObject windowPrefab)
    {
        GameObject win = PlacePrefab(windowPrefab, parent, "GarageWindow");
        RectTransform winRt = win.GetComponent<RectTransform>();
        Stretch(winRt, Margin, Margin + BottomBarHeight, Margin, TopBarHeight);

        SetTitle(win, "ГАРАЖ");

        Transform content = win.transform.Find("Content");
        if (content == null) return win;

        // Площадка, куда GarageView ставит модель машины
        GameObject showcase = NewUI("Showcase", content);
        RectTransform showRt = showcase.GetComponent<RectTransform>();
        showRt.anchorMin = new Vector2(0f, 0.35f);
        showRt.anchorMax = new Vector2(1f, 1f);
        showRt.offsetMin = Vector2.zero;
        showRt.offsetMax = Vector2.zero;

        // Тексты: название, описание, доход, статус
        string[] fields = { "NameText", "DescriptionText", "IncomeText", "StatusText" };
        string[] defaults = { "Название машины", "Описание", "Доход: —", "Открыто" };
        float[] sizes = { 44f, 30f, 32f, 32f };

        for (int i = 0; i < fields.Length; i++)
        {
            GameObject box = NewUI(fields[i], content);
            RectTransform rt = box.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, sizes[i] + 8f);
            rt.anchoredPosition = new Vector2(0f, 120f + (fields.Length - 1 - i) * (sizes[i] + 16f));

            Text(box, "Text", defaults[i], sizes[i],
                i == 0 ? CubeSmasher.UI.UITheme.TextPrimary : CubeSmasher.UI.UITheme.TextSecondary,
                TextAlignmentOptions.Left).name = "Text";
        }

        // Кнопки переключения машин
        GameObject prev = NewUI("PrevButton", content);
        PlaceAt(prev.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(96f, 96f));
        prev.AddComponent<Image>().color = CubeSmasher.UI.UITheme.MetalLight;
        prev.AddComponent<Button>().targetGraphic = prev.GetComponent<Image>();
        Text(prev, "Arrow", "‹", 56f, CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);

        GameObject next = NewUI("NextButton", content);
        PlaceAt(next.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(96f, 96f));
        next.AddComponent<Image>().color = CubeSmasher.UI.UITheme.MetalLight;
        next.AddComponent<Button>().targetGraphic = next.GetComponent<Image>();
        Text(next, "Arrow", "›", 56f, CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);

        return win;
    }

    private static void SetTitle(GameObject window, string title)
    {
        Transform header = window.transform.Find("Title");
        if (header == null) return;

        var tmp = header.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = title;
    }

    // ------------------------------------------------------------------
    // Привязка компонентов: скрипты вешаются на нужные объекты, поля назначаются по имени
    // ------------------------------------------------------------------

    private static T Child<T>(GameObject root, string path) where T : Component
    {
        Transform tr = root.transform.Find(path);
        if (tr == null)
        {
            Debug.LogError("[HUD] Не найден объект: " + path);
            return null;
        }
        return tr.GetComponent<T>() ?? tr.gameObject.AddComponent<T>();
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[HUD] Нет поля " + field + " в " + target.GetType().Name);
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BindTopHud(GameObject root)
    {
        // TopHud держит TopHudView
        Transform top = root.transform.Find("SafeArea/TopHud");
        if (top == null) { Debug.LogError("[HUD] Нет SafeArea/TopHud"); return; }

        var view = top.gameObject.AddComponent<TopHudView>();

        Transform badge = top.Find("BalanceBadge");
        TextMeshProUGUI balance = badge != null ? badge.Find("Value")?.GetComponent<TextMeshProUGUI>() : null;
        Transform income = top.Find("Income/IncomeValue");
        TextMeshProUGUI incomeText = income != null ? income.GetComponent<TextMeshProUGUI>() : null;
        Transform stage = top.Find("Stage/StageLabel");
        TextMeshProUGUI stageText = stage != null ? stage.GetComponent<TextMeshProUGUI>() : null;
        Transform bar = top.Find("StageProgress");
        Slider slider = bar != null ? bar.GetComponent<Slider>() : null;

        SetRef(view, "_balanceText", balance);
        SetRef(view, "_incomeText", incomeText);
        SetRef(view, "_progressText", stageText);
        SetRef(view, "_progressBar", slider);
    }

    private static void BindShop(GameObject root, GameObject shop)
    {
        var view = shop.AddComponent<ShopView>();

        Transform close = shop.transform.Find("Content/Close");
        if (close == null) close = shop.transform.Find("Close");
        Button closeBtn = close != null ? close.GetComponent<Button>() : null;

        Transform container = shop.transform.Find("Content/Scroll/Viewport/List");
        Transform sample = shop.transform.Find("Content/Scroll/Viewport/List/UpgradeCard1");
        UpgradeItemView prefabItem = sample != null ? sample.GetComponent<UpgradeItemView>() : null;

        SetRef(view, "_closeButton", closeBtn);
        SetRef(view, "_itemsContainer", container);
        SetRef(view, "_itemPrefab", prefabItem);
    }

    private static void BindGarage(GameObject root, GameObject garage)
    {
        var view = garage.AddComponent<GarageView>();

        Transform content = garage.transform.Find("Content");
        if (content == null) return;

        SetRef(view, "_window", garage);
        SetRef(view, "_showcase", content.Find("Showcase"));
        SetRef(view, "_nameText", content.Find("NameText/Text")?.GetComponent<TextMeshProUGUI>());
        SetRef(view, "_descriptionText", content.Find("DescriptionText/Text")?.GetComponent<TextMeshProUGUI>());
        SetRef(view, "_incomeText", content.Find("IncomeText/Text")?.GetComponent<TextMeshProUGUI>());
        SetRef(view, "_statusText", content.Find("StatusText/Text")?.GetComponent<TextMeshProUGUI>());
        SetRef(view, "_prevButton", content.Find("PrevButton")?.GetComponent<Button>());
        SetRef(view, "_nextButton", content.Find("NextButton")?.GetComponent<Button>());

        Transform close = garage.transform.Find("Close");
        SetRef(view, "_closeButton", close != null ? close.GetComponent<Button>() : null);
    }
}
