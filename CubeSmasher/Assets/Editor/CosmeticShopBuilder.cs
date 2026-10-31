using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CubeSmasher.UI;
using CubeSmasher.UI.Skins;

/// <summary>
/// Tools > CubeSmasher > Build Cosmetic Shop
///
/// Собирает экран магазина косметики по макету: заголовок, скролл со списком
/// групп, внутри каждой группы три слота — два маленьких за монеты и один
/// крупный премиальный.
///
/// Создаёт два префаба слотов и сам экран, пишет в них ссылки.
/// Повторный запуск пересобирает экран заново: так проще, чем проверять
/// каждую ссылку, и макет ещё будет меняться.
/// </summary>
public static class CosmeticShopBuilder
{
    private const string UIFolder = "Assets/Prefabs/UI";
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";
    private const string ShopPath = UIFolder + "/Cosmetic_Shop.prefab";
    private const string CoinSlotPath = UIFolder + "/CosmeticSlot_Coin.prefab";
    private const string PremiumSlotPath = UIFolder + "/CosmeticSlot_Premium.prefab";

    [MenuItem("Tools/CubeSmasher/Build Cosmetic Shop")]
    public static void Build()
    {
        EnsureFolder();

        // Без шрифта с кириллицей русские названия превращаются в квадраты
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogWarning("[CosmeticShop] Не найден шрифт " + FontPath +
                             ". Русский текст не отрисуется.");
        }

        GameObject coinSlot = BuildSlot(CoinSlotPath, false, font);
        GameObject premiumSlot = BuildSlot(PremiumSlotPath, true, font);

        GameObject root = BuildScreen(coinSlot.GetComponent<CosmeticSlotView>(),
                                      premiumSlot.GetComponent<CosmeticSlotView>(), font);

        PrefabUtility.SaveAsPrefabAsset(root, ShopPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CosmeticShop] Готово: " + ShopPath);
        Debug.Log("[CosmeticShop] Дальше в сцене: перетащите Cosmetic_Shop на Canvas, " +
                  "в Bootstrapper назначьте _cosmeticShopView и _openSkinsButton " +
                  "(кнопка BtnSkins в HUD).");
        Debug.Log("[CosmeticShop] И назначьте CosmeticConfig в GameSettings.Cosmetics.");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(UIFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    private static GameObject BuildScreen(CosmeticSlotView coinSlot, CosmeticSlotView premiumSlot,
                                          TMP_FontAsset font)
    {
        var root = NewUI("Cosmetic_Shop", null);
        var rootRect = (RectTransform)root.transform;
        Stretch(rootRect);

        var background = root.AddComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.13f, 0.96f);

        var view = root.AddComponent<CosmeticShopView>();

        // Заголовок
        TextMeshProUGUI title = NewText(root.transform, "Title", UiText.Get("skins.title"), 64, UITheme.NeonGreen);
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(48f, -140f);
        titleRect.offsetMax = new Vector2(-180f, -48f);

        // Кнопка закрытия
        Button close = NewButton(root.transform, "CloseButton", "X");
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-40f, -40f);
        closeRect.sizeDelta = new Vector2(96f, 96f);

        // Скролл со списком групп. Никаких LayoutGroup в контенте: магазин
        // расставляет группы сам, отступами и высотами
        ScrollRect scroll = NewScroll(root.transform, "Scroll", out RectTransform content);

        // Ссылки и значения в компонент экрана. Через SetObject/SetFloat/SetBool:
        // FindProperty возвращает null, если поле переименовали или удалили,
        // и прямое обращение к .objectReferenceValue падает NullReferenceException
        var so = new SerializedObject(view);

        SetObject(so, "_font", font);
        SetObject(so, "_groupsContainer", content);
        SetObject(so, "_closeButton", close);
        SetObject(so, "_coinSlotPrefab", coinSlot);
        SetObject(so, "_premiumSlotPrefab", premiumSlot);

        SetFloat(so, "_premiumSlotHeight", 420f);
        SetFloat(so, "_titleHeight", 72f);
        SetFloat(so, "_groupSpacing", 40f);
        SetFloat(so, "_slotSpacing", 16f);
        SetFloat(so, "_contentPadding", 8f);
        SetFloat(so, "_titleFontSize", 44f);
        SetFloat(so, "_premiumShare", 0.55f);
        SetBool(so, "_premiumOnRight", true);

        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    /// <summary>Слот: фон, название, цена, состояние, кнопка на весь слот.</summary>
    private static GameObject BuildSlot(string path, bool premium, TMP_FontAsset font)
    {
        var root = NewUI(premium ? "CosmeticSlot_Premium" : "CosmeticSlot_Coin", null);
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(400f, premium ? 420f : 200f);

        var background = root.AddComponent<Image>();
        background.color = premium
            ? new Color(0.63f, 0.20f, 0.63f)
            : new Color(0.36f, 0.55f, 0.22f);

        var button = root.AddComponent<Button>();
        button.targetGraphic = background;

        var view = root.AddComponent<CosmeticSlotView>();

        // LayoutElement обязателен: без него VerticalLayoutGroup с childControlHeight
        // считает preferredHeight слота нулевым, и карточка схлопывается в пиксель
        var element = root.AddComponent<LayoutElement>();
        element.preferredHeight = premium ? 420f : 200f;
        element.minHeight = 120f;
        element.flexibleWidth = 1f;

        // Название
        TextMeshProUGUI name = NewText(root.transform, "Name", "Name", 32, Color.white);
        var nameRect = (RectTransform)name.transform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(20f, -96f);
        nameRect.offsetMax = new Vector2(-20f, -16f);
        name.alignment = TextAlignmentOptions.Top;

        // Цена
        TextMeshProUGUI price = NewText(root.transform, "Price", "0", 40, Color.white);
        var priceRect = (RectTransform)price.transform;
        priceRect.anchorMin = new Vector2(0f, 0.5f);
        priceRect.anchorMax = new Vector2(1f, 0.5f);
        priceRect.offsetMin = new Vector2(20f, -70f);
        priceRect.offsetMax = new Vector2(-20f, 20f);

        // Состояние / кнопка
        TextMeshProUGUI state = NewText(root.transform, "State", "Купить", 30, Color.white);
        var stateRect = (RectTransform)state.transform;
        stateRect.anchorMin = new Vector2(0f, 0f);
        stateRect.anchorMax = new Vector2(1f, 0f);
        stateRect.pivot = new Vector2(0.5f, 0f);
        stateRect.offsetMin = new Vector2(20f, 16f);
        stateRect.offsetMax = new Vector2(-20f, 80f);

        var so = new SerializedObject(view);
        SetObject(so, "_nameText", name);
        SetObject(so, "_priceText", price);
        SetObject(so, "_stateText", state);
        SetObject(so, "_button", button);
        SetObject(so, "_background", background);
        SetObject(so, "_premiumBadge", null);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return asset;
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI NewText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = NewUI(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        // Шрифт с кириллицей. Ищем один раз на сборку
        TMP_FontAsset font = GetFont();
        if (font != null) tmp.font = font;

        return tmp;
    }

    private static TMP_FontAsset _cachedFont;
    private static bool _fontLookedUp;

    private static TMP_FontAsset GetFont()
    {
        if (_fontLookedUp) return _cachedFont;

        _fontLookedUp = true;
        _cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (_cachedFont == null)
        {
            Debug.LogWarning("[CosmeticShop] Не найден шрифт " + FontPath + ": кириллица не отрисуется.");
        }
        return _cachedFont;
    }

    private static Button NewButton(Transform parent, string name, string label)
    {
        GameObject go = NewUI(name, parent);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.24f, 0.28f, 0.44f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = NewText(go.transform, "Label", label, 40f, Color.white);
        Stretch((RectTransform)text.transform);

        return button;
    }

    private static ScrollRect NewScroll(Transform parent, string name, out RectTransform content)
    {
        GameObject go = NewUI(name, parent);
        var rootRect = (RectTransform)go.transform;
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.offsetMin = new Vector2(48f, 48f);
        rootRect.offsetMax = new Vector2(-48f, -160f);

        var scroll = go.AddComponent<ScrollRect>();

        GameObject viewport = NewUI("Viewport", go.transform);
        var viewportRect = (RectTransform)viewport.transform;
        Stretch(viewportRect);

        // RectMask2D вместо Mask с почти прозрачной картинкой: Mask работает
        // через стенсил и требует отрисовки графики, из-за чего содержимое
        // могло не показываться вовсе. RectMask2D просто обрезает по прямоугольнику.
        viewport.AddComponent<RectMask2D>();

        GameObject contentGo = NewUI("Content", viewport.transform);
        var contentRect = (RectTransform)contentGo.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30f;

        content = contentRect;
        return scroll;
    }

    /// <summary>
    /// Записать значение в сериализованное поле по имени. FindProperty возвращает null,
    /// если поле переименовали или удалили: без проверки сборщик падал
    /// NullReferenceException на строке с boolValue.
    /// </summary>
    private static void SetFloat(SerializedObject so, string field, float value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) { WarnMissing(field); return; }
        prop.floatValue = value;
    }

    private static void SetBool(SerializedObject so, string field, bool value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) { WarnMissing(field); return; }
        prop.boolValue = value;
    }

    private static void SetObject(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) { WarnMissing(field); return; }
        prop.objectReferenceValue = value;
    }

    private static void WarnMissing(string field) =>
        Debug.LogWarning("[CosmeticShop] В CosmeticShopView нет поля " + field +
                         ": значение пропущено. Поле переименовали?");

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
