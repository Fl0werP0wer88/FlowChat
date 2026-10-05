#requires -Version 5.1
<#
Idempotent Kafka UI bootstrap (Provectus):
- Starts Kafka UI via docker compose
- Ensures container is running
- Waits for HTTP 200 on / (UI ready)

Run:
  .\bootstrap-kafka-ui.ps1

Optional:
  .\bootstrap-kafka-ui.ps1 -ComposeFile ".\docker-compose.kafka-ui.yml" -ServiceName "kafka-ui" -Port 8080
#>

param(
  [string]$ComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Kafka/docker-compose.kafka-ui.yml"),
  [string]$KafkaComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Kafka/docker-compose.kafka.yml"),
  [string]$ServiceName = "kafka-ui",
  [int]$Port = 8080,
  [int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step($msg) { Write-Host "`n==> $msg" }

function Assert-Command($cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd. Make sure it is installed and available in PATH."
  }
}

function Get-ContainerIdForService([string]$service) {
  (docker compose -f $KafkaComposeFile -f $ComposeFile ps -q $service 2>$null).Trim()
}

function Wait-ForHttpOk([string]$url, [int]$timeoutSeconds) {
  Write-Step "Waiting for Kafka UI to be ready at $url (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    try {
      # Use basic parsing for Windows PowerShell 5.1
      $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
      if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 400) {
        Write-Host "Kafka UI is ready ✅"
        return
      }
    } catch {
      Start-Sleep -Seconds 2
    }
  }

  throw "Kafka UI did not become ready within ${timeoutSeconds}s. Check logs: docker logs kafka-ui"
}

# -------------------- Main --------------------
Assert-Command "docker"

Write-Step "Starting Kafka UI via docker compose"
docker compose -f $KafkaComposeFile -f $ComposeFile up -d $ServiceName | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"

Wait-ForHttpOk -url "http://localhost:$Port/" -timeoutSeconds $TimeoutSeconds

Write-Step "Done. Open Kafka UI:"
Write-Host "http://localhost:$Port/"
