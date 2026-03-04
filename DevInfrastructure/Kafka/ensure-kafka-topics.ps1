#requires -Version 5.1
<#
Ensures FlowChat Kafka topics exist on a running broker.

Can be run standalone:
  .\ensure-kafka-topics.ps1
  .\ensure-kafka-topics.ps1 -ComposeFile ".\docker-compose.kafka.yml"
  .\ensure-kafka-topics.ps1 -ContainerId "<docker-container-id>"

Can also be called from bootstrap scripts after containers are started.
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

function Ensure-Topic(
  [string]$ResolvedContainerId,
  [string]$Topic,
  [int]$Partitions = 1,
  [int]$ReplicationFactor = 1,
  [hashtable]$Config = $null
) {
  if (Topic-Exists -ResolvedContainerId $ResolvedContainerId -Topic $Topic) {
    Write-Host "Topic exists: $Topic"
    return
  }

  Write-Host "Creating topic: $Topic"

  $args = @(
    $KafkaTopics,
    "--create",
    "--if-not-exists",
    "--topic", $Topic,
    "--bootstrap-server", $BootstrapServer,
    "--partitions", $Partitions.ToString(),
    "--replication-factor", $ReplicationFactor.ToString()
  )

  if ($null -ne $Config) {
    foreach ($key in $Config.Keys) {
      $args += @("--config", "$key=$($Config[$key])")
    }
  }

  $code = Invoke-DockerQuiet -ResolvedContainerId $ResolvedContainerId -Args $args
  if ($code -ne 0) {
    throw "Failed to create topic '$Topic' (exit=$code). Check logs: docker logs $ResolvedContainerId"
  }
}

function Get-TopicDefinitions {
  return @(
    @{
      name = "dev.flowchat.identity.user.v1"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    }
  )
}

Assert-Command "docker"

$resolvedContainerId = Resolve-ContainerId
Write-Step "Using broker container id: $resolvedContainerId"
Wait-ForKafkaReady -ResolvedContainerId $resolvedContainerId -WaitTimeoutSeconds $TimeoutSeconds

Write-Step "Ensuring topics exist"
foreach ($topic in (Get-TopicDefinitions)) {
  Ensure-Topic `
    -ResolvedContainerId $resolvedContainerId `
    -Topic $topic.name `
    -Partitions $topic.partitions `
    -ReplicationFactor $topic.rf `
    -Config $topic.config
}

Write-Step "Final topic list"
docker exec $resolvedContainerId $KafkaTopics --bootstrap-server $BootstrapServer --list
