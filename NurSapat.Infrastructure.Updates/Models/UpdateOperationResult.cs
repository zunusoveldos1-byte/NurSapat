namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateOperationResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Data { get; init; }

    public static UpdateOperationResult Ok(string message = "", string? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static UpdateOperationResult Fail(string message) =>
        new() { Success = false, Message = message };
}
