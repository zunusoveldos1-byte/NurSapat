using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Documents;

namespace NurSapat.App.ViewModels;

public sealed class RecentProjectMenuItem
{
    private readonly IDocumentManager _documentManager;

    public string DisplayName { get; }

    public string FilePath { get; }

    public IAsyncRelayCommand OpenCommand { get; }

    public RecentProjectMenuItem(RecentFileEntry entry, IDocumentManager documentManager)
    {
        _documentManager = documentManager;
        DisplayName = entry.DisplayName;
        FilePath = entry.FilePath;
        OpenCommand = new AsyncRelayCommand(OpenAsync);
    }

    private Task OpenAsync() => _documentManager.OpenOrderAsync(FilePath);
}
