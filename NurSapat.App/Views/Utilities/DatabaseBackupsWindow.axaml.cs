using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NurSapat.App.Views.Utilities;

public partial class DatabaseBackupsWindow : Window
{
    public DatabaseBackupsWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
