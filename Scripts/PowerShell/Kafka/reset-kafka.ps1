#requires -Version 5.1
<#
Resets FlowChat Kafka state by resetting consumer groups and topics.

Run:
  .\reset-kafka.ps1

Optional:
  .\reset-kafka.ps1 -ComposeFile ".\docker-compose.kafka.yml" -ServiceName "broker"
#>

param(
  [string]$ContainerId,
  [string]$ComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Kafka/docker-compose.kafka.yml"),
  [string]$ProjectName,
  [string]$KafkaComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Kafka/docker-compose.kafka.yml"),
  [string]$UiComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Kafka/docker-compose.kafka-ui.yml"),
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

function Resolve-ScriptRelativePath([string]$Path) {
  if ([string]::IsNullOrWhiteSpace($Path)) {
    return $Path
  }

  if ([System.IO.Path]::IsPathRooted($Path)) {
    return $Path
  }

  return Join-Path (Resolve-Path (Join-Path $PSScriptRoot "../../Infrastructure/Kafka")) $Path
}

$ComposeFile = Resolve-ScriptRelativePath $ComposeFile
$KafkaComposeFile = Resolve-ScriptRelativePath $KafkaComposeFile
$UiComposeFile = Resolve-ScriptRelativePath $UiComposeFile

$resetConsumerGroupsScript = Join-Path $PSScriptRoot "reset-consumer-groups.ps1"
if (-not (Test-Path $resetConsumerGroupsScript)) {
  throw "Missing shared script: $resetConsumerGroupsScript"
}

$resetTopicsScript = Join-Path $PSScriptRoot "reset-topics.ps1"
if (-not (Test-Path $resetTopicsScript)) {
  throw "Missing shared script: $resetTopicsScript"
}

Write-Step "Resetting FlowChat consumer groups"
& $resetConsumerGroupsScript `
  -ContainerId $ContainerId `
  -ComposeFile $ComposeFile `
  -ProjectName $ProjectName `
  -KafkaComposeFile $KafkaComposeFile `
  -UiComposeFile $UiComposeFile `
  -ServiceName $ServiceName `
  -KafkaBin $KafkaBin `
  -BootstrapServer $BootstrapServer `
  -TimeoutSeconds $TimeoutSeconds

Write-Step "Resetting FlowChat topics"
& $resetTopicsScript `
  -ContainerId $ContainerId `
  -ComposeFile $ComposeFile `
  -ProjectName $ProjectName `
  -KafkaComposeFile $KafkaComposeFile `
  -UiComposeFile $UiComposeFile `
  -ServiceName $ServiceName `
  -KafkaBin $KafkaBin `
  -BootstrapServer $BootstrapServer `
  -TimeoutSeconds $TimeoutSeconds

Write-Step "Done. FlowChat Kafka was reset"
