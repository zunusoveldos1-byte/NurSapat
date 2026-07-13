using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.ViewModels.Materials;
using NurSapat.Core.Models;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<NavigationItem> _menuItems;

    [ObservableProperty]
    private object? _selectedContent;

    [ObservableProperty]
    private NavigationItem? _selectedMenuItem;

    public DashboardViewModel(
        CustomersDictionaryViewModel customersViewModel,
        MaterialsDictionaryViewModel materialsViewModel,
        OrdersDictionaryViewModel ordersViewModel,
        OrderDrawingViewModel orderDrawingViewModel,
        SystemLogsViewModel systemLogsViewModel)
    {
        customersViewModel.ShowCloseButton = false;
        ordersViewModel.ShowCloseButton = false;

        MenuItems =
        [
            new() { Title = "Клиенты", Content = customersViewModel },
            new() { Title = "Материалы", Content = materialsViewModel },
            new() { Title = "Заказы", Content = ordersViewModel },
            new() { Title = "Конструктор", Content = orderDrawingViewModel },
            new() { Title = "Реестр дел", Content = systemLogsViewModel }
        ];

        SelectedMenuItem = MenuItems[0];
        SelectedContent = SelectedMenuItem.Content;
        _ = InitializeCurrentSectionAsync();
    }

    partial void OnSelectedMenuItemChanged(NavigationItem? value)
    {
        if (value?.Content is not { } content)
        {
            return;
        }

        SelectedContent = content;
        _ = InitializeSectionAsync(content);
    }

    private Task InitializeCurrentSectionAsync() =>
        SelectedContent is null ? Task.CompletedTask : InitializeSectionAsync(SelectedContent);

    private static async Task InitializeSectionAsync(object content)
    {
        switch (content)
        {
            case CustomersDictionaryViewModel customers:
                await customers.InitializeAsync();
                break;
            case MaterialsDictionaryViewModel materials:
                await materials.InitializeAsync();
                break;
            case OrdersDictionaryViewModel orders:
                await orders.InitializeAsync();
                break;
            case SystemLogsViewModel logs:
                await logs.LoadCommand.ExecuteAsync(null);
                break;
        }
    }
}
