using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Help;

namespace NurSapat.App.ViewModels.Help;

public sealed partial class HelpGuideViewModel : HelpDialogViewModelBase
{
    private readonly IUserGuideService _userGuideService;

    public HelpGuideViewModel(IUserGuideService userGuideService)
    {
        _userGuideService = userGuideService;
        OpenPdfCommand = new RelayCommand(OpenPdf, () => IsGuideAvailable);
        LoadGuide();
    }

    public IRelayCommand OpenPdfCommand { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNotFoundPanel))]
    [NotifyPropertyChangedFor(nameof(ShowFallbackPanel))]
    private bool _isGuideAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFallbackPanel))]
    private bool _hasRenderedPages;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool ShowNotFoundPanel => !IsGuideAvailable;

    public bool ShowFallbackPanel => IsGuideAvailable && !HasRenderedPages;

    public IReadOnlyList<Bitmap> GuidePages { get; private set; } = [];

    private void LoadGuide()
    {
        IsGuideAvailable = _userGuideService.GuideExists;

        if (!IsGuideAvailable)
        {
            StatusMessage = "Руководство пользователя не найдено.";
            GuidePages = [];
            HasRenderedPages = false;
            OpenPdfCommand.NotifyCanExecuteChanged();
            return;
        }

        GuidePages = _userGuideService.LoadPreviewPages();
        HasRenderedPages = GuidePages.Count > 0;
        OnPropertyChanged(nameof(GuidePages));
        StatusMessage = HasRenderedPages
            ? $"Руководство пользователя — {_userGuideService.GuideFilePath}"
            : "Не удалось отобразить PDF. Откройте файл во внешнем просмотрщике.";
        OpenPdfCommand.NotifyCanExecuteChanged();
    }

    private void OpenPdf() => _userGuideService.OpenInDefaultViewer();
}
