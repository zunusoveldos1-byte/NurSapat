using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Core.Models;

namespace NurSapat.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<NavigationItem> _menuItems;

    [ObservableProperty]
    private object? _selectedContent;

    [ObservableProperty]
    private NavigationItem? _selectedMenuItem;

    public DashboardViewModel(SystemLogsViewModel systemLogsViewModel)
    {
        var customersVM = new PlaceholderViewModel("Клиенты");
        var materialsVM = new PlaceholderViewModel("Материалы");
        var ordersVM = new PlaceholderViewModel("Заказы");
        var constructorVM = new PlaceholderViewModel("Конструктор");

        MenuItems = new ObservableCollection<NavigationItem>
        {
            new() { Title = "Клиенты", Content = customersVM },
            new() { Title = "Материалы", Content = materialsVM },
            new() { Title = "Заказы", Content = ordersVM },
            new() { Title = "Конструктор", Content = constructorVM },
            new() { Title = "Реестр дел", Content = systemLogsViewModel }
        };

        SelectedMenuItem = MenuItems[0];
        SelectedContent = SelectedMenuItem.Content;
    }

    partial void OnSelectedMenuItemChanged(NavigationItem? value)
    {
        if (value != null)
        {
            SelectedContent = value.Content;
        }
    }
}
