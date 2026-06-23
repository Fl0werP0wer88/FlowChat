#requires -Version 5.1
<#
Idempotent Kafka + Kafka UI + Topics bootstrap (Windows PowerShell)

- Starts Kafka (KRaft) + Kafka UI as one compose project
- Delegates topic creation to ensure-kafka-topics.ps1
- Prints Kafka UI URL

Run:
  .\bootstrap-kafka-stack.ps1
#>

param(
  [string]$ProjectName = "flowchat-kafka",
  [string]$KafkaComposeFile = ".\docker-compose.kafka.yml",
  [string]$UiComposeFile = ".\docker-compose.kafka-ui.yml",
  [string]$KafkaServiceName = "broker",
  [string]$KafkaBin = "/opt/kafka/bin",
  [string]$BootstrapServer = "localhost:9092",
  [int]$TimeoutSeconds = 180
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

Write-Step "Starting Kafka + Kafka UI as one compose project: $ProjectName"
docker compose -p $ProjectName -f $KafkaComposeFile -f $UiComposeFile up -d --remove-orphans | Out-Null

$ensureScript = Join-Path $PSScriptRoot "ensure-kafka-topics.ps1"
if (-not (Test-Path $ensureScript)) {
  throw "Missing shared script: $ensureScript"
}

& $ensureScript `
  -ProjectName $ProjectName `
  -KafkaComposeFile $KafkaComposeFile `
  -UiComposeFile $UiComposeFile `
  -ServiceName $KafkaServiceName `
  -KafkaBin $KafkaBin `
  -BootstrapServer $BootstrapServer `
  -TimeoutSeconds $TimeoutSeconds

Write-Step "Kafka UI"
Write-Host "http://localhost:8080/"

Write-Step "Done."
