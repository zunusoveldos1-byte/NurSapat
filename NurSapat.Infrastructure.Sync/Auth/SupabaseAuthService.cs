using Microsoft.Extensions.Logging;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;
using Supabase.Gotrue;

namespace NurSapat.Infrastructure.Sync.Auth;

public sealed class SupabaseAuthService : ISupabaseAuthService
{
    private readonly SupabaseClientFactory _clientFactory;
    private readonly IConnectivityMonitor _connectivity;
    private readonly ILogger<SupabaseAuthService>? _logger;
    private AuthSessionInfo? _current;

    public SupabaseAuthService(
        SupabaseClientFactory clientFactory,
        IConnectivityMonitor connectivity,
        ILogger<SupabaseAuthService>? logger = null)
    {
        _clientFactory = clientFactory;
        _connectivity = connectivity;
        _logger = logger;
    }

    public AuthSessionInfo? CurrentSession => _current;

    public Guid? CurrentUserId => _current?.UserId;

    public bool IsAuthenticated => _current is not null && _current.UserId != Guid.Empty;

    public async Task<AuthResult> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!_clientFactory.IsConfigured)
        {
            return AuthResult.Fail("Supabase is not configured.");
        }

        try
        {
            var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);

            // InitializeAsync already loads persistence; try refresh if online.
            if (await _connectivity.IsOnlineAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await client.Auth.RetrieveSessionAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (IsNetworkFailure(ex))
                {
                    _logger?.LogWarning(ex, "Could not refresh Supabase session online; using cached session.");
                }
            }

            var session = client.Auth.CurrentSession;
            if (session?.User?.Id is null)
            {
                _current = null;
                return AuthResult.Fail("No saved session.", offline: true);
            }

            _current = MapSession(session);
            return AuthResult.Ok(_current);
        }
        catch (Exception ex) when (IsNetworkFailure(ex))
        {
            return AuthResult.Fail("Network unavailable while restoring session.", offline: true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RestoreSession failed.");
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<AuthResult> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Fail("Email and password are required.");
        }

        if (!_clientFactory.IsConfigured)
        {
            return AuthResult.Fail("Supabase is not configured.");
        }

        if (!await _connectivity.IsOnlineAsync(cancellationToken).ConfigureAwait(false))
        {
            // Offline password check is handled by ICachedUserRepository in LoginViewModel.
            return AuthResult.Fail(
                "Нет сети. Будет выполнена попытка входа по локальному кэшу.",
                offline: true);
        }

        try
        {
            var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);
            var session = await client.Auth.SignInWithPassword(email.Trim(), password).ConfigureAwait(false);
            if (session?.User?.Id is null)
            {
                return AuthResult.Fail("Invalid email or password.");
            }

            _current = MapSession(session);
            return AuthResult.Ok(_current);
        }
        catch (Exception ex) when (IsNetworkFailure(ex))
        {
            return AuthResult.Fail("Network error during sign-in. Try again when online.", offline: true);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Sign-in failed for {Email}", email);
            return AuthResult.Fail(SimplifyAuthError(ex.Message));
        }
    }

    public void EstablishLocalSession(AuthSessionInfo session)
    {
        _current = session;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_clientFactory.IsConfigured &&
                await _connectivity.IsOnlineAsync(cancellationToken).ConfigureAwait(false))
            {
                var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);
                await client.Auth.SignOut().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Remote sign-out failed; clearing local session anyway.");
        }
        finally
        {
            _current = null;
        }
    }

    private static AuthSessionInfo MapSession(Session session)
    {
        var userId = Guid.TryParse(session.User!.Id, out var id) ? id : Guid.Empty;
        DateTimeOffset? expires = null;
        var expiresAt = session.ExpiresAt();
        if (expiresAt != default)
        {
            expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc));
        }

        return new AuthSessionInfo
        {
            UserId = userId,
            Email = session.User.Email ?? string.Empty,
            AccessToken = session.AccessToken ?? string.Empty,
            RefreshToken = session.RefreshToken ?? string.Empty,
            ExpiresAt = expires
        };
    }

    private static bool IsNetworkFailure(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or TimeoutException ||
        ex.InnerException is HttpRequestException or TaskCanceledException;

    private static string SimplifyAuthError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Authentication failed.";
        }

        if (message.Contains("Invalid login", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return "Invalid email or password.";
        }

        return message;
    }
}
