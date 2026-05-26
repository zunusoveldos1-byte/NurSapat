using System.Threading.Tasks;
using Dapper;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;

namespace NurSapat.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DatabaseService _db;

    public UserRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM users WHERE username = @username", new { username });
    }

    public async Task CreateAsync(User user)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO users (id, username, password_hash, salt, role) 
              VALUES (@Id, @Username, @PasswordHash, @Salt, @Role)", user);
    }
}