#requires -Version 5.1
param(
    [switch]$AutoGenerateMigration,
    [string]$MigrationName,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$services = @(
    @{ Name = 'AuthService'; Project = 'AuthService\src\FlowChat.AuthService.Persistence\FlowChat.AuthService.Persistence.csproj'; Startup = 'AuthService\src\FlowChat.AuthService.API\FlowChat.AuthService.API.csproj' },
    @{ Name = 'ChatService'; Project = 'ChatService\src\FlowChat.ChatService.Persistence\FlowChat.ChatService.Persistence.csproj'; Startup = 'ChatService\src\FlowChat.ChatService.API\FlowChat.ChatService.API.csproj' },
    @{ Name = 'NotificationService'; Project = 'NotificationService\src\FlowChat.NotificationService.Persistence\FlowChat.NotificationService.Persistence.csproj'; Startup = 'NotificationService\src\FlowChat.NotificationService.API\FlowChat.NotificationService.API.csproj' },
    @{ Name = 'PresenceService'; Project = 'PresenceService\src\FlowChat.PresenceService.Persistence\FlowChat.PresenceService.Persistence.csproj'; Startup = 'PresenceService\src\FlowChat.PresenceService.API\FlowChat.PresenceService.API.csproj' },
    @{ Name = 'SocialGraphService'; Project = 'SocialGraphService\src\FlowChat.SocialGraphService.Persistence\FlowChat.SocialGraphService.Persistence.csproj'; Startup = 'SocialGraphService\src\FlowChat.SocialGraphService.API\FlowChat.SocialGraphService.API.csproj' },
    @{ Name = 'UserProfileService'; Project = 'UserProfileService\src\FlowChat.UserProfileService.Persistence\FlowChat.UserProfileService.Persistence.csproj'; Startup = 'UserProfileService\src\FlowChat.UserProfileService.API\FlowChat.UserProfileService.API.csproj' },
    @{ Name = 'HarnessService'; Project = 'HarnessService\src\FlowChat.HarnessService.Persistence\FlowChat.HarnessService.Persistence.csproj'; Startup = 'HarnessService\src\FlowChat.HarnessService.API\FlowChat.HarnessService.API.csproj' }
)

function GetMigrationName([bool]$isInitialMigration) {
    if ($MigrationName) {
        return $MigrationName
    }

    if ($isInitialMigration) {
        return 'InitialCreate'
    }

    return "AutoMigration_$(Get-Date -Format yyyyMMdd_HHmmss)"
}

function Test-HasMigrationFiles([string]$projectPath) {
    $migrationsDirectory = Join-Path (Split-Path $projectPath -Parent) 'Migrations'

    if (-not (Test-Path -LiteralPath $migrationsDirectory)) {
        return $false
    }

    return $null -ne (Get-ChildItem -LiteralPath $migrationsDirectory -Filter '*.cs' -File | Select-Object -First 1)
}

# Native tools often write warnings to stderr, so rely on their exit codes instead of PowerShell error records
function Invoke-DotnetCommand([string[]]$Arguments) {
    $previousErrorActionPreference = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $output = & dotnet @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @(
            $output | ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord]) {
                    $_.ToString()
                }
                else {
                    "$_"
                }
            }
        )
    }
}

function RunMigration($service) {
    $name = $service.Name
    $proj = Join-Path $repoRoot $service.Project
    $startup = Join-Path $repoRoot $service.Startup

    Write-Host "`n=== ${name}: update database ==="

    if (-not $SkipBuild) {
        Write-Host "Building startup project..."
        dotnet build $startup -c Debug
    }

    $hasMigrationFiles = Test-HasMigrationFiles $proj
    if ($AutoGenerateMigration -and -not $hasMigrationFiles) {
        $migName = GetMigrationName $true
        Write-Host "${name}: no migration files found, creating migration $migName"

        $addResult = Invoke-DotnetCommand @('ef', 'migrations', 'add', $migName, '--project', $proj, '--startup-project', $startup, '--no-build')
        if ($addResult.ExitCode -ne 0) {
            Write-Error "${name}: initial migration add failed: $($addResult.Output -join "`n")"
            throw "Initial migration add failed"
        }

        Write-Host "${name}: rebuilding startup project to include the newly generated migration"
        dotnet build $startup -c Debug
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed after initial migration add"
        }
    }

    $updateResult = Invoke-DotnetCommand @('ef', 'database', 'update', '--project', $proj, '--startup-project', $startup, '--no-build')
    $efOutput = $updateResult.Output
    if ($updateResult.ExitCode -eq 0) {
        Write-Host "${name}: database is up to date"
    }
    else {
        $m = $efOutput -join "`n"
        $hasPendingModelChanges = $m -match 'PendingModelChangesWarning'
        $hasNoMigrations = $m -match 'No migrations were found'

        if ($AutoGenerateMigration -and ($hasPendingModelChanges -or $hasNoMigrations)) {
            $isInitialMigration = $hasNoMigrations
            $migName = GetMigrationName $isInitialMigration
            $migrationReason = if ($isInitialMigration) { 'no migrations found' } else { 'pending model changes detected' }

            Write-Host "${name}: $migrationReason, creating migration $migName"

            $addResult = Invoke-DotnetCommand @('ef', 'migrations', 'add', $migName, '--project', $proj, '--startup-project', $startup, '--no-build')
            if ($addResult.ExitCode -ne 0) {
                Write-Error "${name}: migration add failed: $($addResult.Output -join "`n")"
                throw "Migration add failed"
            }

            Write-Host "${name}: rebuilding startup project to include the newly generated migration"
            dotnet build $startup -c Debug
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed after migration add"
            }

            $updateResult = Invoke-DotnetCommand @('ef', 'database', 'update', '--project', $proj, '--startup-project', $startup, '--no-build')
            if ($updateResult.ExitCode -ne 0) {
                Write-Error "${name}: database update after migration add failed: $($updateResult.Output -join "`n")"
                throw "Database update after migration add failed"
            }

            Write-Host "${name}: migration generated and applied"
        }
        else {
            Write-Error "${name}: migration update failed: $m"
            throw "Database update failed"
        }
    }
}

foreach ($svc in $services) {
    RunMigration $svc
}

Write-Host "All migrations complete."

