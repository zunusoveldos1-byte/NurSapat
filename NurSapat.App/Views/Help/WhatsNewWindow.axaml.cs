using Avalonia.Markup.Xaml;

using NurSapat.App.ViewModels.Help;



namespace NurSapat.App.Views.Help;



public partial class WhatsNewWindow : HelpDialogWindowBase

{

    public WhatsNewWindow()

    {

        InitializeComponent();

    }



    public WhatsNewWindow(WhatsNewViewModel viewModel)

        : this()

    {

        AttachViewModel(viewModel);

    }



    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

}

