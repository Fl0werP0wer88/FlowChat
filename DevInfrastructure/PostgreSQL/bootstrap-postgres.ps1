#requires -Version 5.1
<#
Idempotent PostgreSQL bootstrap for FlowChat (Windows PowerShell)

Creates if missing:
- role: flowchat_migrator
- role: flowchat_app
- databases:
    - flowchat_auth_db
    - flowchat_userprofile_db
    - flowchat_socialgraph_db
    - flowchat_notification_db

Additionally:
- assigns database and schema ownership to flowchat_migrator
- grants CRUD-only access to flowchat_app
- sets default privileges for future EF Core migrations

Safe to run multiple times.
#>

param(
  [string]$ComposeFile = ".\docker-compose.yml",
  [string]$ServiceName = "postgres",

  # Admin (from docker-compose)
  [string]$AdminUser = "flowchat",
  [string]$AdminPassword = "flowchat_pw",

  # Roles
  [string]$MigratorUser = "flowchat_migrator",
  [string]$MigratorPassword = "flowchat_migrator_pw",
  [string]$AppUser = "flowchat_app",
  [string]$AppPassword = "flowchat_app_pw",

  # Databases
  [string]$AuthDb = "flowchat_auth_db",
  [string]$UserProfileDb = "flowchat_userprofile_db",
  [string]$SocialGraphDb = "flowchat_socialgraph_db",
  [string]$NotificationDb = "flowchat_notification_db",

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

function Ensure-Role([string]$containerId, [string]$role, [string]$password, [switch]$CreateDb) {
  Write-Step "Ensuring role exists: $role"

  $createDbClause = if ($CreateDb) { "CREATEDB" } else { "NOCREATEDB" }
  $sqlTemplate = @'
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{ROLE}') THEN
    CREATE ROLE {ROLE} LOGIN PASSWORD '{PASSWORD}' {CREATEDB};
  ELSE
    ALTER ROLE {ROLE} WITH LOGIN PASSWORD '{PASSWORD}' {CREATEDB};
  END IF;
END
$$;
'@

  $sql = $sqlTemplate.Replace('{ROLE}', $role).Replace('{PASSWORD}', $password).Replace('{CREATEDB}', $createDbClause)
  $res = Exec-PSQL -containerId $containerId -database "postgres" -sql $sql
  if ($res.Code -ne 0) { throw "Failed ensuring role '$role'." }
}

function Grant-Role([string]$containerId, [string]$grantee, [string]$roleName) {
  Write-Step "Granting role $roleName to $grantee"

  $res = Exec-PSQL -containerId $containerId -database "postgres" -sql "GRANT $roleName TO $grantee;"
  if ($res.Code -ne 0) { throw "Failed granting role '$roleName' to '$grantee'." }
}

function Ensure-Database([string]$containerId, [string]$dbName, [string]$owner) {
  Write-Step "Ensuring database exists: $dbName"

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

  Write-Step "Configuring ownership in $dbName"

  Exec-PSQL -containerId $containerId -database "postgres" `
    -sql "ALTER DATABASE $dbName OWNER TO $owner;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "ALTER SCHEMA public OWNER TO $owner;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "REVOKE CREATE ON SCHEMA public FROM PUBLIC;"

  $ownershipSql = @"
DO $$
DECLARE
  item record;
BEGIN
  FOR item IN
    SELECT tablename
    FROM pg_tables
    WHERE schemaname = 'public'
  LOOP
    EXECUTE format('ALTER TABLE public.%I OWNER TO $owner;', item.tablename);
  END LOOP;

  FOR item IN
    SELECT sequence_name
    FROM information_schema.sequences
    WHERE sequence_schema = 'public'
  LOOP
    EXECUTE format('ALTER SEQUENCE public.%I OWNER TO $owner;', item.sequence_name);
  END LOOP;

  FOR item IN
    SELECT table_name
    FROM information_schema.views
    WHERE table_schema = 'public'
  LOOP
    EXECUTE format('ALTER VIEW public.%I OWNER TO $owner;', item.table_name);
  END LOOP;
END
$$;
"@

  Exec-PSQL -containerId $containerId -database $dbName -sql $ownershipSql | Out-Null
}

function Ensure-AppCrudAccess([string]$containerId, [string]$dbName, [string]$owner, [string]$appRole) {
  Write-Step "Granting CRUD access to $appRole in $dbName"

  Exec-PSQL -containerId $containerId -database "postgres" `
    -sql "GRANT CONNECT ON DATABASE $dbName TO $appRole;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "GRANT USAGE ON SCHEMA public TO $appRole;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "REVOKE CREATE ON SCHEMA public FROM $appRole;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $appRole;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO $appRole;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "ALTER DEFAULT PRIVILEGES FOR ROLE $owner IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $appRole;"

  Exec-PSQL -containerId $containerId -database $dbName `
    -sql "ALTER DEFAULT PRIVILEGES FOR ROLE $owner IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO $appRole;"
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

Ensure-Role -containerId $containerId -role $MigratorUser -password $MigratorPassword -CreateDb
Grant-Role -containerId $containerId -grantee $MigratorUser -roleName "pg_signal_backend"
Ensure-Role -containerId $containerId -role $AppUser -password $AppPassword

Ensure-Database -containerId $containerId -dbName $AuthDb -owner $MigratorUser
Ensure-AppCrudAccess -containerId $containerId -dbName $AuthDb -owner $MigratorUser -appRole $AppUser

Ensure-Database -containerId $containerId -dbName $UserProfileDb -owner $MigratorUser
Ensure-AppCrudAccess -containerId $containerId -dbName $UserProfileDb -owner $MigratorUser -appRole $AppUser

Ensure-Database -containerId $containerId -dbName $SocialGraphDb -owner $MigratorUser
Ensure-AppCrudAccess -containerId $containerId -dbName $SocialGraphDb -owner $MigratorUser -appRole $AppUser

Ensure-Database -containerId $containerId -dbName $NotificationDb -owner $MigratorUser
Ensure-AppCrudAccess -containerId $containerId -dbName $NotificationDb -owner $MigratorUser -appRole $AppUser

Write-Step "Done. PostgreSQL is fully initialized ✅"
Write-Host ""
Write-Host "Admin user      : $AdminUser"
Write-Host "Migrator user   : $MigratorUser"
Write-Host "App user        : $AppUser"
Write-Host "Auth DB         : $AuthDb"
Write-Host "UserProfile DB  : $UserProfileDb"
Write-Host "SocialGraph DB  : $SocialGraphDb"
Write-Host "Notification DB : $NotificationDb"
Write-Host "Host            : localhost"
Write-Host "Port            : 5432"
