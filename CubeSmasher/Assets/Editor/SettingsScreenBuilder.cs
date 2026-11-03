using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CubeSmasher.Settings;
using CubeSmasher.UI;

/// <summary>
/// Tools > CubeSmasher > Build Settings Screen
///
/// Собирает экран настроек в той же схеме, что HUD и гараж:
///   Settings_Screen (фон, SettingsView)
///     SafeArea (SafeAreaFitter — вырезы и полоса жестов)
///       Title, CloseButton
///       Content (VerticalLayoutGroup)
///         MusicRow, SoundRow  — подпись, процент, ползунок
///         LanguageCaption     — слово «Язык»
///         LanguageRow         — три кнопки с местом под флаг
///
/// Почему VerticalLayoutGroup здесь уместен, а в магазине косметики нет:
/// у каждой строки есть LayoutElement с preferredHeight. Вертикальная группа
/// с childControlHeight берёт высоту именно оттуда, поэтому строки не могут
/// схлопнуться в ноль. Группа без LayoutElement у детей — та самая ловушка,
/// из-за которой слоты магазина были невидимы.
///
/// Ползунки строятся одной функцией, поэтому совпадают по размеру по построению.
/// Ручка ползунка: Slider сам переписывает её якоря в (x,0)-(x,1) и растягивает
/// по высоте родителя, поэтому высоту задаёт Handle Slide Area, а не sizeDelta.y.
/// </summary>
public static class SettingsScreenBuilder
{
    private const string UIFolder = "Assets/Prefabs/UI";
    private const string Path = UIFolder + "/Settings_Screen.prefab";
    private const string FontPath = "Assets/References/ofont.ru_Franklin Gothic Medium SDF.asset";

    // Размеры
    private const float RowHeight = 150f;
    private const float HeaderHeight = 52f;
    private const float SliderHeight = 56f;
    private const float SliderBottom = 10f;
    private const float HandleWidth = 44f;
    private const float LanguageCaptionHeight = 44f;
    private const float LanguageRowHeight = 132f;
    private const float LanguageButtonHeight = 110f;
    private const float FlagSize = 72f;
    private const float RowSpacing = 40f;

    // Отступы контента внутри SafeArea
    private const float SideMargin = 96f;
    private const float TopOffset = 200f;
    private const float BottomMargin = 64f;

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
        Debug.Log("[Settings] Флаги: у Btn_ru / Btn_en / Btn_tr есть дочерний объект Flag. " +
                  "Перетащите картинку в его поле Source Image.");
        Debug.Log("[Settings] Дальше: Settings_Screen на Canvas, в Bootstrapper назначить " +
                  "_settingsView и _openSettingsButton (BtnSettings в HUD).");
    }

    private static GameObject BuildScreen(TMP_FontAsset font)
    {
        RectTransform root = NewUI("Settings_Screen", null);
        Stretch(root);

        var background = root.gameObject.AddComponent<Image>();
        background.color = UITheme.WithAlpha(UITheme.MetalDarkest, 245);
        background.raycastTarget = true;

        var view = root.gameObject.AddComponent<SettingsView>();

        // --- SafeArea: тот же приём, что в HUD ---
        RectTransform safeArea = NewUI("SafeArea", root);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // --- Заголовок ---
        TextMeshProUGUI title = AddText(safeArea, "Title", "settings.title", 64,
                                        UITheme.NeonGreen, font, TextAlignmentOptions.Center);
        Band(title.rectTransform, top: true, height: 92f, offset: 40f);
        title.fontStyle = FontStyles.Bold;
        AddLabelKey(title, "settings.title");

        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = UITheme.NeonGreenDark;
        titleShadow.effectDistance = new Vector2(3f, -3f);

        // --- Кнопка закрытия ---
        Button close = AddButton(safeArea, "CloseButton", "X", font, 40f);
        Point((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
              new Vector2(-40f, -40f), new Vector2(96f, 96f));

        // --- Контент ---
        RectTransform content = NewUI("Content", safeArea);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(SideMargin, BottomMargin);
        content.offsetMax = new Vector2(-SideMargin, -TopOffset);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Строки громкости
        (Slider musicSlider, TextMeshProUGUI musicValue) =
            BuildVolumeRow(content, "MusicRow", "settings.music", font);

        (Slider soundSlider, TextMeshProUGUI soundValue) =
            BuildVolumeRow(content, "SoundRow", "settings.sound", font);

        // Подпись «Язык»
        TextMeshProUGUI caption = AddText(content, "LanguageCaption", "settings.language", 32,
                                          UITheme.TextSecondary, font, TextAlignmentOptions.MidlineLeft);
        AddRowHeight(caption.gameObject, LanguageCaptionHeight);
        AddLabelKey(caption, "settings.language");

        // Кнопки языка
        LanguageSettingsPanel.LanguageButton[] languages = BuildLanguageRow(content, font);

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
    /// Строка громкости: сверху подпись и процент, снизу ползунок.
    /// Высота объявлена через LayoutElement — по ней строку ставит группа.
    /// </summary>
    private static (Slider, TextMeshProUGUI) BuildVolumeRow(
        RectTransform parent, string name, string key, TMP_FontAsset font)
    {
        RectTransform row = NewUI(name, parent);
        AddRowHeight(row.gameObject, RowHeight);

        // Шапка: слева название, справа процент
        RectTransform header = NewUI("Header", row);
        Band(header, top: true, height: HeaderHeight);

        TextMeshProUGUI caption = AddText(header, "Label", key, 36, UITheme.TextPrimary, font,
                                          TextAlignmentOptions.MidlineLeft);
        caption.rectTransform.anchorMin = new Vector2(0f, 0f);
        caption.rectTransform.anchorMax = new Vector2(0.7f, 1f);
        AddLabelKey(caption, key);

        TextMeshProUGUI value = AddText(header, "Value", "50%", 36, UITheme.NeonGreen, font,
                                        TextAlignmentOptions.MidlineRight);
        value.rectTransform.anchorMin = new Vector2(0.7f, 0f);
        value.rectTransform.anchorMax = new Vector2(1f, 1f);

        // Ползунок прижат к низу строки, во всю ширину
        RectTransform sliderRect = NewUI("Slider", row);
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, SliderBottom);
        sliderRect.sizeDelta = new Vector2(0f, SliderHeight);

        Slider slider = BuildSlider(sliderRect);

        return (slider, value);
    }

    /// <summary>
    /// Ползунок: дорожка, заполнение, ручка.
    /// Высота ручки задаётся зоной Handle Slide Area (56 − 2×8 = 40),
    /// а не sizeDelta.y: Slider растягивает ручку по высоте родителя сам.
    /// </summary>
    private static Slider BuildSlider(RectTransform parent)
    {
        var slider = parent.gameObject.AddComponent<Slider>();

        // Дорожка
        RectTransform track = NewUI("Background", parent);
        track.anchorMin = new Vector2(0f, 0.5f);
        track.anchorMax = new Vector2(1f, 0.5f);
        track.sizeDelta = new Vector2(0f, 20f);

        var trackImage = track.gameObject.AddComponent<Image>();
        trackImage.color = UITheme.MetalMid;
        trackImage.raycastTarget = false;

        // Заполнение: отступы по краям равны половине ручки,
        // иначе на крайних значениях оно заезжает под ручку
        RectTransform fillArea = NewUI("Fill Area", parent);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-HandleWidth, 20f);

        RectTransform fill = NewUI("Fill", fillArea);
        Stretch(fill);

        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = UITheme.NeonGreen;
        fillImage.raycastTarget = false;

        // Ручка: высоту даёт эта зона, ширину — sizeDelta.x
        RectTransform handleArea = NewUI("Handle Slide Area", parent);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(HandleWidth * 0.5f, 8f);
        handleArea.offsetMax = new Vector2(-HandleWidth * 0.5f, -8f);

        RectTransform handle = NewUI("Handle", handleArea);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(HandleWidth, 0f);

        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = UITheme.NeonGreenLight;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }

    /// <summary>
    /// Три кнопки языка. Горизонтальная группа делит ширину поровну,
    /// высота кнопки задаётся LayoutElement — иначе она нулевая и кнопку не видно.
    /// Внутри: слева место под флаг, справа название.
    /// </summary>
    private static LanguageSettingsPanel.LanguageButton[] BuildLanguageRow(
        RectTransform parent, TMP_FontAsset font)
    {
        RectTransform row = NewUI("LanguageRow", parent);
        AddRowHeight(row.gameObject, LanguageRowHeight);

        string[] codes = { "ru", "en", "tr" };
        string[] labels = { "Русский", "English", "Türkçe" };

        const float ButtonWidth = 200f;
        const float ButtonHeight = 150f;
        const float Step = 604f;

        var result = new LanguageSettingsPanel.LanguageButton[codes.Length];

        for (int i = 0; i < codes.Length; i++)
        {
            RectTransform slot = NewUI("Btn_" + codes[i], row);

            // Якоря и размер задаются ЯВНО. Если оставить кнопку растянутой
            // (anchorMin 0,0 / anchorMax 1,1), она накрывает весь ряд
            // и перехватывает клики у соседей — снаружи «кнопки не работают».
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            // Первая кнопка по центру своей ячейки, дальше — постоянный шаг
            slot.anchoredPosition = new Vector2(292f + Step * i, -50f);

            var image = slot.gameObject.AddComponent<Image>();
            image.color = UITheme.ButtonSecondaryMain;

            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // Флаг слева, фиксированного размера
            RectTransform flagRect = NewUI("Flag", slot);
            flagRect.anchorMin = new Vector2(0f, 0.5f);
            flagRect.anchorMax = new Vector2(0f, 0.5f);
            flagRect.pivot = new Vector2(0f, 0.5f);
            flagRect.anchoredPosition = new Vector2(14f, 0f);
            flagRect.sizeDelta = new Vector2(FlagSize, FlagSize);

            var flag = flagRect.gameObject.AddComponent<Image>();
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            flag.enabled = false;

            // Название справа от флага
            TextMeshProUGUI label = AddText(slot, "Label", labels[i], 30,
                                            UITheme.TextOnAccent, font, TextAlignmentOptions.MidlineLeft);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(FlagSize + 24f, 0f);
            label.rectTransform.offsetMax = new Vector2(-10f, 0f);

            result[i] = new LanguageSettingsPanel.LanguageButton
            {
                Code = codes[i],
                Button = button,
                Flag = flag
            };
        }

        return result;
    }

    // ------------------------------------------------------------------
    // Хелперы — те же приёмы, что в HudBuilderTool и UIBuilderTool
    // ------------------------------------------------------------------

    private static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));

        // Слой UI может отсутствовать: -1 в go.layer недопустим
        int uiLayer = LayerMask.NameToLayer("UI");
        go.layer = uiLayer >= 0 ? uiLayer : 0;

        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>Полоса на всю ширину, прижатая к верху или низу родителя.</summary>
    private static void Band(RectTransform rt, bool top, float height, float offset = 0f)
    {
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, top ? -offset : offset);
        rt.localScale = Vector3.one;
    }

    /// <summary>Точка в углу родителя: anchor задаёт угол, pivot — куда растёт элемент.</summary>
    private static void Point(RectTransform rt, Vector2 anchor, Vector2 pivot,
                              Vector2 offset, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    /// <summary>Высота строки для VerticalLayoutGroup.</summary>
    private static void AddRowHeight(GameObject go, float height)
    {
        var element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        element.flexibleWidth = 1f;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string text,
                                           float size, Color color, TMP_FontAsset font,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rect = NewUI(name, parent);

        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;

        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        Stretch(rect);
        return tmp;
    }

    private static Button AddButton(Transform parent, string name, string label,
                                    TMP_FontAsset font, float fontSize = 40f)
    {
        RectTransform rect = NewUI(name, parent);

        var image = rect.gameObject.AddComponent<Image>();
        image.color = UITheme.ButtonSecondaryMain;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = AddText(rect, "Label", label, fontSize,
                                       UITheme.TextOnAccent, font, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);

        return button;
    }

    /// <summary>
    /// Помечает текст как переводимый. В редакторе таблица локализации
    /// не загружена и UiText.Get вернул бы сам ключ — поэтому в префаб
    /// кладётся ключ, а перевод подставляет UiTextLabel в рантайме.
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
}
