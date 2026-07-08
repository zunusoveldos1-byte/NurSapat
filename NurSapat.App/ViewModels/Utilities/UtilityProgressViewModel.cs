using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.App.ViewModels.Utilities;

public sealed partial class UtilityProgressViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "Выполняется оптимизация и сжатие таблиц базы данных...";

    [ObservableProperty]
    private bool _isIndeterminate = true;

    public event Action? RequestClose;

    public void Configure(Func<CancellationToken, Task> operation)
    {
        _ = RunAsync(operation);
    }

    private async Task RunAsync(Func<CancellationToken, Task> operation)
    {
        try
        {
            await operation(CancellationToken.None);
        }
        finally
        {
            RequestClose?.Invoke();
        }
    }
}
