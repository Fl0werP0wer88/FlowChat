#requires -Version 5.1
<#
Idempotent FlowChat observability stack bootstrap:
- Starts the full observability stack as one Docker Compose project
- Ensures all services appear under one Docker Desktop group

Run:
  .\bootstrap-observability-stack.ps1
#>

param(
  [string]$ProjectName = "flowchat-observability",
  [string]$NetworkName = "flowchat-observability",
  [int]$TimeoutSeconds = 180
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

function Wait-ForHttpOk([string]$url, [int]$waitTimeoutSeconds, [string]$displayName) {
  Write-Step "Waiting for $displayName at $url (timeout ${waitTimeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($waitTimeoutSeconds)

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

  throw "$displayName did not become ready within ${waitTimeoutSeconds}s."
}

$observabilityRoot = $PSScriptRoot

$composeFiles = @(
  (Join-Path $observabilityRoot "Loki\docker-compose.yml"),
  (Join-Path $observabilityRoot "Tempo\docker-compose.yml"),
  (Join-Path $observabilityRoot "Prometheus\docker-compose.yml"),
  (Join-Path $observabilityRoot "Alloy\docker-compose.yml"),
  (Join-Path $observabilityRoot "Grafana\docker-compose.yml")
)

Assert-Command "docker"

foreach ($composeFile in $composeFiles) {
  Assert-PathExists $composeFile "compose file"
}

Ensure-Network -networkName $NetworkName

$dockerComposeArgs = @("-p", $ProjectName)
$dockerComposeArgs += @("--project-directory", $observabilityRoot)
foreach ($composeFile in $composeFiles) {
  $dockerComposeArgs += @("-f", $composeFile)
}
$dockerComposeArgs += @("up", "-d", "--remove-orphans")

Write-Step "Starting FlowChat observability stack as compose project: $ProjectName"
docker compose @dockerComposeArgs | Out-Null
if ($LASTEXITCODE -ne 0) {
  throw "Failed to start Docker Compose project '$ProjectName'."
}

Wait-ForHttpOk -url "http://localhost:3100/ready" -waitTimeoutSeconds $TimeoutSeconds -displayName "Loki"
Wait-ForHttpOk -url "http://localhost:3200/ready" -waitTimeoutSeconds $TimeoutSeconds -displayName "Tempo"
Wait-ForHttpOk -url "http://localhost:9090/-/ready" -waitTimeoutSeconds $TimeoutSeconds -displayName "Prometheus"
Wait-ForHttpOk -url "http://localhost:12345/-/ready" -waitTimeoutSeconds $TimeoutSeconds -displayName "Grafana Alloy"
Wait-ForHttpOk -url "http://localhost:3000/api/health" -waitTimeoutSeconds $TimeoutSeconds -displayName "Grafana"

Write-Step "FlowChat observability stack ready"
Write-Host "Docker group  : $ProjectName"
Write-Host "Grafana      : http://localhost:3000"
Write-Host "Prometheus   : http://localhost:9090"
Write-Host "Loki         : http://localhost:3100"
Write-Host "Tempo        : http://localhost:3200"
Write-Host "OTLP gRPC    : http://localhost:4317"
Write-Host "OTLP HTTP    : http://localhost:4318"
