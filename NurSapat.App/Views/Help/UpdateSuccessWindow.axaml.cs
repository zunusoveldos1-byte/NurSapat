using Avalonia.Markup.Xaml;
using NurSapat.App.ViewModels.Help;

namespace NurSapat.App.Views.Help;

public partial class UpdateSuccessWindow : HelpDialogWindowBase
{
    public UpdateSuccessWindow()
    {
        InitializeComponent();
    }

    public UpdateSuccessWindow(UpdateSuccessViewModel viewModel)
        : this()
    {
        AttachViewModel(viewModel);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
