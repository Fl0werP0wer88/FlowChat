#requires -Version 5.1
<#
Idempotent PostgreSQL bootstrap for FlowChat (Windows PowerShell)

Creates if missing:
- role: flowchat_app
- databases:
    - flowchat_auth_db
    - flowchat_settings_db

Additionally:
- fixes schema public ownership
- grants CREATE/USAGE on schema
- sets default privileges for EF Core

Safe to run multiple times.
#>

param(
  [string]$ComposeFile = ".\docker-compose.yml",
  [string]$ServiceName = "postgres",

  # Admin (from docker-compose)
  [string]$AdminUser = "flowchat",
  [string]$AdminPassword = "flowchat_pw",

  # App role
  [string]$AppUser = "flowchat_app",
  [string]$AppPassword = "flowchat_app_pw",

  # Databases
  [string]$AuthDb = "flowchat_auth_db",
  [string]$UserProfileDb = "flowchat_userprofile_db",

  [int]$TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step([string]$msg) { Write-Host "`n==> $msg" }

function Assert-Command([string]$cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd"
  }
}

function Get-ContainerIdForService([string]$service) {
  (docker compose -f $ComposeFile ps -q $service 2>$null).Trim()
}

function Wait-ForPostgresReady([string]$containerId, [int]$timeoutSeconds) {
  Write-Step "Waiting for PostgreSQL to be ready (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    $old = $ErrorActionPreference
    try {
      $ErrorActionPreference = "Continue"
      docker exec -e PGPASSWORD=$AdminPassword $containerId pg_isready `
        -h 127.0.0.1 -p 5432 -U $AdminUser -d postgres 2>$null | Out-Null
      if ($LASTEXITCODE -eq 0) {
        Write-Host "PostgreSQL is ready ✅"
        return
      }
    } finally {
      $ErrorActionPreference = $old
    }
    Start-Sleep -Seconds 2
  }

  Write-Step "Last logs:"
  docker logs --tail 80 $containerId
  throw "PostgreSQL did not become ready in time"
}

function Exec-PSQL([string]$containerId, [string]$database, [string]$sql, [switch]$CaptureOutput) {
  $args = @(
    "psql",
    "-h", "127.0.0.1",
    "-p", "5432",
    "-U", $AdminUser,
    "-d", $database,
    "-v", "ON_ERROR_STOP=1",
    "-c", $sql
  )

  $old = $ErrorActionPreference
  try {
    $ErrorActionPreference = "Continue"
    if ($CaptureOutput) {
      $out = docker exec -e PGPASSWORD=$AdminPassword $containerId @args 2>&1 | Out-String
      $code = $LASTEXITCODE
      return @{ Code = $code; Output = $out }
    } else {
      docker exec -e PGPASSWORD=$AdminPassword $containerId @args 2>$null | Out-Null
      $code = $LASTEXITCODE
      return @{ Code = $code; Output = "" }
    }
  } finally {
    $ErrorActionPreference = $old
  }
}

function Ensure-Role([string]$containerId, [string]$role, [string]$password) {
  Write-Step "Ensuring role exists: $role"

  $sqlTemplate = @'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{ROLE}') THEN
    CREATE ROLE {ROLE} LOGIN PASSWORD '{PASSWORD}';
  END IF;
END
$$;
'@

  $sql = $sqlTemplate.Replace('{ROLE}', $role).Replace('{PASSWORD}', $password)
  $res = Exec-PSQL -containerId $containerId -database "postgres" -sql $sql
  if ($res.Code -ne 0) { throw "Failed ensuring role '$role'." }
}

function Ensure-Database([string]$containerId, [string]$dbName, [string]$owner) {
  Write-Step "Ensuring database exists: $dbName"

  # check existence
  $check = Exec-PSQL -containerId $containerId -database "postgres" `
    -sql "SELECT 1 FROM pg_database WHERE datname = '$dbName' LIMIT 1;" `
    -CaptureOutput

  if ($check.Output -match "\b1\b") {
    Write-Host "Database exists: $dbName"
  }
  else {
    $create = Exec-PSQL -containerId $containerId -database "postgres" `
      -sql "CREATE DATABASE $dbName OWNER $owner;"
    if ($create.Code -ne 0) {
      throw "Failed creating database '$dbName'"
    }
    Write-Host "Created database: $dbName"
  }

  # ----- FIX SCHEMA PERMISSIONS (KEY FOR EF) -----
  Write-Step "Fixing schema permissions in $dbName"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "ALTER SCHEMA public OWNER TO $owner;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "GRANT USAGE, CREATE ON SCHEMA public TO $owner;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $owner;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $owner;"
}

# -------------------- MAIN --------------------
Assert-Command "docker"

Write-Step "Starting PostgreSQL via docker compose"
docker compose -f $ComposeFile up -d --remove-orphans | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'"
}

Write-Step "Using container id: $containerId"
Wait-ForPostgresReady -containerId $containerId -timeoutSeconds $TimeoutSeconds

Ensure-Role -containerId $containerId -role $AppUser -password $AppPassword
Ensure-Database -containerId $containerId -dbName $AuthDb -owner $AppUser
Ensure-Database -containerId $containerId -dbName $UserProfileDb -owner $AppUser

Write-Step "Done. PostgreSQL is fully initialized ✅"
Write-Host ""
Write-Host "Admin user : $AdminUser"
Write-Host "App user   : $AppUser"
Write-Host "Auth DB    : $AuthDb"
Write-Host "Settings DB: $UserProfileDb"
Write-Host "Host       : localhost"
Write-Host "Port       : 5432"
