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
        var sql = "SELECT * FROM users WHERE username = @username ORDER BY id DESC LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { username });
    }

    public async Task CreateAsync(User user)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO users (id, username, passwordhash, role) 
              VALUES (@Id, @Username, @PasswordHash, @Role)", user);
    }
}