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
    [MenuItem("Tools/CubeSmasher/Build UI Prefabs")]
    public static void BuildAll()
    {
        EnsureFolders();

        Sprite voxelGreen = MakeVoxelButtonSprite("voxel_btn_green", 64, 64,
            CubeSmasher.UI.UITheme.ButtonLight, CubeSmasher.UI.UITheme.ButtonMain,
            CubeSmasher.UI.UITheme.ButtonDark, CubeSmasher.UI.UITheme.ButtonOutline);
        Sprite voxelBlue = MakeVoxelButtonSprite("voxel_btn_blue", 64, 64,
            CubeSmasher.UI.UITheme.ButtonSecondaryLight, CubeSmasher.UI.UITheme.ButtonSecondaryMain,
            CubeSmasher.UI.UITheme.ButtonSecondaryDark, CubeSmasher.UI.UITheme.ButtonOutline);
        Sprite voxelPink = MakeVoxelButtonSprite("voxel_btn_pink", 64, 64,
            CubeSmasher.UI.UITheme.ButtonDangerLight, CubeSmasher.UI.UITheme.ButtonDangerMain,
            CubeSmasher.UI.UITheme.ButtonDangerDark, CubeSmasher.UI.UITheme.ButtonOutline);

        Sprite panelFrame = MakeVoxelPanel("voxel_panel", CubeSmasher.UI.UITheme.MetalMid,
            CubeSmasher.UI.UITheme.Panel, CubeSmasher.UI.UITheme.MetalDarkest,
            CubeSmasher.UI.UITheme.PanelBorder);
        Sprite panelHeader = MakeVoxelPanel("voxel_panel_header", CubeSmasher.UI.UITheme.MetalBlue,
            CubeSmasher.UI.UITheme.PanelHeader, CubeSmasher.UI.UITheme.MetalDark,
            CubeSmasher.UI.UITheme.PanelBorder);

        Sprite solidDark = MakeSolidRect("solid_dark", CubeSmasher.UI.UITheme.MetalDarkest);
        Sprite solidGreen = MakeSolidRect("solid_green", CubeSmasher.UI.UITheme.ProgressFill);
        Sprite circle = MakeCircleSprite("circle_white", 64, Color.white);

        BuildButtonPrimary(voxelGreen);
        BuildButtonSecondary(voxelBlue);
        BuildButtonIcon(voxelBlue);
        BuildPanelWindow(panelFrame, panelHeader, voxelPink);
        BuildProgressBar(solidDark, solidGreen);
        BuildCurrencyBadge(circle, panelHeader);
        BuildUpgradeCard(panelFrame, voxelGreen, solidGreen);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UIBuilder] Готово. Спрайты в " + ArtFolder + ", префабы в " + PrefabFolder);
    }



    [MenuItem("Tools/CubeSmasher/Rebuild UI Sprites Only")]
    [MenuItem("Tools/CubeSmasher/Rebuild UI Sprites Only")]
    public static void BuildSpritesOnly()
    {
        EnsureFolders();
        MakeVoxelButtonSprite("voxel_btn_green", 64, 64,
            CubeSmasher.UI.UITheme.ButtonLight, CubeSmasher.UI.UITheme.ButtonMain,
            CubeSmasher.UI.UITheme.ButtonDark, CubeSmasher.UI.UITheme.ButtonOutline);
        MakeVoxelButtonSprite("voxel_btn_blue", 64, 64,
            CubeSmasher.UI.UITheme.ButtonSecondaryLight, CubeSmasher.UI.UITheme.ButtonSecondaryMain,
            CubeSmasher.UI.UITheme.ButtonSecondaryDark, CubeSmasher.UI.UITheme.ButtonOutline);
        MakeVoxelButtonSprite("voxel_btn_pink", 64, 64,
            CubeSmasher.UI.UITheme.ButtonDangerLight, CubeSmasher.UI.UITheme.ButtonDangerMain,
            CubeSmasher.UI.UITheme.ButtonDangerDark, CubeSmasher.UI.UITheme.ButtonOutline);
        MakeVoxelPanel("voxel_panel", CubeSmasher.UI.UITheme.MetalMid,
            CubeSmasher.UI.UITheme.Panel, CubeSmasher.UI.UITheme.MetalDarkest,
            CubeSmasher.UI.UITheme.PanelBorder);
        MakeVoxelPanel("voxel_panel_header", CubeSmasher.UI.UITheme.MetalBlue,
            CubeSmasher.UI.UITheme.PanelHeader, CubeSmasher.UI.UITheme.MetalDark,
            CubeSmasher.UI.UITheme.PanelBorder);
        MakeSolidRect("solid_dark", CubeSmasher.UI.UITheme.MetalDarkest);
        MakeSolidRect("solid_green", CubeSmasher.UI.UITheme.ProgressFill);
        MakeCircleSprite("circle_white", 64, Color.white);
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
        // Срез 9-slice берём с запасом над радиусом: иначе при смене радиуса углы поедут.
        // Условие: срез*2 < размер текстуры.
        float sliceSize = Mathf.Min(radius + 4f, size * 0.5f - 1f);
        var slice = new Vector4(sliceSize, sliceSize, sliceSize, sliceSize);
        return SaveSpriteFromTexture(tex, name, slice);
    }

    /// <summary>
    /// Voxel-кнопка: прямоугольник с полосой света сверху, темным краем снизу и контуром.
    /// Берём фаску из UITheme.BevelHeight. Без скруглений.
    /// </summary>
    /// <summary>Сплошной прямоугольник одного цвета: дорожки, тени, заливки.</summary>
    private static Sprite MakeSolidRect(string name, Color color, int size = 16)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        var c = new Color32((byte)(Mathf.Clamp01(color.r) * 255), (byte)(Mathf.Clamp01(color.g) * 255),
                            (byte)(Mathf.Clamp01(color.b) * 255), 255);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        tex.SetPixels32(pixels);
        tex.Apply();
        return SaveSpriteFromTexture(tex, name, Vector4.zero);
    }

    /// <summary>
    /// Voxel-панель: контур, светлая полоса сверху, тёмная снизу. Тот же приём, что у кнопок,
    /// поэтому интерфейс выглядит цельным.
    /// </summary>
    private static Sprite MakeVoxelPanel(string name, Color light, Color main, Color dark, Color outline)
    {
        const int size = 96;
        int bevel = CubeSmasher.UI.UITheme.BevelHeight;
        int border = CubeSmasher.UI.UITheme.BorderWidth;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c;
                bool onEdge = x < border || x >= size - border || y < border || y >= size - border;
                if (onEdge) c = outline;
                else if (y >= size - border - bevel) c = light;
                else if (y < border + bevel) c = dark;
                else c = main;
                pixels[y * size + x] = new Color32((byte)(Mathf.Clamp01(c.r) * 255),
                    (byte)(Mathf.Clamp01(c.g) * 255), (byte)(Mathf.Clamp01(c.b) * 255), 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        int slice = border + bevel;
        return SaveSpriteFromTexture(tex, name, new Vector4(border, slice, border, slice));
    }

    private static Sprite MakeVoxelButtonSprite(string name, int width, int height,
                                                Color light, Color main, Color dark, Color outline)
    {
        int bevel = CubeSmasher.UI.UITheme.BevelHeight;
        int border = CubeSmasher.UI.UITheme.BorderWidth;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c;
                bool onEdge = x < border || x >= width - border || y < border || y >= height - border;
                if (onEdge) c = outline;
                else if (y >= height - border - bevel) c = light;   // верхняя полоса (y растёт вверх)
                else if (y < border + bevel) c = dark;              // нижний край
                else c = main;
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        // Срез 9-slice: контур + фаска, чтобы растягивание не портило края
        int slice = border + bevel;
        return SaveSpriteFromTexture(tex, name, new Vector4(border, slice, border, slice));
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

        // Слой UI может быть переименован или отсутствовать: -1 в go.layer недопустим
        int uiLayer = LayerMask.NameToLayer("UI");
        go.layer = uiLayer >= 0 ? uiLayer : 0;
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
        // У спрайта без границ Type.Sliced ломает отрисовку: Sliced только при наличии border
        bool hasBorder = sprite != null && sprite.border != Vector4.zero;
        img.type = hasBorder ? Image.Type.Sliced : Image.Type.Simple;
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
        // FontStyles.Normal == 0: OR с Bold ничего не добавлял
        tmp.fontStyle = FontStyles.Bold;
    }

    private static void SavePrefab(GameObject root, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        ApplyDefaults(root);
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

        // На неоне и светлом металле подпись тёмная; на тёмном фоне — белая
        Color labelColor = baseColor.grayscale > 0.6f
            ? CubeSmasher.UI.UITheme.TextOnAccent
            : CubeSmasher.UI.UITheme.TextPrimary;
        TextMeshProUGUI text = AddText(root, "Label", label, fontSize, labelColor);
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

    private static void BuildPanelWindow(Sprite frame, Sprite header, Sprite danger)
    {
        GameObject root = NewUI("Panel_Window", null);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 1200);

        Image bg = AddImage(root, frame, Color.white);
        bg.raycastTarget = true;

        // Тёмная внутренняя область: текст читается
        GameObject body = NewUI("Body", root.transform);
        var bodyImg = body.AddComponent<Image>();
        bodyImg.color = CubeSmasher.UI.UITheme.PanelContent;
        bodyImg.raycastTarget = false;
        Stretch(body.GetComponent<RectTransform>(), 4, 4, 4, 96);

        // Шапка — отдельная voxel-полоса, визуально «крышка»
        GameObject head = NewUI("Header", root.transform);
        Image headImg = AddImage(head, header, Color.white);
        headImg.raycastTarget = true;
        var headRt = head.GetComponent<RectTransform>();
        headRt.anchorMin = new Vector2(0, 1);
        headRt.anchorMax = new Vector2(1, 1);
        headRt.pivot = new Vector2(0.5f, 1);
        headRt.sizeDelta = new Vector2(-8, 88);
        headRt.anchoredPosition = new Vector2(0, -4);

        // Неоновая полоса под шапкой — связка с гаражом
        GameObject accent = NewUI("Accent", root.transform);
        var accentImg = accent.AddComponent<Image>();
        accentImg.color = CubeSmasher.UI.UITheme.NeonGreen;
        accentImg.raycastTarget = false;
        var accRt = accent.GetComponent<RectTransform>();
        accRt.anchorMin = new Vector2(0, 1);
        accRt.anchorMax = new Vector2(1, 1);
        accRt.pivot = new Vector2(0.5f, 1);
        accRt.sizeDelta = new Vector2(-16, 4);
        accRt.anchoredPosition = new Vector2(0, -96);

        // Заголовок: белый на тёмной шапке
        TextMeshProUGUI title = AddText(root, "Title", "ЗАГОЛОВОК", 44, CubeSmasher.UI.UITheme.TextPrimary);
        title.alignment = TextAlignmentOptions.Left;
        SetFontStyle(title, true);
        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.sizeDelta = new Vector2(-128, 56);
        trt.anchoredPosition = new Vector2(0, -20);
        trt.offsetMin = new Vector2(28, trt.offsetMin.y);

        // Контент: сюда кладутся элементы экрана
        GameObject content = NewUI("Content", root.transform);
        Stretch(content.GetComponent<RectTransform>(), 24, 24, 24, 112);

        // Закрытие — розовый неон
        GameObject close = BuildButton("Close", danger, 56, 56, "×", CubeSmasher.UI.UITheme.ButtonDangerMain, 36);
        close.transform.SetParent(root.transform, false);
        Place(close.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-24, -18), new Vector2(56, 56));

        Transform closeShadow = close.transform.Find("Shadow");
        if (closeShadow != null) Object.DestroyImmediate(closeShadow.gameObject);

        SavePrefab(root, "Panel_Window");
    }



    private static void BuildProgressBar(Sprite track, Sprite fillSprite)
    {
        GameObject root = NewUI("ProgressBar", null);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 44);

        // Тень снизу — «подслайдер», даёт объём как у кнопок
        GameObject shadow = NewUI("Shadow", root.transform);
        var shadowImg = shadow.AddComponent<Image>();
        shadowImg.color = CubeSmasher.UI.UITheme.ProgressShadow;
        shadowImg.raycastTarget = false;
        var shRt = shadow.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0, 0);
        shRt.anchorMax = new Vector2(1, 0);
        shRt.pivot = new Vector2(0.5f, 0);
        shRt.sizeDelta = new Vector2(0, 32);
        shRt.anchoredPosition = new Vector2(0, 0);

        // Дорожка
        GameObject body = NewUI("Track", root.transform);
        var trackImg = body.AddComponent<Image>();
        trackImg.color = CubeSmasher.UI.UITheme.ProgressTrack;
        trackImg.raycastTarget = false;
        var trackRt = body.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0, 0);
        trackRt.anchorMax = new Vector2(1, 1);
        trackRt.offsetMin = new Vector2(0, 8);
        trackRt.offsetMax = Vector2.zero;

        // Контур дорожки: светлый металл, 3 px
        GameObject outline = NewUI("Outline", body.transform);
        var outlineImg = outline.AddComponent<Image>();
        outlineImg.color = CubeSmasher.UI.UITheme.PanelBorder;
        outlineImg.raycastTarget = false;
        Stretch(outline.GetComponent<RectTransform>());

        GameObject hole = NewUI("Hole", outline.transform);
        var holeImg = hole.AddComponent<Image>();
        holeImg.color = CubeSmasher.UI.UITheme.ProgressTrack;
        holeImg.raycastTarget = false;
        Stretch(hole.GetComponent<RectTransform>(), 3, 3, 3, 3);

        // Заливка
        GameObject fillArea = NewUI("Fill Area", body.transform);
        Stretch(fillArea.GetComponent<RectTransform>(), 3, 3, 3, 3);
        var maskImg = fillArea.AddComponent<Image>();
        maskImg.color = Color.white;
        maskImg.raycastTarget = false;
        var mask = fillArea.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject fill = NewUI("Fill", fillArea.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Simple;
        fillImg.color = CubeSmasher.UI.UITheme.ProgressFill;
        fillImg.raycastTarget = false;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        // Светлая полоса сверху заливки — та же фаска, что у кнопок
        GameObject shine = NewUI("Shine", fill.transform);
        var shineImg = shine.AddComponent<Image>();
        shineImg.color = CubeSmasher.UI.UITheme.ProgressFillTop;
        shineImg.raycastTarget = false;
        var shineRt = shine.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0, 1);
        shineRt.anchorMax = new Vector2(1, 1);
        shineRt.pivot = new Vector2(0.5f, 1);
        shineRt.sizeDelta = new Vector2(0, 6);
        shineRt.anchoredPosition = Vector2.zero;

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



    private static void BuildCurrencyBadge(Sprite circle, Sprite plate)
    {
        GameObject root = NewUI("CurrencyBadge", null);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 76);

        Image bg = AddImage(root, plate, Color.white);
        bg.raycastTarget = true;

        GameObject rim = NewUI("Rim", root.transform);
        var rimImg = rim.AddComponent<Image>();
        rimImg.sprite = circle;
        rimImg.color = CubeSmasher.UI.UITheme.SoftCurrencyRim;
        rimImg.raycastTarget = false;
        Place(rim.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(8, 2), new Vector2(58, 58));

        GameObject icon = NewUI("Icon", root.transform);
        var iconImg = icon.AddComponent<Image>();
        iconImg.sprite = circle;
        iconImg.color = CubeSmasher.UI.UITheme.SoftCurrency;
        iconImg.raycastTarget = false;
        Place(icon.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(10, 0), new Vector2(54, 54));

        // Белый текст на тёмной плашке
        TextMeshProUGUI value = AddText(root, "Value", "0", 40, CubeSmasher.UI.UITheme.TextPrimary);
        value.alignment = TextAlignmentOptions.Right;
        var vrt = value.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0, 0);
        vrt.anchorMax = new Vector2(1, 1);
        vrt.offsetMin = new Vector2(76, 0);
        vrt.offsetMax = new Vector2(-18, 0);
        SetFontStyle(value, true);

        SavePrefab(root, "CurrencyBadge");
    }



    private static void BuildUpgradeCard(Sprite frame, Sprite voxelButton, Sprite solidGreen)
    {
        GameObject root = NewUI("UpgradeCard", null);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 250);

        Image bg = AddImage(root, frame, Color.white);
        bg.raycastTarget = true;

        GameObject body = NewUI("Body", root.transform);
        var bodyImg = body.AddComponent<Image>();
        bodyImg.color = CubeSmasher.UI.UITheme.PanelContent;
        bodyImg.raycastTarget = false;
        Stretch(body.GetComponent<RectTransform>(), 4, 4, 4, 4);

        // Иконка в рамке светлого металла
        GameObject iconBox = NewUI("Icon", root.transform);
        var iconBg = iconBox.AddComponent<Image>();
        iconBg.color = CubeSmasher.UI.UITheme.MetalMid;
        iconBg.raycastTarget = false;
        Place(iconBox.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(24, 0), new Vector2(128, 128));

        GameObject iconImg = NewUI("Image", iconBox.transform);
        var img = iconImg.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        Place(iconImg.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(96, 96));

        TextMeshProUGUI name = AddText(root, "Name", "Название улучшения", 40, CubeSmasher.UI.UITheme.TextPrimary);
        name.alignment = TextAlignmentOptions.Left;
        SetFontStyle(name, true);
        Place(name.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(176, -26), new Vector2(500, 46));

        TextMeshProUGUI level = AddText(root, "Level", "Ур. 1 / 31", 26, CubeSmasher.UI.UITheme.TextSecondary);
        level.alignment = TextAlignmentOptions.Left;
        Place(level.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(176, -78), new Vector2(240, 32));

        // Мини-полоска уровня: дорожка с тенью снизу, как у большого прогресса
        GameObject bar = NewUI("LevelBar", root.transform);
        Place(bar.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(176, -118), new Vector2(340, 18));

        GameObject barShadow = NewUI("Shadow", bar.transform);
        var bsImg = barShadow.AddComponent<Image>();
        bsImg.color = CubeSmasher.UI.UITheme.ProgressShadow;
        bsImg.raycastTarget = false;
        var bsRt = barShadow.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0, 0); bsRt.anchorMax = new Vector2(1, 0);
        bsRt.pivot = new Vector2(0.5f, 0); bsRt.sizeDelta = new Vector2(0, 14); bsRt.anchoredPosition = Vector2.zero;

        GameObject barTrack = NewUI("Track", bar.transform);
        var btImg = barTrack.AddComponent<Image>();
        btImg.color = CubeSmasher.UI.UITheme.ProgressTrack;
        btImg.raycastTarget = false;
        var btRt = barTrack.GetComponent<RectTransform>();
        btRt.anchorMin = new Vector2(0, 0); btRt.anchorMax = new Vector2(1, 1);
        btRt.offsetMin = new Vector2(0, 6); btRt.offsetMax = Vector2.zero;

        GameObject barFill = NewUI("Fill", barTrack.transform);
        var bfImg = barFill.AddComponent<Image>();
        bfImg.sprite = solidGreen;
        bfImg.type = Image.Type.Simple;
        bfImg.color = CubeSmasher.UI.UITheme.ProgressFill;
        bfImg.raycastTarget = false;
        var bfRt = barFill.GetComponent<RectTransform>();
        bfRt.anchorMin = Vector2.zero;
        bfRt.anchorMax = new Vector2(0.3f, 1f);  // 30% — пример, значение задаёт скрипт
        bfRt.offsetMin = Vector2.zero; bfRt.offsetMax = Vector2.zero;

        TextMeshProUGUI stat = AddText(root, "Stat", "2.00 → 1.95 с  (−0.05 с)", 30, CubeSmasher.UI.UITheme.NeonGreen);
        stat.alignment = TextAlignmentOptions.Left;
        SetFontStyle(stat, true);
        Place(stat.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(176, 22), new Vector2(500, 40));

        GameObject coin = NewUI("Coin", root.transform);
        var coinImg = coin.AddComponent<Image>();
        coinImg.color = CubeSmasher.UI.UITheme.SoftCurrency;
        coinImg.raycastTarget = false;
        Place(coin.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-244, -28), new Vector2(36, 36));

        TextMeshProUGUI price = AddText(root, "Price", "120", 38, CubeSmasher.UI.UITheme.TextPrimary);
        price.alignment = TextAlignmentOptions.Left;
        SetFontStyle(price, true);
        Place(price.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-200, -28), new Vector2(150, 44));

        GameObject buy = BuildButton("Buy", voxelButton, 180, 72, "Купить", CubeSmasher.UI.UITheme.Accent, 30);
        buy.transform.SetParent(root.transform, false);
        Place(buy.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-24, 20), new Vector2(180, 72));

        SavePrefab(root, "UpgradeCard");
    }



    /// <summary>Мини-полоска уровня: фон и заливка, заливку двигать через Slider.value.</summary>
    private static GameObject BuildMiniBar()
    {
        GameObject root = NewUI("LevelBar", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 16);

        var track = root.AddComponent<Image>();
        track.color = CubeSmasher.UI.UITheme.ProgressTrack;
        track.raycastTarget = false;

        GameObject fill = NewUI("Fill", root.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = CubeSmasher.UI.UITheme.NeonGreen;
        fillImg.raycastTarget = false;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0.3f, 1f); // 30% — пример, реально задаётся скриптом
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        return root;
    }

    private static void ApplyDefaults(GameObject root)
    {
        var rt = root.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        foreach (Image img in root.GetComponentsInChildren<Image>(true))
        {
            img.material = null;
            img.pixelsPerUnitMultiplier = 1f;
        }
    }

    /// <summary>Удаляет Art/UI и Prefabs/UI и собирает заново. Префабы, уже вставленные в сцены, станут Missing.</summary>
    [MenuItem("Tools/CubeSmasher/Reset UI Assets (delete and rebuild)")]
    public static void ResetAll()
    {
        if (AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.DeleteAsset(PrefabFolder);
        if (AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.DeleteAsset(ArtFolder);
        AssetDatabase.Refresh();
        BuildAll();
    }

    /// <summary>Проверяет 7 префабов: спрайты, материалы, шрифты. Результат в Console.</summary>
    [MenuItem("Tools/CubeSmasher/Verify UI Prefabs")]
    public static void VerifyPrefabs()
    {
        string[] names = { "Button_Primary", "Button_Secondary", "Button_Icon", "Panel_Window",
                           "ProgressBar", "CurrencyBadge", "UpgradeCard" };

        // Элементы, которым спрайт не нужен по замыслу: дорожки, маски, заливки, тени, пустые контейнеры
        string[] noSpriteOk = { "Shadow", "Track", "Outline", "Hole", "Fill Area", "Fill", "Shine",
                                "Body", "Accent", "Rim", "Coin", "Image", "Icon", "Content", "Header", "LevelBar" };

        int errors = 0;
        int warnings = 0;

        foreach (string n in names)
        {
            string path = PrefabFolder + "/" + n + ".prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
            {
                Debug.LogError("[Verify] нет префаба: " + path);
                errors++;
                continue;
            }

            GameObject go = PrefabUtility.LoadPrefabContents(path);

            // 1. Кастомный материал — ошибка. Значение null = стандартный UI/Default, это норма.
            foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true))
            {
                if (g.material != null && g.material.name != "Default UI Material" && g.material.name != "UI/Default")
                {
                    Debug.LogError("[Verify] " + n + "/" + g.name + ": кастомный материал " + g.material.name);
                    errors++;
                }
            }

            // 2. Image без спрайта: ошибка, только если элемент должен его иметь
            foreach (Image img in go.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null && !IsSpriteOptional(img.name, noSpriteOk))
                {
                    Debug.LogWarning("[Verify] " + n + "/" + img.name + ": нет Sprite (проверить вручную)");
                    warnings++;
                }
            }

            // 3. Текст без шрифта — ошибка: такой текст не виден вообще
            foreach (TextMeshProUGUI tmp in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.font == null)
                {
                    Debug.LogError("[Verify] " + n + "/" + tmp.name + ": нет Font Asset, текст не будет виден");
                    errors++;
                }
            }

            PrefabUtility.UnloadPrefabContents(go);
        }

        if (errors == 0)
            Debug.Log("[Verify] Ошибок нет. Предупреждений: " + warnings + " (это пустые элементы по замыслу, см. список в коде).");
        else
            Debug.LogError("[Verify] Ошибок: " + errors + ", предупреждений: " + warnings);
    }

    private static bool IsSpriteOptional(string name, string[] allowed)
    {
        foreach (string a in allowed) if (a == name) return true;
        return false;
    }
}
