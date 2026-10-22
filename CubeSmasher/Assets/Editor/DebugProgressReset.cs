using UnityEditor;
using UnityEngine;
using YG;

/// <summary>
/// Сброс прогресса для отладки через штатный механизм плагина YG2.
/// Tools → CubeSmasher → Debug: Сбросить прогресс.
///
/// YG2.SetDefaultSaves() создаёт пустой SavesYG (сохраняется только idSave) и вызывает
/// onDefaultSaves. Сам по себе он НЕ сохраняет: нужен YG2.SaveProgress() после него.
/// Файл в папке Editor, в сборку не попадает.
/// </summary>
public static class DebugProgressReset
{
    [MenuItem("Tools/CubeSmasher/Debug: Сбросить прогресс")]
    public static void ResetProgress()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Сброс прогресса",
                "Сброс работает только во время Play Mode: плагин инициализируется при запуске игры.",
                "Понятно");
            return;
        }

        if (!EditorUtility.DisplayDialog("Сброс прогресса",
            "Удалить весь прогресс: баланс, стадию, открытые машины, апгрейды?\nЭто нельзя отменить.",
            "Сбросить", "Отмена"))
        {
            return;
        }

        YG2.SetDefaultSaves();
        YG2.SaveProgress();
        Debug.Log("[Reset] Сохранения сброшены через YG2.SetDefaultSaves() и сохранены.");
    }
}
