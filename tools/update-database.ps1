$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot
try {
    dotnet tool restore
    dotnet ef database update `
        --project "NurSapat.Infrastructure.Persistence/NurSapat.Infrastructure.Persistence.csproj" `
        --startup-project "NurSapat.App/NurSapat.App.csproj" `
        --context NurSapatDbContext
}
finally {
    Pop-Location
}
