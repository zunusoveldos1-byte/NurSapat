using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NurSapat.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NurSapatDbContext))]
[Migration("20261121010300_EnsureSystemLogs")]
public sealed class EnsureSystemLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.system_logs (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                timestamp timestamptz NOT NULL DEFAULT now(),
                "user" varchar(150) NOT NULL,
                operation_type varchar(80) NOT NULL,
                action_details text NOT NULL
            );

            DO $migration$
            BEGIN
                IF EXISTS (
                    SELECT FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'system_logs'
                    AND column_name = 'operator_name'
                ) AND NOT EXISTS (
                    SELECT FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'system_logs'
                    AND column_name = 'user'
                ) THEN
                    ALTER TABLE public.system_logs RENAME COLUMN operator_name TO "user";
                END IF;
            END;
            $migration$;

            CREATE INDEX IF NOT EXISTS ix_system_logs_timestamp ON public.system_logs(timestamp DESC);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The table may predate EF migrations. Preserve its audit records on rollback.
    }
}
