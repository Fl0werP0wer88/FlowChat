$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $PSScriptRoot 'FlowChat.TestGeneration.psm1'

Import-Module $modulePath -Force

try {
    $result = Start-TestGenerationRun -RepoRoot $repoRoot -ArgumentList $args

    if ($result.HelpRequested) {
        Write-Host $result.Usage
        exit 0
    }

    $manifestRelativePath = Get-TestGenerationRelativePath -BasePath $repoRoot -TargetPath $result.ManifestPath
    $summaryRelativePath = Get-TestGenerationRelativePath -BasePath $repoRoot -TargetPath $result.SummaryPath

    Write-Host "Run ID: $($result.RunId)"
    Write-Host "Manifest: $manifestRelativePath"
    Write-Host "Summary: $summaryRelativePath"

    if (Test-Path -LiteralPath $result.SummaryPath) {
        Write-Host ''
        Write-Host ([System.IO.File]::ReadAllText($result.SummaryPath, [System.Text.Encoding]::UTF8))
    }
}
catch {
    Write-Error $_
    exit 1
}
