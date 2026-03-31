Set-StrictMode -Version Latest

function Get-TestGenerationServiceCatalog {
    return @(
        [pscustomobject]@{
            Name = 'AuthService'
            Root = 'AuthService'
            FullTestTargets = @('AuthService/FlowChat.AuthService.slnx')
            SharedLane = $false
        },
        [pscustomobject]@{
            Name = 'ChatService'
            Root = 'ChatService'
            FullTestTargets = @('ChatService/FlowChat.ChatService.slnx')
            SharedLane = $false
        },
        [pscustomobject]@{
            Name = 'NotificationService'
            Root = 'NotificationService'
            FullTestTargets = @('NotificationService/FlowChat.NotificationService.slnx')
            SharedLane = $false
        },
        [pscustomobject]@{
            Name = 'SocialGraphService'
            Root = 'SocialGraphService'
            FullTestTargets = @('SocialGraphService/FlowChat.SocialGraphService.slnx')
            SharedLane = $false
        },
        [pscustomobject]@{
            Name = 'UserProfileService'
            Root = 'UserProfileService'
            FullTestTargets = @('UserProfileService/FlowChat.UserProfileService.slnx')
            SharedLane = $false
        },
        [pscustomobject]@{
            Name = 'RealtimeService'
            Root = 'RealtimeService'
            FullTestTargets = @(
                'RealtimeService/tests/FlowChat.RealtimeService.IntegrationTests/FlowChat.RealtimeService.IntegrationTests.csproj',
                'RealtimeService/tests/FlowChat.RealtimeService.UnitTests/FlowChat.RealtimeService.UnitTests.csproj'
            )
            SharedLane = $false
        },
        [pscustomobject]@{
            Name = 'Common'
            Root = 'Common'
            FullTestTargets = @(
                'Common/tests/FlowChat.Shared.API.UnitTests/FlowChat.Shared.API.UnitTests.csproj',
                'Common/tests/FlowChat.Shared.Persistance.IntegrationTests/FlowChat.Shared.Persistance.IntegrationTests.csproj',
                'Common/tests/FlowChat.Shared.Persistance.UnitTests/FlowChat.Shared.Persistance.UnitTests.csproj'
            )
            SharedLane = $true
        }
    )
}

function Get-TestGenerationTerminalTaskStatuses {
    return @('passed', 'skipped-final', 'needs-human-review')
}

function Get-TestGenerationRunRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    return Join-Path $RepoRoot '.codex/testgen/runs'
}

function New-TestGenerationRunId {
    return (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
}

function ConvertTo-TestGenerationPath {
    param(
        [AllowNull()]
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $Path
    }

    return $Path.Replace('\', '/')
}

function ConvertFrom-TestGenerationKeyName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Key
    )

    $segments = $Key.ToLowerInvariant().Split('_')
    if ($segments.Count -eq 1) {
        return $segments[0]
    }

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append($segments[0])

    foreach ($segment in ($segments | Select-Object -Skip 1)) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        [void]$builder.Append($segment.Substring(0, 1).ToUpperInvariant())
        if ($segment.Length -gt 1) {
            [void]$builder.Append($segment.Substring(1))
        }
    }

    return $builder.ToString()
}

function Get-TestGenerationServiceDefinition {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $catalog = Get-TestGenerationServiceCatalog
    $service = $catalog | Where-Object { $_.Name -eq $Name } | Select-Object -First 1

    if ($null -eq $service) {
        throw "Unsupported service '$Name'. Supported values: $((($catalog | ForEach-Object { $_.Name }) -join ', '))."
    }

    return $service
}

function Parse-TestGenerationArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList
    )

    $validTestTypes = @('unit', 'integration', 'both')
    $validServices = (Get-TestGenerationServiceCatalog | ForEach-Object { $_.Name }) + @('all')

    if ($ArgumentList.Count -eq 0) {
        throw 'Missing required service argument. Usage: generate-tests <service|all> [--type unit|integration|both] [--dry-run] [--resume <run-id>] [--max-retries <n>] [--max-parallel-services <n>] [--cleanup-worktrees]'
    }

    if ($ArgumentList.Count -eq 1 -and ($ArgumentList[0] -eq '--help' -or $ArgumentList[0] -eq '-h')) {
        return [pscustomobject]@{
            HelpRequested = $true
            ScopeName = $null
            TestType = 'both'
            DryRun = $false
            ResumeRunId = $null
            MaxRetries = 2
            MaxParallelServices = 2
            CleanupWorktrees = $false
        }
    }

    $scopeName = $ArgumentList[0]
    if ($scopeName.StartsWith('-')) {
        throw "Expected service name or 'all' as the first argument, got '$scopeName'."
    }

    if ($validServices -notcontains $scopeName) {
        throw "Unsupported service '$scopeName'. Supported values: $($validServices -join ', ')."
    }

    $testType = 'both'
    $dryRun = $false
    $resumeRunId = $null
    $maxRetries = 2
    $maxParallelServices = 2
    $cleanupWorktrees = $false

    $index = 1
    while ($index -lt $ArgumentList.Count) {
        $token = $ArgumentList[$index]
        switch ($token) {
            '--dry-run' {
                $dryRun = $true
                $index++
            }
            '--cleanup-worktrees' {
                $cleanupWorktrees = $true
                $index++
            }
            '--type' {
                if ($index + 1 -ge $ArgumentList.Count) {
                    throw 'Missing value for --type.'
                }

                $candidateType = $ArgumentList[$index + 1]
                if ($validTestTypes -notcontains $candidateType) {
                    throw "Unsupported test type '$candidateType'. Supported values: $($validTestTypes -join ', ')."
                }

                $testType = $candidateType
                $index += 2
            }
            '--resume' {
                if ($index + 1 -ge $ArgumentList.Count) {
                    throw 'Missing value for --resume.'
                }

                $resumeRunId = $ArgumentList[$index + 1]
                $index += 2
            }
            '--max-retries' {
                if ($index + 1 -ge $ArgumentList.Count) {
                    throw 'Missing value for --max-retries.'
                }

                $candidateMaxRetries = 0
                if (-not [int]::TryParse($ArgumentList[$index + 1], [ref]$candidateMaxRetries) -or $candidateMaxRetries -lt 1) {
                    throw 'The value for --max-retries must be a positive integer.'
                }

                $maxRetries = $candidateMaxRetries
                $index += 2
            }
            '--max-parallel-services' {
                if ($index + 1 -ge $ArgumentList.Count) {
                    throw 'Missing value for --max-parallel-services.'
                }

                $candidateThrottle = 0
                if (-not [int]::TryParse($ArgumentList[$index + 1], [ref]$candidateThrottle) -or $candidateThrottle -lt 1) {
                    throw 'The value for --max-parallel-services must be a positive integer.'
                }

                $maxParallelServices = $candidateThrottle
                $index += 2
            }
            default {
                throw "Unknown argument '$token'."
            }
        }
    }

    return [pscustomobject]@{
        HelpRequested = $false
        ScopeName = $scopeName
        TestType = $testType
        DryRun = $dryRun
        ResumeRunId = $resumeRunId
        MaxRetries = $maxRetries
        MaxParallelServices = $maxParallelServices
        CleanupWorktrees = $cleanupWorktrees
    }
}

function Get-TestGenerationUsageText {
    return @'
Usage:
  generate-tests <service|all>
    [--type unit|integration|both]
    [--dry-run]
    [--resume <run-id>]
    [--max-retries <n>]
    [--max-parallel-services <n>]
    [--cleanup-worktrees]

Examples:
  powershell -ExecutionPolicy Bypass -File scripts/generate-tests.ps1 NotificationService
  powershell -ExecutionPolicy Bypass -File scripts/generate-tests.ps1 NotificationService --dry-run
  powershell -ExecutionPolicy Bypass -File scripts/generate-tests.ps1 all --type unit --max-parallel-services 2
'@
}

function Resolve-TestGenerationServices {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Options
    )

    if ($Options.ScopeName -eq 'all') {
        return Get-TestGenerationServiceCatalog
    }

    return @(Get-TestGenerationServiceDefinition -Name $Options.ScopeName)
}

function Split-TestGenerationServicesByLane {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Services
    )

    $parallel = @()
    $shared = @()

    foreach ($service in $Services) {
        if ($service.SharedLane) {
            $shared += $service
        }
        else {
            $parallel += $service
        }
    }

    return [pscustomobject]@{
        Parallel = $parallel
        Shared = $shared
    }
}

function Ensure-TestGenerationDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        [void](New-Item -ItemType Directory -Path $Path -Force)
    }
}

function Write-TestGenerationJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        $Value
    )

    Ensure-TestGenerationDirectory -Path (Split-Path -Parent $Path)
    $json = $Value | ConvertTo-Json -Depth 12
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
}

function Read-TestGenerationJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "JSON file not found: $Path"
    }

    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
}

function Write-TestGenerationText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    Ensure-TestGenerationDirectory -Path (Split-Path -Parent $Path)
    [System.IO.File]::WriteAllText($Path, $Value, [System.Text.Encoding]::UTF8)
}

function Invoke-TestGenerationCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [string]$InputText,
        [string]$LogPath
    )

    $originalLocation = Get-Location

    try {
        Set-Location -LiteralPath $WorkingDirectory

        if ([string]::IsNullOrEmpty($InputText)) {
            $output = & $FilePath @ArgumentList 2>&1 | ForEach-Object { $_.ToString() }
        }
        else {
            $output = $InputText | & $FilePath @ArgumentList 2>&1 | ForEach-Object { $_.ToString() }
        }

        $exitCode = $LASTEXITCODE
        $joinedOutput = ($output -join [Environment]::NewLine)

        if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
            Write-TestGenerationText -Path $LogPath -Value $joinedOutput
        }

        return [pscustomobject]@{
            ExitCode = $exitCode
            Output = $joinedOutput
        }
    }
    finally {
        Set-Location -LiteralPath $originalLocation
    }
}

function Assert-TestGenerationPreflight {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Options,
        [Parameter(Mandatory = $true)]
        [object[]]$Services,
        [Parameter(Mandatory = $true)]
        [string]$RunRoot
    )

    foreach ($commandName in @('codex', 'git', 'dotnet')) {
        if ($null -eq (Get-Command $commandName -ErrorAction SilentlyContinue)) {
            throw "Required command '$commandName' is not available in PATH."
        }
    }

    foreach ($service in $Services) {
        $serviceRoot = Join-Path $RepoRoot $service.Root
        if (-not (Test-Path -LiteralPath $serviceRoot)) {
            throw "Service root not found: $serviceRoot"
        }
    }

    if ($Options.ResumeRunId) {
        if (-not (Test-Path -LiteralPath $RunRoot)) {
            throw "Cannot resume run '$($Options.ResumeRunId)' because '$RunRoot' does not exist."
        }
    }
    elseif (Test-Path -LiteralPath $RunRoot) {
        throw "Run directory '$RunRoot' already exists."
    }

    $branchResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('branch', '--show-current') -WorkingDirectory $RepoRoot
    $statusResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('status', '--short') -WorkingDirectory $RepoRoot
    $worktreeResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('worktree', 'list') -WorkingDirectory $RepoRoot

    return [pscustomobject]@{
        Branch = $branchResult.Output.Trim()
        IsDirty = -not [string]::IsNullOrWhiteSpace($statusResult.Output)
        GitStatus = $statusResult.Output
        Worktrees = @($worktreeResult.Output -split "(`r`n|`n)" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        DprintAvailable = $null -ne (Get-Command 'dprint' -ErrorAction SilentlyContinue)
    }
}

function Get-TestGenerationTemplateText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $RepoRoot $RelativePath
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function New-TestGenerationAnalysisPrompt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ServiceName
    )

    $template = Get-TestGenerationTemplateText -RepoRoot $RepoRoot -RelativePath '.codex/generate-tests/analysis-prompt.md'
    $prompt = $template.Replace('{ServiceName}', $ServiceName)

    return @"
$prompt

Return only the analysis block between ===ANALYSIS_START=== and ===ANALYSIS_END===.
Do not wrap the response in Markdown fences.
"@
}

function New-TestGenerationTaskList {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Tasks
    )

    $orderedTasks = $Tasks | Sort-Object Priority, Layer, ClassName
    $lines = New-Object System.Collections.Generic.List[string]
    $counter = 1

    foreach ($task in $orderedTasks) {
        $lines.Add("### Task $counter`: $($task.ClassName)Tests")
        $lines.Add("- Source file: $($task.SourcePath)")
        $lines.Add("- Target test file: $($task.TargetTestPath)")
        $lines.Add("- Test type: $($task.TestType)")
        $lines.Add("- Layer: $($task.Layer)")
        $lines.Add("- Description: $($task.Description)")
        $lines.Add('')
        $counter++
    }

    return ($lines -join [Environment]::NewLine).Trim()
}

function New-TestGenerationImplementationPrompt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [object[]]$Tasks
    )

    $template = Get-TestGenerationTemplateText -RepoRoot $RepoRoot -RelativePath '.codex/generate-tests/implementation-prompt.md'
    $patterns = Get-TestGenerationTemplateText -RepoRoot $RepoRoot -RelativePath '.codex/generate-tests/patterns.md'
    $taskList = New-TestGenerationTaskList -Tasks $Tasks
    $fullSuiteTargets = ($Service.FullTestTargets -join ', ')

    $prompt = $template.Replace('{ServiceName}', $Service.Name)
    $prompt = $prompt.Replace('{service-solution-or-project-path}', $fullSuiteTargets)
    $prompt = $prompt.Replace('{TASK_LIST_PLACEHOLDER}', $taskList)

    return @"
$prompt

## Testing Conventions Reference

$patterns

Return only the implementation report between ===IMPLEMENTATION_REPORT=== markers.
Do not wrap the response in Markdown fences.
"@
}

function Parse-TestGenerationAnalysisText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $normalized = $Text -replace "`r", ''
    $analysisMatch = [regex]::Match($normalized, '===ANALYSIS_START===\n(?<body>[\s\S]*?)\n===ANALYSIS_END===')
    if (-not $analysisMatch.Success) {
        throw 'Could not locate the analysis block in the agent output.'
    }

    $body = $analysisMatch.Groups['body'].Value
    $lines = $body -split "`n"
    $section = ''
    $serviceName = $null
    $gaps = New-Object System.Collections.Generic.List[object]
    $covered = New-Object System.Collections.Generic.List[object]
    $skipped = New-Object System.Collections.Generic.List[object]
    $summary = [ordered]@{}
    $current = $null

    foreach ($rawLine in $lines) {
        $line = $rawLine.TrimEnd()
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -like 'SERVICE:*') {
            $serviceName = $line.Substring('SERVICE:'.Length).Trim()
            continue
        }

        if ($line -eq 'GAPS:') {
            $section = 'gaps'
            $current = $null
            continue
        }

        if ($line -eq 'COVERED:') {
            $section = 'covered'
            $current = $null
            continue
        }

        if ($line -eq 'SKIPPED:') {
            $section = 'skipped'
            $current = $null
            continue
        }

        if ($line -eq 'SUMMARY:') {
            $section = 'summary'
            $current = $null
            continue
        }

        if ($line -eq '---GAP---') {
            $current = [ordered]@{}
            $gaps.Add($current)
            continue
        }

        if ($line -eq '---COVERED---') {
            $current = [ordered]@{}
            $covered.Add($current)
            continue
        }

        if ($line -eq '---SKIPPED---') {
            $current = [ordered]@{}
            $skipped.Add($current)
            continue
        }

        if ($line -notmatch '^(?<key>[A-Z_]+):\s*(?<value>.*)$') {
            continue
        }

        $key = $matches['key']
        $value = $matches['value']

        if ($section -eq 'summary') {
            $summary[$key] = $value
            continue
        }

        if ($null -eq $current) {
            continue
        }

        $propertyName = ConvertFrom-TestGenerationKeyName -Key $key
        $current[$propertyName] = $value
    }

    return [pscustomobject]@{
        Service = $serviceName
        Gaps = @($gaps.ToArray() | ForEach-Object { [pscustomobject]$_ })
        Covered = @($covered.ToArray() | ForEach-Object { [pscustomobject]$_ })
        Skipped = @($skipped.ToArray() | ForEach-Object { [pscustomobject]$_ })
        Summary = [pscustomobject]$summary
    }
}

function ConvertTo-TestGenerationTasks {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Analysis,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string]$RequestedTestType
    )

    $seenKeys = @{}
    $tasks = New-Object System.Collections.Generic.List[object]

    foreach ($gap in $Analysis.Gaps) {
        if ($RequestedTestType -ne 'both' -and $gap.testType -ne $RequestedTestType) {
            continue
        }

        $key = '{0}|{1}|{2}' -f $gap.source, $gap.testType, $gap.target
        if ($seenKeys.ContainsKey($key)) {
            continue
        }

        $seenKeys[$key] = $true
        $sourcePath = ConvertTo-TestGenerationPath $gap.source
        $targetPath = ConvertTo-TestGenerationPath $gap.target
        $sharedLane = $Service.SharedLane -or $sourcePath.StartsWith('Common/') -or $targetPath.StartsWith('Common/')

        $tasks.Add([pscustomobject]@{
                Id = ('{0}:{1}:{2}' -f $Service.Name, $gap.className, $gap.testType)
                Service = $Service.Name
                TestType = $gap.testType
                SourcePath = $sourcePath
                TargetTestPath = $targetPath
                Layer = $gap.layer
                Priority = [int]$gap.priority
                ClassName = $gap.className
                Description = $gap.description
                DependsOn = @()
                SharedLane = $sharedLane
                Status = 'queued'
            })
    }

    return , @($tasks.ToArray() | Sort-Object Priority, Layer, ClassName, TestType)
}

function Get-TestGenerationAnalysisArtifactPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [string]$ServiceName
    )

    return Join-Path $RunRoot "analysis.$ServiceName.json"
}

function Get-TestGenerationTasksArtifactPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [string]$ServiceName
    )

    return Join-Path $RunRoot "tasks.$ServiceName.json"
}

function Get-TestGenerationResultsArtifactPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [string]$ServiceName
    )

    return Join-Path $RunRoot "results.$ServiceName.json"
}

function Get-TestGenerationLogDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RunRoot
    )

    return Join-Path $RunRoot 'logs'
}

function Get-TestGenerationServiceState {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest,
        [Parameter(Mandatory = $true)]
        [string]$ServiceName
    )

    return $Manifest.services | Where-Object { $_.name -eq $ServiceName } | Select-Object -First 1
}

function Get-TestGenerationPendingTasks {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Tasks
    )

    $terminal = Get-TestGenerationTerminalTaskStatuses
    return @($Tasks | Where-Object { $terminal -notcontains $_.Status })
}

function Get-TestGenerationTestProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$TargetTestPath
    )

    $fullTargetPath = Join-Path $RepoRoot $TargetTestPath
    $currentDirectory = Split-Path -Parent $fullTargetPath

    while (-not [string]::IsNullOrWhiteSpace($currentDirectory) -and (Test-Path -LiteralPath $currentDirectory)) {
        $leaf = Split-Path -Leaf $currentDirectory
        if ($leaf -like 'FlowChat.*Tests' -or $leaf -like 'FlowChat.*AATs') {
            return Join-Path $currentDirectory "$leaf.csproj"
        }

        $parent = Split-Path -Parent $currentDirectory
        if ($parent -eq $currentDirectory) {
            break
        }

        $currentDirectory = $parent
    }

    throw "Could not determine the test project for '$TargetTestPath'."
}

function Get-TestGenerationFailureSignatures {
    param(
        [AllowNull()]
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    $signatures = New-Object System.Collections.Generic.List[string]
    $lines = $Text -replace "`r", '' -split "`n"

    foreach ($rawLine in $lines) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -like 'Failed *' -or $line -like 'error *' -or $line -like '*: error *' -or $line -like 'Unhandled exception*') {
            $signatures.Add($line)
        }
    }

    return @($signatures | Sort-Object -Unique)
}

function Invoke-TestGenerationFullSuite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [string]$Phase
    )

    $logDirectory = Get-TestGenerationLogDirectory -RunRoot $RunRoot
    $targetResults = New-Object System.Collections.Generic.List[object]
    $allSignatures = New-Object System.Collections.Generic.List[string]
    $overallExitCode = 0

    foreach ($target in $Service.FullTestTargets) {
        $safeTargetName = (ConvertTo-TestGenerationPath $target) -replace '[^a-zA-Z0-9\.-]', '_'
        $logPath = Join-Path $logDirectory "$Phase.$($Service.Name).$safeTargetName.log"
        $result = Invoke-TestGenerationCommand -FilePath 'dotnet' -ArgumentList @('test', $target) -WorkingDirectory $WorkingDirectory -LogPath $logPath

        if ($result.ExitCode -ne 0) {
            $overallExitCode = $result.ExitCode
        }

        $signatures = Get-TestGenerationFailureSignatures -Text $result.Output
        foreach ($signature in $signatures) {
            $allSignatures.Add($signature)
        }

        $targetResults.Add([pscustomobject]@{
                Target = ConvertTo-TestGenerationPath $target
                ExitCode = $result.ExitCode
                LogPath = ConvertTo-TestGenerationPath $logPath
                FailureSignatures = $signatures
            })
    }

    return [pscustomobject]@{
        Status = if ($overallExitCode -eq 0) { 'green' } else { 'red' }
        ExitCode = $overallExitCode
        FailureSignatures = @($allSignatures | Sort-Object -Unique)
        Targets = @($targetResults)
    }
}

function Get-TestGenerationWorktreePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$RunId,
        [Parameter(Mandatory = $true)]
        [string]$ServiceName
    )

    $slug = ($ServiceName.ToLowerInvariant() -replace '[^a-z0-9]+', '-')
    return Join-Path $RepoRoot ".codex/worktrees/testgen-$RunId-$slug"
}

function Ensure-TestGenerationWorktree {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string]$RunId,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest
    )

    $serviceState = Get-TestGenerationServiceState -Manifest $Manifest -ServiceName $Service.Name
    if ($null -ne $serviceState -and -not [string]::IsNullOrWhiteSpace($serviceState.worktree) -and (Test-Path -LiteralPath (Join-Path $RepoRoot $serviceState.worktree))) {
        return [pscustomobject]@{
            Path = Join-Path $RepoRoot $serviceState.worktree
            RelativePath = $serviceState.worktree
            Branch = $serviceState.branch
        }
    }

    $branchName = "testgen/$RunId/$($Service.Name.ToLowerInvariant())"
    $worktreePath = Get-TestGenerationWorktreePath -RepoRoot $RepoRoot -RunId $RunId -ServiceName $Service.Name
    $relativeWorktreePath = ConvertTo-TestGenerationPath ([System.IO.Path]::GetRelativePath($RepoRoot, $worktreePath))

    if (-not (Test-Path -LiteralPath $worktreePath)) {
        Ensure-TestGenerationDirectory -Path (Split-Path -Parent $worktreePath)

        $branchExists = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('show-ref', '--verify', '--quiet', "refs/heads/$branchName") -WorkingDirectory $RepoRoot
        if ($branchExists.ExitCode -eq 0) {
            $worktreeResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('worktree', 'add', $worktreePath, $branchName) -WorkingDirectory $RepoRoot
        }
        else {
            $worktreeResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('worktree', 'add', '-b', $branchName, $worktreePath, 'HEAD') -WorkingDirectory $RepoRoot
        }

        if ($worktreeResult.ExitCode -ne 0) {
            throw "Failed to create worktree for $($Service.Name): $($worktreeResult.Output)"
        }
    }

    return [pscustomobject]@{
        Path = $worktreePath
        RelativePath = $relativeWorktreePath
        Branch = $branchName
    }
}

function Invoke-TestGenerationCodex {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [Parameter(Mandatory = $true)]
        [string]$Prompt,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [Parameter(Mandatory = $true)]
        [string]$LogPath
    )

    $arguments = @(
        'exec',
        '-C',
        $WorkingDirectory,
        '-s',
        'danger-full-access',
        '-o',
        $OutputPath,
        '-'
    )

    return Invoke-TestGenerationCommand -FilePath 'codex' -ArgumentList $arguments -WorkingDirectory $WorkingDirectory -InputText $Prompt -LogPath $LogPath
}

function Invoke-TestGenerationAnalysisForService {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string]$TestType
    )

    $logDirectory = Get-TestGenerationLogDirectory -RunRoot $RunRoot
    Ensure-TestGenerationDirectory -Path $logDirectory

    $prompt = New-TestGenerationAnalysisPrompt -RepoRoot $RepoRoot -ServiceName $Service.Name
    $outputPath = Join-Path $logDirectory "analysis.$($Service.Name).response.txt"
    $logPath = Join-Path $logDirectory "analysis.$($Service.Name).exec.log"
    $codexResult = Invoke-TestGenerationCodex -WorkingDirectory $RepoRoot -Prompt $prompt -OutputPath $outputPath -LogPath $logPath

    if ($codexResult.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $outputPath)) {
        throw "Analysis agent failed for $($Service.Name): $($codexResult.Output)"
    }

    $responseText = [System.IO.File]::ReadAllText($outputPath, [System.Text.Encoding]::UTF8)
    $analysis = Parse-TestGenerationAnalysisText -Text $responseText
    $tasks = ConvertTo-TestGenerationTasks -Analysis $analysis -Service $Service -RequestedTestType $TestType

    Write-TestGenerationJson -Path (Get-TestGenerationAnalysisArtifactPath -RunRoot $RunRoot -ServiceName $Service.Name) -Value $analysis
    Write-TestGenerationJson -Path (Get-TestGenerationTasksArtifactPath -RunRoot $RunRoot -ServiceName $Service.Name) -Value $tasks

    return [pscustomobject]@{
        Service = $Service.Name
        Analysis = $analysis
        Tasks = $tasks
        ExitCode = $codexResult.ExitCode
    }
}

function Parse-TestGenerationImplementationText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $normalized = $Text -replace "`r", ''
    $reportMatch = [regex]::Match($normalized, '===IMPLEMENTATION_REPORT===\n(?<body>[\s\S]*?)\n===IMPLEMENTATION_REPORT===')
    if (-not $reportMatch.Success) {
        throw 'Could not locate the implementation report block in the agent output.'
    }

    $body = $reportMatch.Groups['body'].Value
    $lines = $body -split "`n"
    $section = ''
    $serviceName = $null
    $results = New-Object System.Collections.Generic.List[object]
    $fullSuite = [ordered]@{}
    $current = $null

    foreach ($rawLine in $lines) {
        $line = $rawLine.TrimEnd()
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -like 'SERVICE:*') {
            $serviceName = $line.Substring('SERVICE:'.Length).Trim()
            continue
        }

        if ($line -eq 'RESULTS:') {
            $section = 'results'
            $current = $null
            continue
        }

        if ($line -eq 'FULL_SUITE:') {
            $section = 'fullSuite'
            $current = $null
            continue
        }

        if ($line -eq '---RESULT---') {
            $current = [ordered]@{}
            $results.Add($current)
            continue
        }

        if ($line -notmatch '^(?<key>[A-Z_]+):\s*(?<value>.*)$') {
            continue
        }

        $key = $matches['key']
        $value = $matches['value']
        $propertyName = ConvertFrom-TestGenerationKeyName -Key $key

        if ($section -eq 'fullSuite') {
            $fullSuite[$propertyName] = $value
            continue
        }

        if ($null -ne $current) {
            $current[$propertyName] = $value
        }
    }

    return [pscustomobject]@{
        Service = $serviceName
        Results = @($results.ToArray() | ForEach-Object { [pscustomobject]$_ })
        FullSuite = [pscustomobject]$fullSuite
    }
}

function Update-TestGenerationTasksFromReport {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Tasks,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Report
    )

    $tasksByTarget = @{}
    foreach ($task in $Tasks) {
        $tasksByTarget[$task.TargetTestPath] = $task
    }

    foreach ($result in $Report.Results) {
        $target = ConvertTo-TestGenerationPath $result.target
        if (-not $tasksByTarget.ContainsKey($target)) {
            continue
        }

        switch ($result.status) {
            'PASS' {
                $tasksByTarget[$target].Status = 'passed'
            }
            'FAIL' {
                $tasksByTarget[$target].Status = 'skipped-final'
            }
            'SKIPPED' {
                $tasksByTarget[$target].Status = 'skipped-final'
            }
        }
    }

    return $Tasks
}

function Get-TestGenerationChangedFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    $result = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('status', '--short') -WorkingDirectory $WorkingDirectory
    $lines = @($result.Output -split "(`r`n|`n)" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $files = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        if ($line.Length -lt 4) {
            continue
        }

        $path = $line.Substring(3).Trim()
        if ($path.Contains('->')) {
            $path = $path.Split('->')[-1].Trim()
        }

        $files.Add((ConvertTo-TestGenerationPath $path))
    }

    return @($files | Sort-Object -Unique)
}

function Get-TestGenerationDisallowedWrites {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string[]]$ChangedFiles
    )

    $allowedPrefix = '{0}/' -f (ConvertTo-TestGenerationPath $Service.Root)
    return @($ChangedFiles | Where-Object { $_ -and -not $_.StartsWith($allowedPrefix) })
}

function Invoke-TestGenerationDprintIfAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string]$RunRoot
    )

    if ($null -eq (Get-Command 'dprint' -ErrorAction SilentlyContinue)) {
        return $null
    }

    $logPath = Join-Path (Get-TestGenerationLogDirectory -RunRoot $RunRoot) "dprint.$($Service.Name).log"
    return Invoke-TestGenerationCommand -FilePath 'dprint' -ArgumentList @('fmt', $Service.Root) -WorkingDirectory $WorkingDirectory -LogPath $logPath
}

function Commit-TestGenerationWorktreeChanges {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [string]$RunId
    )

    $changedFiles = Get-TestGenerationChangedFiles -WorkingDirectory $WorkingDirectory
    if ($changedFiles.Count -eq 0) {
        return $null
    }

    $addResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('add', '--all') -WorkingDirectory $WorkingDirectory
    if ($addResult.ExitCode -ne 0) {
        throw "Failed to stage generated files for $($Service.Name): $($addResult.Output)"
    }

    $message = "testgen: add generated tests for $($Service.Name) (run $RunId)"
    $commitResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('commit', '-m', $message) -WorkingDirectory $WorkingDirectory
    if ($commitResult.ExitCode -ne 0) {
        throw "Failed to commit generated files for $($Service.Name): $($commitResult.Output)"
    }

    $shaResult = Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('rev-parse', 'HEAD') -WorkingDirectory $WorkingDirectory
    return $shaResult.Output.Trim()
}

function Compare-TestGenerationSuiteState {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Baseline,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Final
    )

    $baselineFailures = @($Baseline.FailureSignatures)
    $finalFailures = @($Final.FailureSignatures)
    $introduced = New-Object System.Collections.Generic.List[string]

    foreach ($signature in $finalFailures) {
        if ($baselineFailures -notcontains $signature) {
            $introduced.Add($signature)
        }
    }

    return [pscustomobject]@{
        IntroducedFailures = @($introduced)
        HasIntroducedRegression = $introduced.Count -gt 0 -or ($Baseline.Status -eq 'green' -and $Final.Status -eq 'red')
        PreExistingFailures = $baselineFailures
    }
}

function Invoke-TestGenerationWorkerForService {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [string]$RunId,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Service,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest
    )

    $tasksPath = Get-TestGenerationTasksArtifactPath -RunRoot $RunRoot -ServiceName $Service.Name
    $tasks = @(Read-TestGenerationJson -Path $tasksPath)
    $pendingTasks = Get-TestGenerationPendingTasks -Tasks $tasks

    $worktree = Ensure-TestGenerationWorktree -RepoRoot $RepoRoot -Service $Service -RunId $RunId -Manifest $Manifest

    if ($pendingTasks.Count -eq 0) {
        return [pscustomobject]@{
            Service = $Service.Name
            Status = 'skipped'
            Worktree = $worktree.RelativePath
            Branch = $worktree.Branch
            Commit = $null
            ChangedFiles = @()
            Results = @()
        }
    }

    $logDirectory = Get-TestGenerationLogDirectory -RunRoot $RunRoot
    $prompt = New-TestGenerationImplementationPrompt -RepoRoot $RepoRoot -Service $Service -Tasks $pendingTasks
    $outputPath = Join-Path $logDirectory "implementation.$($Service.Name).response.txt"
    $logPath = Join-Path $logDirectory "implementation.$($Service.Name).exec.log"
    $codexResult = Invoke-TestGenerationCodex -WorkingDirectory $worktree.Path -Prompt $prompt -OutputPath $outputPath -LogPath $logPath

    $report = $null
    if (Test-Path -LiteralPath $outputPath) {
        try {
            $reportText = [System.IO.File]::ReadAllText($outputPath, [System.Text.Encoding]::UTF8)
            $report = Parse-TestGenerationImplementationText -Text $reportText
            $tasks = Update-TestGenerationTasksFromReport -Tasks $tasks -Report $report
        }
        catch {
            foreach ($task in $pendingTasks) {
                if ($task.Status -eq 'queued') {
                    $task.Status = 'failed-retryable'
                }
            }
        }
    }

    Invoke-TestGenerationDprintIfAvailable -WorkingDirectory $worktree.Path -Service $Service -RunRoot $RunRoot | Out-Null

    $changedFiles = Get-TestGenerationChangedFiles -WorkingDirectory $worktree.Path
    $disallowedWrites = Get-TestGenerationDisallowedWrites -Service $Service -ChangedFiles $changedFiles
    $csprojWrites = @($changedFiles | Where-Object { $_ -like '*.csproj' })

    if ($disallowedWrites.Count -gt 0 -or $csprojWrites.Count -gt 0) {
        foreach ($task in $tasks) {
            if ($task.Status -eq 'queued' -or $task.Status -eq 'failed-retryable') {
                $task.Status = 'needs-human-review'
            }
        }
    }
    else {
        foreach ($task in $tasks) {
            if ($task.Status -eq 'failed-retryable') {
                $task.Status = 'skipped-final'
            }
        }
    }

    Write-TestGenerationJson -Path $tasksPath -Value $tasks

    $finalSuite = Invoke-TestGenerationFullSuite -WorkingDirectory $worktree.Path -Service $Service -RunRoot $RunRoot -Phase 'final'
    $baselineEntry = $Manifest.baseline.PSObject.Properties[$Service.Name].Value
    $baselineObject = if ($null -eq $baselineEntry) { [pscustomobject]@{ Status = 'green'; FailureSignatures = @() } } else { $baselineEntry }
    $suiteComparison = Compare-TestGenerationSuiteState -Baseline $baselineObject -Final $finalSuite

    $commitSha = $null
    $status = 'completed'
    if ($disallowedWrites.Count -gt 0 -or $csprojWrites.Count -gt 0) {
        $status = 'needs-human-review'
    }
    elseif ($suiteComparison.HasIntroducedRegression) {
        $status = 'failed'
    }
    else {
        $commitSha = Commit-TestGenerationWorktreeChanges -WorkingDirectory $worktree.Path -Service $Service -RunId $RunId
    }

    $result = [pscustomobject]@{
        Service = $Service.Name
        Status = $status
        Worktree = $worktree.RelativePath
        Branch = $worktree.Branch
        Commit = $commitSha
        CodexExitCode = $codexResult.ExitCode
        ChangedFiles = $changedFiles
        DisallowedWrites = $disallowedWrites
        CsprojWrites = $csprojWrites
        IntroducedFailures = $suiteComparison.IntroducedFailures
        FinalSuite = $finalSuite
        ImplementationReport = $report
    }

    Write-TestGenerationJson -Path (Get-TestGenerationResultsArtifactPath -RunRoot $RunRoot -ServiceName $Service.Name) -Value $result
    return $result
}

function New-TestGenerationSummaryMarkdown {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest,
        [Parameter(Mandatory = $true)]
        [hashtable]$AnalysisResults,
        [Parameter(Mandatory = $true)]
        [hashtable]$ServiceResults
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Test Generation Run $($Manifest.runId)")
    $lines.Add('')
    $lines.Add("- Mode: $($Manifest.mode)")
    $lines.Add("- Status: $($Manifest.status)")
    $lines.Add("- Scope: $((@($Manifest.scope)) -join ', ')")
    $lines.Add("- Test type: $($Manifest.testType)")
    $lines.Add('')

    foreach ($serviceState in $Manifest.services) {
        $serviceName = $serviceState.name
        $analysis = $AnalysisResults[$serviceName]
        $result = $ServiceResults[$serviceName]
        $baseline = $Manifest.baseline.PSObject.Properties[$serviceName].Value

        $lines.Add("## $serviceName")
        $lines.Add('')

        if ($null -ne $analysis) {
            $lines.Add("- Missing tests: $(@($analysis.Tasks).Count)")
            $lines.Add("- Covered files: $(@($analysis.Analysis.Covered).Count)")
        }

        if ($null -ne $baseline) {
            $lines.Add("- Baseline suite: $($baseline.Status)")
        }

        if ($null -ne $result) {
            $lines.Add("- Worker status: $($result.Status)")
            $lines.Add("- Branch: $($result.Branch)")
            $lines.Add("- Worktree: $($result.Worktree)")
            if (-not [string]::IsNullOrWhiteSpace($result.Commit)) {
                $lines.Add("- Commit: $($result.Commit)")
            }
            $lines.Add("- Final suite: $($result.FinalSuite.Status)")

            if (@($result.ChangedFiles).Count -gt 0) {
                $lines.Add("- Changed files:")
                foreach ($file in $result.ChangedFiles) {
                    $lines.Add("  - $file")
                }
            }

            if (@($result.IntroducedFailures).Count -gt 0) {
                $lines.Add("- Introduced regressions:")
                foreach ($failure in $result.IntroducedFailures) {
                    $lines.Add("  - $failure")
                }
            }

            if (@($result.DisallowedWrites).Count -gt 0) {
                $lines.Add("- Disallowed writes:")
                foreach ($file in $result.DisallowedWrites) {
                    $lines.Add("  - $file")
                }
            }
        }

        $lines.Add('')
    }

    return ($lines -join [Environment]::NewLine).Trim() + [Environment]::NewLine
}

function Save-TestGenerationManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RunRoot,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest
    )

    Write-TestGenerationJson -Path (Join-Path $RunRoot 'manifest.json') -Value $Manifest
}

function Remove-TestGenerationWorktrees {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest,
        [Parameter(Mandatory = $true)]
        [hashtable]$ServiceResults
    )

    foreach ($serviceState in $Manifest.services) {
        $serviceName = $serviceState.name
        $result = $ServiceResults[$serviceName]
        if ($null -eq $result -or $result.Status -ne 'completed') {
            continue
        }

        if ([string]::IsNullOrWhiteSpace($serviceState.worktree)) {
            continue
        }

        $worktreePath = Join-Path $RepoRoot $serviceState.worktree
        if (-not (Test-Path -LiteralPath $worktreePath)) {
            continue
        }

        Invoke-TestGenerationCommand -FilePath 'git' -ArgumentList @('worktree', 'remove', '--force', $worktreePath) -WorkingDirectory $RepoRoot | Out-Null
    }
}

function Invoke-TestGenerationPhaseInParallel {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Factory,
        [Parameter(Mandatory = $true)]
        [object[]]$Services,
        [int]$Throttle = 1
    )

    if ($Services.Count -le 1 -or $Throttle -le 1) {
        $results = @{}
        foreach ($service in $Services) {
            $result = & $Factory $service
            $results[$service.Name] = $result
        }

        return $results
    }

    $queue = New-Object System.Collections.Queue
    foreach ($service in $Services) {
        $queue.Enqueue($service)
    }

    $runningJobs = @()
    $results = @{}

    while ($queue.Count -gt 0 -or $runningJobs.Count -gt 0) {
        while ($queue.Count -gt 0 -and $runningJobs.Count -lt $Throttle) {
            $service = $queue.Dequeue()
            $job = & $Factory $service -AsJob
            $runningJobs += $job
        }

        $completedJob = Wait-Job -Job $runningJobs -Any -Timeout 5
        if ($null -eq $completedJob) {
            continue
        }

        $payload = Receive-Job -Job $completedJob
        Remove-Job -Job $completedJob | Out-Null
        $runningJobs = @($runningJobs | Where-Object { $_.Id -ne $completedJob.Id })

        if ($payload -is [string]) {
            $payload = $payload | ConvertFrom-Json
        }

        $results[$payload.Service] = $payload
    }

    return $results
}

function New-TestGenerationJobFactory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ModulePath,
        [Parameter(Mandatory = $true)]
        [string]$FunctionName,
        [Parameter(Mandatory = $true)]
        [hashtable]$CommonArguments
    )

    return {
        param(
            [pscustomobject]$Service,
            [switch]$AsJob
        )

        $invoke = {
            param($ModulePathValue, $FunctionNameValue, $SerializedCommonArguments, $SerializedService)

            Import-Module $ModulePathValue -Force
            $serviceObject = $SerializedService | ConvertFrom-Json
            $commonObject = $SerializedCommonArguments | ConvertFrom-Json
            $arguments = @{
                RepoRoot = $commonObject.RepoRoot
                RunRoot = $commonObject.RunRoot
                Service = $serviceObject
            }

            if ($commonObject.PSObject.Properties.Name -contains 'TestType') {
                $arguments.TestType = $commonObject.TestType
            }

            if ($commonObject.PSObject.Properties.Name -contains 'RunId') {
                $arguments.RunId = $commonObject.RunId
            }

            if ($commonObject.PSObject.Properties.Name -contains 'Manifest') {
                $arguments.Manifest = $commonObject.Manifest
            }

            $result = & $FunctionNameValue @arguments
            return $result | ConvertTo-Json -Depth 12
        }

        if ($AsJob) {
            return Start-Job -ScriptBlock $invoke -ArgumentList $ModulePath, $FunctionName, ($CommonArguments | ConvertTo-Json -Depth 12), ($Service | ConvertTo-Json -Depth 6)
        }

        $raw = & $invoke $ModulePath $FunctionName ($CommonArguments | ConvertTo-Json -Depth 12) ($Service | ConvertTo-Json -Depth 6)
        return $raw | ConvertFrom-Json
    }.GetNewClosure()
}

function Start-TestGenerationRun {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList
    )

    $options = Parse-TestGenerationArguments -ArgumentList $ArgumentList
    if ($options.HelpRequested) {
        return [pscustomobject]@{
            HelpRequested = $true
            Usage = Get-TestGenerationUsageText
        }
    }

    $services = Resolve-TestGenerationServices -Options $options
    $runId = if ([string]::IsNullOrWhiteSpace($options.ResumeRunId)) { New-TestGenerationRunId } else { $options.ResumeRunId }
    $runRoot = Join-Path (Get-TestGenerationRunRoot -RepoRoot $RepoRoot) $runId
    $preflight = Assert-TestGenerationPreflight -RepoRoot $RepoRoot -Options $options -Services $services -RunRoot $runRoot

    Ensure-TestGenerationDirectory -Path $runRoot
    Ensure-TestGenerationDirectory -Path (Get-TestGenerationLogDirectory -RunRoot $runRoot)

    $manifestPath = Join-Path $runRoot 'manifest.json'
    if ($options.ResumeRunId) {
        $manifest = Read-TestGenerationJson -Path $manifestPath
    }
    else {
        $manifest = [pscustomobject]@{
            runId = $runId
            mode = if ($options.DryRun) { 'dry-run' } else { 'execute' }
            scope = @($services | ForEach-Object { $_.Name })
            testType = $options.TestType
            status = 'running'
            createdAtUtc = (Get-Date).ToUniversalTime().ToString('o')
            maxRetries = $options.MaxRetries
            maxParallelServices = $options.MaxParallelServices
            repoState = [pscustomobject]@{
                branch = $preflight.Branch
                isDirty = $preflight.IsDirty
                worktrees = @($preflight.Worktrees)
            }
            baseline = [pscustomobject]@{}
            services = @(
                $services | ForEach-Object {
                    [pscustomobject]@{
                        name = $_.Name
                        sharedLane = $_.SharedLane
                        status = 'pending'
                        branch = $null
                        worktree = $null
                        analysisFile = ConvertTo-TestGenerationPath ([System.IO.Path]::GetRelativePath($RepoRoot, (Get-TestGenerationAnalysisArtifactPath -RunRoot $runRoot -ServiceName $_.Name)))
                        tasksFile = ConvertTo-TestGenerationPath ([System.IO.Path]::GetRelativePath($RepoRoot, (Get-TestGenerationTasksArtifactPath -RunRoot $runRoot -ServiceName $_.Name)))
                        resultsFile = ConvertTo-TestGenerationPath ([System.IO.Path]::GetRelativePath($RepoRoot, (Get-TestGenerationResultsArtifactPath -RunRoot $runRoot -ServiceName $_.Name)))
                    }
                }
            )
        }
    }

    if (-not $options.ResumeRunId -and -not $options.DryRun) {
        foreach ($service in $services) {
            $baseline = Invoke-TestGenerationFullSuite -WorkingDirectory $RepoRoot -Service $service -RunRoot $runRoot -Phase 'baseline'
            Add-Member -InputObject $manifest.baseline -NotePropertyName $service.Name -NotePropertyValue $baseline -Force
            $serviceState = Get-TestGenerationServiceState -Manifest $manifest -ServiceName $service.Name
            if ($null -ne $serviceState) {
                $serviceState.status = "baseline-$($baseline.Status)"
            }
        }
    }

    Save-TestGenerationManifest -RunRoot $runRoot -Manifest $manifest

    $modulePath = Join-Path $RepoRoot 'scripts/FlowChat.TestGeneration.psm1'
    $analysisResults = @{}
    $servicesNeedingAnalysis = @()
    foreach ($service in $services) {
        $analysisPath = Get-TestGenerationAnalysisArtifactPath -RunRoot $runRoot -ServiceName $service.Name
        $tasksPath = Get-TestGenerationTasksArtifactPath -RunRoot $runRoot -ServiceName $service.Name

        if ($options.ResumeRunId -and (Test-Path -LiteralPath $analysisPath) -and (Test-Path -LiteralPath $tasksPath)) {
            $analysisResults[$service.Name] = [pscustomobject]@{
                Service = $service.Name
                Analysis = Read-TestGenerationJson -Path $analysisPath
                Tasks = @(Read-TestGenerationJson -Path $tasksPath)
                ExitCode = 0
            }
            continue
        }

        $servicesNeedingAnalysis += $service
    }

    if ($servicesNeedingAnalysis.Count -gt 0) {
        $analysisFactory = New-TestGenerationJobFactory -ModulePath $modulePath -FunctionName 'Invoke-TestGenerationAnalysisForService' -CommonArguments @{
            RepoRoot = $RepoRoot
            RunRoot = $runRoot
            TestType = $options.TestType
        }

        $freshAnalysisResults = Invoke-TestGenerationPhaseInParallel -Factory $analysisFactory -Services $servicesNeedingAnalysis -Throttle $options.MaxParallelServices
        foreach ($key in $freshAnalysisResults.Keys) {
            $analysisResults[$key] = $freshAnalysisResults[$key]
        }
    }
    foreach ($service in $services) {
        $serviceState = Get-TestGenerationServiceState -Manifest $manifest -ServiceName $service.Name
        if ($null -ne $serviceState) {
            $serviceState.status = 'planned'
        }
    }
    Save-TestGenerationManifest -RunRoot $runRoot -Manifest $manifest

    if ($options.DryRun) {
        $manifest.status = 'dry-run-completed'
        $serviceResults = @{}
        $summary = New-TestGenerationSummaryMarkdown -Manifest $manifest -AnalysisResults $analysisResults -ServiceResults $serviceResults
        Write-TestGenerationText -Path (Join-Path $runRoot 'summary.md') -Value $summary
        Save-TestGenerationManifest -RunRoot $runRoot -Manifest $manifest

        return [pscustomobject]@{
            HelpRequested = $false
            RunId = $runId
            RunRoot = $runRoot
            ManifestPath = $manifestPath
            SummaryPath = Join-Path $runRoot 'summary.md'
            AnalysisResults = $analysisResults
            ServiceResults = $serviceResults
        }
    }

    $lanes = Split-TestGenerationServicesByLane -Services $services
    $serviceResults = @{}

    if ($lanes.Parallel.Count -gt 0) {
        $workerFactory = New-TestGenerationJobFactory -ModulePath $modulePath -FunctionName 'Invoke-TestGenerationWorkerForService' -CommonArguments @{
            RepoRoot = $RepoRoot
            RunRoot = $runRoot
            RunId = $runId
            Manifest = $manifest
        }
        $parallelResults = Invoke-TestGenerationPhaseInParallel -Factory $workerFactory -Services $lanes.Parallel -Throttle $options.MaxParallelServices
        foreach ($key in $parallelResults.Keys) {
            $serviceResults[$key] = $parallelResults[$key]
        }
    }

    foreach ($service in $lanes.Shared) {
        $result = Invoke-TestGenerationWorkerForService -RepoRoot $RepoRoot -RunRoot $runRoot -RunId $runId -Service $service -Manifest $manifest
        $serviceResults[$service.Name] = $result
    }

    foreach ($service in $services) {
        $serviceState = Get-TestGenerationServiceState -Manifest $manifest -ServiceName $service.Name
        $result = $serviceResults[$service.Name]
        if ($null -ne $serviceState -and $null -ne $result) {
            $serviceState.status = $result.Status
            $serviceState.branch = $result.Branch
            $serviceState.worktree = $result.Worktree
        }
    }

    $hasFailures = $false
    foreach ($result in $serviceResults.Values) {
        if ($result.Status -ne 'completed' -and $result.Status -ne 'skipped') {
            $hasFailures = $true
            break
        }
    }

    $manifest.status = if ($hasFailures) { 'completed-with-failures' } else { 'completed' }
    Save-TestGenerationManifest -RunRoot $runRoot -Manifest $manifest

    if ($options.CleanupWorktrees -and -not $hasFailures) {
        Remove-TestGenerationWorktrees -RepoRoot $RepoRoot -Manifest $manifest -ServiceResults $serviceResults
    }

    $summaryMarkdown = New-TestGenerationSummaryMarkdown -Manifest $manifest -AnalysisResults $analysisResults -ServiceResults $serviceResults
    Write-TestGenerationText -Path (Join-Path $runRoot 'summary.md') -Value $summaryMarkdown
    Save-TestGenerationManifest -RunRoot $runRoot -Manifest $manifest

    return [pscustomobject]@{
        HelpRequested = $false
        RunId = $runId
        RunRoot = $runRoot
        ManifestPath = $manifestPath
        SummaryPath = Join-Path $runRoot 'summary.md'
        AnalysisResults = $analysisResults
        ServiceResults = $serviceResults
    }
}

Export-ModuleMember -Function @(
    'ConvertTo-TestGenerationTasks',
    'Get-TestGenerationServiceCatalog',
    'Get-TestGenerationUsageText',
    'Invoke-TestGenerationAnalysisForService',
    'Invoke-TestGenerationWorkerForService',
    'New-TestGenerationRunId',
    'Parse-TestGenerationArguments',
    'Parse-TestGenerationAnalysisText',
    'Split-TestGenerationServicesByLane',
    'Start-TestGenerationRun'
)
