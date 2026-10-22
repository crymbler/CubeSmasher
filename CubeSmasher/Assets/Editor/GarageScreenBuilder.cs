using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Собирает экран гаража: Assets/Prefabs/UI/Garage_Screen.prefab.
/// Запуск: Tools → CubeSmasher → Build Garage Screen.
///
/// Раскладка (по фото владельца):
///   прозрачный фон, машину видно насквозь;
///   сверху по центру — плашка "Откроется на стадии N" ИЛИ кнопка "ОТКРЫТЬ";
///   в центре пусто: там 3D-витрина с машиной, её показывает GarageView через _showcase;
///   снизу по центру — иконки машин, по бокам от них — стрелки.
///
/// HUD на время гаража скрывается через _hideWhileOpen.
/// </summary>
public static class GarageScreenBuilder
{
    private const string UIPrefabFolder = "Assets/Prefabs/UI";
    private const string OutPath = UIPrefabFolder + "/Garage_Screen.prefab";
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";

    private const float Margin = 32f;
    private const float TopPlateHeight = 120f;
    private const float BottomBarHeight = 200f;
    private const float ThumbSize = 132f;
    private const float ThumbGap = 20f;
    private const float ArrowSize = 110f;

    private static TMP_FontAsset _font;

    [MenuItem("Tools/CubeSmasher/Build Garage Screen")]
    public static void BuildGarage()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
        {
            Debug.LogError("[Garage] Не найден шрифт " + FontPath + ". Кириллица не отрисуется.");
            return;
        }

        GameObject thumbPrefab = Load("Button_Icon");
        if (thumbPrefab == null)
        {
            Debug.LogError("[Garage] Нет Button_Icon. Запустите Build UI Prefabs.");
            return;
        }

        GameObject root = BuildCanvas();

        // Window — отдельный дочерний объект. Если назначить _window = root,
        // Close() выключит сам объект с GarageView, и его больше нельзя будет включить.
        GameObject window = NewUI("Window", root.transform);
        Stretch(window.GetComponent<RectTransform>());

        GameObject safe = NewUI("SafeArea", window.transform);
        Stretch(safe.GetComponent<RectTransform>());

        BuildLockPlate(safe.transform);
        BuildOpenButton(safe.transform);
        BuildInfoBlock(safe.transform);
        BuildBottom(safe.transform, thumbPrefab);
        BuildClose(safe.transform);

        Bind(root, window);

        PrefabUtility.SaveAsPrefabAsset(root, OutPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Garage] Готово: " + OutPath);
        Debug.Log("[Garage] В сцене назначить вручную: _arenaCamera, _garageCamera, _showcase, " +
                  "_hideWhileOpen (HUD_Screen/TopHud и BottomNav)");
        Debug.Log("[Garage] Кружки-уведомления (кнопки и миниатюры) создаются кодом, назначать нечего.");
    }

    // ------------------------------------------------------------------
    // Верх: плашка закрытой машины и кнопка открытия на одном месте
    // ------------------------------------------------------------------

    private static void BuildLockPlate(Transform parent)
    {
        GameObject plate = NewUI("LockPlate", parent);
        RectTransform rt = plate.GetComponent<RectTransform>();
        AnchorTopCentre(rt, TopPlateHeight);
        var img = plate.AddComponent<Image>();
        img.color = new Color(0.10f, 0.14f, 0.22f, 0.92f);
        img.raycastTarget = false;

        var tmp = Label(plate, "Text", "Откроется на стадии 10", 40f,
            CubeSmasher.UI.UITheme.MetalSheen, TextAlignmentOptions.Center);
        tmp.name = "LockText";
    }

    private static void BuildOpenButton(Transform parent)
    {
        GameObject holder = NewUI("OpenButton", parent);
        RectTransform rt = holder.GetComponent<RectTransform>();
        AnchorTopCentre(rt, TopPlateHeight);

        var img = holder.AddComponent<Image>();
        img.color = CubeSmasher.UI.UITheme.NeonGreen;

        var btn = holder.AddComponent<Button>();
        btn.targetGraphic = img;

        // Фаска как у кнопок: тёмная полоса снизу
        GameObject shade = NewUI("Shade", holder.transform);
        var shadeImg = shade.AddComponent<Image>();
        shadeImg.color = CubeSmasher.UI.UITheme.NeonGreenDark;
        shadeImg.raycastTarget = false;
        var srt = shade.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.sizeDelta = new Vector2(0f, 10f);
        srt.anchoredPosition = Vector2.zero;

        var tmp = Label(holder, "Text", "ОТКРЫТЬ", 44f,
            CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);
        tmp.name = "OpenText";

        holder.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Информация об открытой машине: название, описание, характеристики, доход
    // ------------------------------------------------------------------

    private static void BuildInfoBlock(Transform parent)
    {
        GameObject info = NewUI("Info", parent);
        RectTransform rt = info.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(Margin, BottomBarHeight + Margin);
        rt.offsetMax = new Vector2(-Margin, BottomBarHeight + Margin + 260f);
        rt.localScale = Vector3.one;

        GameObject nameBox = NewUI("Name", info.transform);
        Point(nameBox, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(700f, 56f));
        Label(nameBox, "Text", "RETRO-01", 46f,
            CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Left);

        GameObject descBox = NewUI("description", info.transform);
        Point(descBox, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -64f), new Vector2(840f, 96f));
        var desc = Label(descBox, "Text", "Описание машины", 28f,
            CubeSmasher.UI.UITheme.TextSecondary, TextAlignmentOptions.TopLeft);
        desc.enableWordWrapping = true;

        GameObject statsBox = NewUI("stats", info.transform);
        Point(statsBox, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -168f), new Vector2(840f, 56f));
        Label(statsBox, "Text", "+5% к урону", 30f,
            CubeSmasher.UI.UITheme.NeonGreen, TextAlignmentOptions.Left);

        GameObject incomeBox = NewUI("income", info.transform);
        Point(incomeBox, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -216f), new Vector2(420f, 44f));
        Label(incomeBox, "Text", "Доход: 1/сек", 32f,
            CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Left);

        info.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Низ: стрелки по бокам, иконки машин по центру
    // ------------------------------------------------------------------

    private static void BuildBottom(Transform parent, GameObject thumbPrefab)
    {
        GameObject bar = NewUI("BottomBar", parent);
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, BottomBarHeight);
        rt.localScale = Vector3.one;

        // Стрелки по краям. Размер и отступы правит GarageView под ориентацию экрана.
        BuildArrow(bar.transform, "PrevButton", "‹", new Vector2(0f, 0.5f), new Vector2(Margin, 0f));
        BuildArrow(bar.transform, "NextButton", "›", new Vector2(1f, 0.5f), new Vector2(-Margin, 0f));

        // Лента миниатюр в прокрутке: машин может быть много, сжимать их нельзя.
        // Viewport обрезает, Content растягивается под число иконок.
        GameObject viewport = NewUI("ThumbViewport", bar.transform);
        RectTransform vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0.5f);
        vrt.anchorMax = new Vector2(1f, 0.5f);
        vrt.pivot = new Vector2(0.5f, 0.5f);
        vrt.offsetMin = new Vector2(Margin + ArrowSize + 24f, -ThumbSize * 0.5f);
        vrt.offsetMax = new Vector2(-Margin - ArrowSize - 24f, ThumbSize * 0.5f);
        vrt.localScale = Vector3.one;
        viewport.AddComponent<RectMask2D>();

        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 30f;

        GameObject strip = NewUI("Thumbnails", viewport.transform);
        RectTransform srt = strip.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 0.5f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        srt.localScale = Vector3.one;

        var layout = strip.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = ThumbGap;
        layout.padding = new RectOffset(8, 8, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = strip.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = vrt;
        scroll.content = srt;

        // Шаблон иконки. GarageView клонирует его по числу машин и выключает сам шаблон
        GameObject template = PlacePrefab(thumbPrefab, strip.transform, "ThumbnailTemplate");
        var le = template.AddComponent<LayoutElement>();
        le.preferredWidth = ThumbSize;
        le.preferredHeight = ThumbSize;
        template.SetActive(false);

        var tmplBtn = template.GetComponent<Button>();
        if (tmplBtn != null) tmplBtn.onClick.RemoveAllListeners();

    }

    private static void BuildArrow(Transform parent, string name, string glyph, Vector2 anchor, Vector2 offset)
    {
        GameObject arrow = NewUI(name, parent);
        Point(arrow, anchor, anchor, offset, new Vector2(ArrowSize, ArrowSize));

        var img = arrow.AddComponent<Image>();
        img.color = CubeSmasher.UI.UITheme.MetalMid;

        var btn = arrow.AddComponent<Button>();
        btn.targetGraphic = img;

        Label(arrow, "Text", glyph, 64f,
            CubeSmasher.UI.UITheme.TextPrimary, TextAlignmentOptions.Center);
    }

    private static void BuildClose(Transform parent)
    {
        GameObject close = NewUI("CloseButton", parent);
        Point(close, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-Margin, -Margin), new Vector2(84f, 84f));

        var img = close.AddComponent<Image>();
        img.color = CubeSmasher.UI.UITheme.NeonPink;

        var btn = close.AddComponent<Button>();
        btn.targetGraphic = img;

        Label(close, "Text", "×", 48f,
            CubeSmasher.UI.UITheme.TextOnAccent, TextAlignmentOptions.Center);
    }

    // ------------------------------------------------------------------
    // Привязка полей
    // ------------------------------------------------------------------

    private static void Bind(GameObject root, GameObject window)
    {
        var view = root.AddComponent<GarageView>();

        SetRef(view, "_window", window);
        SetRef(view, "_closeButton", Child(root, "Window/SafeArea/CloseButton", typeof(Button)));
        SetRef(view, "_lockPlate", Child(root, "Window/SafeArea/LockPlate", typeof(GameObject)));
        SetRef(view, "_lockText", Child(root, "Window/SafeArea/LockPlate/LockText", typeof(TextMeshProUGUI)));
        SetRef(view, "_openButtonRoot", Child(root, "Window/SafeArea/OpenButton", typeof(GameObject)));
        SetRef(view, "_openButton", Child(root, "Window/SafeArea/OpenButton", typeof(Button)));
        SetRef(view, "_openButtonText", Child(root, "Window/SafeArea/OpenButton/OpenText", typeof(TextMeshProUGUI)));
        SetRef(view, "_infoRoot", Child(root, "Window/SafeArea/Info", typeof(GameObject)));
        SetRef(view, "_nameText", Child(root, "Window/SafeArea/Info/Name/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_descriptionText", Child(root, "Window/SafeArea/Info/description/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_statsText", Child(root, "Window/SafeArea/Info/stats/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_incomeText", Child(root, "Window/SafeArea/Info/income/Text", typeof(TextMeshProUGUI)));
        SetRef(view, "_prevButton", Child(root, "Window/SafeArea/BottomBar/PrevButton", typeof(Button)));
        SetRef(view, "_nextButton", Child(root, "Window/SafeArea/BottomBar/NextButton", typeof(Button)));
        SetRef(view, "_bottomBar", Child(root, "Window/SafeArea/BottomBar", typeof(RectTransform)));
        SetRef(view, "_thumbnailsScroll", Child(root, "Window/SafeArea/BottomBar/ThumbViewport", typeof(ScrollRect)));
        SetRef(view, "_thumbnailsContainer", Child(root, "Window/SafeArea/BottomBar/ThumbViewport/Thumbnails", typeof(Transform)));
        SetRef(view, "_thumbnailPrefab", Child(root, "Window/SafeArea/BottomBar/ThumbViewport/Thumbnails/ThumbnailTemplate", typeof(Button)));

        Debug.LogWarning("[Garage] Назначить в сцене вручную: _arenaCamera, _garageCamera, _showcase, " +
                         "_hideWhileOpen, _unlockParticles, _audioSource, _unlockSound");
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
        var root = new GameObject("Garage_Screen", typeof(RectTransform), typeof(Canvas),
                                  typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        int layer = LayerMask.NameToLayer("UI");
        go.layer = layer >= 0 ? layer : 0;
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
        rt.localScale = Vector3.one;
    }

    private static void AnchorTopCentre(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -Margin);
        rt.sizeDelta = new Vector2(560f, height);
        rt.localScale = Vector3.one;
    }

    private static void Point(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
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
        tmp.overflowMode = TextOverflowModes.Overflow;
        Stretch(go.GetComponent<RectTransform>());
        return tmp;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[Garage] У " + target.GetType().Name + " нет поля " + field);
            return;
        }
        if (value == null) Debug.LogWarning("[Garage] " + field + " осталось пустым");
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object Child(GameObject root, string path, System.Type type)
    {
        Transform tr = root.transform.Find(path);
        if (tr == null)
        {
            Debug.LogError("[Garage] Не найден путь " + path);
            return null;
        }
        return type == typeof(GameObject) ? (Object)tr.gameObject : (Object)tr.GetComponent(type);
    }
}
