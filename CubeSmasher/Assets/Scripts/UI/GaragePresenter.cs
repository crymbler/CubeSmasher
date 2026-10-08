using System;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;

/// <summary>
/// Связывает окно гаража с сервисом гаража. Правил открытия здесь нет: решает GarageService.
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
        _garage.SelectionChanged += HandleSelectionChanged;
        _garage.CarUnlocked += HandleCarUnlocked;

        view.SetCarCount(garage.AllCars.Length);
        ShowSelected();
    }

    private void HandleCarSelected(int index)
    {
        _garage.TrySelect(index);
    }

    private void HandleSelectionChanged(int index)
    {
        ShowSelected();
    }

    private void HandleCarUnlocked(int index)
    {
        if (index == _garage.SelectedIndex) ShowSelected();
    }

    private void ShowSelected()
    {
        GarageCarData[] cars = _garage.AllCars;
        if (cars.Length == 0) return;

        int i = _garage.SelectedIndex;
        GarageCarData car = cars[i];
        _view.ShowCar(i, car.CarName, car.Description, car.IncomePerSecond,
                      _garage.IsUnlocked(i), car.UnlockStage, car.ModelPrefab);
    }

    public void Dispose()
    {
        _view.CarSelected -= HandleCarSelected;
        _garage.SelectionChanged -= HandleSelectionChanged;
        _garage.CarUnlocked -= HandleCarUnlocked;
    }
}
