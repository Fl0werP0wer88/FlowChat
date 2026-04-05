#requires -Version 5.1
param(
    [string]$MigrationName = 'InitialCreate',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$bootstrapScript = Join-Path $PSScriptRoot 'bootstrap-postgres.ps1'
$migrateAllScript = Join-Path $PSScriptRoot 'migrate-all.ps1'
$composeFile = Join-Path $PSScriptRoot 'docker-compose.yml'

function Clear-MigrationDirectory([string]$path) {
    Write-Host "Clearing migrations in $path"

    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host "Skipping missing directory $path"
        return
    }

    $items = Get-ChildItem -LiteralPath $path -Force
    if ($null -eq $items -or $items.Count -eq 0) {
        Write-Host "Directory already empty: $path"
        return
    }

    foreach ($item in $items) {
        Remove-Item -LiteralPath $item.FullName -Recurse -Force
    }
}

Write-Host "Resetting FlowChat databases..."
& $bootstrapScript -ComposeFile $composeFile -DropDb

$migrationDirectories = Get-ChildItem -Path $repoRoot -Recurse -Directory |
    Where-Object { $_.Name -ieq 'Migrations' -and $_.FullName -notmatch '\\(bin|obj)\\' }

if ($null -eq $migrationDirectories -or $migrationDirectories.Count -eq 0) {
    Write-Host 'No migration directories found.'
}

foreach ($directory in $migrationDirectories) {
    Clear-MigrationDirectory -path $directory.FullName
}

Write-Host "Recreating initial migrations and applying them..."
if ($SkipBuild) {
    & $migrateAllScript -AutoGenerateMigration -MigrationName $MigrationName -SkipBuild
}
else {
    & $migrateAllScript -AutoGenerateMigration -MigrationName $MigrationName
}

if ($LASTEXITCODE -ne 0) {
    throw 'Database reset failed'
}

Write-Host 'Database reset complete.'
