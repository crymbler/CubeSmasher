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
    private const string ShopPath = UIFolder + "/Cosmetic_Shop.prefab";
    private const string CoinSlotPath = UIFolder + "/CosmeticSlot_Coin.prefab";
    private const string PremiumSlotPath = UIFolder + "/CosmeticSlot_Premium.prefab";

    [MenuItem("Tools/CubeSmasher/Build Cosmetic Shop")]
    public static void Build()
    {
        EnsureFolder();

        GameObject coinSlot = BuildSlot(CoinSlotPath, false);
        GameObject premiumSlot = BuildSlot(PremiumSlotPath, true);

        GameObject root = BuildScreen(coinSlot.GetComponent<CosmeticSlotView>(),
                                      premiumSlot.GetComponent<CosmeticSlotView>());

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

    private static GameObject BuildScreen(CosmeticSlotView coinSlot, CosmeticSlotView premiumSlot)
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

        // Скролл со списком групп
        ScrollRect scroll = NewScroll(root.transform, "Scroll", out Transform content);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Ссылки в компонент экрана
        var so = new SerializedObject(view);
        so.FindProperty("_groupsContainer").objectReferenceValue = content;
        so.FindProperty("_closeButton").objectReferenceValue = close;
        so.FindProperty("_coinSlotPrefab").objectReferenceValue = coinSlot;
        so.FindProperty("_premiumSlotPrefab").objectReferenceValue = premiumSlot;
        so.FindProperty("_groupTitlePrefab").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    /// <summary>Слот: фон, название, цена, состояние, кнопка на весь слот.</summary>
    private static GameObject BuildSlot(string path, bool premium)
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
        so.FindProperty("_nameText").objectReferenceValue = name;
        so.FindProperty("_priceText").objectReferenceValue = price;
        so.FindProperty("_stateText").objectReferenceValue = state;
        so.FindProperty("_button").objectReferenceValue = button;
        so.FindProperty("_background").objectReferenceValue = background;
        so.FindProperty("_premiumBadge").objectReferenceValue = null;
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
        return tmp;
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

    private static ScrollRect NewScroll(Transform parent, string name, out Transform content)
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
        var mask = viewport.AddComponent<Image>();
        mask.color = new Color(0f, 0f, 0f, 0.001f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

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

        content = contentGo.transform;
        return scroll;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
