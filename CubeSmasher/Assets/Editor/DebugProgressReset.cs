using UnityEditor;
using UnityEngine;
using YG;

/// <summary>
/// Сброс прогресса для проверки. Только в редакторе Unity (меню Tools → CubeSmasher).
/// Очищает сохранение игры: баланс, стадию, открытые машины, апгрейды.
/// В сборку не попадает: файл лежит в папке Editor.
/// </summary>
public static class DebugProgressReset
{
    [MenuItem("Tools/CubeSmasher/Debug: Сбросить прогресс")]
    public static void ResetProgress()
    {
        if (!EditorUtility.DisplayDialog("Сброс прогресса",
            "Удалить весь прогресс: баланс, стадию, машины, апгрейды?\nЭто нельзя отменить.",
            "Сбросить", "Отмена"))
        {
            return;
        }

        if (YG2.saves == null)
        {
            Debug.LogError("[Reset] YG2.saves не инициализирован. Запустите Play хотя бы раз.");
            return;
        }

        YG2.saves.level = 1;
        YG2.saves.stageDetails = 0;
        YG2.saves.balance = 1;
        YG2.saves.lastGarageCarIndex = 0;
        if (YG2.saves.openedGarageCars == null) YG2.saves.openedGarageCars = new System.Collections.Generic.List<int>();
        YG2.saves.openedGarageCars.Clear();
        YG2.saves.garageOpenMigrated = true; // с нуля миграция не нужна
        if (YG2.saves.passiveUpgrades != null) YG2.saves.passiveUpgrades.Clear();
        if (YG2.saves.tapUpgrades != null) YG2.saves.tapUpgrades.Clear();
        YG2.saves.hasPendingLocalSave = false;

        YG2.SaveProgress();
        Debug.Log("[Reset] Прогресс сброшен. Запустите игру заново.");
    }
}
