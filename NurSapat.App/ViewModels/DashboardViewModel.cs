using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using NurSapat.Core.Models;

namespace NurSapat.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<NavigationItem> _menuItems;

    [ObservableProperty]
    private object? _selectedContent;

    public DashboardViewModel()
    {
        var customersVM = new PlaceholderViewModel("Клиенты");
        var materialsVM = new PlaceholderViewModel("Материалы");
        var ordersVM = new PlaceholderViewModel("Заказы");
        var constructorVM = new PlaceholderViewModel("Конструктор");

        MenuItems = new ObservableCollection<NavigationItem>
        {
            new NavigationItem { Title = "Клиенты", Content = customersVM },
            new NavigationItem { Title = "Материалы", Content = materialsVM },
            new NavigationItem { Title = "Заказы", Content = ordersVM },
            new NavigationItem { Title = "Конструктор", Content = constructorVM }
        };

        SelectedContent = customersVM;
    }

    [RelayCommand]
    private void SelectMenuItem(NavigationItem item)
    {
        if (item != null)
            SelectedContent = item.Content;
    }
}