using Avalonia.Markup.Xaml;

using NurSapat.App.ViewModels.Help;



namespace NurSapat.App.Views.Help;



public partial class AboutWindow : HelpDialogWindowBase

{

    public AboutWindow()

    {

        InitializeComponent();

    }



    public AboutWindow(AboutViewModel viewModel)

        : this()

    {

        AttachViewModel(viewModel);

    }



    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

}

