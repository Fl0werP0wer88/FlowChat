#requires -Version 5.1
<#
Idempotent MailHog bootstrap:
- Starts MailHog via docker compose
- Waits until the Web UI is reachable
- Ends with a running container and working UI/SMTP

Run:
  .\bootstrap-mailhog.ps1

Optional:
  .\bootstrap-mailhog.ps1 -ComposeFile ".\docker-compose.yml" -ServiceName "mailhog"
#>

param(
  [string]$ProjectName = "flowchat",
  [string]$ComposeFile = ".\docker-compose.yml",
  [string]$ServiceName = "mailhog",
  [string]$UiUrl = "http://localhost:8025",
  [int]$TimeoutSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step($msg) {
  Write-Host "`n==> $msg"
}

function Assert-Command($cmd) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
    throw "Missing required command: $cmd. Make sure it is installed and available in PATH."
  }
}

function Get-ContainerIdForService([string]$service) {
  (docker compose -p $ProjectName -f $ComposeFile ps -q $service 2>$null).Trim()
}

function Wait-ForHttpOk([string]$url, [int]$timeoutSeconds) {
  Write-Step "Waiting for MailHog UI at $url (timeout ${timeoutSeconds}s)..."
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)

  while ((Get-Date) -lt $deadline) {
    try {
      # HEAD is light, but some setups may not like it; fallback to GET automatically.
      $resp = Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing -TimeoutSec 3
      if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
        Write-Host "MailHog UI is reachable ✅"
        return
      }
    } catch {
      # Try GET as fallback (some proxies/servers don't love HEAD)
      try {
        $resp2 = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 3
        if ($resp2.StatusCode -ge 200 -and $resp2.StatusCode -lt 500) {
          Write-Host "MailHog UI is reachable ✅"
          return
        }
      } catch {
        # keep waiting
      }
    }

    Start-Sleep -Seconds 2
  }

  throw "MailHog UI did not become reachable within ${timeoutSeconds}s. Check logs: docker logs mailhog"
}

# -------------------- Main --------------------
Assert-Command "docker"

Write-Step "Starting MailHog via docker compose"
# Idempotent: creates if missing, starts if stopped, leaves it if already running
docker compose -p $ProjectName -f $ComposeFile up -d | Out-Null

$containerId = Get-ContainerIdForService -service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
  throw "Could not find container for service '$ServiceName'. Check: docker compose -p $ProjectName -f $ComposeFile ps"
}

Write-Step "Using container id: $containerId"
Wait-ForHttpOk -url $UiUrl -timeoutSeconds $TimeoutSeconds

Write-Step "MailHog ready"
Write-Host ("SMTP: localhost:1025")
Write-Host ("UI  : {0}" -f $UiUrl)
