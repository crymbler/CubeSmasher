using CubeSmasher.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class OfflineRewardPopupBuilder
{
    private const string Folder = "Assets/Prefabs/UI";
    private const string PrefabPath = Folder + "/OfflineRewardPopup.prefab";
    private const string PanelWindowPath = Folder + "/Panel_Window.prefab";
    private const string ButtonPrimaryPath = Folder + "/Button_Primary.prefab";
    private const string ButtonSecondaryPath = Folder + "/Button_Secondary.prefab";

    [MenuItem("Tools/CubeSmasher/Build Offline Reward Popup")]
    public static void Build()
    {
        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelWindowPath);
        var primaryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrimaryPath);
        var secondaryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonSecondaryPath);
        if (panelPrefab == null || primaryPrefab == null || secondaryPrefab == null)
        {
            Debug.LogError("[OfflineRewardPopup] Не найден один из базовых префабов: " +
                           PanelWindowPath + ", " + ButtonPrimaryPath + ", " + ButtonSecondaryPath);
            return;
        }

        var root = new GameObject("OfflineRewardPopup", typeof(RectTransform), typeof(OfflineRewardPopup));
        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, root.transform);
        panel.name = "Panel";

        var title = FindDeep<TMP_Text>(panel.transform, "Title");
        var amountLabel = FindDeep<TMP_Text>(panel.transform, "Label");
        if (title == null || amountLabel == null)
        {
            Debug.LogError("[OfflineRewardPopup] В Panel_Window не найдены Title или Label");
            Object.DestroyImmediate(root);
            return;
        }
        AttachLocalized(title, "ad.offline.title");

        var claimButton = InstantiateButton(secondaryPrefab, panel.transform, "ClaimButton",
            new Vector2(-170f, -260f), "ad.offline.take");
        var doubleButton = InstantiateButton(primaryPrefab, panel.transform, "DoubleButton",
            new Vector2(170f, -260f), "ad.offline.double");

        var popup = root.GetComponent<OfflineRewardPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("_panel").objectReferenceValue = panel;
        so.FindProperty("_amountText").objectReferenceValue = amountLabel;
        so.FindProperty("_claimButton").objectReferenceValue = claimButton.GetComponent<Button>();
        so.FindProperty("_doubleButton").objectReferenceValue = doubleButton.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[OfflineRewardPopup] Готово: " + PrefabPath);
    }

    private static GameObject InstantiateButton(GameObject prefab, Transform parent, string name,
        Vector2 position, string key)
    {
        var button = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        button.name = name;

        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;

        var label = FindDeep<TMP_Text>(button.transform, "Label");
        if (label != null) AttachLocalized(label, key);
        return button;
    }

    private static void AttachLocalized(TMP_Text label, string key)
    {
        var component = label.gameObject.AddComponent<LocalizedUiLabel>();
        var so = new SerializedObject(component);
        so.FindProperty("_label").objectReferenceValue = label;
        so.FindProperty("_key").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T FindDeep<T>(Transform root, string name) where T : Component
    {
        foreach (var component in root.GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == name) return component;
        }
        return null;
    }
}
