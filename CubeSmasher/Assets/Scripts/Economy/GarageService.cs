using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using YG;

/// <summary>
/// Состояние гаража: открытые машины по стадии и выбранная машина (сохраняется в YG2).
/// Не считает доход: это делает PassiveIncome по тем же машинам.
/// </summary>
public class GarageService : IGarageService
{
    private readonly GarageCarData[] _cars;
    private readonly HashSet<int> _unlocked = new HashSet<int>();
    private readonly ISaveService _saveService;
    private int _selected;

    public event Action<int> CarUnlocked;
    public event Action<int> SelectionChanged;

    public GarageService(GarageConfig config, ISaveService saveService)
    {
        _saveService = saveService;
        _cars = config != null && config.GarageCars != null ? config.GarageCars : Array.Empty<GarageCarData>();

        // Выбранная машина из сохранения, но только если она существует
        int saved = YG2.saves.lastGarageCarIndex;
        _selected = saved >= 0 && saved < _cars.Length ? saved : 0;
    }

    public GarageCarData[] AllCars => _cars;

    public int SelectedIndex => _selected;

    public bool IsUnlocked(int index) => _unlocked.Contains(index);

    public bool TrySelect(int index)
    {
        if (index < 0 || index >= _cars.Length || !_unlocked.Contains(index)) return false;
        if (index == _selected) return true;

        _selected = index;
        YG2.saves.lastGarageCarIndex = index;
        _saveService.Save();
        SelectionChanged?.Invoke(index);
        return true;
    }

    public void RefreshForStage(int stage)
    {
        for (int i = 0; i < _cars.Length; i++)
        {
            if (_unlocked.Contains(i) || stage < _cars[i].UnlockStage) continue;

            _unlocked.Add(i);
            CarUnlocked?.Invoke(i);
        }

        // Если сохранённая машина ещё закрыта (например, сейв испорчен), остаёмся на открытой
        if (!_unlocked.Contains(_selected))
        {
            foreach (int i in _unlocked)
            {
                _selected = i;
                break;
            }
        }
    }
}
