#requires -Version 5.1
<#
Removes all FlowChat Kafka topics defined in kafka-topic-definitions.ps1.

Can be run standalone:
  .\remove-topics.ps1
  .\remove-topics.ps1 -ComposeFile ".\docker-compose.kafka.yml"
  .\remove-topics.ps1 -ContainerId "<docker-container-id>"
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

$topicDefinitionsScript = Join-Path $PSScriptRoot "kafka-topic-definitions.ps1"
if (-not (Test-Path $topicDefinitionsScript)) {
  throw "Missing shared script: $topicDefinitionsScript"
}

. $topicDefinitionsScript

$KafkaTopics = "$KafkaBin/kafka-topics.sh"

function Write-Step($msg) {
  Write-Host "`n==> $msg"
}

function Assert-Command($cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd. Make sure it is installed and available in PATH."
  }
}

function Invoke-DockerQuiet {
  param(
    [Parameter(Mandatory = $true)][string]$ResolvedContainerId,
    [Parameter(Mandatory = $true)][string[]]$Args
  )

  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    docker exec $ResolvedContainerId @Args 2>$null | Out-Null
  } finally {
    $ErrorActionPreference = $old
  }

  return $LASTEXITCODE
}

function Resolve-ContainerId {
  if (-not [string]::IsNullOrWhiteSpace($ContainerId)) {
    return $ContainerId.Trim()
  }

  if (-not [string]::IsNullOrWhiteSpace($ProjectName)) {
    $projectResult = docker compose -p $ProjectName -f $KafkaComposeFile -f $UiComposeFile ps -q $ServiceName 2>$null
    if ($null -ne $projectResult) {
      $projectId = $projectResult.Trim()
      if (-not [string]::IsNullOrWhiteSpace($projectId)) {
        return $projectId
      }
    }
  }

  if (-not [string]::IsNullOrWhiteSpace($ComposeFile)) {
    $composeResult = docker compose -f $ComposeFile ps -q $ServiceName 2>$null
    if ($null -ne $composeResult) {
      $composeId = $composeResult.Trim()
      if (-not [string]::IsNullOrWhiteSpace($composeId)) {
        return $composeId
      }
    }
  }

  $dockerPsResult = docker ps -q --filter "name=^/${ServiceName}$"
  if ($null -ne $dockerPsResult) {
    $dockerPsId = $dockerPsResult.Trim()
    if (-not [string]::IsNullOrWhiteSpace($dockerPsId)) {
      return $dockerPsId
    }
  }

  throw "Could not resolve container id for service '$ServiceName'."
}

function Wait-ForKafkaReady([string]$ResolvedContainerId, [int]$WaitTimeoutSeconds) {
  Write-Step "Waiting for Kafka to be ready (timeout ${WaitTimeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($WaitTimeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    $code = Invoke-DockerQuiet -ResolvedContainerId $ResolvedContainerId -Args @(
      $KafkaTopics, "--bootstrap-server", $BootstrapServer, "--list"
    )

    if ($code -eq 0) {
      Write-Host "Kafka is ready."
      return
    }

    Start-Sleep -Seconds 2
  }

  throw "Kafka did not become ready within ${WaitTimeoutSeconds}s. Check logs: docker logs $ResolvedContainerId"
}

function Topic-Exists([string]$ResolvedContainerId, [string]$Topic) {
  $code = Invoke-DockerQuiet -ResolvedContainerId $ResolvedContainerId -Args @(
    $KafkaTopics, "--bootstrap-server", $BootstrapServer, "--describe", "--topic", $Topic
  )

  return ($code -eq 0)
}

function Remove-Topic([string]$ResolvedContainerId, [string]$Topic) {
  if (-not (Topic-Exists -ResolvedContainerId $ResolvedContainerId -Topic $Topic)) {
    Write-Host "Topic missing, skipping: $Topic"
    return
  }

  Write-Host "Deleting topic: $Topic"

  $code = Invoke-DockerQuiet -ResolvedContainerId $ResolvedContainerId -Args @(
    $KafkaTopics,
    "--delete",
    "--topic", $Topic,
    "--bootstrap-server", $BootstrapServer
  )

  if ($code -ne 0) {
    throw "Failed to delete topic '$Topic' (exit=$code). Check logs: docker logs $ResolvedContainerId"
  }
}

Assert-Command "docker"

$resolvedContainerId = Resolve-ContainerId
Write-Step "Using broker container id: $resolvedContainerId"
Wait-ForKafkaReady -ResolvedContainerId $resolvedContainerId -WaitTimeoutSeconds $TimeoutSeconds

Write-Step "Removing FlowChat topics"
$topicNames = @((Get-TopicDefinitions | ForEach-Object { $_.name }))

if (Get-Command Get-LegacyTopicNames -ErrorAction SilentlyContinue) {
  $topicNames += Get-LegacyTopicNames
}

foreach ($topicName in ($topicNames | Select-Object -Unique)) {
  Remove-Topic -ResolvedContainerId $resolvedContainerId -Topic $topicName
}

Write-Step "Final topic list"
docker exec $resolvedContainerId $KafkaTopics --bootstrap-server $BootstrapServer --list
