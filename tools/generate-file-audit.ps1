param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$output = Join-Path $root "docs\NurSapat-File-Audit.json"

function Get-RelativeAuditPath([string]$path) {
    return [IO.Path]::GetFullPath($path).Substring($root.Length).TrimStart("\", "/").Replace("\", "/")
}

$excludedSegments = @(
    "\.git\", "\.vs\", "\.cursor\", "\bin\", "\obj\", "\packages\",
    "\publish\", "\updater_build\"
)

$allFiles = Get-ChildItem -LiteralPath $root -Recurse -File -Force |
    Where-Object {
        $full = $_.FullName
        -not ($excludedSegments | Where-Object { $full.IndexOf($_, [StringComparison]::OrdinalIgnoreCase) -ge 0 })
    }

$projectFiles = Get-ChildItem -LiteralPath $root -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|packages|publish|updater_build)\\' }

$projectByDirectory = @{}
$projects = foreach ($projectFile in $projectFiles) {
    [xml]$xml = Get-Content -Raw -LiteralPath $projectFile.FullName
    $propertyGroups = @($xml.Project.PropertyGroup)
    $tfm = ($propertyGroups.TargetFramework | Where-Object { $_ } | Select-Object -First 1)
    $outputType = ($propertyGroups.OutputType | Where-Object { $_ } | Select-Object -First 1)
    $refs = @($xml.Project.ItemGroup.ProjectReference.Include) |
        Where-Object { $_ } |
        ForEach-Object { $_.Replace("\", "/") }
    $packages = @($xml.Project.ItemGroup.PackageReference) |
        Where-Object { $_ } |
        ForEach-Object {
            if ($_.Include) { "$($_.Include)@$($_.Version)" }
        }
    $relative = Get-RelativeAuditPath $projectFile.FullName
    $directory = Split-Path -Parent $projectFile.FullName
    $name = [IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
    $projectByDirectory[$directory] = $name
    [ordered]@{
        name = $name
        path = $relative
        targetFramework = [string]$tfm
        outputType = if ($outputType) { [string]$outputType } else { "Library" }
        inSolution = ((dotnet sln (Join-Path $root "NurSapat.sln") list) -join "`n").Contains($relative.Replace("/", "\"))
        projectReferences = @($refs)
        packageReferences = @($packages)
    }
}

function Get-ProjectName([string]$filePath) {
    $directory = Split-Path -Parent $filePath
    while ($directory -and $directory.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        if ($projectByDirectory.ContainsKey($directory)) {
            return $projectByDirectory[$directory]
        }
        $parent = Split-Path -Parent $directory
        if ($parent -eq $directory) { break }
        $directory = $parent
    }
    return ""
}

function Get-Category([string]$relative, [string]$extension) {
    if ($relative -match '(^|/)Views?/' -or $extension -eq ".axaml") { return "ui" }
    if ($relative -match 'ViewModels?') { return "viewmodel" }
    if ($relative -match 'Cad\.|/Geometry/|/Rendering/|/Tools/') { return "cad" }
    if ($relative -match 'Persistence|Migrations|Repositories|Entities|Configurations') { return "database" }
    if ($relative -match 'Sync|Supabase|Auth') { return "sync-auth" }
    if ($relative -match 'Updates|Updater|version') { return "update" }
    if ($relative -match 'Services') { return "service" }
    if ($relative -match 'Models') { return "model" }
    if ($relative -match 'Tests') { return "test" }
    if ($extension -in @(".json", ".ini", ".config", ".props", ".targets")) { return "config" }
    if ($extension -in @(".sql", ".dbf", ".mdb", ".dbt")) { return "data" }
    if ($extension -in @(".png", ".bmp", ".ico", ".cur", ".pdf")) { return "resource" }
    if ($extension -in @(".zip", ".exe", ".dll", ".runtimeconfig.json")) { return "artifact" }
    if ($extension -eq ".csproj" -or $extension -eq ".sln") { return "project" }
    if ($extension -eq ".cs") { return "code" }
    return "other"
}

$deletionPatterns = @(
    '^build_output\.txt$',
    '^ccalc_extract\.txt$',
    '^_encode_verify\.txt$',
    '^Ruler$',
    '^NurSapat-win-x64\.zip$',
    '^Versions/',
    '^ArcContourTest/',
    '^NurSapat\.Cad\.Core/Templates/CadTemplateSerializer\.cs$'
)

$searchable = $allFiles |
    Where-Object { $_.Extension.ToLowerInvariant() -in @(".cs", ".axaml", ".csproj", ".json", ".sql", ".md", ".ps1") } |
    ForEach-Object {
        try {
            [pscustomobject][ordered]@{
                path = Get-RelativeAuditPath $_.FullName
                content = Get-Content -Raw -LiteralPath $_.FullName -ErrorAction Stop
            }
        }
        catch {
            $null
        }
    } |
    Where-Object { $_ }

$records = foreach ($file in $allFiles) {
    $relative = Get-RelativeAuditPath $file.FullName
    $extension = $file.Extension.ToLowerInvariant()
    $category = Get-Category $relative $extension
    $project = Get-ProjectName $file.FullName
    $isCandidate = [bool]($deletionPatterns | Where-Object { $relative -match $_ })
    $isLegacy = $relative -match 'Legacy|Распакованные формы|\.old$'
    $isGenerated = $relative -match 'Migrations/.*\.Designer\.cs$|ModelSnapshot\.cs$'
    $stem = [IO.Path]::GetFileNameWithoutExtension($file.Name)

    $references = if ($stem.Length -ge 4 -and $extension -in @(".cs", ".axaml")) {
        @($searchable |
            Where-Object { $_.path -ne $relative -and $_.content.IndexOf($stem, [StringComparison]::Ordinal) -ge 0 } |
            Select-Object -ExpandProperty path -Unique -First 8)
    }
    else {
        @()
    }

    $dependencies = @()
    if ($extension -in @(".cs", ".axaml", ".csproj")) {
        try {
            $content = Get-Content -Raw -LiteralPath $file.FullName
            $matches = [regex]::Matches(
                $content,
                '(?m)^\s*using\s+([A-Za-z0-9_.]+)|ProjectReference\s+Include="([^"]+)"|x:DataType="([^"]+)"|Source="([^"]+)"')
            $dependencies = @($matches | ForEach-Object {
                @($_.Groups[1].Value, $_.Groups[2].Value, $_.Groups[3].Value, $_.Groups[4].Value) |
                    Where-Object { $_ }
            } | Select-Object -Unique -First 10)
        }
        catch {
            $dependencies = @()
        }
    }

    $status = if ($isCandidate) { "deletion-candidate" }
        elseif ($isLegacy) { "legacy-review" }
        elseif ($isGenerated) { "generated-active" }
        else { "active-or-resource" }

    $risk = if ($relative -match 'Program\.cs$|appsettings\.json$|LoginViewModel\.cs$|FileSessionPersistence\.cs$|OfflinePasswordHasher\.cs$|UpdateValidator\.cs$|CadEditorViewModel\.cs$') { "critical-or-high" }
        elseif ($category -in @("cad", "database", "sync-auth", "update")) { "high" }
        elseif ($category -in @("ui", "viewmodel", "service", "model")) { "medium" }
        else { "low" }

    $evidence = @(
        "exists in audited working tree",
        "size=$($file.Length) bytes",
        "compile-time/text references=$($references.Count)"
    )
    if ($project) { $evidence += "located under project $project" }
    if ($isCandidate) { $evidence += "matches audited deletion-candidate rule; delete only after build/test" }
    if ($isLegacy) { $evidence += "legacy/manual-review path marker" }
    if ($extension -eq ".axaml") { $evidence += "Avalonia XAML resource; XAML/reflection checks required before deletion" }
    if ($relative -match 'Models|Dto|Snapshot|Payload') { $evidence += "possible serialization contract; do not classify from compile-time references alone" }

    [ordered]@{
        path = $relative
        project = $project
        category = $category
        purpose = switch ($category) {
            "ui" { "Avalonia view, style, control, or UI resource" }
            "viewmodel" { "MVVM presentation state and commands" }
            "cad" { "CAD model, tool, geometry, rendering, or editor implementation" }
            "database" { "Persistence entity, mapping, repository, migration, or schema" }
            "sync-auth" { "Offline cache, Supabase authentication, or synchronization" }
            "update" { "Application update, release, validation, install, or updater code" }
            "service" { "Application or domain service" }
            "model" { "Domain, UI, persistence, or transport model" }
            "test" { "Automated test source or test project" }
            "config" { "Configuration or settings file" }
            "data" { "SQL or legacy data artifact" }
            "resource" { "Application or legacy visual/document resource" }
            "artifact" { "Build, publish, or release artifact" }
            "project" { "Solution/project build metadata" }
            "code" { "C# source" }
            default { "Repository file requiring context-specific interpretation" }
        }
        referencedBy = @($references)
        dependencies = @($dependencies)
        status = $status
        risk = $risk
        deletionCandidate = $isCandidate
        evidence = @($evidence)
    }
}

$issues = @(
    [ordered]@{ id = "SEC-001"; severity = "critical"; file = "NurSapat.App/appsettings.json"; line = 4; summary = "PostgreSQL credential committed in configuration" },
    [ordered]@{ id = "SEC-002"; severity = "critical"; file = "NurSapat.App/ViewModels/LoginViewModel.cs"; line = 251; summary = "Every successful login receives local administrator permissions" },
    [ordered]@{ id = "SEC-003"; severity = "high"; file = "NurSapat.App/ViewModels/LoginViewModel.cs"; line = 237; summary = "Remember Me persists plaintext password" },
    [ordered]@{ id = "SEC-004"; severity = "high"; file = "NurSapat.Infrastructure.Sync/Auth/FileSessionPersistence.cs"; line = 34; summary = "Supabase access and refresh tokens are serialized without OS protection" },
    [ordered]@{ id = "SEC-005"; severity = "high"; file = "NurSapat.Infrastructure.Sync/Auth/OfflinePasswordHasher.cs"; line = 55; summary = "Fast SHA-256 is used as a password hash" },
    [ordered]@{ id = "UPD-001"; severity = "high"; file = "NurSapat.Infrastructure.Updates/Services/UpdateValidator.cs"; line = 61; summary = "Hash validation has no trusted digital signature chain" },
    [ordered]@{ id = "CAD-001"; severity = "high"; file = "NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs"; line = 1; summary = "God ViewModel: 9167 lines in the main partial" },
    [ordered]@{ id = "CAD-002"; severity = "high"; file = "NurSapat.Cad.UI/Controls/CadCanvasControl.cs"; line = 130; summary = "CAD invalidation and async mutation are fragmented across many call sites" },
    [ordered]@{ id = "TEST-001"; severity = "medium"; file = "NurSapat.Cad.Core.Tests/NurSapat.Cad.Core.Tests.csproj"; line = 1; summary = "CAD test project is not included in NurSapat.sln" }
)

$deletionCandidates = @($records | Where-Object { $_.deletionCandidate } | ForEach-Object {
    [ordered]@{
        path = $_.path
        risk = $_.risk
        recommendation = "remove only after owner review, clean build, and all tests"
        evidence = $_.evidence
    }
})

$directoryCandidates = @(
    [ordered]@{
        path = "publish/"
        risk = "low"
        recommendation = "remove local publish output after confirming no external tooling depends on this path"
        evidence = @("ignored build/publish directory", "excluded from source-file inventory")
    },
    [ordered]@{
        path = "updater_build/"
        risk = "low"
        recommendation = "remove local updater build output after confirming release packaging"
        evidence = @("build output directory", "excluded from source-file inventory")
    },
    [ordered]@{
        path = "Versions/"
        risk = "medium"
        recommendation = "move release/version artifacts to release storage after owner review"
        evidence = @("release/version artifact directory", "not a project and not referenced by solution")
    },
    [ordered]@{
        path = "ArcContourTest/"
        risk = "medium"
        recommendation = "remove after owner confirms the binary-only repro is no longer needed"
        evidence = @("contains bin/obj only", "no csproj", "not included in solution")
    }
)
$deletionCandidates = @($deletionCandidates) + @($directoryCandidates)

$audit = [ordered]@{
    generatedAt = [DateTimeOffset]::Now.ToString("o")
    solution = "NurSapat.sln"
    auditedFileCountAtStart = 1409
    projects = @($projects)
    files = @($records)
    issues = $issues
    deletionCandidates = $deletionCandidates
}

$json = $audit | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText($output, $json, [Text.UTF8Encoding]::new($false))
Write-Output "Wrote $output"
Write-Output "Projects: $($projects.Count)"
Write-Output "File records: $($records.Count)"
Write-Output "Issues: $($issues.Count)"
Write-Output "Deletion candidates: $($deletionCandidates.Count)"
