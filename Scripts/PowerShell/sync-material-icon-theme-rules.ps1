#requires -Version 5.1
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName System.Web.Extensions

$managedRules = @(
    [ordered]@{
        Name = 'flowchat-application-folders'
        MatchType = 'Suffix'
        MatchValue = '.Application'
        Base = 'app'
        Color = '#d95c6a'
        LightColor = '#d95c6a'
    },
    [ordered]@{
        Name = 'flowchat-api-folders'
        MatchType = 'Suffix'
        MatchValue = '.API'
        Base = 'api'
        Color = '#d99a2b'
        LightColor = '#d99a2b'
    },
    [ordered]@{
        Name = 'flowchat-infrastructure-folders'
        MatchType = 'Suffix'
        MatchValue = '.Infrastructure'
        Base = 'server'
        Color = '#4f93b3'
        LightColor = '#4f93b3'
    },
    [ordered]@{
        Name = 'flowchat-persistence-folders'
        MatchType = 'Suffix'
        MatchValue = '.Persistence'
        Base = 'database'
        Color = '#6f9d4a'
        LightColor = '#6f9d4a'
    },
    [ordered]@{
        Name = 'domain'
        MatchType = 'Suffix'
        MatchValue = '.Domain'
        Base = 'core'
        Color = '#7c6bcf'
        LightColor = '#7c6bcf'
    },
    [ordered]@{
        Name = 'workers'
        MatchType = 'Exact'
        MatchValue = 'Workers'
        Base = 'tasks'
        Color = '#5f73d8'
        LightColor = '#5f73d8'
    }
)

function Get-ManagedCloneNames {
    return @($managedRules | ForEach-Object { $_.Name })
}

function Get-RepoDirectories {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    return @(Get-ChildItem -Path $RootPath -Recurse -Directory -Force |
        Where-Object {
            $_.FullName -notmatch '\\(\.git|bin|obj|node_modules|dist|out|TestResults)(\\|$)'
        })
}

function Get-FolderNamesForRule {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Rule,
        [Parameter(Mandatory = $true)]
        [object[]]$Directories
    )

    $matchedNames = switch ($Rule.MatchType) {
        'Suffix' {
            $suffix = [string]$Rule.MatchValue
            $Directories |
                Where-Object { $_.Name.EndsWith($suffix, [System.StringComparison]::OrdinalIgnoreCase) } |
                Select-Object -ExpandProperty Name
            break
        }
        'Exact' {
            $expectedName = [string]$Rule.MatchValue
            $Directories |
                Where-Object { [string]::Equals($_.Name, $expectedName, [System.StringComparison]::OrdinalIgnoreCase) } |
                Select-Object -ExpandProperty Name
            break
        }
        default {
            throw "Unsupported match type '$($Rule.MatchType)'"
        }
    }

    return [string[]]@($matchedNames | Sort-Object -Unique)
}

function New-CustomClone {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Rule,
        [Parameter(Mandatory = $true)]
        [string[]]$FolderNames
    )

    return [pscustomobject]@{
        name = $Rule.Name
        base = $Rule.Base
        color = $Rule.Color
        lightColor = $Rule.LightColor
        folderNames = @($FolderNames)
    }
}

function Set-ObjectProperty {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Target,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        $Value
    )

    if ($Target -is [System.Collections.IDictionary]) {
        $Target[$Name] = $Value
        return
    }

    $property = $Target.PSObject.Properties[$Name]
    if ($null -eq $property) {
        $Target | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
        return
    }

    $property.Value = $Value
}

function Update-SettingsObject {
    param(
        [Parameter(Mandatory = $true)]
        [object]$SettingsObject,
        [Parameter(Mandatory = $true)]
        [object[]]$ManagedClones
    )

    Set-ObjectProperty -Target $SettingsObject -Name 'material-icon-theme.folders.theme' -Value 'specific'

    if ($SettingsObject -is [System.Collections.IDictionary]) {
        $existingClones = @($SettingsObject['material-icon-theme.folders.customClones'])
    }
    else {
        $existingClones = @($SettingsObject.'material-icon-theme.folders.customClones')
    }

    $managedCloneNames = Get-ManagedCloneNames

    $preservedClones = @(
        $existingClones |
            Where-Object {
                $null -ne $_ -and
                $managedCloneNames -notcontains [string]$_.name
            }
    )

    Set-ObjectProperty `
        -Target $SettingsObject `
        -Name 'material-icon-theme.folders.customClones' `
        -Value @($preservedClones + $ManagedClones)
}

function Save-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$Content
    )

    $json = $Content | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rawContent = Get-Content $Path -Raw

    # VS Code workspace files are often edited by hand, so tolerate trailing commas
    $sanitizedContent = [System.Text.RegularExpressions.Regex]::Replace(
        $rawContent,
        ',(?=\s*[\]}])',
        ''
    )

    $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
    $serializer.MaxJsonLength = [int]::MaxValue
    return $serializer.DeserializeObject($sanitizedContent)
}

$resolvedRepoRoot = (Resolve-Path $RepoRoot).Path
$workspaceSettingsPath = Join-Path $resolvedRepoRoot 'FlowChat.code-workspace'
$vscodeSettingsPath = Join-Path $resolvedRepoRoot '.vscode\settings.json'

if (-not (Test-Path $workspaceSettingsPath)) {
    throw "Workspace settings file not found: $workspaceSettingsPath"
}

if (-not (Test-Path $vscodeSettingsPath)) {
    throw "VS Code settings file not found: $vscodeSettingsPath"
}

$directories = Get-RepoDirectories -RootPath $resolvedRepoRoot
$generatedClones = @()

foreach ($rule in $managedRules) {
    $folderNames = [string[]](Get-FolderNamesForRule -Rule $rule -Directories $directories)

    if ($folderNames.Count -eq 0) {
        Write-Host ("Skipping rule '{0}' because no matching folders were found" -f $rule.Name) -ForegroundColor DarkYellow
        continue
    }

    $generatedClones += New-CustomClone -Rule $rule -FolderNames $folderNames
    Write-Host ("Prepared rule '{0}' with {1} folder name(s)" -f $rule.Name, $folderNames.Count) -ForegroundColor Cyan
}

$workspaceDocument = Read-JsonFile -Path $workspaceSettingsPath
$repoSettingsDocument = Read-JsonFile -Path $vscodeSettingsPath

if ($workspaceDocument -is [System.Collections.IDictionary]) {
    $workspaceSettings = $workspaceDocument['settings']
}
else {
    $workspaceSettings = $workspaceDocument.settings
}

Update-SettingsObject -SettingsObject $workspaceSettings -ManagedClones $generatedClones
Update-SettingsObject -SettingsObject $repoSettingsDocument -ManagedClones $generatedClones

if ($DryRun) {
    Write-Host 'Dry run complete. No files were written.' -ForegroundColor Yellow
    return
}

Save-JsonFile -Path $workspaceSettingsPath -Content $workspaceDocument
Save-JsonFile -Path $vscodeSettingsPath -Content $repoSettingsDocument

Write-Host 'Material Icon Theme rules synchronized successfully.' -ForegroundColor Green
