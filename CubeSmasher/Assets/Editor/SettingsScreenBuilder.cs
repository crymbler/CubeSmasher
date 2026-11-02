using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CubeSmasher.Settings;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Build Settings Screen
///
/// Собирает экран настроек: заголовок, две строки громкости с ползунками
/// и три кнопки языка. Как и магазин косметики, пересоздаёт префаб с нуля,
/// поэтому при существующем префабе спрашивает подтверждение.
/// </summary>
public static class SettingsScreenBuilder
{
    private const string UIFolder = "Assets/Prefabs/UI";
    private const string Path = UIFolder + "/Settings_Screen.prefab";
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";

    [MenuItem("Tools/CubeSmasher/Build Settings Screen")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null &&
            !EditorUtility.DisplayDialog(
                "Пересобрать экран настроек?",
                "Префаб будет создан заново. Правки, сделанные в нём вручную, пропадут.\n\nПродолжить?",
                "Пересобрать", "Отмена"))
        {
            Debug.Log("[Settings] Сборка отменена.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(UIFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogWarning("[Settings] Не найден шрифт " + FontPath + ": кириллица не отрисуется.");
        }

        GameObject root = BuildScreen(font);

        PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Settings] Готово: " + Path);
        Debug.Log("[Settings] Дальше: перетащите Settings_Screen на Canvas, " +
                  "в Bootstrapper назначьте _settingsView и _openSettingsButton (BtnSettings в HUD).");
    }

    private static GameObject BuildScreen(TMP_FontAsset font)
    {
        var root = NewRect("Settings_Screen", null);
        Stretch(root);

        var background = root.gameObject.AddComponent<Image>();
        background.color = UITheme.WithAlpha(UITheme.MetalDarkest, 245);

        var view = root.gameObject.AddComponent<SettingsView>();

        // Заголовок
        TextMeshProUGUI title = NewText(root, "Title", UiText.Get("settings.title"), 64, UITheme.NeonGreen, font);
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(48f, -140f);
        titleRect.offsetMax = new Vector2(-180f, -48f);
        title.fontStyle = FontStyles.Bold;

        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = UITheme.NeonGreenDark;
        shadow.effectDistance = new Vector2(3f, -3f);

        // Закрыть
        Button close = NewButton(root, "CloseButton", "X", font);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-40f, -40f);
        closeRect.sizeDelta = new Vector2(96f, 96f);

        // Контент: строки сверху вниз
        RectTransform content = NewRect("Content", root);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 1f);
        content.offsetMin = new Vector2(64f, 64f);
        content.offsetMax = new Vector2(-64f, -170f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 40f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Музыка
        (Slider musicSlider, TextMeshProUGUI musicValue) =
            BuildVolumeRow(content, "Music", UiText.Get("settings.music"), font);

        // Звуки
        (Slider soundSlider, TextMeshProUGUI soundValue) =
            BuildVolumeRow(content, "Sound", UiText.Get("settings.sound"), font);

        // Язык
        LanguageSettingsPanel.LanguageButton[] languages = BuildLanguageRow(content, font);

        var so = new SerializedObject(view);
        SetObject(so, "_closeButton", close);
        SetObject(so, "_musicSlider", musicSlider);
        SetObject(so, "_soundSlider", soundSlider);
        SetObject(so, "_musicValue", musicValue);
        SetObject(so, "_soundValue", soundValue);
        so.ApplyModifiedPropertiesWithoutUndo();

        // Массив кнопок языка пишется отдельно: SerializedProperty для массива
        SerializedProperty langs = so.FindProperty("_languageButtons");
        if (langs != null)
        {
            langs.arraySize = languages.Length;
            for (int i = 0; i < languages.Length; i++)
            {
                SerializedProperty item = langs.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Code").stringValue = languages[i].Code;
                item.FindPropertyRelative("Button").objectReferenceValue = languages[i].Button;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[Settings] В SettingsView нет поля _languageButtons: кнопки языка не связаны.");
        }

        return root.gameObject;
    }

    /// <summary>Строка громкости: подпись, процент, ползунок.</summary>
    private static (Slider, TextMeshProUGUI) BuildVolumeRow(
        RectTransform parent, string name, string label, TMP_FontAsset font)
    {
        RectTransform row = NewRect(name + "Row", parent);
        var rowElement = row.gameObject.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 130f;
        rowElement.minHeight = 130f;

        var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childControlHeight = false;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = true;

        // Верхняя строка: название слева, процент справа
        RectTransform header = NewRect("Header", row);
        var headerElement = header.gameObject.AddComponent<LayoutElement>();
        headerElement.preferredHeight = 44f;
        headerElement.minHeight = 44f;

        TextMeshProUGUI caption = NewText(header, "Label", label, 36, UITheme.TextPrimary, font);
        var captionRect = (RectTransform)caption.transform;
        captionRect.anchorMin = new Vector2(0f, 0f);
        captionRect.anchorMax = new Vector2(0.7f, 1f);
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;
        caption.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI value = NewText(header, "Value", "100%", 36, UITheme.NeonGreen, font);
        var valueRect = (RectTransform)value.transform;
        valueRect.anchorMin = new Vector2(0.7f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        value.alignment = TextAlignmentOptions.MidlineRight;

        // Ползунок
        RectTransform sliderRect = NewRect("Slider", row);
        var sliderElement = sliderRect.gameObject.AddComponent<LayoutElement>();
        sliderElement.preferredHeight = 48f;
        sliderElement.minHeight = 48f;

        Slider slider = BuildSlider(sliderRect);

        return (slider, value);
    }

    /// <summary>Ползунок: фон, заполнение, ручка. Без них Slider нечего перетаскивать.</summary>
    private static Slider BuildSlider(RectTransform parent)
    {
        var slider = parent.gameObject.AddComponent<Slider>();

        RectTransform background = NewRect("Background", parent);
        background.anchorMin = new Vector2(0f, 0.35f);
        background.anchorMax = new Vector2(1f, 0.65f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.color = UITheme.MetalDark;

        RectTransform fillArea = NewRect("Fill Area", parent);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);

        RectTransform fill = NewRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = UITheme.NeonGreen;

        RectTransform handleArea = NewRect("Handle Slide Area", parent);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(16f, 0f);
        handleArea.offsetMax = new Vector2(-16f, 0f);

        RectTransform handle = NewRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(40f, 40f);

        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = UITheme.NeonGreenLight;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        return slider;
    }

    /// <summary>Три кнопки языка в ряд.</summary>
    private static LanguageSettingsPanel.LanguageButton[] BuildLanguageRow(
        RectTransform parent, TMP_FontAsset font)
    {
        RectTransform row = NewRect("LanguageRow", parent);
        var rowElement = row.gameObject.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 120f;
        rowElement.minHeight = 120f;

        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 20f;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;

        string[] codes = { "ru", "en", "tr" };
        string[] labels = { "Русский", "English", "Türkçe" };

        var result = new LanguageSettingsPanel.LanguageButton[codes.Length];

        for (int i = 0; i < codes.Length; i++)
        {
            Button button = NewButton(row, "Btn_" + codes[i], labels[i], font, 30);

            result[i] = new LanguageSettingsPanel.LanguageButton
            {
                Code = codes[i],
                Button = button
            };
        }

        return result;
    }

    // --- хелперы ---

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TextMeshProUGUI NewText(Transform parent, string name, string text,
                                           float size, Color color, TMP_FontAsset font)
    {
        RectTransform rect = NewRect(name, parent);

        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;

        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return label;
    }

    private static Button NewButton(Transform parent, string name, string label,
                                    TMP_FontAsset font, float fontSize = 40f)
    {
        RectTransform rect = NewRect(name, parent);

        var image = rect.gameObject.AddComponent<Image>();
        image.color = UITheme.ButtonSecondaryMain;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = NewText(rect, "Label", label, fontSize, UITheme.TextOnAccent, font);
        Stretch((RectTransform)text.transform);

        return button;
    }

    private static void SetObject(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning("[Settings] В SettingsView нет поля " + field + ": пропущено.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
