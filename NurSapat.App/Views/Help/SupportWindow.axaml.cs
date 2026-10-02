using Avalonia.Markup.Xaml;

using NurSapat.App.ViewModels.Help;



namespace NurSapat.App.Views.Help;



public partial class SupportWindow : HelpDialogWindowBase

{

    public SupportWindow()

    {

        InitializeComponent();

    }



    public SupportWindow(SupportViewModel viewModel)

        : this()

    {

        AttachViewModel(viewModel);

    }



    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

}

