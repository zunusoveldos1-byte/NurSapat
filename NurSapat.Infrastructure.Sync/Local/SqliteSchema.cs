using Microsoft.Data.Sqlite;

namespace NurSapat.Infrastructure.Sync.Local;

internal static class SqliteSchema
{
    public const string MaterialsTable = "materials";
    public const string OrdersTable = "orders";

    public static async Task EnsureCreatedAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS materials (
                id TEXT NOT NULL PRIMARY KEY,
                article TEXT NOT NULL DEFAULT '',
                name TEXT NOT NULL DEFAULT '',
                note TEXT NOT NULL DEFAULT '',
                manufacturer TEXT NOT NULL DEFAULT '',
                section INTEGER NOT NULL DEFAULT 0,
                main_unit INTEGER NOT NULL DEFAULT 0,
                linear_group INTEGER NULL,
                square_group INTEGER NULL,
                currency INTEGER NOT NULL DEFAULT 0,
                base_price REAL NOT NULL DEFAULT 0,
                waste_percent REAL NOT NULL DEFAULT 0,
                print_on_assembly_drawing INTEGER NOT NULL DEFAULT 0,
                print_on_commercial_offer INTEGER NOT NULL DEFAULT 0,
                print_on_shipping_sheet INTEGER NOT NULL DEFAULT 0,
                group_print_positions INTEGER NOT NULL DEFAULT 0,
                is_warm_profile INTEGER NOT NULL DEFAULT 0,
                inner_coating_area_per_meter REAL NOT NULL DEFAULT 0,
                outer_coating_area_per_meter REAL NOT NULL DEFAULT 0,
                enable_length_cut_optimization INTEGER NOT NULL DEFAULT 0,
                bar_length_mm REAL NOT NULL DEFAULT 0,
                illiquid_threshold_mm REAL NOT NULL DEFAULT 0,
                scrap_mm REAL NOT NULL DEFAULT 0,
                enable_plane_cut_optimization INTEGER NOT NULL DEFAULT 0,
                sheet_width_mm REAL NOT NULL DEFAULT 0,
                sheet_height_mm REAL NOT NULL DEFAULT 0,
                glass_tint REAL NOT NULL DEFAULT 0,
                minimum_calculated_area_sq_m REAL NOT NULL DEFAULT 0,
                use_secondary_unit INTEGER NOT NULL DEFAULT 0,
                secondary_unit_coefficient REAL NOT NULL DEFAULT 0,
                price_by_kilogram INTEGER NOT NULL DEFAULT 0,
                prices_json TEXT NOT NULL DEFAULT '[]',
                user_id TEXT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                sync_status INTEGER NOT NULL DEFAULT 0,
                updated_at TEXT NOT NULL,
                last_sync_error TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_materials_sync_status ON materials(sync_status);
            CREATE INDEX IF NOT EXISTS ix_materials_user_id ON materials(user_id);

            CREATE TABLE IF NOT EXISTS orders (
                id TEXT NOT NULL PRIMARY KEY,
                profitability_percent REAL NOT NULL DEFAULT 0,
                order_number TEXT NOT NULL DEFAULT '',
                order_date TEXT NOT NULL,
                status INTEGER NOT NULL DEFAULT 0,
                customer_id TEXT NULL,
                dealer_id TEXT NULL,
                current_price_mode INTEGER NOT NULL DEFAULT 0,
                total_discount_percent REAL NOT NULL DEFAULT 0,
                total_discount_sum REAL NOT NULL DEFAULT 0,
                customer_name TEXT NOT NULL DEFAULT '',
                customer_phone TEXT NOT NULL DEFAULT '',
                site_address TEXT NOT NULL DEFAULT '',
                dealer_name TEXT NOT NULL DEFAULT '',
                order_manager_name TEXT NOT NULL DEFAULT '',
                customer_city TEXT NOT NULL DEFAULT '',
                customer_code TEXT NOT NULL DEFAULT '',
                contract_number TEXT NOT NULL DEFAULT '',
                contract_date TEXT NULL,
                production_date TEXT NULL,
                delivery_date TEXT NULL,
                installation_date TEXT NULL,
                has_delivery INTEGER NOT NULL DEFAULT 0,
                has_installation INTEGER NOT NULL DEFAULT 0,
                profile_note TEXT NOT NULL DEFAULT '',
                production_note TEXT NOT NULL DEFAULT '',
                note TEXT NOT NULL DEFAULT '',
                currency_code TEXT NOT NULL DEFAULT 'KGS',
                constructions_subtotal REAL NOT NULL DEFAULT 0,
                discount2_percent REAL NOT NULL DEFAULT 0,
                delivery_cost REAL NOT NULL DEFAULT 0,
                additional_materials_cost REAL NOT NULL DEFAULT 0,
                additional_materials_installation_cost REAL NOT NULL DEFAULT 0,
                vat_percent REAL NOT NULL DEFAULT 0,
                vat_mode INTEGER NOT NULL DEFAULT 0,
                agreed_amount REAL NOT NULL DEFAULT 0,
                paid_amount REAL NOT NULL DEFAULT 0,
                payment_currency_code TEXT NOT NULL DEFAULT 'KGS',
                selected_construction_id TEXT NULL,
                export_file_path TEXT NULL,
                constructions_json TEXT NOT NULL DEFAULT '[]',
                user_id TEXT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                sync_status INTEGER NOT NULL DEFAULT 0,
                updated_at TEXT NOT NULL,
                last_sync_error TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_orders_sync_status ON orders(sync_status);
            CREATE INDEX IF NOT EXISTS ix_orders_user_id ON orders(user_id);

            CREATE TABLE IF NOT EXISTS cached_users (
                id TEXT NOT NULL PRIMARY KEY,
                email TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                last_login_at TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_cached_users_email
                ON cached_users(email COLLATE NOCASE);

            CREATE TABLE IF NOT EXISTS github_releases (
                id TEXT NOT NULL PRIMARY KEY,
                tag_name TEXT NOT NULL,
                name TEXT NOT NULL DEFAULT '',
                body TEXT NOT NULL DEFAULT '',
                published_at TEXT NOT NULL,
                fetched_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_github_releases_published_at
                ON github_releases(published_at);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
