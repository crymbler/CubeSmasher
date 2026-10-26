using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Tools > CubeSmasher > Import UI CSV
/// Импортирует CSV (первая колонка Key, дальше колонки с кодами языков: ru, en, tr)
/// напрямую в String Table Collection "UI".
/// Встроенный импорт Unity сопоставляет колонки сам и часто оставляет значения пустыми,
/// поэтому колонки берутся по имени из заголовка.
/// Существующие ключи обновляются, новые добавляются, отсутствующие в CSV не удаляются.
/// </summary>
public static class UiCsvImport
{
    private const string Collection = "UI";

    [MenuItem("Tools/CubeSmasher/Import UI CSV")]
    public static void Import()
    {
        string path = EditorUtility.OpenFilePanel("CSV для таблицы " + Collection, "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var collection = LocalizationEditorSettings.GetStringTableCollection(Collection);
        if (collection == null)
        {
            Debug.LogError("[UI CSV] Не найдена коллекция \"" + Collection + "\".");
            return;
        }

        List<string[]> rows = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
        if (rows.Count < 2)
        {
            Debug.LogError("[UI CSV] В файле нет строк с данными.");
            return;
        }

        // Заголовок: колонка 0 = ключ, остальные = коды языков
        string[] header = rows[0];
        var columns = new List<(int Index, string Code)>();
        for (int c = 1; c < header.Length; c++)
        {
            string code = header[c].Trim().Trim('"').ToLower();
            if (code.Length > 0) columns.Add((c, code));
        }

        if (columns.Count == 0)
        {
            Debug.LogError("[UI CSV] Не найдены колонки языков в заголовке.");
            return;
        }

        SharedTableData shared = collection.SharedData;
        int added = 0, updated = 0, skipped = 0;

        for (int r = 1; r < rows.Count; r++)
        {
            string[] row = rows[r];
            if (row.Length == 0) continue;

            string key = row[0].Trim();
            if (key.Length == 0) { skipped++; continue; }

            SharedTableData.SharedTableEntry sharedEntry = shared.GetEntry(key);
            if (sharedEntry == null)
            {
                sharedEntry = shared.AddKey(key);
                added++;
            }

            foreach ((int index, string code) in columns)
            {
                if (index >= row.Length) continue;

                var table = collection.GetTable(code) as StringTable;
                if (table == null)
                {
                    Debug.LogWarning("[UI CSV] Нет таблицы для языка '" + code + "'. Добавьте локаль в Localization Settings.");
                    continue;
                }

                string value = row[index];
                StringTableEntry entry = table.GetEntry(sharedEntry.Id);
                if (entry == null) table.AddEntry(key, value);
                else entry.Value = value;

                EditorUtility.SetDirty(table);
                updated++;
            }
        }

        EditorUtility.SetDirty(shared);
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[UI CSV] Готово. Строк: " + (rows.Count - 1) +
                  ", ключей добавлено: " + added +
                  ", значений записано: " + updated +
                  ", пропущено: " + skipped +
                  ". Колонки: " + string.Join(", ", columns.Select(c => c.Code)));
    }

    /// <summary>Разбор CSV с поддержкой кавычек, запятых и переводов строк внутри значений.</summary>
    private static List<string[]> ParseCsv(string text)
    {
        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);

        var rows = new List<string[]>();
        var current = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(ch);
                continue;
            }

            switch (ch)
            {
                case '"': inQuotes = true; break;
                case ',': current.Add(field.ToString()); field.Clear(); break;
                case '\r': break;
                case '\n':
                    current.Add(field.ToString()); field.Clear();
                    rows.Add(current.ToArray()); current.Clear();
                    break;
                default: field.Append(ch); break;
            }
        }

        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString());
            rows.Add(current.ToArray());
        }

        return rows.Where(r => r.Any(f => f.Trim().Length > 0)).ToList();
    }
}
