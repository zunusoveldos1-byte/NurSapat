# NurSapat PostgreSQL Runbook

## Prerequisites

- .NET 8 SDK.
- PostgreSQL client tools (`psql`, `pg_dump`, `pg_restore`) in `PATH`.
- PostgreSQL server reachable through `ConnectionStrings:PostgreSQL`.
- EF CLI installed:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
```

Do not commit production credentials. Override the checked-in development connection string:

```powershell
$env:ConnectionStrings__PostgreSQL = "Host=localhost;Port=5432;Database=nursapat_db;Username=postgres;Password=<password>"
```

Run all commands from the repository root.

## Create a migration

```powershell
dotnet ef migrations add <MigrationName> `
  --project .\NurSapat.Infrastructure.Persistence\NurSapat.Infrastructure.Persistence.csproj `
  --startup-project .\NurSapat.App\NurSapat.App.csproj `
  --context NurSapatDbContext `
  --output-dir Migrations
```

Review both the generated migration and model snapshot before committing.

## Apply migrations

The application applies pending migrations on PostgreSQL startup through
`InitialDataSeeder.SeedAsync()` and `Database.MigrateAsync()`.

For controlled deployment, use the checked-in script:

```powershell
.\tools\update-database.ps1
```

Equivalent direct command:

```powershell
dotnet ef database update `
  --project .\NurSapat.Infrastructure.Persistence\NurSapat.Infrastructure.Persistence.csproj `
  --startup-project .\NurSapat.App\NurSapat.App.csproj `
  --context NurSapatDbContext
```

For production, take a backup first and run the command from a single deployment
instance while application writes are stopped.

## Create a compressed backup

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$env:PGPASSWORD = "<password>"
pg_dump `
  --host localhost `
  --port 5432 `
  --username postgres `
  --dbname nursapat_db `
  --format custom `
  --compress 9 `
  --file ".\backups\nursapat_db-$stamp.dump"
Remove-Item Env:\PGPASSWORD
```

Verify the archive:

```powershell
pg_restore --list ".\backups\nursapat_db-<timestamp>.dump"
```

## Restore

Restore into a new or explicitly approved target database. The commands below
drop and recreate `nursapat_restore`; they must not be pointed at an unverified
production database.

```powershell
$env:PGPASSWORD = "<password>"
dropdb --host localhost --port 5432 --username postgres --if-exists nursapat_restore
createdb --host localhost --port 5432 --username postgres nursapat_restore
pg_restore `
  --host localhost `
  --port 5432 `
  --username postgres `
  --dbname nursapat_restore `
  --clean `
  --if-exists `
  --no-owner `
  --exit-on-error `
  ".\backups\nursapat_db-<timestamp>.dump"
Remove-Item Env:\PGPASSWORD
```

After restore, run:

```powershell
$env:ConnectionStrings__PostgreSQL = "Host=localhost;Port=5432;Database=nursapat_restore;Username=postgres;Password=<password>"
dotnet ef database update `
  --project .\NurSapat.Infrastructure.Persistence\NurSapat.Infrastructure.Persistence.csproj `
  --startup-project .\NurSapat.App\NurSapat.App.csproj `
  --context NurSapatDbContext
```

Validate row counts, the latest order snapshots, and a representative order
round-trip before switching application traffic to the restored database.
