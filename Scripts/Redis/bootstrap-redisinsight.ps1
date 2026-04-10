#requires -Version 5.1
<#
Idempotent RedisInsight bootstrap:
- Ensures the local Redis container is running
- Starts RedisInsight via docker compose
- Waits until the RedisInsight health endpoint is reachable
- Ends with RedisInsight available on localhost:5540 and preconfigured with the FlowChat Redis connection

Run:
  .\bootstrap-redisinsight.ps1

Optional:
  .\bootstrap-redisinsight.ps1 -ComposeFile ".\docker-compose.redisinsight.yml" -ServiceName "redisinsight"
#>

param(
  [string]$ComposeFile = ".\docker-compose.redisinsight.yml",
  [string]$ServiceName = "redisinsight",
  [string]$UiUrl = "http://localhost:5540",
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

function Assert-PathExists([string]$path, [string]$label) {
  if (-not (Test-Path $path)) {
    throw "Missing ${label}: $path"
  }
}

function Get-ContainerIdForService([string]$service) {
  $containerId = docker compose -f $ComposeFile ps -q $service 2>$null
  if ($null -eq $containerId) {
    return ""
  }

  return ($containerId | Out-String).Trim()
}

function Wait-ForHttpOk([string]$url, [int]$timeoutSeconds, [string]$displayName) {
  Write-Step "Waiting for $displayName at $url (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    try {
      $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 5
      if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
        Write-Host "$displayName is ready"
        return
      }
    } catch {
    }

    Start-Sleep -Seconds 2
  }

  throw "$displayName did not become ready within ${timeoutSeconds}s."
}

Assert-Command "docker"

$redisBootstrapScript = Join-Path $PSScriptRoot "bootstrap-redis.ps1"
Assert-PathExists -path $redisBootstrapScript -label "Redis bootstrap script"

Write-Step "Ensuring Redis is running"
& $redisBootstrapScript

Write-Step "Starting RedisInsight via docker compose"
# Idempotent: creates if missing, starts if stopped, and leaves it running if already up
docker compose -f $ComposeFile up -d --remove-orphans | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForHttpOk -url "$UiUrl/api/health/" -timeoutSeconds $TimeoutSeconds -displayName "RedisInsight"

Write-Step "RedisInsight ready"
Write-Host "UI            : $UiUrl"
Write-Host "Redis alias   : FlowChat Redis"
Write-Host "Redis target  : host.docker.internal:6379"
