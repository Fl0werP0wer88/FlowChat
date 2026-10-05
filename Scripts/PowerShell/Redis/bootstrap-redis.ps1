#requires -Version 5.1
<#
Idempotent Redis bootstrap:
- Starts Redis via docker compose
- Waits until the container responds to PING
- Ends with a running container ready on localhost:6379

Run:
  .\bootstrap-redis.ps1

Optional:
  .\bootstrap-redis.ps1 -ComposeFile ".\docker-compose.yml" -ServiceName "redis"
#>

param(
  [string]$ProjectName = "flowchat",
  [string]$ComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Redis/docker-compose.yml"),
  [string]$ServiceName = "redis",
  [string]$RedisUsername = "default",
  [string]$RedisPassword = "flowchat_redis_pw",
  [int]$TimeoutSeconds = 60
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

function Get-ContainerIdForService([string]$service) {
  $containerId = docker compose -p $ProjectName -f $ComposeFile ps -q $service 2>$null
  if ($null -eq $containerId) {
    return ""
  }

  return ($containerId | Out-String).Trim()
}

function Wait-ForRedisReady([string]$containerId, [string]$redisUsername, [string]$redisPassword, [int]$timeoutSeconds) {
  Write-Step "Waiting for Redis to be ready (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    $old = $ErrorActionPreference
    try {
      $ErrorActionPreference = "Continue"
      $response = docker exec $containerId redis-cli --user $redisUsername --pass $redisPassword ping 2>$null
      if ($LASTEXITCODE -eq 0 -and ($response | Out-String).Trim() -eq "PONG") {
        Write-Host "Redis is ready"
        return
      }
    } finally {
      $ErrorActionPreference = $old
    }

    Start-Sleep -Seconds 2
  }

  Write-Step "Last logs:"
  docker logs --tail 80 $containerId
  throw "Redis did not become ready within ${timeoutSeconds}s."
}

Assert-Command "docker"
Ensure-DockerVolume -volumeName "flowchat-redis_redis_data"

$env:FLOWCHAT_REDIS_USERNAME = $RedisUsername
$env:FLOWCHAT_REDIS_PASSWORD = $RedisPassword

Write-Step "Starting Redis via docker compose"
# Idempotent: creates if missing, starts if stopped, and leaves it running if already up
docker compose -p $ProjectName -f $ComposeFile up -d | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -p $ProjectName -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForRedisReady -containerId $containerId -redisUsername $RedisUsername -redisPassword $RedisPassword -timeoutSeconds $TimeoutSeconds

Write-Step "Redis ready"
Write-Host "Host     : localhost"
Write-Host "Port     : 6379"
Write-Host "Username : $RedisUsername"
Write-Host "Password : $RedisPassword"
