using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderConstructionsLookupViewModel : ObservableObject
{
    public const int GalleryColumns = 4;

    private readonly OrderWorkspaceViewModel _workspace;

    public ObservableCollection<object> GalleryItems { get; } = [];

    public int GridRowCount { get; private set; }

    public OrderConstructionsLookupViewModel(OrderWorkspaceViewModel workspace, Order order)
    {
        _workspace = workspace;

        var activeId = order.SelectedConstruction?.Id;
        var items = new List<object>();
        foreach (var construction in order.Constructions.OrderBy(c => c.Number))
        {
            items.Add(new OrderConstructionLookupItemViewModel(
                construction,
                isActive: construction.Id == activeId));
        }

        items.Add(new OrderConstructionLookupAddItemViewModel());
        GridRowCount = ArrangeGallery(items, GalleryColumns);

        foreach (var item in items)
        {
            GalleryItems.Add(item);
        }
    }

    public event Action? CloseRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    [RelayCommand]
    private async Task SelectConstructionAsync(OrderConstructionLookupItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        CloseRequested?.Invoke();
        await _workspace.SwitchToConstructionAsync(item.Id);
    }

    [RelayCommand]
    private async Task AddNewConstructionAsync()
    {
        CloseRequested?.Invoke();
        await _workspace.AddNewConstructionAsync();
    }

    private static int ArrangeGallery(IReadOnlyList<object> items, int columns)
    {
        var column = 0;
        var row = 0;
        var maxRow = 0;

        foreach (var item in items)
        {
            var span = item switch
            {
                OrderConstructionLookupItemViewModel construction => construction.ColumnSpan,
                OrderConstructionLookupAddItemViewModel => 1,
                _ => 1
            };

            if (column + span > columns)
            {
                column = 0;
                row++;
            }

            SetGridPosition(item, row, column);
            maxRow = Math.Max(maxRow, row);

            column += span;
            if (column >= columns)
            {
                column = 0;
                row++;
            }
        }

        return maxRow + 1;
    }

    private static void SetGridPosition(object item, int row, int column)
    {
        switch (item)
        {
            case OrderConstructionLookupItemViewModel construction:
                construction.SetGridPosition(row, column);
                break;
            case OrderConstructionLookupAddItemViewModel add:
                add.SetGridPosition(row, column);
                break;
        }
    }
}
