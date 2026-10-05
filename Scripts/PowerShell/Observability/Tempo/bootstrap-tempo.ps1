#requires -Version 5.1
<#
Idempotent Tempo bootstrap:
- Ensures the shared observability Docker network exists
- Starts Tempo via docker compose
- Waits until Tempo reports ready

Run:
  .\bootstrap-tempo.ps1
#>

param(
  [string]$ComposeFile = (Join-Path $PSScriptRoot "../../../Infrastructure/Observability/Tempo/docker-compose.yml"),
  [string]$ConfigFile = (Join-Path $PSScriptRoot "../../../Infrastructure/Observability/Tempo/tempo.yml"),
  [string]$ServiceName = "tempo",
  [string]$NetworkName = "flowchat-observability",
  [string]$ReadyUrl = "http://localhost:3200/ready",
  [int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step([string]$msg) {
  Write-Host "`n==> $msg"
}

function Assert-Command([string]$cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd. Make sure it is installed and available in PATH."
  }
}

function Resolve-InputPath([string]$path) {
  if ([System.IO.Path]::IsPathRooted($path)) {
    return $path
  }

  return Join-Path (Resolve-Path (Join-Path $PSScriptRoot "../../../Infrastructure/Observability/Tempo")) $path
}

function Assert-PathExists([string]$path, [string]$label) {
  if (-not (Test-Path $path)) {
    throw "Missing ${label}: $path"
  }
}

function Ensure-Network([string]$networkName) {
  Write-Step "Ensuring Docker network exists: $networkName"

  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    docker network inspect $networkName 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
      docker network create $networkName | Out-Null
    }
  } finally {
    $ErrorActionPreference = $old
  }
}

function Get-ProjectDirectory() {
  return (Resolve-Path (Join-Path $PSScriptRoot "../../../Infrastructure/Observability")).Path
}

function Get-ComposeArgs([string]$composeFile) {
  return @("--project-directory", (Get-ProjectDirectory), "-f", $composeFile)
}

function Get-ContainerIdForService([string]$composeFile, [string]$service) {
  $composeArgs = Get-ComposeArgs -composeFile $composeFile
  (docker compose @composeArgs ps -q $service 2>$null).Trim()
}

function Wait-ForHttpOk([string]$url, [int]$timeoutSeconds, [string]$displayName) {
  Write-Step "Waiting for $displayName at $url (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    try {
      $resp = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 5
      if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 400) {
        Write-Host "$displayName is ready."
        return
      }
    } catch {
    }

    Start-Sleep -Seconds 2
  }

  throw "$displayName did not become ready within ${timeoutSeconds}s. Check logs: docker logs flowchat-tempo"
}

$ComposeFile = Resolve-InputPath $ComposeFile
$ConfigFile = Resolve-InputPath $ConfigFile

Assert-Command "docker"
Assert-PathExists $ComposeFile "compose file"
Assert-PathExists $ConfigFile "Tempo config"

Ensure-Network -networkName $NetworkName

Write-Step "Starting Tempo via docker compose"
$composeArgs = Get-ComposeArgs -composeFile $ComposeFile
docker compose @composeArgs up -d --remove-orphans | Out-Null

$containerId = Get-ContainerIdForService -composeFile $ComposeFile -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForHttpOk -url $ReadyUrl -timeoutSeconds $TimeoutSeconds -displayName "Tempo"

Write-Step "Tempo ready"
Write-Host "Query API       : http://localhost:3200"
Write-Host "Internal OTLP   : flowchat-tempo:4317"
