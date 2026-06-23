#requires -Version 5.1
<#
Idempotent Kafka bootstrap:
- Starts Kafka via docker compose
- Delegates topic creation to ensure-kafka-topics.ps1

Run:
  .\bootstrap-kafka.ps1

Optional:
  .\bootstrap-kafka.ps1 -ComposeFile ".\docker-compose.kafka.yml" -ServiceName "broker"
#>

param(
  [string]$ComposeFile = ".\docker-compose.kafka.yml",
  [string]$ServiceName = "broker"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step($msg) {
  Write-Host "`n==> $msg"
}

function Assert-Command($cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd. Make sure it is installed and available in PATH."
  }
}

Assert-Command "docker"

Write-Step "Starting Kafka via docker compose"
docker compose -f $ComposeFile up -d --remove-orphans | Out-Null

$ensureScript = Join-Path $PSScriptRoot "ensure-kafka-topics.ps1"
if (-not (Test-Path $ensureScript)) {
  throw "Missing shared script: $ensureScript"
}

& $ensureScript -ComposeFile $ComposeFile -ServiceName $ServiceName -TimeoutSeconds 120

Write-Step "Done. Kafka is up and topics are ensured"
