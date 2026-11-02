using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CubeSmasher.Settings;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Build Settings Screen
///
/// Раскладка задана числами и проверена арифметически, чтобы строки не могли
/// наложиться. Схема по вертикали от верха контента:
///
///   Music   0 .. -160     шапка -8..-60, ползунок -80..-144
///   Sound  -190 .. -350   шапка -198..-250, ползунок -270..-334
///   Язык   -390 .. -440   подпись строки
///   Кнопки -455 .. -595   три кнопки с местом под флаг
///
/// Оба ползунка строятся одной функцией и получают одинаковые размеры
/// по построению: ширину родителя и высоту 64.
///
/// Ручка ползунка настраивается под то, что делает сам Slider: он переписывает
/// anchorMin/anchorMax ручки в (x,0)-(x,1), то есть растягивает её по высоте.
/// Поэтому ширина задаётся через sizeDelta.x, а высота — нулём в sizeDelta.y.
/// Раньше высота шла через sizeDelta.y, Slider её игнорировал, и ручки
/// выглядели разного размера в соседних строках.
/// </summary>
public static class SettingsScreenBuilder
{
    private const string UIFolder = "Assets/Prefabs/UI";
    private const string Path = UIFolder + "/Settings_Screen.prefab";
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";

    // Вертикальная сетка
    private const float ContentTop = -180f;
    private const float VolumeRowHeight = 160f;
    private const float VolumeRowStep = 190f;
    private const float HeaderHeight = 52f;
    private const float HeaderInset = 8f;
    private const float SliderHeight = 64f;
    private const float SliderInset = 16f;

    private const float LanguageCaptionTop = -390f;
    private const float LanguageCaptionHeight = 50f;
    private const float LanguageRowTop = -455f;
    private const float LanguageRowHeight = 140f;

    // Горизонталь
    private const float ContentSideMargin = 192f;
    private const float ButtonGap = 20f;
    private const float FlagSize = 72f;

    [MenuItem("Tools/CubeSmasher/Build Settings Screen")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null &&
            !EditorUtility.DisplayDialog(
                "Пересобрать экран настроек?",
                "Префаб будет создан заново. Правки, сделанные в нём вручную " +
                "(включая картинки флагов), пропадут.\n\nПродолжить?",
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
        Debug.Log("[Settings] Флаги: в префабе у Btn_ru / Btn_en / Btn_tr есть дочерний " +
                  "объект Flag. Перетащите картинку в его поле Source Image.");
        Debug.Log("[Settings] Дальше: Settings_Screen на Canvas, в Bootstrapper назначить " +
                  "_settingsView и _openSettingsButton (BtnSettings в HUD).");
    }

    private static GameObject BuildScreen(TMP_FontAsset font)
    {
        RectTransform root = NewRect("Settings_Screen", null);
        Stretch(root);

        var background = root.gameObject.AddComponent<Image>();
        background.color = UITheme.WithAlpha(UITheme.MetalDarkest, 245);

        var view = root.gameObject.AddComponent<SettingsView>();

        // --- Заголовок ---
        TextMeshProUGUI title = NewText(root, "Title", "settings.title", 64,
                                        UITheme.NeonGreen, font);
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -48f);
        titleRect.sizeDelta = new Vector2(-360f, 92f);
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        AddLabelKey(title, "settings.title");

        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = UITheme.NeonGreenDark;
        titleShadow.effectDistance = new Vector2(3f, -3f);

        // --- Закрыть ---
        Button close = NewButton(root, "CloseButton", "X", font, 40f);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-40f, -40f);
        closeRect.sizeDelta = new Vector2(96f, 96f);

        // --- Контент ---
        RectTransform content = NewRect("Content", root);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, ContentTop);
        content.sizeDelta = new Vector2(-ContentSideMargin, 0f);

        // Музыка
        (Slider musicSlider, TextMeshProUGUI musicValue) =
            BuildVolumeRow(content, "Music", "settings.music", 0f, font);

        // Звуки
        (Slider soundSlider, TextMeshProUGUI soundValue) =
            BuildVolumeRow(content, "Sound", "settings.sound", -VolumeRowStep, font);

        // Подпись над кнопками языка
        TextMeshProUGUI languageCaption = NewText(content, "LanguageCaption",
                                                  "settings.language", 32,
                                                  UITheme.TextSecondary, font);
        var captionRect = (RectTransform)languageCaption.transform;
        captionRect.anchorMin = new Vector2(0f, 1f);
        captionRect.anchorMax = new Vector2(1f, 1f);
        captionRect.pivot = new Vector2(0.5f, 1f);
        captionRect.anchoredPosition = new Vector2(0f, LanguageCaptionTop);
        captionRect.sizeDelta = new Vector2(0f, LanguageCaptionHeight);
        languageCaption.alignment = TextAlignmentOptions.MidlineLeft;
        AddLabelKey(languageCaption, "settings.language");

        // Кнопки языка
        LanguageSettingsPanel.LanguageButton[] languages =
            BuildLanguageRow(content, font);

        var so = new SerializedObject(view);
        SetObject(so, "_closeButton", close);
        SetObject(so, "_musicSlider", musicSlider);
        SetObject(so, "_soundSlider", soundSlider);
        SetObject(so, "_musicValue", musicValue);
        SetObject(so, "_soundValue", soundValue);
        SetLanguageButtons(so, languages);
        so.ApplyModifiedPropertiesWithoutUndo();

        return root.gameObject;
    }

    /// <summary>
    /// Строка громкости: шапка сверху (подпись и процент), ползунок снизу.
    /// Обе строки строятся этим методом, поэтому совпадают по размерам.
    /// </summary>
    private static (Slider, TextMeshProUGUI) BuildVolumeRow(
        RectTransform parent, string name, string key, float topOffset, TMP_FontAsset font)
    {
        RectTransform row = NewRect(name + "Row", parent);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, topOffset);
        row.sizeDelta = new Vector2(0f, VolumeRowHeight);

        // Шапка: подпись слева, процент справа
        RectTransform header = NewRect("Header", row);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -HeaderInset);
        header.sizeDelta = new Vector2(0f, HeaderHeight);

        TextMeshProUGUI caption = NewText(header, "Label", key, 36, UITheme.TextPrimary, font);
        var captionRect = (RectTransform)caption.transform;
        captionRect.anchorMin = new Vector2(0f, 0f);
        captionRect.anchorMax = new Vector2(0.7f, 1f);
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        AddLabelKey(caption, key);

        TextMeshProUGUI value = NewText(header, "Value", "50%", 36, UITheme.NeonGreen, font);
        var valueRect = (RectTransform)value.transform;
        valueRect.anchorMin = new Vector2(0.7f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        value.alignment = TextAlignmentOptions.MidlineRight;

        // Ползунок прижат к низу строки, высота фиксированная
        RectTransform sliderRect = NewRect("Slider", row);
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, SliderInset);
        sliderRect.sizeDelta = new Vector2(0f, SliderHeight);

        Slider slider = BuildSlider(sliderRect);

        return (slider, value);
    }

    /// <summary>
    /// Ползунок: дорожка, заполнение, ручка.
    ///
    /// Ручка растягивается по высоте: Slider сам ставит ей anchorMin (x,0)
    /// и anchorMax (x,1). Значит высоту задаёт родитель, а ширина — sizeDelta.x.
    /// </summary>
    private static Slider BuildSlider(RectTransform parent)
    {
        var slider = parent.gameObject.AddComponent<Slider>();

        // Дорожка
        RectTransform track = NewRect("Background", parent);
        track.anchorMin = new Vector2(0f, 0.5f);
        track.anchorMax = new Vector2(1f, 0.5f);
        track.sizeDelta = new Vector2(0f, 20f);

        var trackImage = track.gameObject.AddComponent<Image>();
        trackImage.color = UITheme.MetalMid;
        trackImage.raycastTarget = false;

        // Зона заполнения: отступы по краям равны половине ручки,
        // иначе заполнение заезжает под ручку при крайних значениях
        const float handleWidth = 44f;

        RectTransform fillArea = NewRect("Fill Area", parent);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-handleWidth, 20f);

        RectTransform fill = NewRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = UITheme.NeonGreen;
        fillImage.raycastTarget = false;

        // Зона ручки
        RectTransform handleArea = NewRect("Handle Slide Area", parent);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(handleWidth * 0.5f, 8f);
        handleArea.offsetMax = new Vector2(-handleWidth * 0.5f, -8f);

        RectTransform handle = NewRect("Handle", handleArea);
        // Slider перепишет anchorMin/anchorMax по X, а Y растянет от 0 до 1.
        // Поэтому высота в sizeDelta = 0, ширина = handleWidth.
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(handleWidth, 0f);

        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = UITheme.NeonGreenLight;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        return slider;
    }

    /// <summary>
    /// Три кнопки языка: светлая подложка, слева место под флаг, справа название.
    /// Флаг выключен, пока не назначена картинка — пустой квадрат не висит.
    /// </summary>
    private static LanguageSettingsPanel.LanguageButton[] BuildLanguageRow(
        RectTransform parent, TMP_FontAsset font)
    {
        RectTransform row = NewRect("LanguageRow", parent);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, LanguageRowTop);
        row.sizeDelta = new Vector2(0f, LanguageRowHeight);

        string[] codes = { "ru", "en", "tr" };
        string[] labels = { "Русский", "English", "Türkçe" };

        var result = new LanguageSettingsPanel.LanguageButton[codes.Length];

        for (int i = 0; i < codes.Length; i++)
        {
            float from = i / (float)codes.Length;
            float to = (i + 1) / (float)codes.Length;

            RectTransform slot = NewRect("Btn_" + codes[i], row);
            slot.anchorMin = new Vector2(from, 0f);
            slot.anchorMax = new Vector2(to, 1f);
            // Зазор между кнопками: половина с каждой стороны
            slot.offsetMin = new Vector2(ButtonGap * 0.5f, 0f);
            slot.offsetMax = new Vector2(-ButtonGap * 0.5f, 0f);

            Button button = AttachButton(slot);

            // Флаг слева
            RectTransform flagRect = NewRect("Flag", slot);
            flagRect.anchorMin = new Vector2(0f, 0.5f);
            flagRect.anchorMax = new Vector2(0f, 0.5f);
            flagRect.pivot = new Vector2(0f, 0.5f);
            flagRect.anchoredPosition = new Vector2(18f, 0f);
            flagRect.sizeDelta = new Vector2(FlagSize, FlagSize);

            var flag = flagRect.gameObject.AddComponent<Image>();
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            flag.enabled = false; // включится, когда появится спрайт

            // Название справа от флага
            TextMeshProUGUI label = NewText(slot, "Label", labels[i], 32,
                                            UITheme.TextOnAccent, font);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(FlagSize + 32f, 0f);
            labelRect.offsetMax = new Vector2(-16f, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            result[i] = new LanguageSettingsPanel.LanguageButton
            {
                Code = codes[i],
                Button = button,
                Flag = flag
            };
        }

        return result;
    }

    // --- хелперы ---

    /// <summary>
    /// Вешает Button на готовый RectTransform: якоря задаются до добавления
    /// компонента, чтобы не зависеть от порядка инициализации.
    /// </summary>
    private static Button AttachButton(RectTransform rect)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = UITheme.ButtonSecondaryMain;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        return button;
    }

    /// <summary>
    /// Помечает текст как переводимый: в рантайме UiTextLabel подставит
    /// значение из таблицы UI вместо ключа. В редакторе таблица не загружена,
    /// поэтому в префаб попадает ключ.
    /// </summary>
    private static void AddLabelKey(TextMeshProUGUI label, string key)
    {
        var localized = label.gameObject.AddComponent<UiTextLabel>();
        localized.Key = key;
        label.text = key;
    }

    private static void SetLanguageButtons(SerializedObject so,
                                           LanguageSettingsPanel.LanguageButton[] languages)
    {
        SerializedProperty array = so.FindProperty("_languageButtons");
        if (array == null)
        {
            Debug.LogWarning("[Settings] В SettingsView нет поля _languageButtons.");
            return;
        }

        array.arraySize = languages.Length;

        for (int i = 0; i < languages.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("Code").stringValue = languages[i].Code;
            item.FindPropertyRelative("Button").objectReferenceValue = languages[i].Button;
            item.FindPropertyRelative("Flag").objectReferenceValue = languages[i].Flag;
        }
    }

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
        Button button = AttachButton(rect);

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
