#requires -Version 5.1
<#
Idempotent Kafka bootstrap:
- Starts Kafka via docker compose
- Waits until Kafka is ready
- Ensures topics exist (create if missing)

Run:
  .\bootstrap-kafka.ps1

Optional:
  .\bootstrap-kafka.ps1 -ComposeFile ".\docker-compose.yml" -ServiceName "broker"
#>

param(
  [string]$ComposeFile = ".\docker-compose.yml",
  [string]$ServiceName = "broker"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Fix console encoding so ✅ doesn't turn into âœ…
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$KafkaTopics = "/opt/kafka/bin/kafka-topics.sh"

function Write-Step($msg) {
  Write-Host "`n==> $msg"
}

function Assert-Command($cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd. Make sure it is installed and available in PATH."
  }
}

function Get-ContainerIdForService([string]$service) {
  (docker compose -f $ComposeFile ps -q $service 2>$null).Trim()
}

function Invoke-DockerQuiet {
  param(
    [Parameter(Mandatory=$true)][string]$ContainerId,
    [Parameter(Mandatory=$true)][string[]]$Args
  )

  # We want to decide based on exit code, not on stderr text.
  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    # Redirect stderr to null to avoid NativeCommandError noise
    docker exec $ContainerId @Args 2>$null | Out-Null
  } finally {
    $ErrorActionPreference = $old
  }

  return $LASTEXITCODE
}

function Wait-ForKafkaReady([string]$containerId, [int]$timeoutSeconds = 90) {
  Write-Step "Waiting for Kafka to be ready (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    $code = Invoke-DockerQuiet -ContainerId $containerId -Args @(
      $KafkaTopics, "--bootstrap-server", "localhost:9092", "--list"
    )

    if ($code -eq 0) {
      Write-Host "Kafka is ready ✅"
      return
    }

    Start-Sleep -Seconds 2
  }

  throw "Kafka did not become ready within ${timeoutSeconds}s. Check logs: docker logs $containerId"
}

function Topic-Exists([string]$containerId, [string]$topic) {
  $code = Invoke-DockerQuiet -ContainerId $containerId -Args @(
    $KafkaTopics, "--bootstrap-server", "localhost:9092", "--describe", "--topic", $topic
  )
  return ($code -eq 0)
}

function Ensure-Topic(
  [string]$containerId,
  [string]$topic,
  [int]$partitions = 3,
  [int]$replicationFactor = 1,
  [hashtable]$config = $null
) {
  if (Topic-Exists -containerId $containerId -topic $topic) {
    Write-Host "Topic exists: $topic"
    return
  }

  Write-Host "Creating topic: $topic"

  $args = @(
    $KafkaTopics,
    "--create",
    "--if-not-exists",
    "--topic", $topic,
    "--bootstrap-server", "localhost:9092",
    "--partitions", $partitions.ToString(),
    "--replication-factor", $replicationFactor.ToString()
  )

  if ($null -ne $config) {
    foreach ($k in $config.Keys) {
      $args += @("--config", "$k=$($config[$k])")
    }
  }

  $code = Invoke-DockerQuiet -ContainerId $containerId -Args $args

  if ($code -ne 0) {
    throw "Failed to create topic '$topic' (exit=$code). Check logs: docker logs $containerId"
  }
}

# -------------------- Main --------------------
Assert-Command "docker"

Write-Step "Starting Kafka via docker compose"
docker compose -f $ComposeFile up -d --remove-orphans | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForKafkaReady -containerId $containerId -timeoutSeconds 120

$topics = @(
  @{
    name = "dev.flowchat.identity.user.v1"
    partitions = 1
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "2419200000" # 28 days
    }
  },
  @{
    name = "dev.flowchat.identity.user.v1.retry"
    partitions = 1
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "3600000"   # 1 hour
    }
  },
  @{
    name = "dev.flowchat.identity.user.v1.dlq"
    partitions = 1
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "1209600000" # 14 days
    }
  },
  @{
    name = "dev.flowchat.notification.email.v1"
    partitions = 1
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "2419200000" # 28 days
    }
  },
  @{
    name = "dev.flowchat.notification.email.v1.retry"
    partitions = 1
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "3600000"   # 1 hour
    }
  },
  @{
    name = "dev.flowchat.notification.email.v1.dlq"
    partitions = 1
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "1209600000" # 14 days
    }
  }
)

Write-Step "Ensuring topics exist"
foreach ($t in $topics) {
  Ensure-Topic `
    -containerId $containerId `
    -topic $t.name `
    -partitions $t.partitions `
    -replicationFactor $t.rf `
    -config $t.config
}

Write-Step "Final topic list"
docker exec $containerId $KafkaTopics --bootstrap-server localhost:9092 --list

Write-Step "Done. Kafka is up and topics are ensured"
