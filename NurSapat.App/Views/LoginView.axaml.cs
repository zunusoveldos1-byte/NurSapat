using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels;

namespace NurSapat.App.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _viewModel;

    public LoginView()
    {
        InitializeComponent();
        PasswordToggle.GettingFocus += OnPasswordToggleGettingFocus;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is LoginViewModel vm)
        {
            _viewModel = vm;
            vm.RequestClose += OnRequestClose;
            vm.LoadSavedCredentials();
            vm.Activate();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_viewModel is { } vm)
        {
            vm.RequestClose -= OnRequestClose;
            vm.Deactivate();
            _viewModel = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPasswordToggleGettingFocus(object? sender, FocusChangingEventArgs e)
    {
        if (e.NavigationMethod == NavigationMethod.Pointer && e.OldFocusedElement == PasswordField)
        {
            e.TryCancel();
        }
    }

    private async void OnContactClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not { } vm || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var dialog = new SupportWindow
        {
            DataContext = vm,
            RequestedThemeVariant = owner.ActualThemeVariant
        };
        if (owner.Screens.ScreenFromWindow(owner) is { } screen)
        {
            dialog.MaxHeight = screen.WorkingArea.Height / screen.Scaling;
        }

        await dialog.ShowDialog(owner);
        if (owner.IsVisible && IsEffectivelyVisible)
        {
            ContactButton.Focus();
        }
    }

    private void OnRequestClose()
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }
}
