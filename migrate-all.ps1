#requires -Version 5.1
param(
    [switch]$AutoGenerateMigration,
    [string]$MigrationName,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$services = @(
    @{ Name = 'AuthService'; Project = 'AuthService\\src\\FlowChat.AuthService.Persistence\\FlowChat.AuthService.Persistence.csproj'; Startup = 'AuthService\\src\\FlowChat.AuthService.API\\FlowChat.AuthService.API.csproj' },
    @{ Name = 'ChatService'; Project = 'ChatService\\src\\FlowChat.ChatService.Persistence\\FlowChat.ChatService.Persistence.csproj'; Startup = 'ChatService\\src\\FlowChat.ChatService.API\\FlowChat.ChatService.API.csproj' },
    @{ Name = 'NotificationService'; Project = 'NotificationService\\src\\FlowChat.NotificationService.Persistence\\FlowChat.NotificationService.Persistence.csproj'; Startup = 'NotificationService\\src\\FlowChat.NotificationService.API\\FlowChat.NotificationService.API.csproj' },
    @{ Name = 'SocialGraphService'; Project = 'SocialGraphService\\src\\FlowChat.SocialGraphService.Persistence\\FlowChat.SocialGraphService.Persistence.csproj'; Startup = 'SocialGraphService\\src\\FlowChat.SocialGraphService.API\\FlowChat.SocialGraphService.API.csproj' },
    @{ Name = 'UserProfileService'; Project = 'UserProfileService\\src\\FlowChat.UserProfileService.Persistence\\FlowChat.UserProfileService.Persistence.csproj'; Startup = 'UserProfileService\\src\\FlowChat.UserProfileService.API\\FlowChat.UserProfileService.API.csproj' }
)

function RunMigration($service) {
    $name = $service.Name
    $proj = $service.Project
    $startup = $service.Startup

    Write-Host "`n=== ${name}: update database ==="

    if (-not $SkipBuild) {
        Write-Host "Building startup project..."
        dotnet build $startup -c Debug
    }

    $efOutput = dotnet ef database update --project $proj --startup-project $startup --no-build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "${name}: database is up to date"
    }
    else {
        $m = $efOutput -join "`n"
        if ($AutoGenerateMigration -and $m -match 'PendingModelChangesWarning') {
            $migName = if ($MigrationName) { $MigrationName } else { "AutoMigration_$(Get-Date -Format yyyyMMdd_HHmmss)" }
            Write-Host "${name}: pending model changes detected, creating migration $migName"

            $addOutput = dotnet ef migrations add $migName --project $proj --startup-project $startup --no-build 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Error "${name}: migration add failed: $($addOutput -join "`n")"
                throw "Migration add failed"
            }

            $updateOutput = dotnet ef database update --project $proj --startup-project $startup --no-build 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Error "${name}: database update after migration add failed: $($updateOutput -join "`n")"
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

