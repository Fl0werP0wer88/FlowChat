#requires -Version 5.1
<#
Idempotent Infisical bootstrap:
- Creates a local .env with generated secrets on first run
- Starts Infisical, PostgreSQL and Redis via docker compose
- Waits until Infisical responds on the health endpoint
- Leaves the stack running if it is already up

Run:
  .\bootstrap-infisical.ps1

Optional:
  .\bootstrap-infisical.ps1 -Port 18180
#>

param(
  [string]$ComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Infisical/docker-compose.yml"),
  [string]$EnvFile = (Join-Path $PSScriptRoot "../../Infrastructure/Infisical/.env"),
  [string]$ProjectName = "flowchat",
  [string]$ServiceName = "backend",
  [int]$Port = 18180,
  [int]$TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step([string]$Message) {
  Write-Host "`n==> $Message"
}

function Assert-Command([string]$CommandName) {
  if (-not (Get-Command $CommandName -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $CommandName. Make sure it is installed and available in PATH."
  }
}

function Ensure-DockerVolume([string]$volumeName) {
  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    docker volume inspect $volumeName 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
      docker volume create $volumeName | Out-Null
      if ($LASTEXITCODE -ne 0) { throw "Failed to create Docker volume '$volumeName'." }
    }
  } finally {
    $ErrorActionPreference = $old
  }
}

function Resolve-LocalPath([string]$Path) {
  if ([System.IO.Path]::IsPathRooted($Path)) {
    return $Path
  }

  return Join-Path (Resolve-Path (Join-Path $PSScriptRoot "../../Infrastructure/Infisical")) $Path
}

function New-RandomHex([int]$ByteCount) {
  $bytes = New-Object byte[] $ByteCount
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  return ([System.BitConverter]::ToString($bytes) -replace "-", "").ToLowerInvariant()
}

function New-RandomBase64([int]$ByteCount) {
  $bytes = New-Object byte[] $ByteCount
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  return [Convert]::ToBase64String($bytes)
}

function Ensure-EnvFile([string]$TargetPath, [int]$HostPort) {
  if (Test-Path $TargetPath) {
    Write-Step "Using existing environment file: $TargetPath"

    $lines = [System.IO.File]::ReadAllLines($TargetPath)
    $legacyRedisUrl = 'REDIS_URL=redis://redis:6379'
    if ($lines -contains $legacyRedisUrl) {
      $updatedLines = @($lines | ForEach-Object {
        if ($_ -eq $legacyRedisUrl) { 'REDIS_URL=redis://infisical-redis:6379' } else { $_ }
      })
      $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
      [System.IO.File]::WriteAllLines($TargetPath, $updatedLines, $utf8WithoutBom)
      Write-Host 'Updated the local Redis endpoint for the unified FlowChat Compose project'
    }

    return
  }

  Write-Step "Creating local environment file: $TargetPath"

  $postgresUser = "infisical"
  $postgresPassword = New-RandomHex -ByteCount 16
  $postgresDb = "infisical"
  $encryptionKey = New-RandomHex -ByteCount 16
  $authSecret = New-RandomBase64 -ByteCount 32
  $siteUrl = "http://localhost:$HostPort"
  $dbConnectionUri = "postgres://${postgresUser}:${postgresPassword}@db:5432/${postgresDb}"

  $content = @(
    "INFISICAL_PORT=$HostPort",
    "ENCRYPTION_KEY=$encryptionKey",
    "AUTH_SECRET=$authSecret",
    "POSTGRES_USER=$postgresUser",
    "POSTGRES_PASSWORD=$postgresPassword",
    "POSTGRES_DB=$postgresDb",
    "DB_CONNECTION_URI=$dbConnectionUri",
    "REDIS_URL=redis://infisical-redis:6379",
    "SITE_URL=$siteUrl",
    "HTTPS_ENABLED=false"
  )

  $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllLines($TargetPath, $content, $utf8WithoutBom)
}

function Get-EnvValue([string]$TargetPath, [string]$Key) {
  if (-not (Test-Path $TargetPath)) {
    return $null
  }

  $prefix = "$Key="
  foreach ($line in [System.IO.File]::ReadAllLines($TargetPath)) {
    if ($line.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
      return $line.Substring($prefix.Length)
    }
  }

  return $null
}

function Get-ComposeCommandArgs([string]$Project, [string]$ComposePath) {
  return @("compose", "-p", $Project, "-f", $ComposePath)
}

function Invoke-DockerCompose([string[]]$ComposeArgs, [string[]]$ExtraArgs) {
  & docker @ComposeArgs @ExtraArgs
  if ($LASTEXITCODE -ne 0) {
    throw "docker compose command failed with exit code $LASTEXITCODE"
  }
}

function Get-ContainerIdForService([string[]]$ComposeArgs, [string]$Name) {
  $containerId = (& docker @ComposeArgs ps -q $Name 2>$null | Out-String).Trim()
  return $containerId
}

function Wait-ForHttpOk([string]$HealthUrl, [int]$TimeoutInSeconds, [string[]]$ComposeArgs, [string]$BackendServiceName) {
  Write-Step "Waiting for Infisical health endpoint at $HealthUrl (timeout ${TimeoutInSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($TimeoutInSeconds)

  while ((Get-Date) -lt $deadline) {
    try {
      $response = Invoke-WebRequest -Uri $HealthUrl -Method Get -UseBasicParsing -TimeoutSec 5
      if ($response.StatusCode -eq 200) {
        Write-Host "Infisical health endpoint is reachable"
        return
      }
    } catch {
      Start-Sleep -Seconds 3
      continue
    }

    Start-Sleep -Seconds 3
  }

  Write-Step "Backend logs:"
  & docker @ComposeArgs logs --tail 120 $BackendServiceName
  throw "Infisical did not become ready within ${TimeoutInSeconds}s."
}

$resolvedComposeFile = Resolve-LocalPath -Path $ComposeFile
$resolvedEnvFile = Resolve-LocalPath -Path $EnvFile

if (-not (Test-Path $resolvedComposeFile)) {
  throw "Missing docker compose file: $resolvedComposeFile"
}

Assert-Command "docker"
Ensure-DockerVolume -volumeName "flowchat-infisical_pg_data"
Ensure-DockerVolume -volumeName "flowchat-infisical_redis_data"
Ensure-EnvFile -TargetPath $resolvedEnvFile -HostPort $Port

$composeArgs = Get-ComposeCommandArgs -Project $ProjectName -ComposePath $resolvedComposeFile
$configuredPort = Get-EnvValue -TargetPath $resolvedEnvFile -Key "INFISICAL_PORT"
$effectivePort = $Port
if (-not [string]::IsNullOrWhiteSpace($configuredPort)) {
  $effectivePort = [int]$configuredPort
}

$healthUrl = "http://localhost:$effectivePort/api/status"
$uiUrl = "http://localhost:$effectivePort"

Write-Step "Starting Infisical via docker compose"
# Idempotent: creates containers if missing, starts them if stopped, and keeps them running if already up
Invoke-DockerCompose -ComposeArgs $composeArgs -ExtraArgs @("up", "-d")

$containerId = Get-ContainerIdForService -ComposeArgs $composeArgs -Name $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -p $ProjectName -f $resolvedComposeFile ps"
}

Write-Step "Using backend container id: $containerId"
Wait-ForHttpOk -HealthUrl $healthUrl -TimeoutInSeconds $TimeoutSeconds -ComposeArgs $composeArgs -BackendServiceName $ServiceName

Write-Step "Infisical ready"
Write-Host "UI                : $uiUrl"
Write-Host "Health            : $healthUrl"
Write-Host "Signup / login    : create the first account in the UI"
Write-Host "Admin note        : the first account becomes the server administrator"
