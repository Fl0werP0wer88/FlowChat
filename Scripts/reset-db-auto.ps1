#requires -Version 5.1
<#
Resets all FlowChat databases, builds and starts the same applications as the
"All Services" workspace compound, waits for them to become ready, and then
registers the development users.

Run from any directory:
  .\Scripts\reset-db-auto.ps1
#>

param(
    [int]$StartupTimeoutSeconds = 180,
    [int]$WorkerStabilizationSeconds = 10,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$resetDbScript = Join-Path $repoRoot 'Scripts\PostgreSQL\reset-db.ps1'
$registerUsersScript = Join-Path $repoRoot 'Scripts\register-auth-users.ps1'
$runId = Get-Date -Format 'yyyyMMdd-HHmmss'
$logDirectory = Join-Path $repoRoot ".codex\temp\local-environment-$runId"

$apiProcesses = @(
    [PSCustomObject]@{ Name = 'AuthService API'; Project = 'AuthService\src\FlowChat.AuthService.API'; Dll = 'bin\Debug\net10.0\FlowChat.AuthService.API.dll'; Port = 7236; Urls = 'https://localhost:7236;http://localhost:5234' },
    [PSCustomObject]@{ Name = 'ChatService API'; Project = 'ChatService\src\FlowChat.ChatService.API'; Dll = 'bin\Debug\net10.0\FlowChat.ChatService.API.dll'; Port = 7254; Urls = 'https://localhost:7254;http://localhost:5254' },
    [PSCustomObject]@{ Name = 'UserProfileService API'; Project = 'UserProfileService\src\FlowChat.UserProfileService.API'; Dll = 'bin\Debug\net10.0\FlowChat.UserProfileService.API.dll'; Port = 7148; Urls = 'https://localhost:7148;http://localhost:5054' },
    [PSCustomObject]@{ Name = 'PresenceService API'; Project = 'PresenceService\src\FlowChat.PresenceService.API'; Dll = 'bin\Debug\net10.0\FlowChat.PresenceService.API.dll'; Port = 7198; Urls = 'https://localhost:7198;http://localhost:5098' },
    [PSCustomObject]@{ Name = 'NotificationService API'; Project = 'NotificationService\src\FlowChat.NotificationService.API'; Dll = 'bin\Debug\net10.0\FlowChat.NotificationService.API.dll'; Port = 7206; Urls = 'https://localhost:7206;http://localhost:5206' },
    [PSCustomObject]@{ Name = 'RealtimeService API'; Project = 'RealtimeService\src\FlowChat.RealtimeService.API'; Dll = 'bin\Debug\net10.0\FlowChat.RealtimeService.API.dll'; Port = 7215; Urls = 'https://localhost:7215;http://localhost:5215' },
    [PSCustomObject]@{ Name = 'GatewayService API'; Project = 'GatewayService\src\FlowChat.GatewayService.API'; Dll = 'bin\Debug\net10.0\FlowChat.GatewayService.API.dll'; Port = 7270; Urls = 'https://localhost:7270;http://localhost:5270' },
    [PSCustomObject]@{ Name = 'HarnessService API'; Project = 'HarnessService\src\FlowChat.HarnessService.API'; Dll = 'bin\Debug\net10.0\FlowChat.HarnessService.API.dll'; Port = 7300; Urls = 'https://localhost:7300;http://localhost:5300' }
)

$workerProcesses = @(
    [PSCustomObject]@{ Name = 'AuthService Consumers'; Project = 'AuthService\src\Workers\FlowChat.AuthService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.AuthService.Consumers.dll' },
    [PSCustomObject]@{ Name = 'AuthService OutboxPublisher'; Project = 'AuthService\src\Workers\FlowChat.AuthService.OutboxPublisher'; Dll = 'bin\Debug\net10.0\FlowChat.AuthService.OutboxPublisher.dll' },
    [PSCustomObject]@{ Name = 'ChatService Consumers'; Project = 'ChatService\src\Workers\FlowChat.ChatService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.ChatService.Consumers.dll' },
    [PSCustomObject]@{ Name = 'ChatService OutboxPublisher'; Project = 'ChatService\src\Workers\FlowChat.ChatService.OutboxPublisher'; Dll = 'bin\Debug\net10.0\FlowChat.ChatService.OutboxPublisher.dll' },
    [PSCustomObject]@{ Name = 'UserProfileService Consumers'; Project = 'UserProfileService\src\Workers\FlowChat.UserProfileService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.UserProfileService.Consumers.dll' },
    [PSCustomObject]@{ Name = 'UserProfileService OutboxPublisher'; Project = 'UserProfileService\src\Workers\FlowChat.UserProfileService.OutboxPublisher'; Dll = 'bin\Debug\net10.0\FlowChat.UserProfileService.OutboxPublisher.dll' },
    [PSCustomObject]@{ Name = 'NotificationService Consumers'; Project = 'NotificationService\src\Workers\FlowChat.NotificationService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.NotificationService.Consumers.dll' },
    [PSCustomObject]@{ Name = 'RealtimeService Consumers'; Project = 'RealtimeService\src\Workers\FlowChat.RealtimeService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.RealtimeService.Consumers.dll' },
    [PSCustomObject]@{ Name = 'PresenceService Consumers'; Project = 'PresenceService\src\Workers\FlowChat.PresenceService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.PresenceService.Consumers.dll' },
    [PSCustomObject]@{ Name = 'PresenceService OutboxPublisher'; Project = 'PresenceService\src\Workers\FlowChat.PresenceService.OutboxPublisher'; Dll = 'bin\Debug\net10.0\FlowChat.PresenceService.OutboxPublisher.dll' },
    [PSCustomObject]@{ Name = 'HarnessService Consumers'; Project = 'HarnessService\src\Workers\FlowChat.HarnessService.Consumers'; Dll = 'bin\Debug\net10.0\FlowChat.HarnessService.Consumers.dll' }
)

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message"
}

function Assert-Command([string]$Command) {
    if (-not (Get-Command $Command -ErrorAction SilentlyContinue)) {
        throw "Missing required command: $Command. Make sure it is installed and available in PATH."
    }
}

function Test-TcpPort([int]$Port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $asyncResult = $client.BeginConnect('localhost', $Port, $null, $null)
        if (-not $asyncResult.AsyncWaitHandle.WaitOne(250)) {
            return $false
        }

        $client.EndConnect($asyncResult)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Get-LogPath([string]$Name, [string]$Suffix) {
    $safeName = $Name.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    return Join-Path $logDirectory "$safeName.$Suffix.log"
}

function Start-DotNetApplication($Definition, [bool]$IsApi) {
    $workingDirectory = Join-Path $repoRoot $Definition.Project
    $dllPath = Join-Path $workingDirectory $Definition.Dll
    if (-not (Test-Path -LiteralPath $dllPath)) {
        throw "Missing application binary for '$($Definition.Name)': $dllPath"
    }

    $previousAspNetEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
    $previousUrls = $env:ASPNETCORE_URLS

    try {
        if ($IsApi) {
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $env:ASPNETCORE_URLS = $Definition.Urls
        }
        else {
            $env:DOTNET_ENVIRONMENT = 'Development'
            Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
        }

        return Start-Process `
            -FilePath 'dotnet' `
            -ArgumentList @($dllPath) `
            -WorkingDirectory $workingDirectory `
            -WindowStyle Hidden `
            -RedirectStandardOutput (Get-LogPath $Definition.Name 'out') `
            -RedirectStandardError (Get-LogPath $Definition.Name 'err') `
            -PassThru
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousAspNetEnvironment
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
        $env:ASPNETCORE_URLS = $previousUrls
    }
}

function Assert-ProcessRunning($ApplicationProcess) {
    if ($ApplicationProcess.Process.HasExited) {
        $errorLog = Get-LogPath $ApplicationProcess.Name 'err'
        throw "Process '$($ApplicationProcess.Name)' exited with code $($ApplicationProcess.Process.ExitCode). Check: $errorLog"
    }
}

function Stop-ProcessTree([int]$TargetProcessId) {
    $childProcesses = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $TargetProcessId" -ErrorAction SilentlyContinue)
    foreach ($childProcess in $childProcesses) {
        Stop-ProcessTree -TargetProcessId ([int]$childProcess.ProcessId)
    }

    $process = Get-Process -Id $TargetProcessId -ErrorAction SilentlyContinue
    if ($null -ne $process) {
        Stop-Process -Id $TargetProcessId -Force -ErrorAction SilentlyContinue
    }
}

function Stop-ProcessesListeningOnPort([int]$Port) {
    $processIds = @(
        Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique
    )

    foreach ($processId in $processIds) {
        Stop-ProcessTree -TargetProcessId ([int]$processId)
    }
}

Assert-Command 'dotnet'
Assert-Command 'npm.cmd'

foreach ($requiredScript in @($resetDbScript, $registerUsersScript)) {
    if (-not (Test-Path -LiteralPath $requiredScript)) {
        throw "Missing required script: $requiredScript"
    }
}

$occupiedPorts = @($apiProcesses | Where-Object { Test-TcpPort $_.Port })
if ($occupiedPorts.Count -gt 0) {
    $details = ($occupiedPorts | ForEach-Object { "$($_.Name):$($_.Port)" }) -join ', '
    throw "Some FlowChat API ports are already in use. Stop the existing services before running the reset: $details"
}

Write-Step 'Resetting all FlowChat databases'
if ($SkipBuild) {
    & $resetDbScript -SkipBuild
}
else {
    & $resetDbScript
}

if ($LASTEXITCODE -ne 0) {
    throw "Database reset failed with exit code $LASTEXITCODE."
}

if (-not $SkipBuild) {
    Write-Step 'Building the FlowChat backend'
    & dotnet build (Join-Path $repoRoot 'FlowChat.slnx')
    if ($LASTEXITCODE -ne 0) {
        throw "Backend build failed with exit code $LASTEXITCODE."
    }

    Write-Step 'Building ReactClient'
    Push-Location (Join-Path $repoRoot 'ReactClient')
    try {
        & npm.cmd run build
        if ($LASTEXITCODE -ne 0) {
            throw "ReactClient build failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$startedProcesses = @()

try {
Write-Step 'Starting backend APIs'
foreach ($definition in $apiProcesses) {
    $process = Start-DotNetApplication -Definition $definition -IsApi $true
    $startedProcesses += [PSCustomObject]@{ Name = $definition.Name; Process = $process }
    Write-Host "Started $($definition.Name) (PID $($process.Id))"
}

Write-Step 'Starting consumers and outbox publishers'
foreach ($definition in $workerProcesses) {
    $process = Start-DotNetApplication -Definition $definition -IsApi $false
    $startedProcesses += [PSCustomObject]@{ Name = $definition.Name; Process = $process }
    Write-Host "Started $($definition.Name) (PID $($process.Id))"
}

Write-Step 'Starting ReactClient'
$reactProcess = Start-Process `
    -FilePath 'npm.cmd' `
    -ArgumentList @('run', 'dev') `
    -WorkingDirectory (Join-Path $repoRoot 'ReactClient') `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Get-LogPath 'ReactClient' 'out') `
    -RedirectStandardError (Get-LogPath 'ReactClient' 'err') `
    -PassThru
$startedProcesses += [PSCustomObject]@{ Name = 'ReactClient'; Process = $reactProcess }
Write-Host "Started ReactClient (PID $($reactProcess.Id))"

$readinessTargets = @($apiProcesses | ForEach-Object { [PSCustomObject]@{ Name = $_.Name; Port = $_.Port } })
$readinessTargets += [PSCustomObject]@{ Name = 'ReactClient'; Port = 5173 }
$deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)

Write-Step "Waiting for all HTTP applications (timeout ${StartupTimeoutSeconds}s)"
do {
    foreach ($applicationProcess in $startedProcesses) {
        Assert-ProcessRunning $applicationProcess
    }

    $pendingTargets = @($readinessTargets | Where-Object { -not (Test-TcpPort $_.Port) })
    if ($pendingTargets.Count -eq 0) {
        break
    }

    $pendingNames = ($pendingTargets | ForEach-Object { "$($_.Name):$($_.Port)" }) -join ', '
    Write-Host "Still waiting for: $pendingNames"
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)

if ($pendingTargets.Count -gt 0) {
    $pendingNames = ($pendingTargets | ForEach-Object { "$($_.Name):$($_.Port)" }) -join ', '
    throw "Services did not become ready before the timeout: $pendingNames. Logs: $logDirectory"
}

Write-Step "Checking background workers for ${WorkerStabilizationSeconds}s"
Start-Sleep -Seconds $WorkerStabilizationSeconds
foreach ($applicationProcess in $startedProcesses) {
    Assert-ProcessRunning $applicationProcess
}

Write-Step 'All services are running; registering development users'
& $registerUsersScript
if ($LASTEXITCODE -ne 0) {
    throw "User registration failed with exit code $LASTEXITCODE."
}

Write-Step 'Local environment reset and user registration completed'
Write-Host "Service logs: $logDirectory"
}
finally {
    Write-Step 'Stopping services started by this script'

    for ($index = $startedProcesses.Count - 1; $index -ge 0; $index--) {
        Stop-ProcessTree -TargetProcessId $startedProcesses[$index].Process.Id
    }

    foreach ($port in @($apiProcesses.Port) + 5173) {
        Stop-ProcessesListeningOnPort -Port $port
    }

    Write-Host 'All processes started by this script were stopped.'
}
