using Dapper;
using Npgsql;
using NurSapat.Data.Legacy;

namespace NurSapat.Data.Repositories;

/// <summary>Legacy Dapper-репозиторий таблицы PostgreSQL <c>materials</c> (commercial DB).</summary>
public sealed class LegacyMaterialRepository : ILegacyMaterialRepository
{
    private readonly DatabaseService _db;

    public LegacyMaterialRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LegacyMaterial>> GetAllAsync()
    {
        await using var conn = _db.DataSource.CreateConnection();
        const string sql = "SELECT id AS Id, article AS Article, name AS Name, category AS Category, unit_of_measure AS UnitOfMeasure, price AS Price, color_id AS ColorId FROM materials ORDER BY name";
        return (await conn.QueryAsync<LegacyMaterial>(sql)).AsList();
    }

    public async Task<LegacyMaterial?> GetByIdAsync(Guid id)
    {
        await using var conn = _db.DataSource.CreateConnection();
        const string sql = "SELECT id AS Id, article AS Article, name AS Name, category AS Category, unit_of_measure AS UnitOfMeasure, price AS Price, color_id AS ColorId FROM materials WHERE id = @Id";
        return await conn.QueryFirstOrDefaultAsync<LegacyMaterial>(sql, new { Id = id });
    }

    public async Task AddAsync(LegacyMaterial material)
    {
        await using var conn = _db.DataSource.CreateConnection();
        const string sql = @"
            INSERT INTO materials (id, article, name, category, unit_of_measure, price, color_id)
            VALUES (@Id, @Article, @Name, @Category, @UnitOfMeasure, @Price, @ColorId)";
        await conn.ExecuteAsync(sql, material);
    }

    public async Task UpdateAsync(LegacyMaterial material)
    {
        await using var conn = _db.DataSource.CreateConnection();
        const string sql = @"
            UPDATE materials SET article = @Article, name = @Name, category = @Category, unit_of_measure = @UnitOfMeasure, price = @Price, color_id = @ColorId
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, material);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var conn = _db.DataSource.CreateConnection();
        const string sql = "DELETE FROM materials WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
