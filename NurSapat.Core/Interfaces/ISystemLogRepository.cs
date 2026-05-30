using NurSapat.Core.Models;

namespace NurSapat.Core.Interfaces;

public interface ISystemLogRepository
{
    Task<IReadOnlyList<SystemLogEntry>> GetLatestAsync(int limit = 200);

    Task AddAsync(SystemLogEntry entry);
}
