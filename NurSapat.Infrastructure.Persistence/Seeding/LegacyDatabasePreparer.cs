using Microsoft.EntityFrameworkCore;

namespace NurSapat.Infrastructure.Persistence.Seeding;

/// <summary>
/// Detects the old NurSapat.Data (Dapper) schema that conflicts with EF migrations
/// (lowercase users/materials tables) and moves it aside before MigrateAsync.
/// </summary>
internal static class LegacyDatabasePreparer
{
    public static async Task PrepareAsync(NurSapatDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Old Dapper users table: id/username/passwordhash/role (no GroupNumber / PermissionsJson).
        if (await IsLegacyUsersTableAsync(dbContext, cancellationToken))
        {
            await RenameIfExistsAsync(dbContext, "users", "legacy_users", cancellationToken);
        }

        // Old materials table shape differs from EF Erp materials.
        if (await IsLegacyMaterialsTableAsync(dbContext, cancellationToken))
        {
            await RenameIfExistsAsync(dbContext, "materials", "legacy_materials", cancellationToken);
        }

        await RenameIfExistsAsync(dbContext, "clients", "legacy_clients", cancellationToken);
        await RenameIfExistsAsync(dbContext, "projects", "legacy_projects", cancellationToken);
    }

    public static async Task ImportLegacyUsersAsync(NurSapatDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(dbContext, "legacy_users", cancellationToken))
        {
            return;
        }

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        // EF migrations create PascalCase quoted columns for users.
        // ExecuteSqlRawAsync uses string.Format — double {{ }} so JSON braces are literal.
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO users ("Id", "Username", "PasswordHash", "GroupNumber", "PermissionsJson")
            SELECT
                id,
                username,
                passwordhash,
                CASE WHEN lower(coalesce(role, '')) = 'admin' THEN 1 ELSE 2 END,
                CASE
                    WHEN lower(coalesce(role, '')) = 'admin' THEN
                        '{{"canManageUsers":true,"canAccessMaterialsDictionary":true,"canConfigureConstructionElements":true,"canAccessEconomics":true,"canEditOrderSheetCoefficients":true,"canEditOrderSheetCostCoefficients":true,"canEditOrderSheetExtraExpenseCoefficients":true,"canPrintMaterialPrices":true,"canDeleteOrders":true,"canAccessWarehouse":true,"canEditWarehouse":true,"canEditWarehouseInvoice":true,"groupOnlyDataAccess":false}}'
                    ELSE
                        '{{"canAccessMaterialsDictionary":true,"canAccessWarehouse":true,"groupOnlyDataAccess":true}}'
                END::jsonb
            FROM legacy_users
            ON CONFLICT ("Id") DO NOTHING;
            """,
            cancellationToken);
    }

    private static async Task<bool> IsLegacyUsersTableAsync(NurSapatDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "users", cancellationToken))
        {
            return false;
        }

        // Legacy has "role"; EF users have "PermissionsJson" (or "permissionsjson" depending on provider).
        var hasRole = await ColumnExistsAsync(dbContext, "users", "role", cancellationToken);
        var hasPermissions =
            await ColumnExistsAsync(dbContext, "users", "PermissionsJson", cancellationToken)
            || await ColumnExistsAsync(dbContext, "users", "permissionsjson", cancellationToken);

        return hasRole && !hasPermissions;
    }

    private static async Task<bool> IsLegacyMaterialsTableAsync(NurSapatDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, "materials", cancellationToken))
        {
            return false;
        }

        // Legacy materials have unit_of_measure; EF materials have different pricing tables.
        return await ColumnExistsAsync(dbContext, "materials", "unit_of_measure", cancellationToken);
    }

    private static async Task RenameIfExistsAsync(
        NurSapatDbContext dbContext,
        string from,
        string to,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(dbContext, from, cancellationToken))
        {
            return;
        }

        if (await TableExistsAsync(dbContext, to, cancellationToken))
        {
            return;
        }

        // Unquoted identifiers — PostgreSQL folds to lowercase matching legacy tables.
        await dbContext.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE {from} RENAME TO {to};",
            cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        NurSapatDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = @name
            );
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToBoolean(result);
    }

    private static async Task<bool> ColumnExistsAsync(
        NurSapatDbContext dbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = @table
                  AND column_name = @column
            );
            """;
        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@table";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@column";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToBoolean(result);
    }
}
