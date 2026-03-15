#requires -Version 5.1
<#
Idempotent Grafana bootstrap:
- Ensures the shared observability Docker network exists
- Starts Grafana via docker compose
- Waits until Grafana reports healthy

Run:
  .\bootstrap-grafana.ps1
#>

param(
  [string]$ComposeFile = ".\docker-compose.yml",
  [string]$ProvisioningPath = ".\provisioning",
  [string]$ServiceName = "grafana",
  [string]$NetworkName = "flowchat-observability",
  [string]$ReadyUrl = "http://localhost:3000/api/health",
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

  return Join-Path $PSScriptRoot $path
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

function Get-ContainerIdForService([string]$composeFile, [string]$service) {
  (docker compose -f $composeFile ps -q $service 2>$null).Trim()
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

  throw "$displayName did not become ready within ${timeoutSeconds}s. Check logs: docker logs flowchat-grafana"
}

$ComposeFile = Resolve-InputPath $ComposeFile
$ProvisioningPath = Resolve-InputPath $ProvisioningPath

Assert-Command "docker"
Assert-PathExists $ComposeFile "compose file"
Assert-PathExists $ProvisioningPath "Grafana provisioning directory"

Ensure-Network -networkName $NetworkName

Write-Step "Starting Grafana via docker compose"
docker compose -f $ComposeFile up -d --remove-orphans | Out-Null

$containerId = Get-ContainerIdForService -composeFile $ComposeFile -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForHttpOk -url $ReadyUrl -timeoutSeconds $TimeoutSeconds -displayName "Grafana"

Write-Step "Grafana ready"
Write-Host "URL        : http://localhost:3000"
Write-Host "User       : admin"
Write-Host "Password   : admin"
