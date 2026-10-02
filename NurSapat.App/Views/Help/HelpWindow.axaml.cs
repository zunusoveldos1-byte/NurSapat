using Avalonia.Markup.Xaml;

using NurSapat.App.ViewModels.Help;



namespace NurSapat.App.Views.Help;



public partial class HelpWindow : HelpDialogWindowBase

{

    public HelpWindow()

    {

        InitializeComponent();

    }



    public HelpWindow(HelpGuideViewModel viewModel)

        : this()

    {

        AttachViewModel(viewModel);

    }



    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

}

