using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Options;
using Supabase;

namespace NurSapat.Infrastructure.Sync.Auth;

public sealed class SupabaseClientFactory
{
    private readonly SyncOptions _options;
    private readonly FileSessionPersistence _sessionPersistence;
    private readonly ILogger<SupabaseClientFactory>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Client? _client;

    public SupabaseClientFactory(
        IOptions<SyncOptions> options,
        FileSessionPersistence sessionPersistence,
        ILogger<SupabaseClientFactory>? logger = null)
    {
        _options = options.Value;
        _sessionPersistence = sessionPersistence;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<Client> GetClientAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Sync:SupabaseUrl / Sync:SupabaseAnonKey are not configured in appsettings.json.");
        }

        if (_client is not null)
        {
            return _client;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            var supabaseOptions = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false,
                SessionHandler = _sessionPersistence
            };

            var client = new Client(_options.SupabaseUrl.TrimEnd('/'), _options.SupabaseAnonKey, supabaseOptions);
            await client.InitializeAsync().ConfigureAwait(false);
            _client = client;
            _logger?.LogInformation("Supabase client initialized for {Url}", _options.SupabaseUrl);
            return _client;
        }
        finally
        {
            _gate.Release();
        }
    }
}
