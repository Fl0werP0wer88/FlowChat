#requires -Version 5.1
<#
Performs a full local Kafka reset:
- Removes the broker container together with all Kafka state stored in it
- Recreates Kafka and Kafka UI without removing or pulling Docker images
- Recreates every FlowChat topic defined in kafka-topic-definitions.ps1

Run:
  .\reset-kafka-full.ps1
#>

param(
  [string]$ProjectName = "flowchat-kafka",
  [string]$KafkaComposeFile = ".\docker-compose.kafka.yml",
  [string]$UiComposeFile = ".\docker-compose.kafka-ui.yml",
  [string]$ServiceName = "broker",
  [string]$ContainerName = "broker",
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

function Resolve-ScriptRelativePath([string]$Path) {
  if ([System.IO.Path]::IsPathRooted($Path)) {
    return $Path
  }

  return Join-Path $PSScriptRoot $Path
}

Assert-Command "docker"

$KafkaComposeFile = Resolve-ScriptRelativePath $KafkaComposeFile
$UiComposeFile = Resolve-ScriptRelativePath $UiComposeFile

$ensureScript = Join-Path $PSScriptRoot "ensure-kafka-topics.ps1"
if (-not (Test-Path $ensureScript)) {
  throw "Missing shared script: $ensureScript"
}

$existingContainerId = docker ps -aq --filter "name=^/${ContainerName}$"
if ($LASTEXITCODE -ne 0) {
  throw "Failed to inspect Docker container '$ContainerName'."
}

if (-not [string]::IsNullOrWhiteSpace($existingContainerId)) {
  Write-Step "Removing Kafka broker container and all state stored in it"
  docker rm -f $existingContainerId | Out-Null
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to remove Kafka broker container '$ContainerName'."
  }
} else {
  Write-Step "Kafka broker container does not exist; continuing with a clean start"
}

Write-Step "Recreating Kafka + Kafka UI as compose project: $ProjectName"
docker compose -p $ProjectName -f $KafkaComposeFile -f $UiComposeFile up -d --remove-orphans | Out-Null
if ($LASTEXITCODE -ne 0) {
  throw "Failed to recreate the Kafka compose project '$ProjectName'."
}

Write-Step "Recreating all defined FlowChat topics"
& $ensureScript `
  -ProjectName $ProjectName `
  -KafkaComposeFile $KafkaComposeFile `
  -UiComposeFile $UiComposeFile `
  -ServiceName $ServiceName `
  -KafkaBin $KafkaBin `
  -BootstrapServer $BootstrapServer `
  -TimeoutSeconds $TimeoutSeconds

Write-Step "Done. Kafka was fully reset and all defined FlowChat topics were recreated"
