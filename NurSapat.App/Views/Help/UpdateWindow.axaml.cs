using Avalonia.Markup.Xaml;
using NurSapat.App.ViewModels.Help;

namespace NurSapat.App.Views.Help;

public partial class UpdateWindow : HelpDialogWindowBase
{
    public UpdateWindow()
    {
        InitializeComponent();
    }

    public UpdateWindow(UpdateViewModel viewModel)
        : this()
    {
        AttachViewModel(viewModel);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is UpdateViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
