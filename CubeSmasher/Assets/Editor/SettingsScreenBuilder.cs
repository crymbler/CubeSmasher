using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CubeSmasher.Settings;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Build Settings Screen
///
/// Собирает экран настроек: заголовок, две строки громкости и ряд кнопок языка
/// с местом под флаг.
///
/// Раскладка ручная, без LayoutGroup: у этих компонентов есть привычка
/// пересчитывать друг друга и схлопывать высоту детей в ноль. Здесь каждая
/// строка получает явные координаты и высоту, поэтому наложения невозможны.
///
/// Тексты ставятся компонентом UiTextLabel: в редакторе таблица локализации
/// не загружена, и UiText.Get вернул бы сам ключ. Компонент перечитает текст
/// в рантайме, когда таблица уже доступна.
/// </summary>
public static class SettingsScreenBuilder
{
    private const string UIFolder = "Assets/Prefabs/UI";
    private const string Path = UIFolder + "/Settings_Screen.prefab";
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";

    // Размеры в единицах Canvas (1920×1080)
    private const float VolumeRowHeight = 150f;
    private const float VolumeRowStep = 180f;
    private const float HeaderHeight = 52f;
    private const float SliderHeight = 56f;
    private const float LanguageRowHeight = 140f;
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
        Debug.Log("[Settings] Флаги: откройте префаб, у кнопок Btn_ru / Btn_en / Btn_tr " +
                  "есть дочерний объект Flag. Перетащите в его поле Source Image нужную картинку.");
        Debug.Log("[Settings] Дальше: перетащите Settings_Screen на Canvas, " +
                  "в Bootstrapper назначьте _settingsView и _openSettingsButton (BtnSettings в HUD).");
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

        // --- Контент: строки идут сверху вниз с явным шагом ---
        RectTransform content = NewRect("Content", root);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, -180f);
        content.sizeDelta = new Vector2(-192f, 0f);

        // Музыка
        (Slider musicSlider, TextMeshProUGUI musicValue) =
            BuildVolumeRow(content, "Music", "settings.music", 0f, font);

        // Звуки
        (Slider soundSlider, TextMeshProUGUI soundValue) =
            BuildVolumeRow(content, "Sound", "settings.sound", -VolumeRowStep, font);

        // Язык
        LanguageSettingsPanel.LanguageButton[] languages =
            BuildLanguageRow(content, -VolumeRowStep * 2f, font);

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
    /// Строка громкости: сверху подпись и процент, под ними ползунок.
    /// topOffset отсчитывается вниз от верха контента.
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

        // Шапка строки
        RectTransform header = NewRect("Header", row);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -4f);
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

        // Ползунок под шапкой
        RectTransform sliderRect = NewRect("Slider", row);
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 12f);
        sliderRect.sizeDelta = new Vector2(0f, SliderHeight);

        Slider slider = BuildSlider(sliderRect);

        return (slider, value);
    }

    /// <summary>Ползунок: фон, заполнение, ручка. Без них Slider нечего перетаскивать.</summary>
    private static Slider BuildSlider(RectTransform parent)
    {
        var slider = parent.gameObject.AddComponent<Slider>();

        // Дорожка
        RectTransform background = NewRect("Background", parent);
        background.anchorMin = new Vector2(0f, 0.5f);
        background.anchorMax = new Vector2(1f, 0.5f);
        background.sizeDelta = new Vector2(0f, 20f);

        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.color = UITheme.MetalMid;

        // Зона заполнения: отступ по краям равен половине ручки,
        // иначе заполнение заезжает под ручку при крайних значениях
        RectTransform fillArea = NewRect("Fill Area", parent);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-48f, 20f);

        RectTransform fill = NewRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = UITheme.NeonGreen;

        // Ручка
        RectTransform handleArea = NewRect("Handle Slide Area", parent);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(24f, 0f);
        handleArea.offsetMax = new Vector2(-24f, 0f);

        RectTransform handle = NewRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(44f, 44f);

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
    /// Ряд кнопок языка. В каждой кнопке слева место под флаг, справа название.
    /// Картинку флага вписывает владелец: здесь только пустая Image нужного размера.
    /// </summary>
    private static LanguageSettingsPanel.LanguageButton[] BuildLanguageRow(
        RectTransform parent, float topOffset, TMP_FontAsset font)
    {
        RectTransform row = NewRect("LanguageRow", parent);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, topOffset);
        row.sizeDelta = new Vector2(0f, LanguageRowHeight);

        string[] codes = { "ru", "en", "tr" };
        string[] labels = { "Русский", "English", "Türkçe" };

        const float gap = 12f;
        var result = new LanguageSettingsPanel.LanguageButton[codes.Length];

        for (int i = 0; i < codes.Length; i++)
        {
            float from = i / (float)codes.Length;
            float to = (i + 1) / (float)codes.Length;

            RectTransform slot = NewRect("Btn_" + codes[i], row);
            slot.anchorMin = new Vector2(from, 0f);
            slot.anchorMax = new Vector2(to, 1f);

            // Зазор между кнопками: половина с каждой стороны
            slot.offsetMin = new Vector2(gap * 0.5f, 0f);
            slot.offsetMax = new Vector2(-gap * 0.5f, 0f);

            Button button = AttachButton(slot);

            // Флаг слева
            RectTransform flagRect = NewRect("Flag", slot);
            flagRect.anchorMin = new Vector2(0f, 0.5f);
            flagRect.anchorMax = new Vector2(0f, 0.5f);
            flagRect.pivot = new Vector2(0f, 0.5f);
            flagRect.anchoredPosition = new Vector2(16f, 0f);
            flagRect.sizeDelta = new Vector2(FlagSize, FlagSize);

            var flag = flagRect.gameObject.AddComponent<Image>();
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            flag.enabled = false; // пока картинки нет — не рисуем пустой квадрат

            // Название справа от флага
            TextMeshProUGUI label = NewText(slot, "Label", labels[i], 30,
                                            UITheme.TextOnAccent, font);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(FlagSize + 28f, 0f);
            labelRect.offsetMax = new Vector2(-12f, 0f);
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
    /// Вешает Button на уже созданный RectTransform: так можно задать якоря
    /// до добавления компонента и не бороться с порядком инициализации.
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
    /// значение из таблицы UI вместо ключа.
    /// </summary>
    private static void AddLabelKey(TextMeshProUGUI label, string key)
    {
        var localized = label.gameObject.AddComponent<UiTextLabel>();
        localized.Key = key;
        label.text = key; // заглушка для редактора, в игре заменится
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
