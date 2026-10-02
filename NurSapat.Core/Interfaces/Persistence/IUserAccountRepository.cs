using NurSapat.Core.Models.Users;

namespace NurSapat.Core.Interfaces.Persistence;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
