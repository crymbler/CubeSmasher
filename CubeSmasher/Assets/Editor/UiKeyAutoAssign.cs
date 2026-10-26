using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using CubeSmasher.Data.Configs;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > CubeSmasher > UI Keys
/// Сопоставляет текст TMP и поля UpgradeConfig с записями таблицы "UI" и назначает ключи.
/// Dry run ничего не меняет и пишет отчёт в Library/UI_KeyAssign_Report.txt.
/// Apply назначает: LocalizeStringEvent на TMP в открытой сцене и в префабах, LocalizedName в UpgradeConfig.
/// Текст, которого нет в таблице, не назначается: его сначала надо добавить в таблицу.
/// </summary>
public static class UiKeyAutoAssign
{
    private const string Collection = "UI";

    // Ключи, у которых в таблице есть {0}, {1}... Для них текст выводит код, LocalizeStringEvent не ставится
    private static readonly HashSet<string> DynamicKeys = new HashSet<string>();
    private static readonly string[] Locales = { "ru", "en", "tr" };

    private sealed class Stats
    {
        public int Matched;
        public readonly List<string> Lines = new List<string>();
    }

    [MenuItem("Tools/CubeSmasher/UI Keys/Dry run (report only)")]
    public static void DryRun() => Run(false);

    [MenuItem("Tools/CubeSmasher/UI Keys/Apply")]
    public static void Apply() => Run(true);

    private static void Run(bool apply)
    {
        List<(Regex Pattern, string Key)> index = BuildIndex();
        if (index.Count == 0)
        {
            Debug.LogError("[UI Keys] Таблица \"" + Collection + "\" не найдена или пустая.");
            return;
        }

        var stats = new Stats();

        // 1. Открытая сцена
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            ProcessHierarchy(root, index, apply, stats, scene.name);
        }
        if (apply) EditorSceneManager.MarkSceneDirty(scene);

        // 2. Все префабы проекта
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            int before = stats.Matched;

            ProcessHierarchy(prefab, index, apply, stats, path);

            if (apply && stats.Matched > before) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        // 3. UpgradeConfig
        ProcessUpgrades(index, apply, stats);

        // 4. Машины в GarageConfig
        ProcessGarageCars(index, apply, stats);

        if (apply) AssetDatabase.SaveAssets();

        int unmatched = stats.Lines.Count(l => l.StartsWith("НЕТ КЛЮЧА"));
        int missing = stats.Lines.Count(l => l.StartsWith("MISSING SCRIPT"));

        string report = Path.Combine(Application.dataPath, "..", "Library", "UI_KeyAssign_Report.txt");
        File.WriteAllLines(report, stats.Lines);

        Debug.Log("[UI Keys] " + (apply ? "Применено" : "Dry run") +
                  ". Совпало: " + stats.Matched +
                  ", без ключа: " + unmatched +
                  ", битых компонентов: " + missing +
                  ". Отчёт: " + Path.GetFullPath(report));
    }

    private static void ProcessHierarchy(GameObject root, List<(Regex Pattern, string Key)> index,
                                         bool apply, Stats stats, string where)
    {
        foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string text = tmp.text == null ? string.Empty : tmp.text.Trim();
            if (text.Length == 0) continue;

            string place = where + " / " + GetPath(tmp.transform);
            string key = FindKey(text, index);
            if (key == null)
            {
                stats.Lines.Add("НЕТ КЛЮЧА: " + place + " = \"" + text + "\"");
                continue;
            }

            stats.Matched++;
            stats.Lines.Add("OK: " + place + " -> " + key);
            if (!apply) continue;

            if (DynamicKeys.Contains(key))
            {
                stats.Lines.Add("ДИНАМИЧЕСКИЙ, без LocalizeStringEvent: " + place + " -> " + key);
                continue;
            }

            LocalizeStringEvent ev = tmp.GetComponent<LocalizeStringEvent>();
            if (ev == null) ev = Undo.AddComponent<LocalizeStringEvent>(tmp.gameObject);
            if (ev.StringReference == null || ev.StringReference.IsEmpty)
            {
                ev.StringReference = new LocalizedString(Collection, key);
            }
            EditorUtility.SetDirty(tmp.gameObject);
        }

        // Компоненты с потерянным скриптом (например, удалённые LanguageSwitcher и Localizer)
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (Component c in t.GetComponents<Component>())
            {
                if (c == null) stats.Lines.Add("MISSING SCRIPT: " + where + " / " + GetPath(t));
            }
        }
    }

    private static void ProcessUpgrades(List<(Regex Pattern, string Key)> index, bool apply, Stats stats)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UpgradeConfig"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<UpgradeConfig>(path);
            if (cfg == null) continue;

            if (cfg.LocalizedName != null && !cfg.LocalizedName.IsEmpty) continue;

            string key = FindKey(cfg.UpgradeName ?? string.Empty, index);
            if (key == null)
            {
                stats.Lines.Add("НЕТ КЛЮЧА: UpgradeConfig " + path + " = \"" + cfg.UpgradeName + "\"");
                continue;
            }

            stats.Matched++;
            stats.Lines.Add("OK: UpgradeConfig " + cfg.name + " -> " + key);
            if (!apply) continue;

            Undo.RecordObject(cfg, "Assign UI key");
            cfg.LocalizedName = new LocalizedString(Collection, key);
            EditorUtility.SetDirty(cfg);
        }
    }

    [MenuItem("Tools/CubeSmasher/UI Keys/Remove LocalizeStringEvent from dynamic texts")]
    public static void RemoveDynamic()
    {
        BuildIndex(); // заполняет DynamicKeys
        int removed = 0;

        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects()) removed += RemoveDynamicIn(root);
        if (removed > 0) EditorSceneManager.MarkSceneDirty(scene);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            int n = RemoveDynamicIn(prefab);
            if (n > 0) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
            removed += n;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[UI Keys] Удалено LocalizeStringEvent у динамических текстов: " + removed);
    }

    private static int RemoveDynamicIn(GameObject root)
    {
        int n = 0;
        foreach (LocalizeStringEvent ev in root.GetComponentsInChildren<LocalizeStringEvent>(true))
        {
            LocalizedString ls = ev.StringReference;
            if (ls == null || ls.TableReference.TableCollectionName != Collection) continue;
            if (!DynamicKeys.Contains(ls.TableEntryReference.Key)) continue;

            Object.DestroyImmediate(ev);
            n++;
        }
        return n;
    }

    private static void ProcessGarageCars(List<(Regex Pattern, string Key)> index, bool apply, Stats stats)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:GarageConfig"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<GarageConfig>(path);
            if (cfg == null || cfg.GarageCars == null) continue;

            GarageCarData[] cars = cfg.GarageCars;
            bool changed = false;

            for (int i = 0; i < cars.Length; i++)
            {
                string place = cfg.name + "[" + i + "]";

                if (cars[i].LocalizedName == null || cars[i].LocalizedName.IsEmpty)
                {
                    string key = FindKey((cars[i].CarName ?? string.Empty).Trim(), index);
                    if (key == null) stats.Lines.Add("НЕТ КЛЮЧА: " + place + " имя = \"" + cars[i].CarName + "\"");
                    else
                    {
                        stats.Matched++;
                        stats.Lines.Add("OK: " + place + " имя -> " + key);
                        if (apply) { cars[i].LocalizedName = new LocalizedString(Collection, key); changed = true; }
                    }
                }

                if (cars[i].LocalizedDescription == null || cars[i].LocalizedDescription.IsEmpty)
                {
                    if (string.IsNullOrEmpty(cars[i].Description)) continue;
                    string key = FindKey(cars[i].Description.Trim(), index);
                    if (key == null) stats.Lines.Add("НЕТ КЛЮЧА: " + place + " описание = \"" + cars[i].Description + "\"");
                    else
                    {
                        stats.Matched++;
                        stats.Lines.Add("OK: " + place + " описание -> " + key);
                        if (apply) { cars[i].LocalizedDescription = new LocalizedString(Collection, key); changed = true; }
                    }
                }
            }

            if (apply && changed)
            {
                Undo.RecordObject(cfg, "Assign UI keys");
                cfg.GarageCars = cars;
                EditorUtility.SetDirty(cfg);
            }
        }
    }

    private static string FindKey(string text, List<(Regex Pattern, string Key)> index)
    {
        foreach (var item in index)
        {
            if (item.Pattern.IsMatch(text)) return item.Key;
        }
        return null;
    }

    private static List<(Regex Pattern, string Key)> BuildIndex()
    {
        DynamicKeys.Clear();
        var index = new List<(Regex Pattern, string Key)>();
        var collection = LocalizationEditorSettings.GetStringTableCollection(Collection);
        if (collection == null) return index;

        var tables = new List<StringTable>();
        foreach (string code in Locales)
        {
            tables.Add(collection.GetTable(code) as StringTable);
        }

        foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries)
        {
            foreach (StringTable table in tables)
            {
                if (table == null) continue;
                string value = table.GetEntry(entry.Id)?.Value;
                if (value != null && value.Contains("{")) DynamicKeys.Add(entry.Key);
                Regex pattern = ToPattern(value);
                if (pattern != null) index.Add((pattern, entry.Key));
            }
        }
        return index;
    }

    /// <summary>Строка с {0}, {1}... превращается в регулярное выражение, которое совпадает с любым значением.</summary>
    private static Regex ToPattern(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        string[] parts = Regex.Split(value.Trim(), @"\{\d+\}");
        if (parts.Sum(p => p.Length) < 2) return null;

        string body = string.Join(".*", parts.Select(p => Regex.Escape(p)));
        return new Regex("^" + body + "$", RegexOptions.IgnoreCase);
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
