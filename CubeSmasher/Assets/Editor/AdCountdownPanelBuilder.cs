using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > CubeSmasher > Build Ad Countdown Panel
/// Создаёт Assets/Prefabs/UI/AdCountdownPanel.prefab: полноэкранная затемняющая панель с цифрой отсчёта.
/// Префаб нужно положить в Canvas сцены последним (поверх всех окон). Повторный запуск перезаписывает префаб.
/// </summary>
public static class AdCountdownPanelBuilder
{
    private const string Folder = "Assets/Prefabs/UI";
    private const string PrefabPath = Folder + "/AdCountdownPanel.prefab";

    [MenuItem("Tools/CubeSmasher/Build Ad Countdown Panel")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs", "UI");

        var root = new GameObject("AdCountdownPanel",
            typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(AdCountdownPanel));
        var rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(root.transform, false);

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = "2";
        label.fontSize = 200f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        var labelRt = (RectTransform)labelGo.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var panel = root.GetComponent<AdCountdownPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("_label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AdCountdownPanel] Готово: " + PrefabPath);
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
