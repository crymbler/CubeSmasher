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
        _view.CarOpenRequested += HandleCarOpenRequested;
        _garage.SelectionChanged += HandleSelectionChanged;
        _garage.CarAvailable += HandleCarStateChanged;
        _garage.CarOpened += HandleCarOpened;

        view.SetCarCount(System.Array.ConvertAll(garage.AllCars, c => c.MiniIcon));
        ShowSelected();
    }

    private void HandleCarSelected(int index)
    {
        _garage.TrySelect(index);
    }

    private void HandleCarOpenRequested(int index)
    {
        if (!_garage.Open(index)) return;

        // Эффект только при успешном открытии
        _view.PlayUnlockEffect();
    }

    private void HandleSelectionChanged(int index) => ShowSelected();

    private void HandleCarStateChanged(int index)
    {
        if (index == _garage.SelectedIndex) ShowSelected();
    }

    private void HandleCarOpened(int index)
    {
        if (index == _garage.SelectedIndex) ShowSelected();
    }

    private void ShowSelected()
    {
        GarageCarData[] cars = _garage.AllCars;
        if (cars.Length == 0) return;

        int i = _garage.SelectedIndex;
        GarageCarData car = cars[i];

        _view.ShowCar(i, car.CarName, car.Description, car.Characteristics,
                      car.IncomePerSecond, _garage.GetState(i), car.UnlockStage, car.ModelPrefab);
    }

    public void Dispose()
    {
        _view.CarSelected -= HandleCarSelected;
        _view.CarOpenRequested -= HandleCarOpenRequested;
        _garage.SelectionChanged -= HandleSelectionChanged;
        _garage.CarAvailable -= HandleCarStateChanged;
        _garage.CarOpened -= HandleCarOpened;
    }
}
