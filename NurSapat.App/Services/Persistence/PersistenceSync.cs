namespace NurSapat.App.Services.Persistence;

/// <summary>
/// Bridges async EF repositories to sync App stores without UI-thread deadlocks.
/// Avalonia has a SynchronizationContext; blocking with GetResult() on an await that
/// captures that context freezes startup/UI permanently.
/// </summary>
internal static class PersistenceSync
{
    public static void Run(Func<Task> action) =>
        Task.Run(action).GetAwaiter().GetResult();

    public static T Run<T>(Func<Task<T>> action) =>
        Task.Run(action).GetAwaiter().GetResult();
}
