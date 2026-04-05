#requires -Version 5.1
<#
Resets FlowChat Kafka topics by removing them and creating them again.

Run:
  .\reset-topics.ps1

Optional:
  .\reset-topics.ps1 -ComposeFile ".\docker-compose.kafka.yml" -ServiceName "broker"
#>

param(
  [string]$ContainerId,
  [string]$ComposeFile = ".\docker-compose.kafka.yml",
  [string]$ProjectName,
  [string]$KafkaComposeFile = ".\docker-compose.kafka.yml",
  [string]$UiComposeFile = ".\docker-compose.kafka-ui.yml",
  [string]$ServiceName = "broker",
  [string]$KafkaBin = "/opt/kafka/bin",
  [string]$BootstrapServer = "localhost:9092",
  [int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step($msg) {
  Write-Host "`n==> $msg"
}

$removeScript = Join-Path $PSScriptRoot "remove-topics.ps1"
if (-not (Test-Path $removeScript)) {
  throw "Missing shared script: $removeScript"
}

$ensureScript = Join-Path $PSScriptRoot "ensure-kafka-topics.ps1"
if (-not (Test-Path $ensureScript)) {
  throw "Missing shared script: $ensureScript"
}

Write-Step "Removing existing FlowChat topics"
& $removeScript `
  -ContainerId $ContainerId `
  -ComposeFile $ComposeFile `
  -ProjectName $ProjectName `
  -KafkaComposeFile $KafkaComposeFile `
  -UiComposeFile $UiComposeFile `
  -ServiceName $ServiceName `
  -KafkaBin $KafkaBin `
  -BootstrapServer $BootstrapServer `
  -TimeoutSeconds $TimeoutSeconds

Write-Step "Recreating FlowChat topics"
& $ensureScript `
  -ContainerId $ContainerId `
  -ComposeFile $ComposeFile `
  -ProjectName $ProjectName `
  -KafkaComposeFile $KafkaComposeFile `
  -UiComposeFile $UiComposeFile `
  -ServiceName $ServiceName `
  -KafkaBin $KafkaBin `
  -BootstrapServer $BootstrapServer `
  -TimeoutSeconds $TimeoutSeconds

Write-Step "Done. FlowChat topics were reset"
