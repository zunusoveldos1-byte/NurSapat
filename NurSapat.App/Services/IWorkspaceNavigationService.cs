namespace NurSapat.App.Services;

public interface IWorkspaceNavigationService
{
    void Configure(Action<string> navigate);

    void NavigateTo(string screenKey);
}

public sealed class WorkspaceNavigationService : IWorkspaceNavigationService
{
    private Action<string>? _navigate;

    public void Configure(Action<string> navigate) => _navigate = navigate;

    public void NavigateTo(string screenKey) => _navigate?.Invoke(screenKey);
}
