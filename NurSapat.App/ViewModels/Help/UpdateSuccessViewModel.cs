using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Help;

public sealed partial class UpdateSuccessViewModel : HelpDialogViewModelBase
{
    public UpdateSuccessViewModel(string version)
    {
        Version = version;
        RestartCommand = new RelayCommand(() =>
        {
            RestartRequested = true;
            CloseCommand.Execute(null);
        });
        LaterCommand = CloseCommand;
    }

    public string Version { get; }

    public IRelayCommand RestartCommand { get; }

    public IRelayCommand LaterCommand { get; }

    public bool RestartRequested { get; private set; }
}
