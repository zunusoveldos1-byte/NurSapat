using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Services.Help;

namespace NurSapat.App.ViewModels.Help;

public sealed partial class WhatsNewViewModel : HelpDialogViewModelBase
{
    private readonly IReleaseNotesService _releaseNotesService;

    public WhatsNewViewModel(IReleaseNotesService releaseNotesService)
    {
        _releaseNotesService = releaseNotesService;
        _ = LoadAsync();
    }

    public IReadOnlyList<ChangelogEntryViewModel> ChangelogEntries { get; private set; } = [];

    [ObservableProperty]
    private string _statusMessage = "Загрузка истории обновлений…";

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isBusy = true;

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _releaseNotesService.GetReleaseNotesAsync().ConfigureAwait(true);
            ChangelogEntries = result.Versions
                .Select(version => new ChangelogEntryViewModel
                {
                    Version = version.Version,
                    ReleaseDate = version.ReleaseDate,
                    VersionLabel = version.Version.StartsWith("Версия", StringComparison.OrdinalIgnoreCase)
                        ? version.Version
                        : $"Версия {version.Version}",
                    Changes = version.Changes
                })
                .ToList();

            IsEmpty = result.IsEmpty || ChangelogEntries.Count == 0;
            StatusMessage = result.StatusMessage
                ?? (IsEmpty
                    ? "Здесь появится история обновлений при подключении к сети."
                    : string.Empty);
            OnPropertyChanged(nameof(ChangelogEntries));
        }
        catch (Exception ex)
        {
            IsEmpty = true;
            StatusMessage = $"Не удалось загрузить историю обновлений: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
