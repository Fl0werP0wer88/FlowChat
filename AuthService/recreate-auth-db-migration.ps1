#requires -Version 5.1
<#
Regenerates the initial EF Core migration for AuthService and applies it to PostgreSQL.

Typical usage after deleting AuthService tables and the existing `Migrations` folder:

  powershell -ExecutionPolicy Bypass -File .\AuthService\recreate-auth-db-migration.ps1 -RemoveExistingMigrations -DropDatabaseFirst
#>

[CmdletBinding()]
param(
    [string]$MigrationName = "InitialCreate",
    [string]$Project = ".\src\FlowChat.AuthService.Persistence\FlowChat.AuthService.Persistence.csproj",
    [string]$StartupProject = ".\src\FlowChat.AuthService.API\FlowChat.AuthService.API.csproj",
    [string]$Context = "AppDbContext",
    [string]$OutputDir = "Migrations",
    [string]$ConnectionString = "Host=localhost;Port=5432;Database=flowchat_auth_db;Username=flowchat_migrator;Password=flowchat_migrator_pw",
    [switch]$RemoveExistingMigrations,
    [switch]$DropDatabaseFirst,
    [switch]$SkipDatabaseUpdate,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message"
}

function Resolve-ExistingPath([string]$BasePath, [string]$RelativeOrAbsolutePath) {
    if ([System.IO.Path]::IsPathRooted($RelativeOrAbsolutePath)) {
        return (Resolve-Path $RelativeOrAbsolutePath).Path
    }

    return (Resolve-Path (Join-Path $BasePath $RelativeOrAbsolutePath)).Path
}

function Invoke-DotNet([string[]]$Arguments) {
    Write-Host ("dotnet " + ($Arguments -join " "))
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: dotnet $($Arguments -join ' ')"
    }
}

$authServiceRoot = (Resolve-Path $PSScriptRoot).Path
$projectPath = Resolve-ExistingPath -BasePath $authServiceRoot -RelativeOrAbsolutePath $Project
$startupProjectPath = Resolve-ExistingPath -BasePath $authServiceRoot -RelativeOrAbsolutePath $StartupProject
$migrationsPath = Join-Path (Split-Path -Parent $projectPath) $OutputDir
$dotnetCliHome = Join-Path ([System.IO.Path]::GetTempPath()) "flowchat-dotnet-cli-home"

if (-not (Test-Path $dotnetCliHome)) {
    New-Item -ItemType Directory -Path $dotnetCliHome | Out-Null
}

$previousDotnetCliHome = [Environment]::GetEnvironmentVariable("DOTNET_CLI_HOME", "Process")
$previousDotnetSkipFirstTime = [Environment]::GetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "Process")
$previousDotnetNoLogo = [Environment]::GetEnvironmentVariable("DOTNET_NOLOGO", "Process")

$env:DOTNET_CLI_HOME = $dotnetCliHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"

Write-Step "Validating dotnet ef availability"
Invoke-DotNet @("ef", "--version")

if ((Test-Path $migrationsPath) -and $RemoveExistingMigrations) {
    Write-Step "Removing existing migrations from $migrationsPath"
    Remove-Item -Path $migrationsPath -Recurse -Force
}
elseif (Test-Path $migrationsPath) {
    $existingMigrationFiles = Get-ChildItem -Path $migrationsPath -File -ErrorAction SilentlyContinue
    if ($existingMigrationFiles.Count -gt 0) {
        throw "Existing migrations found in '$migrationsPath'. Rerun with -RemoveExistingMigrations to recreate them from scratch."
    }
}

$previousConnectionString = [Environment]::GetEnvironmentVariable("AUTH_DB_MIGRATION_CONNECTION_STRING", "Process")
$env:AUTH_DB_MIGRATION_CONNECTION_STRING = $ConnectionString

try {
    $commonArgs = @(
        "--project", $projectPath,
        "--startup-project", $startupProjectPath,
        "--context", $Context
    )

    if ($NoBuild) {
        $commonArgs += "--no-build"
    }

    if ($DropDatabaseFirst) {
        Write-Step "Dropping AuthService database"
        Invoke-DotNet (@("ef", "database", "drop", "--force") + $commonArgs)
    }

    Write-Step "Creating migration '$MigrationName'"
    Invoke-DotNet (@("ef", "migrations", "add", $MigrationName, "--output-dir", $OutputDir) + $commonArgs)

    if (-not $SkipDatabaseUpdate) {
        Write-Step "Applying migration to database"
        Invoke-DotNet (@("ef", "database", "update") + $commonArgs)
    }
}
finally {
    if ([string]::IsNullOrWhiteSpace($previousConnectionString)) {
        Remove-Item Env:AUTH_DB_MIGRATION_CONNECTION_STRING -ErrorAction SilentlyContinue
    }
    else {
        $env:AUTH_DB_MIGRATION_CONNECTION_STRING = $previousConnectionString
    }

    if ([string]::IsNullOrWhiteSpace($previousDotnetCliHome)) {
        Remove-Item Env:DOTNET_CLI_HOME -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_CLI_HOME = $previousDotnetCliHome
    }

    if ([string]::IsNullOrWhiteSpace($previousDotnetSkipFirstTime)) {
        Remove-Item Env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = $previousDotnetSkipFirstTime
    }

    if ([string]::IsNullOrWhiteSpace($previousDotnetNoLogo)) {
        Remove-Item Env:DOTNET_NOLOGO -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_NOLOGO = $previousDotnetNoLogo
    }
}

Write-Step "AuthService migration workflow finished"
