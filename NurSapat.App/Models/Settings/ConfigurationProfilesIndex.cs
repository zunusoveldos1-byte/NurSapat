namespace NurSapat.App.Models.Settings;

internal sealed class ConfigurationProfilesIndex
{
    public Guid ActiveProfileId { get; set; } = ConfigurationProfile.AdminProfileId;

    public List<ConfigurationProfile> Profiles { get; set; } = [];
}
