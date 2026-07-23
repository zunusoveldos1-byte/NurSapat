namespace NurSapat.Infrastructure.Sync.Options;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    /// <summary>Supabase project URL, e.g. https://xxxx.supabase.co</summary>
    public string SupabaseUrl { get; set; } = string.Empty;

    /// <summary>Anon (public) key from Supabase Settings → API.</summary>
    public string SupabaseAnonKey { get; set; } = string.Empty;

    /// <summary>
    /// SQLite file name only (e.g. offline-cache.db).
    /// Resolved at runtime to %AppData%/NurSapat/{SqlitePath}.
    /// </summary>
    public string SqlitePath { get; set; } = "offline-cache.db";

    /// <summary>How often the background synchronizer pushes Pending* rows.</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>When false, DI still registers services but the background loop stays idle.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Remote table name for materials.</summary>
    public string MaterialsTable { get; set; } = "materials";

    /// <summary>Remote table name for orders.</summary>
    public string OrdersTable { get; set; } = "orders";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SupabaseUrl) &&
        !string.IsNullOrWhiteSpace(SupabaseAnonKey);
}
