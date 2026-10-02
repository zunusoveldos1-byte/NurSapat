using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Settings;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Settings;

public partial class ChangeConfigurationWindow : Window
{
    private readonly ChangeConfigurationViewModel? _viewModel;

    public ChangeConfigurationWindow()
    {
        InitializeComponent();
    }

    public ChangeConfigurationWindow(ChangeConfigurationViewModel viewModel)
        : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose() => Close();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(this, e);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnProfileDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.SelectProfileCommand.CanExecute(null) == true)
        {
            _viewModel.SelectProfileCommand.Execute(null);
        }
    }
}
