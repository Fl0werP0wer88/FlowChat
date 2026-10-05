#requires -Version 5.1
<#
Resets FlowChat Kafka consumer groups by deleting their committed offsets and metadata.

Can be run standalone:
  .\reset-consumer-groups.ps1
  .\reset-consumer-groups.ps1 -ComposeFile ".\docker-compose.kafka.yml"
  .\reset-consumer-groups.ps1 -ContainerId "<docker-container-id>"

Notes:
- Kafka allows deleting only inactive consumer groups.
- If some worker is running, it may recreate its group immediately after deletion.
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

$consumerGroupDefinitionsScript = Join-Path $PSScriptRoot "kafka-consumer-group-definitions.ps1"
if (-not (Test-Path $consumerGroupDefinitionsScript)) {
  throw "Missing shared script: $consumerGroupDefinitionsScript"
}

. $consumerGroupDefinitionsScript

$KafkaConsumerGroups = "$KafkaBin/kafka-consumer-groups.sh"
$KafkaTopics = "$KafkaBin/kafka-topics.sh"

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

if (-not [string]::IsNullOrWhiteSpace($ComposeFile)) {
  $ComposeFile = Resolve-ScriptRelativePath $ComposeFile
}

if (-not [string]::IsNullOrWhiteSpace($KafkaComposeFile)) {
  $KafkaComposeFile = Resolve-ScriptRelativePath $KafkaComposeFile
}

if (-not [string]::IsNullOrWhiteSpace($UiComposeFile)) {
  $UiComposeFile = Resolve-ScriptRelativePath $UiComposeFile
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

function Invoke-DockerCapture {
  param(
    [Parameter(Mandatory = $true)][string]$ResolvedContainerId,
    [Parameter(Mandatory = $true)][string[]]$Args
  )

  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    $output = docker exec $ResolvedContainerId @Args 2>$null
    $exitCode = $LASTEXITCODE
  } finally {
    $ErrorActionPreference = $old
  }

  return [PSCustomObject]@{
    ExitCode = $exitCode
    Output = @($output)
  }
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

function Get-ExistingConsumerGroups([string]$ResolvedContainerId) {
  $result = Invoke-DockerCapture -ResolvedContainerId $ResolvedContainerId -Args @(
    $KafkaConsumerGroups, "--bootstrap-server", $BootstrapServer, "--list"
  )

  if ($result.ExitCode -ne 0) {
    throw "Failed to list consumer groups (exit=$($result.ExitCode)). Check logs: docker logs $ResolvedContainerId"
  }

  return @(
    $result.Output |
      ForEach-Object { $_.ToString().Trim() } |
      Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
  )
}

function Consumer-Group-Exists([string]$ResolvedContainerId, [string]$GroupId) {
  return (Get-ExistingConsumerGroups -ResolvedContainerId $ResolvedContainerId) -contains $GroupId
}

function Reset-ConsumerGroup([string]$ResolvedContainerId, [string]$GroupId) {
  if (-not (Consumer-Group-Exists -ResolvedContainerId $ResolvedContainerId -GroupId $GroupId)) {
    Write-Host "Consumer group missing, skipping: $GroupId"
    return
  }

  Write-Host "Deleting consumer group: $GroupId"

  $code = Invoke-DockerQuiet -ResolvedContainerId $ResolvedContainerId -Args @(
    $KafkaConsumerGroups,
    "--bootstrap-server", $BootstrapServer,
    "--delete",
    "--group", $GroupId
  )

  if ($code -ne 0) {
    throw "Failed to delete consumer group '$GroupId' (exit=$code). Make sure the group is inactive and check logs: docker logs $ResolvedContainerId"
  }
}

Assert-Command "docker"

$resolvedContainerId = Resolve-ContainerId
Write-Step "Using broker container id: $resolvedContainerId"
Wait-ForKafkaReady -ResolvedContainerId $resolvedContainerId -WaitTimeoutSeconds $TimeoutSeconds

Write-Step "Resetting FlowChat consumer groups"
foreach ($groupId in (Get-ConsumerGroupDefinitions)) {
  Reset-ConsumerGroup -ResolvedContainerId $resolvedContainerId -GroupId $groupId
}

Write-Step "Final consumer group list"
docker exec $resolvedContainerId $KafkaConsumerGroups --bootstrap-server $BootstrapServer --list

Write-Step "Done. FlowChat consumer groups were reset"
