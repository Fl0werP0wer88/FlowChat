$modulePath = Join-Path $PSScriptRoot '..\FlowChat.TestGeneration.psm1'
Import-Module $modulePath -Force

Describe 'Parse-TestGenerationArguments' {
    It 'parses a single service dry run with explicit type' {
        $result = Parse-TestGenerationArguments @('NotificationService', '--dry-run', '--type', 'unit', '--max-retries', '3')

        $result.HelpRequested | Should Be $false
        $result.ScopeName | Should Be 'NotificationService'
        $result.DryRun | Should Be $true
        $result.TestType | Should Be 'unit'
        $result.MaxRetries | Should Be 3
        $result.MaxParallelServices | Should Be 2
    }

    It 'parses all-scope execution with throttle and cleanup' {
        $result = Parse-TestGenerationArguments @('all', '--type', 'integration', '--max-parallel-services', '4', '--cleanup-worktrees')

        $result.ScopeName | Should Be 'all'
        $result.TestType | Should Be 'integration'
        $result.MaxParallelServices | Should Be 4
        $result.CleanupWorktrees | Should Be $true
    }

    It 'throws for an unsupported service' {
        { Parse-TestGenerationArguments @('GatewayService') } | Should Throw
    }
}

Describe 'Parse-TestGenerationAnalysisText' {
    It 'parses the structured analysis block into gaps, covered files and summary' {
        $text = @'
===ANALYSIS_START===
SERVICE: NotificationService

GAPS:
---GAP---
SOURCE: NotificationService/src/FlowChat.NotificationService.Domain/Entities/Notification.cs
TEST_TYPE: unit
TARGET: NotificationService/tests/FlowChat.NotificationService.UnitTests/Domain/Entities/NotificationTests.cs
LAYER: Domain
PRIORITY: 1
CLASS_NAME: Notification
DESCRIPTION: Covers creation rules and emitted domain events.

COVERED:
---COVERED---
SOURCE: NotificationService/src/FlowChat.NotificationService.Persistence/Configurations/NotificationConfiguration.cs
TEST: NotificationService/tests/FlowChat.NotificationService.IntegrationTests/Persistence/Configurations/NotificationConfigurationTests.cs

SKIPPED:
---SKIPPED---
SOURCE: NotificationService/src/FlowChat.NotificationService.Application/Contracts/NotificationDto.cs
REASON: Simple DTO with no logic.

SUMMARY:
TOTAL_SOURCE_FILES: 3
COVERED: 1
MISSING: 1
SKIPPED: 1
===ANALYSIS_END===
'@

        $analysis = Parse-TestGenerationAnalysisText -Text $text

        $analysis.Service | Should Be 'NotificationService'
        $analysis.Gaps.Count | Should Be 1
        $analysis.Gaps[0].className | Should Be 'Notification'
        $analysis.Covered.Count | Should Be 1
        $analysis.Skipped.Count | Should Be 1
        $analysis.Summary.TOTAL_SOURCE_FILES | Should Be '3'
    }
}

Describe 'ConvertTo-TestGenerationTasks' {
    It 'filters by requested test type and sorts by priority' {
        $analysis = [pscustomobject]@{
            Service = 'NotificationService'
            Gaps = @(
                [pscustomobject]@{
                    source = 'NotificationService/src/FlowChat.NotificationService.Application/Foo/Second.cs'
                    testType = 'integration'
                    target = 'NotificationService/tests/FlowChat.NotificationService.IntegrationTests/Foo/SecondTests.cs'
                    layer = 'Persistence'
                    priority = '5'
                    className = 'Second'
                    description = 'Second'
                },
                [pscustomobject]@{
                    source = 'NotificationService/src/FlowChat.NotificationService.Domain/Foo/First.cs'
                    testType = 'unit'
                    target = 'NotificationService/tests/FlowChat.NotificationService.UnitTests/Foo/FirstTests.cs'
                    layer = 'Domain'
                    priority = '1'
                    className = 'First'
                    description = 'First'
                }
            )
            Covered = @()
            Skipped = @()
            Summary = [pscustomobject]@{}
        }

        $service = Get-TestGenerationServiceCatalog | Where-Object { $_.Name -eq 'NotificationService' } | Select-Object -First 1
        $tasks = ConvertTo-TestGenerationTasks -Analysis $analysis -Service $service -RequestedTestType 'unit'

        $tasks.Count | Should Be 1
        $tasks[0].ClassName | Should Be 'First'
        $tasks[0].Priority | Should Be 1
        $tasks[0].Status | Should Be 'queued'
    }
}

Describe 'Split-TestGenerationServicesByLane' {
    It 'keeps Common on the shared lane and service-specific work on the parallel lane' {
        $catalog = Get-TestGenerationServiceCatalog
        $services = @(
            $catalog | Where-Object { $_.Name -eq 'NotificationService' } | Select-Object -First 1
            $catalog | Where-Object { $_.Name -eq 'Common' } | Select-Object -First 1
        )

        $lanes = Split-TestGenerationServicesByLane -Services $services

        $lanes.Parallel.Count | Should Be 1
        $lanes.Parallel[0].Name | Should Be 'NotificationService'
        $lanes.Shared.Count | Should Be 1
        $lanes.Shared[0].Name | Should Be 'Common'
    }
}
