#requires -Version 5.1
<#
Runs the full PostgreSQL/service/user reset and the full Kafka reset in parallel.

Run from any directory:
  .\Scripts\PowerShell\reset-full.ps1
#>

param(
    [int]$PollIntervalSeconds = 2
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runId = Get-Date -Format 'yyyyMMdd-HHmmss'
$logDirectory = Join-Path $repoRoot ".codex\temp\full-reset-$runId"

$resetDefinitions = @(
    [PSCustomObject]@{
        Name = 'Database, services and users reset'
        Script = Join-Path $PSScriptRoot 'reset-db-auto.ps1'
        LogName = 'database-services-users'
    },
    [PSCustomObject]@{
        Name = 'Kafka full reset'
        Script = Join-Path $PSScriptRoot 'Kafka\reset-kafka-full.ps1'
        LogName = 'kafka'
    }
)

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message"
}

if ($PollIntervalSeconds -lt 1) {
    throw 'PollIntervalSeconds must be greater than zero.'
}

foreach ($definition in $resetDefinitions) {
    if (-not (Test-Path -LiteralPath $definition.Script)) {
        throw "Missing required reset script: $($definition.Script)"
    }
}

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$runningResets = @()

Write-Step 'Starting full database and Kafka resets in parallel'
foreach ($definition in $resetDefinitions) {
    $standardOutputLog = Join-Path $logDirectory "$($definition.LogName).out.log"
    $standardErrorLog = Join-Path $logDirectory "$($definition.LogName).err.log"
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', "`"$($definition.Script)`""
    )

    $process = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList $arguments `
        -WorkingDirectory $repoRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $standardOutputLog `
        -RedirectStandardError $standardErrorLog `
        -PassThru

    $runningResets += [PSCustomObject]@{
        Name = $definition.Name
        Process = $process
        StandardOutputLog = $standardOutputLog
        StandardErrorLog = $standardErrorLog
        Reported = $false
    }

    Write-Host "Started $($definition.Name) (PID $($process.Id))"
}

Write-Step 'Waiting for both resets to finish'
while (@($runningResets | Where-Object { -not $_.Process.HasExited }).Count -gt 0) {
    foreach ($reset in $runningResets) {
        if ($reset.Process.HasExited -and -not $reset.Reported) {
            $reset.Reported = $true
            Write-Host "$($reset.Name) finished with exit code $($reset.Process.ExitCode)"
        }
    }

    Start-Sleep -Seconds $PollIntervalSeconds
}

foreach ($reset in $runningResets) {
    if (-not $reset.Reported) {
        $reset.Reported = $true
        Write-Host "$($reset.Name) finished with exit code $($reset.Process.ExitCode)"
    }
}

$failedResets = @($runningResets | Where-Object { $_.Process.ExitCode -ne 0 })
if ($failedResets.Count -gt 0) {
    $failedNames = ($failedResets | ForEach-Object { $_.Name }) -join ', '
    throw "Full reset failed: $failedNames. Logs: $logDirectory"
}

Write-Step 'Full database and Kafka reset completed successfully'
Write-Host "Logs: $logDirectory"
