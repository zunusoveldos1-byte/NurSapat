using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.View;
using NurSapat.Core.Models.Calculation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;

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
        var cost = construction.Cost;

        ConstructionTitle = $"{construction.Name} ({construction.DimensionsDisplay})";
        Items.Clear();

        if (construction.SpecificationLines.Count > 0)
        {
            foreach (var line in construction.SpecificationLines)
            {
                Items.Add(new CostItemViewModel
                {
                    MaterialName = line.MaterialName,
                    MaterialType = MapCategory(line.Category),
                    Quantity = line.Quantity,
                    Unit = line.Unit,
                    UnitPrice = line.UnitPrice
                });
            }
        }
        else
        {
            AddCostLine("Профиль", MaterialCostType.Profile, cost.ProfileCost, "компл.");
            AddCostLine("Комплектующие", MaterialCostType.Consumables, cost.ComponentsCost, "компл.");
            AddCostLine("Уплотнитель", MaterialCostType.Consumables, cost.SealantCost, "компл.");
            AddCostLine("Стеклопакет", MaterialCostType.Glass, cost.GlazingCost, "м²", construction.Metrics.ProductAreaSqM);
            AddCostLine("Фурнитура", MaterialCostType.Hardware, cost.HardwareCost, "компл.");
            AddCostLine("Монтажные материалы", MaterialCostType.Consumables, cost.InstallationMaterialsCost, "компл.");
        }

        AddCostLine("Накладные расходы", MaterialCostType.Consumables, cost.Expenses, "компл.");
        AddCostLine("Работа", MaterialCostType.Consumables, cost.LaborCost, "компл.");

        RefreshTotals();
    }

    private static MaterialCostType MapCategory(CostMaterialCategory category) => category switch
    {
        CostMaterialCategory.Glazing => MaterialCostType.Glass,
        CostMaterialCategory.Hardware => MaterialCostType.Hardware,
        CostMaterialCategory.Profile => MaterialCostType.Profile,
        _ => MaterialCostType.Consumables
    };

    private void AddCostLine(
        string name,
        MaterialCostType type,
        decimal total,
        string unit,
        decimal quantity = 1m)
    {
        if (total <= 0)
        {
            return;
        }

        var safeQuantity = quantity <= 0 ? 1m : quantity;
        Items.Add(new CostItemViewModel
        {
            MaterialName = name,
            MaterialType = type,
            Quantity = safeQuantity,
            Unit = unit,
            UnitPrice = decimal.Round(total / safeQuantity, 2, MidpointRounding.AwayFromZero)
        });
    }

    private static MaterialCostType MapMaterialType(string name) => name switch
    {
        var value when value.Contains("Стекл", StringComparison.OrdinalIgnoreCase) => MaterialCostType.Glass,
        var value when value.Contains("Фурн", StringComparison.OrdinalIgnoreCase) => MaterialCostType.Hardware,
        var value when value.Contains("Проф", StringComparison.OrdinalIgnoreCase) => MaterialCostType.Profile,
        _ => MaterialCostType.Consumables
    };

    private static decimal ParseQuantity(string quantityText)
    {
        var match = Regex.Match(quantityText, @"[\d,\.]+");
        return match.Success && decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 1m;
    }

    private static string ExtractUnit(string quantityText)
    {
        var parts = quantityText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : "шт.";
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
