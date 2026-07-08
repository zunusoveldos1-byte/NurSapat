using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Npgsql;
using NurSapat.Core.Models;

namespace NurSapat.Data;

public class DatabaseService
{
    private readonly string _connectionString;
    public NpgsqlDataSource DataSource { get; } // ДОБАВЛЕНО
    public string ConnectionString => _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<MaterialCategory>("material_category");
        DataSource = dataSourceBuilder.Build();
    }

    public NpgsqlConnection CreateConnection() => DataSource.CreateConnection();

    public async Task EnsureDatabaseAsync()
    {
        if (await IsDatabaseEmptyAsync())
        {
            await ExecuteEmbeddedSchemaAsync();
        }
    }

    private async Task<bool> IsDatabaseEmptyAsync()
    {
        const string checkSql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = 'users'
            );";

        await using var connection = DataSource.CreateConnection();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(checkSql, connection);
        var result = await command.ExecuteScalarAsync();
        return result is bool exists && !exists;
    }

    private async Task ExecuteEmbeddedSchemaAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = "NurSapat.Data.Migrations.InitialSchema.sql";

        string sqlScript;
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                string available = string.Join(", ", assembly.GetManifestResourceNames());
                throw new FileNotFoundException(
                    $"Ресурс '{resourceName}' не найден. Доступные ресурсы: {available}");
            }
            using (StreamReader reader = new StreamReader(stream))
            {
                sqlScript = await reader.ReadToEndAsync();
            }
        }

        await using var connection = DataSource.CreateConnection();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sqlScript, connection);
        await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine("База данных успешно инициализирована из встроенного ресурса.");
    }
}