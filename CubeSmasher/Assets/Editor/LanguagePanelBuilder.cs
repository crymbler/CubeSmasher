using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > CubeSmasher > Build Language Panel
/// Создаёт префаб Assets/Prefabs/UI/LanguagePanel.prefab: панель с тремя кнопками языка (ru, en, tr).
/// Кнопки уже назначены в LanguageSettingsPanel. На сцену и в другие объекты ничего не ставится.
/// Можно запускать повторно: префаб перезаписывается.
/// </summary>
public static class LanguagePanelBuilder
{
    private const string Folder = "Assets/Prefabs/UI";
    private const string PrefabPath = Folder + "/LanguagePanel.prefab";

    private static readonly (string Code, string Label)[] Languages =
    {
        ("ru", "Русский"),
        ("en", "English"),
        ("tr", "Türkçe"),
    };

    [MenuItem("Tools/CubeSmasher/Build Language Panel")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs", "UI");

        var root = new GameObject("LanguagePanel",
            typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LanguageSettingsPanel));
        ((RectTransform)root.transform).sizeDelta = new Vector2(600f, 500f);

        root.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Кнопки создаются дочерними объектами и сразу назначаются в LanguageSettingsPanel
        var panel = root.GetComponent<LanguageSettingsPanel>();
        var so = new SerializedObject(panel);
        SerializedProperty list = so.FindProperty("_buttons");
        list.arraySize = Languages.Length;

        for (int i = 0; i < Languages.Length; i++)
        {
            Button button = MakeButton(root.transform, Languages[i].Label);

            SerializedProperty item = list.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("Code").stringValue = Languages[i].Code;
            item.FindPropertyRelative("Button").objectReferenceValue = button;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[LanguagePanel] Готово: " + PrefabPath + ", кнопок: " + Languages.Length);
    }

    private static Button MakeButton(Transform parent, string label)
    {
        var go = new GameObject(label,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = 120f;
        go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.26f, 1f);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 48f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var rt = (RectTransform)textGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
