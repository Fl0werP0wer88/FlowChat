#requires -Version 5.1
<#
Idempotent Kafka + Kafka UI + Topics bootstrap (Windows PowerShell)

- Starts Kafka (KRaft) + Kafka UI as ONE compose project (two compose files)
- Waits for Kafka readiness
- Ensures topics exist (create if missing)
- Prints final topic list + Kafka UI URL

Run:
  .\bootstrap-kafka-stack.ps1

You need:
- docker-compose.kafka.yml  (contains service "broker")
- docker-compose.kafka-ui.yml (contains service "kafka-ui" and points to broker:9092)
#>

param(
  [string]$ProjectName = "flowchat-kafka",
  [string]$KafkaComposeFile = ".\docker-compose.kafka.yml",
  [string]$UiComposeFile    = ".\docker-compose.kafka-ui.yml",

  # Compose service name for Kafka broker
  [string]$KafkaServiceName = "broker",

  # Kafka inside-container tools path
  [string]$KafkaBin = "/opt/kafka/bin",

  # Kafka bootstrap server FROM INSIDE the broker container
  [string]$BootstrapServer = "localhost:9092",

  [int]$TimeoutSeconds = 180
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
  (docker compose -p $ProjectName -f $KafkaComposeFile -f $UiComposeFile ps -q $service 2>$null).Trim()
}

function Wait-ForKafkaReady([string]$containerId, [int]$timeoutSeconds) {
  Write-Step "Waiting for Kafka to be ready (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    $old = $ErrorActionPreference
    try {
      $ErrorActionPreference = "Continue"
      docker exec $containerId sh -lc "$KafkaBin/kafka-topics.sh --bootstrap-server $BootstrapServer --list" *> $null
      if ($LASTEXITCODE -eq 0) {
        Write-Host "Kafka is ready ✅"
        return
      }
    } finally {
      $ErrorActionPreference = $old
    }
    Start-Sleep -Seconds 2
  }

  throw "Kafka did not become ready within ${timeoutSeconds}s. Check logs: docker logs $containerId"
}

function Ensure-Topic(
  [string]$containerId,
  [string]$topic,
  [int]$partitions = 3,
  [int]$replicationFactor = 1,
  [hashtable]$config = $null
) {
  # Check if exists
  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    docker exec $containerId sh -lc "$KafkaBin/kafka-topics.sh --bootstrap-server $BootstrapServer --describe --topic '$topic'" *> $null
    if ($LASTEXITCODE -eq 0) {
      Write-Host "Topic exists: $topic"
      return
    }
  } finally {
    $ErrorActionPreference = $old
  }

  # Build config args
  $configArgs = ""
  if ($null -ne $config) {
    foreach ($k in $config.Keys) {
      $v = $config[$k]
      $configArgs += " --config $k=$v"
    }
  }

  Write-Host "Creating topic: $topic"
  $cmd = "$KafkaBin/kafka-topics.sh --create --if-not-exists --bootstrap-server $BootstrapServer --topic '$topic' --partitions $partitions --replication-factor $replicationFactor$configArgs"

  docker exec $containerId sh -lc $cmd | Out-Null
}

# -------------------- Main --------------------
Assert-Command "docker"

Write-Step "Starting Kafka + Kafka UI as one compose project: $ProjectName"
docker compose -p $ProjectName -f $KafkaComposeFile -f $UiComposeFile up -d --remove-orphans | Out-Null

$brokerId = Get-ContainerIdForService -service $KafkaServiceName
if ([string]::IsNullOrWhiteSpace($brokerId)) {
  throw "Could not find container for service '$KafkaServiceName'. Check: docker compose -p $ProjectName -f $KafkaComposeFile -f $UiComposeFile ps"
}

Write-Step "Using broker container id: $brokerId"
Wait-ForKafkaReady -containerId $brokerId -timeoutSeconds $TimeoutSeconds

Write-Step "Ensuring topics exist"
$topics = @(
  @{
    name = "flowchat.identity.user.created.v1"
    partitions = 3
    rf = 1
    config = @{ "cleanup.policy" = "compact" }
  },
  @{
    name = "flowchat.identity.user.created.v1.retry"
    partitions = 3
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "3600000"   # 1 hour
    }
  },
  @{
    name = "flowchat.identity.user.created.v1.dlq"
    partitions = 3
    rf = 1
    config = @{
      "cleanup.policy" = "delete"
      "retention.ms"  = "1209600000" # 14 days
    }
  }
)

foreach ($t in $topics) {
  Ensure-Topic -containerId $brokerId -topic $t.name -partitions $t.partitions -replicationFactor $t.rf -config $t.config
}

Write-Step "Final topic list"
docker exec $brokerId sh -lc "$KafkaBin/kafka-topics.sh --bootstrap-server $BootstrapServer --list"

Write-Step "Kafka UI"
Write-Host "http://localhost:8080/"

Write-Step "Done ✅"