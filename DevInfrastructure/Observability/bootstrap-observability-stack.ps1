#requires -Version 5.1
<#
Idempotent FlowChat observability stack bootstrap:
- Starts Loki
- Starts Tempo
- Starts Prometheus
- Starts Grafana Alloy
- Starts Grafana

Run:
  .\bootstrap-observability-stack.ps1
#>

param(
  [string]$NetworkName = "flowchat-observability"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step([string]$msg) {
  Write-Host "`n==> $msg"
}

function Assert-PathExists([string]$path, [string]$label) {
  if (-not (Test-Path $path)) {
    throw "Missing ${label}: $path"
  }
}

function Invoke-Bootstrap([string]$scriptPath, [hashtable]$parameters) {
  Assert-PathExists $scriptPath "bootstrap script"
  & $scriptPath @parameters
}

$observabilityRoot = $PSScriptRoot

$lokiBootstrap = Join-Path $observabilityRoot "Loki\bootstrap-loki.ps1"
$tempoBootstrap = Join-Path $observabilityRoot "Tempo\bootstrap-tempo.ps1"
$prometheusBootstrap = Join-Path $observabilityRoot "Prometheus\bootstrap-prometheus.ps1"
$alloyBootstrap = Join-Path $observabilityRoot "Alloy\bootstrap-alloy.ps1"
$grafanaBootstrap = Join-Path $observabilityRoot "Grafana\bootstrap-grafana.ps1"

Write-Step "Starting FlowChat observability stack"

Invoke-Bootstrap -scriptPath $lokiBootstrap -parameters @{
  ComposeFile = (Join-Path $observabilityRoot "Loki\docker-compose.yml")
  ConfigFile = (Join-Path $observabilityRoot "Loki\loki-config.yml")
  NetworkName = $NetworkName
}

Invoke-Bootstrap -scriptPath $tempoBootstrap -parameters @{
  ComposeFile = (Join-Path $observabilityRoot "Tempo\docker-compose.yml")
  ConfigFile = (Join-Path $observabilityRoot "Tempo\tempo.yml")
  NetworkName = $NetworkName
}

Invoke-Bootstrap -scriptPath $prometheusBootstrap -parameters @{
  ComposeFile = (Join-Path $observabilityRoot "Prometheus\docker-compose.yml")
  ConfigFile = (Join-Path $observabilityRoot "Prometheus\prometheus.yml")
  NetworkName = $NetworkName
}

Invoke-Bootstrap -scriptPath $alloyBootstrap -parameters @{
  ComposeFile = (Join-Path $observabilityRoot "Alloy\docker-compose.yml")
  ConfigFile = (Join-Path $observabilityRoot "Alloy\config.alloy")
  NetworkName = $NetworkName
}

Invoke-Bootstrap -scriptPath $grafanaBootstrap -parameters @{
  ComposeFile = (Join-Path $observabilityRoot "Grafana\docker-compose.yml")
  ProvisioningPath = (Join-Path $observabilityRoot "Grafana\provisioning")
  NetworkName = $NetworkName
}

Write-Step "FlowChat observability stack ready"
Write-Host "Grafana      : http://localhost:3000"
Write-Host "Prometheus   : http://localhost:9090"
Write-Host "Loki         : http://localhost:3100"
Write-Host "Tempo        : http://localhost:3200"
Write-Host "OTLP gRPC    : http://localhost:4317"
Write-Host "OTLP HTTP    : http://localhost:4318"
