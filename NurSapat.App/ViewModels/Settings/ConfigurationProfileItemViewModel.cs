using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Settings;

namespace NurSapat.App.ViewModels.Settings;

public sealed partial class ConfigurationProfileItemViewModel : ObservableObject
{
    public ConfigurationProfileItemViewModel(ConfigurationProfile profile, bool isActive)
    {
        Profile = profile;
        IsActive = isActive;
    }

    public ConfigurationProfile Profile { get; }

    public bool IsActive { get; }

    public string ProfileName => Profile.ProfileName;

    public string LastActiveDisplay =>
        Profile.LastActive == default
            ? "Активность не зафиксирована"
            : $"Последний вход: {Profile.LastActive:dd.MM.yyyy HH:mm}";

    public bool CanDelete => !Profile.IsAdminProfile() && !IsActive;
}
