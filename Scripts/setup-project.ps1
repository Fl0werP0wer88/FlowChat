#requires -Version 5.1
<#
.SYNOPSIS
Prepares the complete local FlowChat development environment.

.DESCRIPTION
Validates host prerequisites, pulls and starts Docker infrastructure, restores
dependencies, builds the solution, applies database migrations, and reports the
local endpoints needed for debugging. The script is idempotent and does not reset
databases, Kafka topics, or Docker volumes.

.EXAMPLE
.\Scripts\setup-project.ps1

.EXAMPLE
.\Scripts\setup-project.ps1 -SkipInfisical -SkipObservability
#>

[CmdletBinding()]
param(
    [switch]$SkipPull,
    [switch]$SkipBuild,
    [switch]$SkipMigrations,
    [switch]$SkipInfisical,
    [switch]$SkipObservability
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solutionPath = Join-Path $repoRoot 'FlowChat.slnx'
$workspacePath = Join-Path $repoRoot 'FlowChat.code-workspace'
$clientRoot = Join-Path $repoRoot 'ReactClient'
$clientPackageLock = Join-Path $clientRoot 'package-lock.json'

$postgresRoot = Join-Path $PSScriptRoot 'PostgreSQL'
$postgresCompose = Join-Path $postgresRoot 'docker-compose.yml'
$postgresBootstrap = Join-Path $postgresRoot 'bootstrap-postgres.ps1'
$migrateAll = Join-Path $postgresRoot 'migrate-all.ps1'

$kafkaRoot = Join-Path $PSScriptRoot 'Kafka'
$kafkaCompose = Join-Path $kafkaRoot 'docker-compose.kafka.yml'
$kafkaUiCompose = Join-Path $kafkaRoot 'docker-compose.kafka-ui.yml'
$kafkaBootstrap = Join-Path $kafkaRoot 'bootstrap-kafka-stack.ps1'

$redisRoot = Join-Path $PSScriptRoot 'Redis'
$redisCompose = Join-Path $redisRoot 'docker-compose.yml'
$redisInsightCompose = Join-Path $redisRoot 'docker-compose.redisinsight.yml'
$redisBootstrap = Join-Path $redisRoot 'bootstrap-redis-stack.ps1'

$mailHogRoot = Join-Path $PSScriptRoot 'MailHog'
$mailHogCompose = Join-Path $mailHogRoot 'docker-compose.yml'
$mailHogBootstrap = Join-Path $mailHogRoot 'bootstrap-mailhog.ps1'

$infisicalRoot = Join-Path $PSScriptRoot 'Infisical'
$infisicalCompose = Join-Path $infisicalRoot 'docker-compose.yml'
$infisicalBootstrap = Join-Path $infisicalRoot 'bootstrap-infisical.ps1'

$observabilityRoot = Join-Path $PSScriptRoot 'Observability'
$observabilityComposeFiles = @(
    (Join-Path $observabilityRoot 'Loki\docker-compose.yml'),
    (Join-Path $observabilityRoot 'Tempo\docker-compose.yml'),
    (Join-Path $observabilityRoot 'Prometheus\docker-compose.yml'),
    (Join-Path $observabilityRoot 'Alloy\docker-compose.yml'),
    (Join-Path $observabilityRoot 'Grafana\docker-compose.yml')
)
$observabilityBootstrap = Join-Path $observabilityRoot 'bootstrap-observability-stack.ps1'

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Success([string]$Message) {
    Write-Host "[ready] $Message" -ForegroundColor Green
}

function Write-WarningMessage([string]$Message) {
    Write-Host "[warning] $Message" -ForegroundColor Yellow
}

function Assert-Command([string]$CommandName, [string]$InstallHint) {
    if (-not (Get-Command $CommandName -ErrorAction SilentlyContinue)) {
        throw "Missing required command '$CommandName'. $InstallHint"
    }
}

function Assert-PathExists([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing ${Label}: $Path"
    }
}

function Invoke-NativeCommand([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command $($Arguments -join ' ')"
    }
}

function Invoke-Stage([string]$Name, [scriptblock]$Action) {
    Write-Step $Name

    try {
        & $Action
        Write-Success $Name
    }
    catch {
        throw "Stage '$Name' failed. $($_.Exception.Message)"
    }
}

function Test-TrustedDevelopmentCertificate {
    $previousErrorActionPreference = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        & dotnet dev-certs https --check --trust *> $null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Show-ComposeStatus([string]$Name, [string[]]$ComposeArguments) {
    Write-Host "`n${Name}:"
    & docker @ComposeArguments ps
    if ($LASTEXITCODE -ne 0) {
        Write-WarningMessage "Could not read Docker Compose status for $Name"
    }
}

Invoke-Stage 'Checking local prerequisites' {
    Assert-Command 'docker' 'Install and start Docker Desktop, then rerun this script.'
    Assert-Command 'dotnet' 'Install the .NET 10 SDK, then rerun this script.'
    Assert-Command 'node' 'Install Node.js 20 or later, then rerun this script.'
    Assert-Command 'npm.cmd' 'Install npm with Node.js and ensure npm.cmd is available in PATH.'

    $requiredPaths = @(
        $solutionPath,
        $workspacePath,
        $clientPackageLock,
        $postgresCompose,
        $postgresBootstrap,
        $migrateAll,
        $kafkaCompose,
        $kafkaUiCompose,
        $kafkaBootstrap,
        $redisCompose,
        $redisInsightCompose,
        $redisBootstrap,
        $mailHogCompose,
        $mailHogBootstrap
    )

    if (-not $SkipInfisical) {
        $requiredPaths += @($infisicalCompose, $infisicalBootstrap)
    }

    if (-not $SkipObservability) {
        $requiredPaths += $observabilityComposeFiles
        $requiredPaths += $observabilityBootstrap
    }

    foreach ($requiredPath in $requiredPaths) {
        Assert-PathExists -Path $requiredPath -Label 'repository file'
    }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $dockerServerVersion = (& docker version --format '{{.Server.Version}}' 2>$null | Out-String).Trim()
        $dockerVersionExitCode = $LASTEXITCODE
        $composeVersion = (& docker compose version --short 2>$null | Out-String).Trim()
        $composeVersionExitCode = $LASTEXITCODE
        $sdkVersions = @(& dotnet --list-sdks 2>$null)
        $sdkExitCode = $LASTEXITCODE
        $efVersionOutput = (& dotnet ef --version 2>$null | Out-String).Trim()
        $efExitCode = $LASTEXITCODE
        $nodeVersionOutput = (& node --version 2>$null | Out-String).Trim()
        $nodeExitCode = $LASTEXITCODE
        $npmVersionOutput = (& npm.cmd --version 2>$null | Out-String).Trim()
        $npmExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($dockerVersionExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($dockerServerVersion)) {
        throw 'Docker Engine is not reachable. Start Docker Desktop and wait until the engine is ready.'
    }

    if ($composeVersionExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($composeVersion)) {
        throw "Docker Compose is unavailable. Install the Docker Compose plugin included with Docker Desktop."
    }

    if ($sdkExitCode -ne 0 -or -not ($sdkVersions | Where-Object { $_ -match '^10\.' })) {
        throw 'The .NET 10 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/10.0.'
    }

    if ($efExitCode -ne 0 -or $efVersionOutput -notmatch '(?m)^10\.') {
        throw "The EF Core 10 CLI is required. Install it with: dotnet tool install --global dotnet-ef --version '10.*'"
    }

    $nodeVersionMatch = [regex]::Match($nodeVersionOutput, '^v?(\d+)\.')
    if ($nodeExitCode -ne 0 -or -not $nodeVersionMatch.Success) {
        throw 'Could not determine the installed Node.js version.'
    }

    if ([int]$nodeVersionMatch.Groups[1].Value -lt 20) {
        throw "Node.js 20 or later is required. Installed version: $nodeVersionOutput"
    }

    if ($npmExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($npmVersionOutput)) {
        throw 'npm is unavailable. Repair the Node.js installation and ensure npm.cmd is available in PATH.'
    }

    Write-Host "PowerShell    : $($PSVersionTable.PSVersion)"
    Write-Host "Docker Engine: $dockerServerVersion"
    Write-Host "Docker Compose: $composeVersion"
    Write-Host "Node.js       : $nodeVersionOutput"
    Write-Host "npm           : $npmVersionOutput"
    Write-Host "EF Core CLI   : $($efVersionOutput -replace "`r?`n", ' ')"
    Write-Host ".NET 10 SDK   : available"
}

if (-not $SkipPull) {
    Invoke-Stage 'Pulling PostgreSQL image' {
        Invoke-NativeCommand 'docker' @('compose', '-f', $postgresCompose, 'pull')
    }

    Invoke-Stage 'Pulling Kafka and Kafka UI images' {
        Invoke-NativeCommand 'docker' @('compose', '-p', 'flowchat-kafka', '-f', $kafkaCompose, '-f', $kafkaUiCompose, 'pull')
    }

    Invoke-Stage 'Pulling Redis and RedisInsight images' {
        Invoke-NativeCommand 'docker' @('compose', '-f', $redisCompose, 'pull')
        Invoke-NativeCommand 'docker' @('compose', '-f', $redisInsightCompose, 'pull')
    }

    Invoke-Stage 'Pulling MailHog image' {
        Invoke-NativeCommand 'docker' @('compose', '-f', $mailHogCompose, 'pull')
    }

    if (-not $SkipInfisical) {
        Invoke-Stage 'Pulling Infisical images' {
            Invoke-NativeCommand 'docker' @('pull', 'infisical/infisical:latest')
            Invoke-NativeCommand 'docker' @('pull', 'postgres:14-alpine')
            Invoke-NativeCommand 'docker' @('pull', 'redis:7-alpine')
        }
    }

    if (-not $SkipObservability) {
        Invoke-Stage 'Pulling observability images' {
            $arguments = @('compose', '-p', 'flowchat-observability', '--project-directory', $observabilityRoot)
            foreach ($composeFile in $observabilityComposeFiles) {
                $arguments += @('-f', $composeFile)
            }
            $arguments += 'pull'
            Invoke-NativeCommand 'docker' $arguments
        }
    }
}

Invoke-Stage 'Bootstrapping PostgreSQL databases and roles' {
    & $postgresBootstrap -ComposeFile $postgresCompose
}

Invoke-Stage 'Bootstrapping Kafka, Kafka UI, and topics' {
    & $kafkaBootstrap -KafkaComposeFile $kafkaCompose -UiComposeFile $kafkaUiCompose
}

Invoke-Stage 'Bootstrapping Redis and RedisInsight' {
    & $redisBootstrap -RedisComposeFile $redisCompose -RedisInsightComposeFile $redisInsightCompose
}

Invoke-Stage 'Bootstrapping MailHog' {
    & $mailHogBootstrap -ComposeFile $mailHogCompose
}

if (-not $SkipInfisical) {
    Invoke-Stage 'Bootstrapping Infisical' {
        & $infisicalBootstrap -ComposeFile $infisicalCompose -EnvFile (Join-Path $infisicalRoot '.env')
    }
}

if (-not $SkipObservability) {
    Invoke-Stage 'Bootstrapping observability stack' {
        & $observabilityBootstrap
    }
}

Invoke-Stage 'Restoring backend dependencies' {
    Invoke-NativeCommand 'dotnet' @('restore', $solutionPath)
}

Invoke-Stage 'Installing frontend dependencies' {
    Invoke-NativeCommand 'npm.cmd' @('ci', '--prefix', $clientRoot)
}

if (-not $SkipBuild) {
    Invoke-Stage 'Building backend' {
        Invoke-NativeCommand 'dotnet' @('build', $solutionPath, '--no-restore')
    }

    Invoke-Stage 'Building frontend' {
        Invoke-NativeCommand 'npm.cmd' @('run', 'build', '--prefix', $clientRoot)
    }
}

if (-not $SkipMigrations) {
    Invoke-Stage 'Applying database migrations' {
        if ($SkipBuild) {
            & $migrateAll
        }
        else {
            & $migrateAll -SkipBuild
        }
    }
}

Write-Step 'Checking the ASP.NET Core development HTTPS certificate'
if (Test-TrustedDevelopmentCertificate) {
    Write-Success 'The development HTTPS certificate exists and is trusted'
}
else {
    Write-WarningMessage 'The development HTTPS certificate is missing or is not trusted.'
    Write-Host 'Run this once before debugging HTTPS endpoints: dotnet dev-certs https --trust'
}

Write-Step 'Docker status'
Show-ComposeStatus 'PostgreSQL' @('compose', '-f', $postgresCompose)
Show-ComposeStatus 'Kafka' @('compose', '-p', 'flowchat-kafka', '-f', $kafkaCompose, '-f', $kafkaUiCompose)
Show-ComposeStatus 'Redis' @('compose', '-f', $redisCompose)
Show-ComposeStatus 'RedisInsight' @('compose', '-f', $redisInsightCompose)
Show-ComposeStatus 'MailHog' @('compose', '-f', $mailHogCompose)

if (-not $SkipInfisical) {
    Show-ComposeStatus 'Infisical' @('compose', '-p', 'flowchat-infisical', '-f', $infisicalCompose)
}

if (-not $SkipObservability) {
    $observabilityStatusArguments = @('compose', '-p', 'flowchat-observability', '--project-directory', $observabilityRoot)
    foreach ($composeFile in $observabilityComposeFiles) {
        $observabilityStatusArguments += @('-f', $composeFile)
    }
    Show-ComposeStatus 'Observability' $observabilityStatusArguments
}

Write-Step 'FlowChat local environment is ready'
Write-Host 'Gateway          : https://localhost:7270'
Write-Host 'React client     : http://localhost:5173'
Write-Host 'Kafka UI         : http://localhost:8080'
Write-Host 'RedisInsight     : http://localhost:5540'
Write-Host 'MailHog          : http://localhost:8025'
if (-not $SkipInfisical) {
    Write-Host 'Infisical        : http://localhost:18180'
}
if (-not $SkipObservability) {
    Write-Host 'Grafana          : http://localhost:3000'
    Write-Host 'Prometheus       : http://localhost:9090'
}
Write-Host ''
Write-Host "Open '$workspacePath' and start the 'All Services' compound."
