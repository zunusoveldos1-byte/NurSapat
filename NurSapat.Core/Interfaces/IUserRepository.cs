using System.Threading.Tasks;
using NurSapat.Core.Models;

namespace NurSapat.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task CreateAsync(User user);
}