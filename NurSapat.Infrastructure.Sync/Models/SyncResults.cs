namespace NurSapat.Infrastructure.Sync.Models;

public sealed class AuthSessionInfo
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed class AuthResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public AuthSessionInfo? Session { get; init; }
    public bool IsOffline { get; init; }
    public bool TimeTamperingDetected { get; init; }

    public static AuthResult Ok(AuthSessionInfo session, bool offline = false) =>
        new() { Success = true, Session = session, IsOffline = offline };

    public static AuthResult Fail(
        string message,
        bool offline = false,
        bool timeTamperingDetected = false) =>
        new()
        {
            Success = false,
            ErrorMessage = message,
            IsOffline = offline,
            TimeTamperingDetected = timeTamperingDetected
        };
}

public sealed class SyncCycleResult
{
    public bool NetworkAvailable { get; init; }
    public int PushedMaterials { get; init; }
    public int PushedOrders { get; init; }
    public int FailedItems { get; init; }
    public string? ErrorMessage { get; init; }
}
