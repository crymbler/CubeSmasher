#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CubeSmasher.EditorTools
{
    /// <summary>
    /// Заполняет список UpgradeConfig в Bootstrapper всеми ассетами из Scripts/Upgrades,
    /// отсортированными по стадии открытия. Не надо перетаскивать 27 ассетов руками.
    /// </summary>
    public static class UpgradeListBuilder
    {
        private const string UpgradesFolder = "Assets/Scripts/Upgrades";

        [MenuItem("Tools/CubeSmasher/Fill Upgrade List")]
        public static void FillUpgradeList()
        {
            var bootstrapper = Object.FindObjectOfType<Bootstrapper>();
            if (bootstrapper == null)
            {
                Debug.LogError("Bootstrapper не найден в сцене");
                return;
            }

            List<UpgradeConfig> configs = LoadAll();
            if (configs.Count == 0)
            {
                Debug.LogError($"В {UpgradesFolder} нет UpgradeConfig");
                return;
            }

            var so = new SerializedObject(bootstrapper);
            SerializedProperty list = so.FindProperty("_upgradeConfigs");

            if (list == null)
            {
                Debug.LogError("В Bootstrapper нет поля _upgradeConfigs");
                return;
            }

            list.ClearArray();
            for (int i = 0; i < configs.Count; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bootstrapper);
            Debug.Log($"Заполнено {configs.Count} апгрейдов");
        }

        private static List<UpgradeConfig> LoadAll()
        {
            return AssetDatabase
                .FindAssets("t:UpgradeConfig", new[] { UpgradesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<UpgradeConfig>)
                .Where(c => c != null)
                .OrderBy(c => c.UnlockStageLevel)
                .ThenBy(c => c.Stat)
                .ToList();
        }
    }
}
#endif
