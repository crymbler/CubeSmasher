using System;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;

/// <summary>
/// Связывает экран гаража с сервисом. Правил открытия здесь нет: решает GarageService.
/// </summary>
public class GaragePresenter : IDisposable
{
    private readonly IGarageService _garage;
    private readonly IGarageView _view;

    public GaragePresenter(IGarageService garage, IGarageView view)
    {
        _garage = garage ?? throw new ArgumentNullException(nameof(garage));
        _view = view ?? throw new ArgumentNullException(nameof(view));

        _view.CarSelected += HandleCarSelected;
        _view.CarOpenRequested += HandleActionRequested;
        _garage.ViewChanged += HandleSelectionChanged;      // показать другую машину
        _garage.SelectionChanged += HandleSelectionChanged; // выбрали другую для игры
        _garage.CarAvailable += HandleCarStateChanged;
        _garage.CarOpened += HandleCarOpened;

        GarageCarData[] all = garage.AllCars;
        var states = new GarageCarState[all.Length];
        for (int i = 0; i < all.Length; i++) states[i] = garage.GetState(i);

        view.SetCarCount(System.Array.ConvertAll(all, c => c.MiniIcon), states);
        ShowSelected();
    }

    /// <summary>
    /// Перерисовать текущую машину. Вызывается после смены языка и при
    /// открытии гаража: описание и доход пишутся через UiText.Get и без
    /// повторного ShowCar остаются на языке прошлого показа.
    /// </summary>
    public void Refresh() => ShowSelected();

    /// <summary>
    /// Игрок ткнул в иконку или стрелку: это только просмотр.
    /// Выбор машины для игры — отдельной кнопкой на карточке.
    /// </summary>
    private void HandleCarSelected(int index)
    {
        _garage.View(index);
    }

    private void HandleSelectionChanged(int index) => ShowSelected();

    private void HandleCarStateChanged(int index)
    {
        // Иконка в ленте обновляется всегда, даже если машина не выбрана
        _view.SetCarState(index, _garage.GetState(index));
        if (index == _garage.ViewedIndex) ShowSelected();
    }

    /// <summary>
    /// Кнопка на карточке: у доступной машины это "Открыть", у открытой — "Выбрать".
    /// Вторая кнопка в префабе не нужна.
    /// </summary>
    private void HandleActionRequested(int index)
    {
        switch (_garage.GetState(index))
        {
            case GarageCarState.Available:
                if (_garage.Open(index)) _view.PlayUnlockEffect();
                break;

            case GarageCarState.Opened:
                _garage.TrySelect(index);
                break;
        }
    }

    private void HandleCarOpened(int index)
    {
        _view.SetCarState(index, _garage.GetState(index));
        if (index == _garage.ViewedIndex) ShowSelected();
    }

    private void ShowSelected()
    {
        GarageCarData[] cars = _garage.AllCars;
        if (cars.Length == 0) return;

        int i = _garage.ViewedIndex;
        GarageCarData car = cars[i];

        _view.ShowCar(i, car.GetName(), car.GetDescription(), car.Characteristics,
                      car.IncomePerSecond, _garage.GetState(i), i == _garage.SelectedIndex,
                      car.UnlockStage, car.ModelPrefab);
    }

    public void Dispose()
    {
        _view.CarSelected -= HandleCarSelected;
        _view.CarOpenRequested -= HandleActionRequested;
        _garage.ViewChanged -= HandleSelectionChanged;
        _garage.SelectionChanged -= HandleSelectionChanged;
        _garage.CarAvailable -= HandleCarStateChanged;
        _garage.CarOpened -= HandleCarOpened;
    }
}
