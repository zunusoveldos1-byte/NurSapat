using Avalonia;
using Avalonia.Controls;
using NurSapat.App.ViewModels;

namespace NurSapat.App.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is LoginViewModel vm)
        {
            vm.LoadSavedCredentials();
        }
    }
}