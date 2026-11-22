param(
    [string]$PostgresBin = 'C:\Program Files\PostgreSQL\16\bin',
    [switch]$PassThru,
    [switch]$ConfigureDirectLaunch
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$localRoot = Join-Path $repoRoot '.postgres-local'
$dataPath = Join-Path $localRoot 'data'
$pgCtl = Join-Path $PostgresBin 'pg_ctl.exe'
$password = $env:NURSAPAT_LOCAL_POSTGRES_PASSWORD
if ([string]::IsNullOrEmpty($password)) { $password = '1234' }
if (!(Test-Path -LiteralPath $pgCtl)) { throw "PostgreSQL not found: $PostgresBin" }
if (!(Test-Path -LiteralPath (Join-Path $dataPath 'PG_VERSION'))) {
    New-Item -ItemType Directory -Force -Path $localRoot | Out-Null
    $passwordFile = Join-Path $localRoot 'password.tmp'
    try {
        [IO.File]::WriteAllText($passwordFile, $password)
        & (Join-Path $PostgresBin 'initdb.exe') -D $dataPath -U admin "--pwfile=$passwordFile" --auth=scram-sha-256 --encoding=UTF8 --locale=C | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL initialization failed.' }
    } finally { Remove-Item -LiteralPath $passwordFile -ErrorAction SilentlyContinue }
}
& $pgCtl -D $dataPath status *> $null
if ($LASTEXITCODE -ne 0) {
    & $pgCtl -D $dataPath -l (Join-Path $localRoot 'server.log') -o '-h 127.0.0.1 -p 5433' -w start | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL did not start. Check .postgres-local/server.log.' }
}
$previousPassword = $env:PGPASSWORD
$previousTimeout = $env:PGCONNECT_TIMEOUT
$previousConnection = $env:ConnectionStrings__PostgreSQL
$previousProvider = $env:Persistence__Provider
$previousConnectionName = $env:Persistence__ConnectionStringName
$createdDatabase = $false
try {
    $env:PGPASSWORD = $password
    $env:PGCONNECT_TIMEOUT = '5'
    $psql = Join-Path $PostgresBin 'psql.exe'
    $psqlOptions = @('-w', '-h', '127.0.0.1', '-p', '5433', '-U', 'admin', '-d', 'postgres', '-v', 'ON_ERROR_STOP=1')
    $exists = & $psql @psqlOptions -tAc "SELECT 1 FROM pg_database WHERE datname = 'nursapat_db'"
    if ($LASTEXITCODE -ne 0) { throw 'Unable to connect to local PostgreSQL on port 5433. Check the server and NURSAPAT_LOCAL_POSTGRES_PASSWORD.' }
    if (([string]$exists).Trim() -ne '1') {
        & $psql @psqlOptions -c 'CREATE DATABASE nursapat_db OWNER admin' | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Database creation failed.' }
        $createdDatabase = $true
    }
    $connection = New-Object System.Data.Common.DbConnectionStringBuilder
    $connection['Host'] = '127.0.0.1'
    $connection['Port'] = '5433'
    $connection['Database'] = 'nursapat_db'
    $connection['Username'] = 'admin'
    $connection['Password'] = $password
    $env:ConnectionStrings__PostgreSQL = $connection.get_ConnectionString()
    $env:Persistence__Provider = 'PostgreSql'
    $env:Persistence__ConnectionStringName = 'PostgreSQL'

    $setupOptions = @($repoRoot)
    if (!$createdDatabase) { $setupOptions += '--prepare-only' }
    dotnet run --project (Join-Path $PSScriptRoot 'LocalDatabase/LocalDatabase.csproj') -- @setupOptions | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'NurSapat database setup failed.' }
} finally {
    $env:PGPASSWORD = $previousPassword
    $env:PGCONNECT_TIMEOUT = $previousTimeout
    $env:ConnectionStrings__PostgreSQL = $previousConnection
    $env:Persistence__Provider = $previousProvider
    $env:Persistence__ConnectionStringName = $previousConnectionName
}
if ($ConfigureDirectLaunch) {
    $localSettings = Join-Path $repoRoot 'NurSapat.App/appsettings.Local.json'
    @{ ConnectionStrings = @{ PostgreSQL = $connection.get_ConnectionString() } } |
        ConvertTo-Json | Set-Content -LiteralPath $localSettings -Encoding UTF8
    Write-Host 'Local database connection saved for direct application launch.'
}
Write-Host 'PostgreSQL ready: 127.0.0.1:5433 / nursapat_db / admin'
if ($PassThru) { Write-Output -NoEnumerate $connection }
