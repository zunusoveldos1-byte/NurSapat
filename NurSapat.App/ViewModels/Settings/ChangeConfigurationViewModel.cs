using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Settings;
using NurSapat.App.Services;
using NurSapat.App.Services.Settings;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.Views.Dialogs;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Settings;

public sealed partial class ChangeConfigurationViewModel : ViewModelBase
{
    private readonly IEnvironmentSettingsService _settingsService;
    private readonly IAppMessageService _messageService;

    public ChangeConfigurationViewModel(
        IEnvironmentSettingsService settingsService,
        IAppMessageService messageService)
    {
        _settingsService = settingsService;
        _messageService = messageService;

        SelectProfileCommand = new RelayCommand(SelectProfile, () => SelectedProfile is not null);
        CreateProfileCommand = new AsyncRelayCommand(CreateProfileAsync);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, () => SelectedProfile?.CanDelete == true);
        CloseCommand = new RelayCommand(CloseWindow);
    }

    public string Title => "Смена текущей конфигурации";

    public ObservableCollection<ConfigurationProfileItemViewModel> Profiles { get; } = [];

    public IRelayCommand SelectProfileCommand { get; }

    public IAsyncRelayCommand CreateProfileCommand { get; }

    public IRelayCommand DeleteProfileCommand { get; }

    public IRelayCommand CloseCommand { get; }

    public Window? OwnerWindow { get; set; }

    public bool ProfileSwitched { get; private set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private ConfigurationProfileItemViewModel? _selectedProfile;

    public Task InitializeAsync()
    {
        ReloadProfiles();
        return Task.CompletedTask;
    }

    partial void OnSelectedProfileChanged(ConfigurationProfileItemViewModel? value)
    {
        SelectProfileCommand.NotifyCanExecuteChanged();
        DeleteProfileCommand.NotifyCanExecuteChanged();
    }

    private void ReloadProfiles()
    {
        var selectedId = SelectedProfile?.Profile.Id ?? _settingsService.ActiveProfile?.Id;
        Profiles.Clear();

        foreach (var profile in _settingsService.LoadAvailableProfiles())
        {
            var isActive = profile.Id == _settingsService.ActiveProfile?.Id;
            Profiles.Add(new ConfigurationProfileItemViewModel(profile, isActive));
        }

        SelectedProfile = Profiles.FirstOrDefault(item => item.Profile.Id == selectedId)
            ?? Profiles.FirstOrDefault(item => item.IsActive)
            ?? Profiles.FirstOrDefault();
    }

    private void SelectProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        try
        {
            _settingsService.SwitchToProfile(SelectedProfile.Profile);
            ProfileSwitched = true;
            _messageService.LogInfo($"Загружена конфигурация «{SelectedProfile.ProfileName}».");
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            _messageService.LogError($"Не удалось переключить конфигурацию: {ex.Message}");
        }
    }

    private async Task CreateProfileAsync()
    {
        if (OwnerWindow is null)
        {
            return;
        }

        var dialog = new CreateProfileDialog(new CreateProfileDialogViewModel());
        var profileName = await dialog.ShowDialog<string?>(OwnerWindow);
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return;
        }

        try
        {
            var created = _settingsService.CreateProfile(profileName, copyFromCurrent: true);
            ReloadProfiles();
            SelectedProfile = Profiles.FirstOrDefault(item => item.Profile.Id == created.Id);
            _messageService.LogInfo($"Создана конфигурация «{created.ProfileName}».");
        }
        catch (Exception ex)
        {
            _messageService.LogError(ex.Message);
        }
    }

    private void DeleteProfile()
    {
        if (SelectedProfile is null || !SelectedProfile.CanDelete)
        {
            return;
        }

        try
        {
            var name = SelectedProfile.ProfileName;
            _settingsService.DeleteProfile(SelectedProfile.Profile);
            ReloadProfiles();
            _messageService.LogInfo($"Конфигурация «{name}» удалена.");
        }
        catch (Exception ex)
        {
            _messageService.LogError(ex.Message);
        }
    }

    private void CloseWindow() => RequestClose?.Invoke();
}
