using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.View;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NurSapat.App.ViewModels;

public sealed partial class CostTableViewModel : ViewModelBase
{
    private readonly OrderWorkspaceViewModel _workspace;

    public ObservableCollection<CostItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private string _constructionTitle = "Текущая конструкция";

    public decimal TotalCost => Items.Sum(static item => item.TotalCost);

    public string TotalCostDisplay => $"Общая себестоимость: {TotalCost:N2} сом";

    public CostTableViewModel(OrderWorkspaceViewModel workspace)
    {
        _workspace = workspace;
        Items.CollectionChanged += OnItemsCollectionChanged;
        CalculateFromCurrentConstruction();
    }

    public void CalculateFromCurrentConstruction()
    {
        var construction = _workspace.Order.SelectedConstruction
            ?? _workspace.Order.EnsureDefaultConstruction();

        ConstructionTitle = $"{construction.Name} ({construction.DimensionsDisplay})";
        Items.Clear();

        // Заглушка расчёта: в будущем данные собираются из геометрии конструкции.
        Items.Add(new CostItemViewModel
        {
            MaterialName = "Профиль рамы арки",
            MaterialType = MaterialCostType.Profile,
            Quantity = 12.40m,
            Unit = "м.п.",
            UnitPrice = 420m
        });
        Items.Add(new CostItemViewModel
        {
            MaterialName = "Импост вертикальный",
            MaterialType = MaterialCostType.Profile,
            Quantity = 1.85m,
            Unit = "м.п.",
            UnitPrice = 390m
        });
        Items.Add(new CostItemViewModel
        {
            MaterialName = "Верхний арочный стеклопакет",
            MaterialType = MaterialCostType.Glass,
            Quantity = 1.12m,
            Unit = "м²",
            UnitPrice = 4800m
        });
        Items.Add(new CostItemViewModel
        {
            MaterialName = "Нижний стеклопакет",
            MaterialType = MaterialCostType.Glass,
            Quantity = 0.96m,
            Unit = "м²",
            UnitPrice = 4200m
        });
        Items.Add(new CostItemViewModel
        {
            MaterialName = "Комплект фурнитуры створки",
            MaterialType = MaterialCostType.Hardware,
            Quantity = 1m,
            Unit = "шт.",
            UnitPrice = 3150m
        });
        Items.Add(new CostItemViewModel
        {
            MaterialName = "Уплотнитель и крепёж",
            MaterialType = MaterialCostType.Consumables,
            Quantity = 1m,
            Unit = "компл.",
            UnitPrice = 680m
        });

        RefreshTotals();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CostItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += (_, _) => RefreshTotals();
            }
        }

        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalCost));
        OnPropertyChanged(nameof(TotalCostDisplay));
    }
}
