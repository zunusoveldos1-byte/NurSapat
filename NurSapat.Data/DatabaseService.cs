using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Npgsql;

namespace NurSapat.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    /// <summary>
    /// Проверяет, есть ли таблицы в БД, и если нет – выполняет InitialSchema.sql из ресурсов.
    /// </summary>
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

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(checkSql, connection);
        var result = await command.ExecuteScalarAsync();
        return result is bool exists && !exists;
    }

    private async Task ExecuteEmbeddedSchemaAsync()
    {
        // Получаем сборку, в которой находится этот класс (NurSapat.Data)
        var assembly = Assembly.GetExecutingAssembly();

        // Имя ресурса формируется как: "ПространствоИмён.Папка.ИмяФайла"
        // Убедитесь, что папка называется Migrations, а файл – InitialSchema.sql
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

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(sqlScript, connection);
        await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine("База данных успешно инициализирована из встроенного ресурса.");
    }
}