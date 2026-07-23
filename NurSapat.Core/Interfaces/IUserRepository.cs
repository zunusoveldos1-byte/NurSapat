using System.Threading.Tasks;
using NurSapat.Core.Models;

namespace NurSapat.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);

    Task<User?> GetByIdAsync(Guid id);

    Task CreateAsync(User user);

    Task UpdatePasswordAsync(Guid id, string passwordHash);
}