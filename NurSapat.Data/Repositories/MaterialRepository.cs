using Dapper;
using Npgsql;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NurSapat.Data.Repositories;

public sealed class MaterialRepository : IMaterialRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public MaterialRepository(DatabaseService db)
    {
        _dataSource = db.DataSource; // ИСПРАВЛЕНИЕ: Используем сконфигурированный DataSource
    }

    public async Task<IReadOnlyList<Material>> GetAllAsync()
    {
        await using var conn = _dataSource.CreateConnection();
        const string sql = "SELECT id AS Id, article AS Article, name AS Name, category AS Category, unit_of_measure AS UnitOfMeasure, price AS Price, color_id AS ColorId FROM materials ORDER BY name";
        return (await conn.QueryAsync<Material>(sql)).AsList();
    }

    public async Task<Material?> GetByIdAsync(Guid id)
    {
        await using var conn = _dataSource.CreateConnection();
        const string sql = "SELECT id AS Id, article AS Article, name AS Name, category AS Category, unit_of_measure AS UnitOfMeasure, price AS Price, color_id AS ColorId FROM materials WHERE id = @Id";
        return await conn.QueryFirstOrDefaultAsync<Material>(sql, new { Id = id });
    }

    public async Task AddAsync(Material material)
    {
        await using var conn = _dataSource.CreateConnection();
        const string sql = @"
            INSERT INTO materials (id, article, name, category, unit_of_measure, price, color_id)
            VALUES (@Id, @Article, @Name, @Category, @UnitOfMeasure, @Price, @ColorId)";
        await conn.ExecuteAsync(sql, material);
    }

    public async Task UpdateAsync(Material material)
    {
        await using var conn = _dataSource.CreateConnection();
        const string sql = @"
            UPDATE materials SET article = @Article, name = @Name, category = @Category, unit_of_measure = @UnitOfMeasure, price = @Price, color_id = @ColorId
            WHERE id = @Id";
        await conn.ExecuteAsync(sql, material);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var conn = _dataSource.CreateConnection();
        const string sql = "DELETE FROM materials WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}