using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NurSapat.App.Views.Dictionaries;

public partial class BaseDictionaryWindow : Window
{
    public BaseDictionaryWindow()
    {
        InitializeComponent();
    }

    public BaseDictionaryWindow(object dataContext, Action<DataGrid>? configureColumns = null)
    {
        InitializeComponent();
        DataContext = dataContext;

        if (configureColumns is null)
        {
            return;
        }

        void OnLoadedHandler(object? sender, RoutedEventArgs e)
        {
            Loaded -= OnLoadedHandler;

            var grid = ItemsDataGrid ?? this.FindControl<DataGrid>("ItemsDataGrid");
            if (grid is null)
            {
                return;
            }

            configureColumns(grid);
        }

        Loaded += OnLoadedHandler;
    }
}
