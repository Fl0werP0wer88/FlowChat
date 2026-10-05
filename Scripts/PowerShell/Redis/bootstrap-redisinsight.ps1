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
  [string]$ProjectName = "flowchat",
  [string]$ComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Redis/docker-compose.redisinsight.yml"),
  [string]$ServiceName = "redisinsight",
  [string]$UiUrl = "http://localhost:5540",
  [string]$RedisUsername = "default",
  [string]$RedisPassword = "flowchat_redis_pw",
  [int]$TimeoutSeconds = 120,
  [switch]$SkipRedisBootstrap
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

function Ensure-DockerVolume([string]$volumeName) {
  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    docker volume inspect $volumeName 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
      docker volume create $volumeName | Out-Null
      if ($LASTEXITCODE -ne 0) { throw "Failed to create Docker volume '$volumeName'." }
    }
  } finally {
    $ErrorActionPreference = $old
  }
}

function Assert-PathExists([string]$path, [string]$label) {
  if (-not (Test-Path $path)) {
    throw "Missing ${label}: $path"
  }
}

function Get-ContainerIdForService([string]$service) {
  $containerId = docker compose -p $ProjectName -f $ComposeFile ps -q $service 2>$null
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
Ensure-DockerVolume -volumeName "flowchat-redisinsight_redisinsight_data"

$redisBootstrapScript = Join-Path $PSScriptRoot "bootstrap-redis.ps1"
Assert-PathExists -path $redisBootstrapScript -label "Redis bootstrap script"

$env:FLOWCHAT_REDIS_USERNAME = $RedisUsername
$env:FLOWCHAT_REDIS_PASSWORD = $RedisPassword

if (-not $SkipRedisBootstrap) {
  Write-Step "Ensuring Redis is running"
  & $redisBootstrapScript -ProjectName $ProjectName -RedisUsername $RedisUsername -RedisPassword $RedisPassword
}

Write-Step "Starting RedisInsight via docker compose"
# Idempotent: creates if missing, starts if stopped, and leaves it running if already up
docker compose -p $ProjectName -f $ComposeFile up -d | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -p $ProjectName -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForHttpOk -url "$UiUrl/api/health/" -timeoutSeconds $TimeoutSeconds -displayName "RedisInsight"

Write-Step "RedisInsight ready"
Write-Host "UI            : $UiUrl"
Write-Host "Redis alias   : FlowChat Redis"
Write-Host "Redis target  : host.docker.internal:6379"
Write-Host "Redis user    : $RedisUsername"
Write-Host "Redis password: $RedisPassword"
