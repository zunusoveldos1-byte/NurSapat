using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NurSapat.App.Views;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
    }

    private void OnTermsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button link && TopLevel.GetTopLevel(this) is MainWindow window)
        {
            window.ShowRegistrationTerms(link);
        }
    }
}
