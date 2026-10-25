using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Tools > CubeSmasher > Sync UI String Table
/// Берёт ключи и переводы из Scripts/Settings/Localization.cs и переносит их в String Table "UI".
/// Значения в таблице перезаписываются источником, ключи, которых нет в источнике, удаляются.
/// Запускать после любого изменения Localization.cs.
/// </summary>
public static class UiStringTableSync
{
    private const string CollectionName = "UI";
    private const string SourceRelative = "Scripts/Settings/Localization.cs";
    private static readonly string[] Locales = { "ru", "en", "tr" };

    [MenuItem("Tools/CubeSmasher/Sync UI String Table")]
    public static void Sync()
    {
        string path = Path.Combine(Application.dataPath, SourceRelative);
        if (!File.Exists(path))
        {
            Debug.LogError("[UI Sync] Не найден " + path);
            return;
        }

        Dictionary<string, Dictionary<string, string>> source = ReadSource(File.ReadAllText(path));
        if (source.Count == 0)
        {
            Debug.LogError("[UI Sync] В Localization.cs не найдено ни одного ключа.");
            return;
        }

        var collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
        if (collection == null)
        {
            Debug.LogError("[UI Sync] Не найдена String Table Collection \"" + CollectionName + "\".");
            return;
        }

        SharedTableData shared = collection.SharedData;
        int added = 0, removed = 0, written = 0;

        // 1. Удаляем ключи, которых нет в источнике (например, старый ".income.none")
        foreach (SharedTableData.SharedTableEntry entry in shared.Entries.ToList())
        {
            if (source.ContainsKey(entry.Key)) continue;

            foreach (string code in Locales)
            {
                var table = collection.GetTable(code) as StringTable;
                if (table != null) table.RemoveEntry(entry.Key);
            }
            shared.RemoveKey(entry.Key);
            removed++;
        }

        // 2. Создаём ключи и пишем значения по языкам
        foreach (var pair in source)
        {
            string key = pair.Key;
            SharedTableData.SharedTableEntry sharedEntry = shared.GetEntry(key);
            if (sharedEntry == null)
            {
                sharedEntry = shared.AddKey(key);
                added++;
            }

            foreach (string code in Locales)
            {
                var table = collection.GetTable(code) as StringTable;
                if (table == null)
                {
                    Debug.LogWarning("[UI Sync] Нет таблицы для языка '" + code + "'. Добавьте локаль в Localization Settings.");
                    continue;
                }

                string value = pair.Value.TryGetValue(code, out string v) ? v : string.Empty;
                StringTableEntry tableEntry = table.GetEntry(sharedEntry.Id);
                if (tableEntry == null) table.AddEntry(key, value);
                else tableEntry.Value = value;

                EditorUtility.SetDirty(table);
                written++;
            }
        }

        EditorUtility.SetDirty(shared);
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();

        Debug.Log("[UI Sync] Готово. Ключей в источнике: " + source.Count +
                  " | добавлено: " + added + " | удалено: " + removed + " | записано значений: " + written);
    }

    /// <summary>Разбирает словарь из Localization.cs: { "key", new Dictionary<string,string> { {"ru","..."}, ... } }</summary>
    private static Dictionary<string, Dictionary<string, string>> ReadSource(string text)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        var entries = Regex.Matches(text, @"\{\s*""([^""]+)"",\s*new Dictionary<string,string>\s*\{(.*?)\}\s*\},", RegexOptions.Singleline);

        foreach (Match entry in entries)
        {
            var langs = new Dictionary<string, string>();
            foreach (Match lang in Regex.Matches(entry.Groups[2].Value, @"\{""(\w+)"",\s*""((?:[^""\\]|\\.)*)""\}"))
            {
                langs[lang.Groups[1].Value] = Regex.Unescape(lang.Groups[2].Value);
            }
            result[entry.Groups[1].Value] = langs;
        }
        return result;
    }
}
