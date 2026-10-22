using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using YG;

/// <summary>
/// Состояние гаража: какие машины доступны (стадия пройдена), какие открыты игроком
/// и какая выбрана. Доход считает PassiveIncome, и только по открытым машинам.
///
/// Три состояния описаны в GarageCarState:
/// Locked    — стадия не пройдена;
/// Available — стадия пройдена, ждём нажатия "Открыть", доход не идёт;
/// Opened    — игрок открыл, доход идёт.
/// </summary>
public class GarageService : IGarageService
{
    private readonly GarageCarData[] _cars;
    private readonly HashSet<int> _available = new HashSet<int>();
    private readonly HashSet<int> _opened = new HashSet<int>();
    private readonly ISaveService _saveService;
    private int _selected;

    public event Action<int> CarAvailable;
    public event Action<int> CarOpened;
    public event Action<int> SelectionChanged;

    public GarageService(GarageConfig config, ISaveService saveService)
    {
        _saveService = saveService;
        _cars = config != null && config.GarageCars != null ? config.GarageCars : Array.Empty<GarageCarData>();

        MigrateOldSaves();
        LoadOpened();

        int saved = YG2.saves.lastGarageCarIndex;
        _selected = saved >= 0 && saved < _cars.Length ? saved : 0;
    }

    public GarageCarData[] AllCars => _cars;

    public int SelectedIndex => _selected;

    public bool IsAvailable(int index) => _available.Contains(index);

    public bool IsOpened(int index) => _opened.Contains(index);

    public GarageCarState GetState(int index)
    {
        if (_opened.Contains(index)) return GarageCarState.Opened;
        if (_available.Contains(index)) return GarageCarState.Available;
        return GarageCarState.Locked;
    }

    /// <summary>Открыть доступную машину. Доход начинает идти сразу после этого.</summary>
    public bool Open(int index)
    {
        if (index < 0 || index >= _cars.Length) return false;
        if (!_available.Contains(index) || _opened.Contains(index)) return false;

        _opened.Add(index);

        // Открытая машина сразу становится выбранной: игрок ждёт увидеть её характеристики
        bool selectionChanged = index != _selected;
        _selected = index;
        YG2.saves.lastGarageCarIndex = index;

        SaveOpened();

        if (selectionChanged) SelectionChanged?.Invoke(index);
        CarOpened?.Invoke(index);
        return true;
    }

    public bool TrySelect(int index)
    {
        if (index < 0 || index >= _cars.Length) return false;

        // Листать можно любую машину: закрытую, доступную и открытую.
        // Иначе игрок не долистает до доступной и не увидит кнопку "Открыть".
        if (index == _selected) return true;

        _selected = index;
        YG2.saves.lastGarageCarIndex = index;
        _saveService.Save();
        SelectionChanged?.Invoke(index);
        return true;
    }

    /// <summary>Отмечает машины, чья стадия уже пройдена, как доступные. Открытие — за игроком.</summary>
    public void RefreshForStage(int stage)
    {
        bool changed = false;

        for (int i = 0; i < _cars.Length; i++)
        {
            if (_available.Contains(i) || stage < _cars[i].UnlockStage) continue;

            _available.Add(i);
            changed = true;
            CarAvailable?.Invoke(i);
        }

        if (changed) _saveService.Save();
    }

    private void LoadOpened()
    {
        if (YG2.saves.openedGarageCars == null) return;

        foreach (int i in YG2.saves.openedGarageCars)
        {
            if (i < 0 || i >= _cars.Length) continue;
            _opened.Add(i);
            _available.Add(i); // открытая машина всегда доступна
        }
    }

    private void SaveOpened()
    {
        YG2.saves.openedGarageCars = new List<int>(_opened);
        _saveService.Save();
    }

    /// <summary>
    /// Перенос старых сохранений. Раньше машина открывалась сама по стадии и сразу давала доход.
    /// Чтобы игроки с уже пройденным прогрессом не потеряли доход, их открытые по стадии машины
    /// переносятся как открытые. Выполняется один раз.
    ///
    /// Новый игрок (и сброс через SetDefaultSaves) НЕ переносится: прогресса нет, машины
    /// с UnlockStage = 1 просто становятся доступными, а открывает их игрок кнопкой.
    /// </summary>
    private void MigrateOldSaves()
    {
        if (YG2.saves.garageOpenMigrated) return;

        bool hasLegacyProgress = YG2.saves.level > 1 || YG2.saves.stageDetails > 0 || YG2.saves.balance > 1;

        if (hasLegacyProgress)
        {
            int stage = YG2.saves.level < 1 ? 1 : YG2.saves.level;
            var opened = new List<int>();

            for (int i = 0; i < _cars.Length; i++)
            {
                if (stage < _cars[i].UnlockStage) continue;
                opened.Add(i);
                _available.Add(i);
            }

            YG2.saves.openedGarageCars = opened;
            foreach (int i in opened) _opened.Add(i);
        }

        // Флаг ставим всегда: миграция для этого сохранения сделана
        YG2.saves.garageOpenMigrated = true;
        _saveService.Save();
    }
}
