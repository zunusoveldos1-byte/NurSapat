namespace NurSapat.App.Models.Settings;

public sealed class ConfigurationProfile
{
    public static readonly Guid AdminProfileId = Guid.Parse("B1000001-0000-4000-8000-000000000001");

    public const string AdminProfileName = "Admin";

    public const string AdminConfigFileName = "env_settings_admin.json";

    public Guid Id { get; set; } = Guid.NewGuid();

    public string ProfileName { get; set; } = string.Empty;

    public string ConfigFileName { get; set; } = string.Empty;

    public DateTime LastActive { get; set; }

    public bool IsAdminProfile() =>
        Id == AdminProfileId ||
        string.Equals(ProfileName, AdminProfileName, StringComparison.OrdinalIgnoreCase);

    public ConfigurationProfile Clone() => new()
    {
        Id = Id,
        ProfileName = ProfileName,
        ConfigFileName = ConfigFileName,
        LastActive = LastActive
    };

    public static ConfigurationProfile CreateAdmin(DateTime? lastActive = null) => new()
    {
        Id = AdminProfileId,
        ProfileName = AdminProfileName,
        ConfigFileName = AdminConfigFileName,
        LastActive = lastActive ?? DateTime.Now
    };
}
