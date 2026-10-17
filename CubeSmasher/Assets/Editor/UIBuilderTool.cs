using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Генератор интерфейса: создаёт спрайты (скругления, градиент, круг) и префабы
/// в стиле из docs/PLAN_V2.md. Запуск: Tools → CubeSmasher → Build UI Prefabs.
/// Сцену скрипт не трогает: префабы потом расставляются руками.
/// </summary>
public static class UIBuilderTool
{
    private const string ArtFolder = "Assets/Art/UI";
    private const string PrefabFolder = "Assets/Prefabs/UI";

    [MenuItem("Tools/CubeSmasher/Build UI Prefabs")]
    public static void BuildAll()
    {
        EnsureFolders();

        Sprite roundedWhite = MakeRoundedSprite("rounded_white", 64, 16, Color.white, Color.white, 0);
        Sprite roundedPanel = MakeRoundedSprite("rounded_panel", 96, 24, Color.white,
            CubeSmasher.UI.UITheme.PanelBorder, CubeSmasher.UI.UITheme.BorderWidth);
        Sprite circle = MakeCircleSprite("circle_white", 64, Color.white);
        Sprite gradient = MakeGradientSprite("gradient_bar", 256, 16,
            CubeSmasher.UI.UITheme.ProgressFillFrom, CubeSmasher.UI.UITheme.ProgressFillTo);

        BuildButtonPrimary(roundedWhite);
        BuildButtonSecondary(roundedWhite);
        BuildButtonIcon(roundedWhite);
        BuildPanelWindow(roundedPanel, roundedWhite);
        BuildProgressBar(roundedWhite, gradient);
        BuildCurrencyBadge(circle, roundedWhite);
        BuildUpgradeCard(roundedPanel, roundedWhite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UIBuilder] Готово. Спрайты в " + ArtFolder + ", префабы в " + PrefabFolder);
    }

    [MenuItem("Tools/CubeSmasher/Rebuild UI Sprites Only")]
    public static void BuildSpritesOnly()
    {
        EnsureFolders();
        MakeRoundedSprite("rounded_white", 64, 16, Color.white, Color.white, 0);
        MakeRoundedSprite("rounded_panel", 96, 24, Color.white,
            CubeSmasher.UI.UITheme.PanelBorder, CubeSmasher.UI.UITheme.BorderWidth);
        MakeCircleSprite("circle_white", 64, Color.white);
        MakeGradientSprite("gradient_bar", 256, 16,
            CubeSmasher.UI.UITheme.ProgressFillFrom, CubeSmasher.UI.UITheme.ProgressFillTo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------
    // Папки и спрайты
    // ------------------------------------------------------------------

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets/Art", "UI");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    /// <summary>Скруглённый прямоугольник. borderWidth = 0 значит без обводки.</summary>
    private static Sprite MakeRoundedSprite(string name, int size, int radius,
                                           Color fill, Color border, int borderWidth)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float innerHalf = half - borderWidth;
        float innerRadius = Mathf.Max(0f, radius - borderWidth);

        var pixels = new Color32[size * size];
        const int samples = 3;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        float px = x + (sx + 0.5f) / samples - half;
                        float py = y + (sy + 0.5f) / samples - half;
                        if (InsideRounded(px, py, half, radius)) covered++;
                    }
                }

                if (covered == 0)
                {
                    pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                float cx = x + 0.5f - half;
                float cy = y + 0.5f - half;
                Color c = (borderWidth > 0 && !InsideRounded(cx, cy, innerHalf, innerRadius)) ? border : fill;
                byte a = (byte)Mathf.RoundToInt(255f * covered / (samples * samples));
                pixels[y * size + x] = new Color32(
                    (byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255),
                    (byte)(Mathf.Clamp01(c.b) * 255), a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        // Срез 9-slice должен быть не меньше радиуса, иначе углы поедут
        var slice = new Vector4(radius, radius, radius, radius);
        return SaveSpriteFromTexture(tex, name, slice);
    }

    private static Sprite MakeCircleSprite(string name, int size, Color fill)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        float radius = half - 1f;
        var pixels = new Color32[size * size];
        const int samples = 3;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        float px = x + (sx + 0.5f) / samples - half;
                        float py = y + (sy + 0.5f) / samples - half;
                        if (px * px + py * py <= radius * radius) covered++;
                    }
                }

                byte a = (byte)Mathf.RoundToInt(255f * covered / (samples * samples));
                pixels[y * size + x] = new Color32(
                    (byte)(Mathf.Clamp01(fill.r) * 255), (byte)(Mathf.Clamp01(fill.g) * 255),
                    (byte)(Mathf.Clamp01(fill.b) * 255), a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return SaveSpriteFromTexture(tex, name, Vector4.zero);
    }

    private static Sprite MakeGradientSprite(string name, int width, int height, Color from, Color to)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = Color.Lerp(from, to, (float)x / (width - 1));
                pixels[y * width + x] = new Color32(
                    (byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255),
                    (byte)(Mathf.Clamp01(c.b) * 255), 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return SaveSpriteFromTexture(tex, name, Vector4.zero);
    }

    private static Sprite SaveSpriteFromTexture(Texture2D tex, string name, Vector4 border)
    {
        string path = ArtFolder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteBorder = border;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 0;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError("[UIBuilder] Не удалось загрузить спрайт: " + path);
        return sprite;
    }

    /// <summary>Точка внутри скруглённого квадрата со стороной 2*half.</summary>
    private static bool InsideRounded(float px, float py, float half, float radius)
    {
        float ax = Mathf.Abs(px);
        float ay = Mathf.Abs(py);
        if (ax > half || ay > half) return false;

        float cx = half - radius;
        float cy = half - radius;
        if (ax <= cx || ay <= cy) return true;

        float dx = ax - cx;
        float dy = ay - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    // ------------------------------------------------------------------
    // Хелперы для UI-объектов
    // ------------------------------------------------------------------

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
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

    private static void TopBar(RectTransform rt, float height, float topOffset = 0)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = new Vector2(0, -topOffset);
        rt.localScale = Vector3.one;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    private static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type = Image.Type.Sliced)
    {
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : type;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI AddText(GameObject parent, string name, string text, float size,
                                            Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = NewUI(name, parent.transform);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        Stretch(go.GetComponent<RectTransform>());
        return tmp;
    }

    private static void SetFontStyle(TextMeshProUGUI tmp, bool bold)
    {
        if (!bold) return;
        if (TMP_Settings.defaultFontAsset != null) tmp.fontStyle = FontStyles.Bold | FontStyles.Normal;
    }

    private static void SavePrefab(GameObject root, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[UIBuilder] префаб: " + path);
    }

    // ------------------------------------------------------------------
    // Префабы
    // ------------------------------------------------------------------

    private static void BuildButtonPrimary(Sprite rounded)
    {
        GameObject root = BuildButton("Button_Primary", rounded, 320, 96, "Купить",
            CubeSmasher.UI.UITheme.Accent, 40);
        SavePrefab(root, "Button_Primary");
    }

    private static void BuildButtonSecondary(Sprite rounded)
    {
        GameObject root = BuildButton("Button_Secondary", rounded, 320, 96, "Отмена",
            CubeSmasher.UI.UITheme.AccentSecond, 40);
        SavePrefab(root, "Button_Secondary");
    }

    private static GameObject BuildButton(string name, Sprite rounded, float width, float height,
                                           string label, Color baseColor, float fontSize)
    {
        GameObject root = NewUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        var bg = root.AddComponent<Image>();
        bg.sprite = rounded;
        bg.type = Image.Type.Sliced;
        bg.color = baseColor;

        var button = root.AddComponent<Button>();
        button.targetGraphic = bg;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var shadow = NewUI("Shadow", root.transform);
        var shadowImg = shadow.AddComponent<Image>();
        shadowImg.sprite = rounded;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.color = CubeSmasher.UI.UITheme.PanelShadow;
        shadowImg.raycastTarget = false;
        Stretch(shadow.GetComponent<RectTransform>(), 0, -4, 0, 4);
        shadow.transform.SetAsFirstSibling();

        TextMeshProUGUI text = AddText(root, "Label", label, fontSize, CubeSmasher.UI.UITheme.TextOnAccent);
        SetFontStyle(text, true);

        return root;
    }

    private static void BuildButtonIcon(Sprite rounded)
    {
        var root = BuildButton("Button_Icon", rounded, 128, 128, "", CubeSmasher.UI.UITheme.AccentLight, 28);
        var label = root.transform.Find("Label");
        if (label != null) label.gameObject.SetActive(false);

        var icon = NewUI("Icon", root.transform);
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = CubeSmasher.UI.UITheme.TextPrimary;
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        Place(icon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(72, 72));

        SavePrefab(root, "Button_Icon");
    }

    private static void BuildPanelWindow(Sprite panelSprite, Sprite rounded)
    {
        GameObject root = NewUI("Panel_Window", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 1200);

        Image bg = AddImage(root, panelSprite, CubeSmasher.UI.UITheme.Panel);
        bg.raycastTarget = true;

        TextMeshProUGUI title = AddText(root, "Title", "Заголовок", 56, CubeSmasher.UI.UITheme.TextPrimary);
        TopBar(title.GetComponent<RectTransform>(), 96, 24);
        title.alignment = TextAlignmentOptions.Left;
        var trt = title.GetComponent<RectTransform>();
        trt.offsetMin = new Vector2(32, trt.offsetMin.y);
        trt.offsetMax = new Vector2(-120, trt.offsetMax.y);
        SetFontStyle(title, true);

        GameObject content = NewUI("Content", root.transform);
        Stretch(content.GetComponent<RectTransform>(), 32, 32, 32, 120);

        GameObject close = BuildButton("Close", rounded, 64, 64, "×", CubeSmasher.UI.UITheme.Negative, 40);
        close.transform.SetParent(root.transform, false);
        Place(close.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-24, -24), new Vector2(64, 64));
        var closeShadow = close.transform.Find("Shadow");
        if (closeShadow != null) Object.DestroyImmediate(closeShadow.gameObject);

        SavePrefab(root, "Panel_Window");
    }

    private static void BuildProgressBar(Sprite rounded, Sprite gradient)
    {
        GameObject root = NewUI("ProgressBar", null);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(900, 36);

        AddImage(root, rounded, CubeSmasher.UI.UITheme.ProgressTrack);

        GameObject fillArea = NewUI("Fill Area", root.transform);
        Stretch(fillArea.GetComponent<RectTransform>());
        var mask = fillArea.AddComponent<Image>();
        mask.sprite = rounded;
        mask.type = Image.Type.Sliced;
        mask.color = Color.white;
        var maskComp = fillArea.AddComponent<Mask>();
        maskComp.showMaskGraphic = false;

        GameObject fill = NewUI("Fill", fillArea.transform);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = gradient;
        fillImg.type = Image.Type.Simple;
        fillImg.color = Color.white;
        fillImg.raycastTarget = false;

        var slider = root.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.targetGraphic = fillImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;

        SavePrefab(root, "ProgressBar");
    }

    private static void BuildCurrencyBadge(Sprite circle, Sprite rounded)
    {
        GameObject root = NewUI("CurrencyBadge", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260, 72);

        Image bg = AddImage(root, rounded, CubeSmasher.UI.UITheme.WithAlpha(CubeSmasher.UI.UITheme.Panel, 200));
        bg.raycastTarget = true;

        // Обводка создаётся первой, чтобы оказаться позади монеты и читаться как кант
        GameObject rim = NewUI("Rim", root.transform);
        var rimImg = rim.AddComponent<Image>();
        rimImg.sprite = circle;
        rimImg.color = CubeSmasher.UI.UITheme.SoftCurrencyRim;
        rimImg.raycastTarget = false;
        Place(rim.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(6, 2), new Vector2(60, 60));

        GameObject icon = NewUI("Icon", root.transform);
        var iconImg = icon.AddComponent<Image>();
        iconImg.sprite = circle;
        iconImg.color = CubeSmasher.UI.UITheme.SoftCurrency;
        iconImg.raycastTarget = false;
        Place(icon.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(8, 0), new Vector2(56, 56));

        TextMeshProUGUI value = AddText(root, "Value", "0", 40, CubeSmasher.UI.UITheme.TextPrimary);
        value.alignment = TextAlignmentOptions.Right;
        var vrt = value.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0, 0);
        vrt.anchorMax = new Vector2(1, 1);
        vrt.offsetMin = new Vector2(74, 0);
        vrt.offsetMax = new Vector2(-16, 0);
        SetFontStyle(value, true);

        SavePrefab(root, "CurrencyBadge");
    }

    private static void BuildUpgradeCard(Sprite panelSprite, Sprite rounded)
    {
        GameObject root = NewUI("UpgradeCard", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(880, 200);

        Image bg = AddImage(root, panelSprite, CubeSmasher.UI.UITheme.Panel);
        bg.raycastTarget = true;

        GameObject iconBox = NewUI("Icon", root.transform);
        var iconBg = iconBox.AddComponent<Image>();
        iconBg.sprite = rounded;
        iconBg.type = Image.Type.Sliced;
        iconBg.color = CubeSmasher.UI.UITheme.ProgressTrack;
        iconBg.raycastTarget = false;
        Place(iconBox.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(24, 0), new Vector2(120, 120));

        GameObject iconImgGo = NewUI("Image", iconBox.transform);
        var iconImg = iconImgGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        Place(iconImgGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(88, 88));

        TextMeshProUGUI name = AddText(root, "Name", "Улучшение", 38, CubeSmasher.UI.UITheme.TextPrimary);
        name.alignment = TextAlignmentOptions.Left;
        Place(name.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(168, -28), new Vector2(480, 44));
        SetFontStyle(name, true);

        TextMeshProUGUI level = AddText(root, "Level", "Ур. 1", 28, CubeSmasher.UI.UITheme.TextSecondary);
        level.alignment = TextAlignmentOptions.Left;
        Place(level.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(168, -76), new Vector2(240, 34));

        TextMeshProUGUI stat = AddText(root, "Stat", "2.00 → 1.95 с", 28, CubeSmasher.UI.UITheme.Positive);
        stat.alignment = TextAlignmentOptions.Left;
        Place(stat.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(168, -114), new Vector2(480, 34));

        GameObject buy = BuildButton("Buy", rounded, 200, 72, "Купить", CubeSmasher.UI.UITheme.Accent, 32);
        buy.transform.SetParent(root.transform, false);
        Place(buy.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-24, -18), new Vector2(200, 72));

        TextMeshProUGUI price = AddText(root, "Price", "120", 26, CubeSmasher.UI.UITheme.TextSecondary);
        price.alignment = TextAlignmentOptions.Right;
        Place(price.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-24, -28), new Vector2(200, 30));

        SavePrefab(root, "UpgradeCard");
    }
}
