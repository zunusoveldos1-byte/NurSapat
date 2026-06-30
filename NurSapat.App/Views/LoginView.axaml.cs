using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
            // ������������� �� ������� ������� �������� ����
            vm.RequestClose += OnRequestClose;
            vm.LoadSavedCredentials();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // ������������, ����� �������� ������
        if (DataContext is LoginViewModel vm)
        {
            vm.RequestClose -= OnRequestClose;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnRequestClose()
    {
        // ��������� ����, � ������� ��������� ���� UserControl
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }
}