$ErrorActionPreference = 'Stop'
$previousConnection = $env:ConnectionStrings__PostgreSQL
$previousAuthMode = $env:Authentication__Mode
$previousProvider = $env:Persistence__Provider
$previousConnectionName = $env:Persistence__ConnectionStringName
Push-Location $PSScriptRoot
try {
    $connection = & (Join-Path $PSScriptRoot 'tools/start-local-postgres.ps1') -PassThru
    $env:ConnectionStrings__PostgreSQL = $connection.get_ConnectionString()
    $env:Authentication__Mode = 'LocalPostgreSQL'
    $env:Persistence__Provider = 'PostgreSql'
    $env:Persistence__ConnectionStringName = 'PostgreSQL'
    dotnet run --project 'NurSapat.App/NurSapat.App.csproj'
    if ($LASTEXITCODE -ne 0) { throw 'NurSapat failed to start.' }
} finally {
    $env:ConnectionStrings__PostgreSQL = $previousConnection
    $env:Authentication__Mode = $previousAuthMode
    $env:Persistence__Provider = $previousProvider
    $env:Persistence__ConnectionStringName = $previousConnectionName
    Pop-Location
}
