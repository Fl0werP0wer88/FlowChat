#requires -Version 5.1
<#
Idempotent Redis stack bootstrap:
- Starts Redis via bootstrap-redis.ps1
- Starts RedisInsight via bootstrap-redisinsight.ps1
- Ends with the full local Redis stack ready for FlowChat

Run:
  .\bootstrap-redis-stack.ps1
#>

param(
  [string]$ProjectName = "flowchat",
  [string]$RedisComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Redis/docker-compose.yml"),
  [string]$RedisServiceName = "redis",
  [string]$RedisInsightComposeFile = (Join-Path $PSScriptRoot "../../Infrastructure/Redis/docker-compose.redisinsight.yml"),
  [string]$RedisInsightServiceName = "redisinsight",
  [string]$RedisInsightUiUrl = "http://localhost:5540",
  [string]$RedisUsername = "default",
  [string]$RedisPassword = "flowchat_redis_pw",
  [int]$RedisTimeoutSeconds = 60,
  [int]$RedisInsightTimeoutSeconds = 120
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

$redisBootstrapScript = Join-Path $PSScriptRoot "bootstrap-redis.ps1"
$redisInsightBootstrapScript = Join-Path $PSScriptRoot "bootstrap-redisinsight.ps1"

Assert-PathExists -path $redisBootstrapScript -label "Redis bootstrap script"
Assert-PathExists -path $redisInsightBootstrapScript -label "RedisInsight bootstrap script"

Write-Step "Bootstrapping Redis"
& $redisBootstrapScript `
  -ProjectName $ProjectName `
  -ComposeFile $RedisComposeFile `
  -ServiceName $RedisServiceName `
  -RedisUsername $RedisUsername `
  -RedisPassword $RedisPassword `
  -TimeoutSeconds $RedisTimeoutSeconds

Write-Step "Bootstrapping RedisInsight"
& $redisInsightBootstrapScript `
  -ProjectName $ProjectName `
  -ComposeFile $RedisInsightComposeFile `
  -ServiceName $RedisInsightServiceName `
  -UiUrl $RedisInsightUiUrl `
  -RedisUsername $RedisUsername `
  -RedisPassword $RedisPassword `
  -TimeoutSeconds $RedisInsightTimeoutSeconds `
  -SkipRedisBootstrap

Write-Step "Redis stack ready"
Write-Host "Redis         : localhost:6379"
Write-Host "RedisInsight  : $RedisInsightUiUrl"
Write-Host "Username      : $RedisUsername"
Write-Host "Password      : $RedisPassword"
