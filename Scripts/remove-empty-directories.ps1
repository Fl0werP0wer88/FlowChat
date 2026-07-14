#requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$RootPath,
    [string]$GitignorePath,
    [string[]]$ExcludePathPattern,
    [switch]$NoGitignore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Patterns that are not represented in .gitignore but should still be treated as
# "not eligible for empty-directory cleanup" (editor/tooling dirs kept under source
# control, or directories the architecture rules require to exist even when empty).
$script:AlwaysExcludePathPattern = @(
    '.git',
    '.vscode',
    'coverage',
    '*\coverage',
    '.next',
    '*\.next',
    '.nuxt',
    '*\.nuxt',
    '.svelte-kit',
    '*\.svelte-kit',
    '.angular',
    '*\.angular',
    '.turbo',
    '*\.turbo',
    '.pnpm-store',
    '*\.pnpm-store',
    '.yarn',
    '*\.yarn',
    '.cache',
    '*\.cache',
    '.parcel-cache',
    '*\.parcel-cache',
    '.vite',
    '*\.vite',
    '.vite-temp',
    '*\.vite-temp',
    'publish',
    '*\publish',
    'ApplicationEvents',
    '*\ApplicationEvents'
)

function Convert-GitIgnoreLineToPatterns {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Line
    )

    $trimmedLine = $Line.TrimEnd()

    if ([string]::IsNullOrWhiteSpace($trimmedLine) -or $trimmedLine.StartsWith('#') -or $trimmedLine.StartsWith('!')) {
        return @()
    }

    $trimmedLine = $trimmedLine.Trim()
    $matchesAnyDepth = $false

    if ($trimmedLine.StartsWith('**/')) {
        $trimmedLine = $trimmedLine.Substring(3)
        $matchesAnyDepth = $true
    }
    elseif ($trimmedLine.StartsWith('/')) {
        $trimmedLine = $trimmedLine.Substring(1)
    }
    else {
        # A slash anywhere but the end anchors the pattern to the .gitignore's
        # directory (per gitignore rules); no interior slash means "any depth".
        $matchesAnyDepth = -not $trimmedLine.TrimEnd('/').Contains('/')
    }

    $trimmedLine = $trimmedLine.TrimEnd('/')

    if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
        return @()
    }

    $windowsPattern = $trimmedLine.Replace('/', '\')

    if ($matchesAnyDepth) {
        return @($windowsPattern, "*\$windowsPattern")
    }

    return @($windowsPattern)
}

function Get-ExcludePatternsFromGitignore {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GitignoreFilePath
    )

    $patterns = [System.Collections.Generic.List[string]]::new()

    if (-not (Test-Path -LiteralPath $GitignoreFilePath -PathType Leaf)) {
        return $patterns
    }

    foreach ($line in Get-Content -LiteralPath $GitignoreFilePath) {
        foreach ($pattern in (Convert-GitIgnoreLineToPatterns -Line $line)) {
            $patterns.Add($pattern)
        }
    }

    return $patterns
}

function Normalize-PathString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)

    if ($fullPath.Length -gt 3) {
        return $fullPath.TrimEnd('\')
    }

    return $fullPath
}

function Normalize-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalizedPath = $Path.Replace('/', '\').Trim()

    if ($normalizedPath.StartsWith('.\')) {
        return $normalizedPath.Substring(2)
    }

    return $normalizedPath.Trim('\')
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    if ($TargetPath.Equals($BasePath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return '.'
    }

    $basePrefix = '{0}\' -f $BasePath

    if ($TargetPath.StartsWith($basePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $TargetPath.Substring($basePrefix.Length)
    }

    return $TargetPath
}

function Get-PathAncestors {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $ancestors = [System.Collections.Generic.List[string]]::new()
    $currentPath = Normalize-RelativePath $RelativePath

    while (-not [string]::IsNullOrWhiteSpace($currentPath) -and $currentPath -ne '.') {
        $ancestors.Add($currentPath)

        $parentPath = Split-Path -Path $currentPath -Parent
        if ([string]::IsNullOrWhiteSpace($parentPath) -or $parentPath -eq $currentPath) {
            break
        }

        $currentPath = Normalize-RelativePath $parentPath
    }

    return $ancestors
}

function Test-IsExcludedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    $ancestors = Get-PathAncestors -RelativePath $RelativePath

    foreach ($ancestor in $ancestors) {
        foreach ($pattern in $Patterns) {
            $normalizedPattern = Normalize-RelativePath $pattern

            if ([string]::IsNullOrWhiteSpace($normalizedPattern)) {
                continue
            }

            if ($ancestor -like $normalizedPattern) {
                return $true
            }
        }
    }

    return $false
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($RootPath)) {
    $RootPath = Join-Path $scriptDirectory '..'
}

function Get-DirectoryTreePostOrder {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CurrentPath,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRootPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    $directories = Get-ChildItem -LiteralPath $CurrentPath -Directory -Force

    foreach ($directory in $directories) {
        $directoryPath = Normalize-PathString -Path $directory.FullName
        $relativeDirectoryPath = Normalize-RelativePath (Get-RelativePath -BasePath $ResolvedRootPath -TargetPath $directoryPath)

        if (Test-IsExcludedPath -RelativePath $relativeDirectoryPath -Patterns $Patterns) {
            continue
        }

        foreach ($nestedDirectory in Get-DirectoryTreePostOrder -CurrentPath $directoryPath -ResolvedRootPath $ResolvedRootPath -Patterns $Patterns) {
            $nestedDirectory
        }

        $directory
    }
}

$resolvedRootPath = Normalize-PathString -Path (Resolve-Path -LiteralPath $RootPath).Path
$removedDirectories = [System.Collections.Generic.List[string]]::new()

if (-not $PSBoundParameters.ContainsKey('ExcludePathPattern')) {
    $effectiveExcludePathPattern = [System.Collections.Generic.List[string]]::new()
    $effectiveExcludePathPattern.AddRange([string[]]$script:AlwaysExcludePathPattern)

    if (-not $NoGitignore) {
        if ([string]::IsNullOrWhiteSpace($GitignorePath)) {
            $GitignorePath = Join-Path $resolvedRootPath '.gitignore'
        }

        $gitignorePatterns = Get-ExcludePatternsFromGitignore -GitignoreFilePath $GitignorePath
        $effectiveExcludePathPattern.AddRange([string[]]$gitignorePatterns)

        if ($gitignorePatterns.Count -gt 0) {
            Write-Host "Loaded $($gitignorePatterns.Count) exclude pattern(s) from: $GitignorePath"
        }
        else {
            Write-Host "No .gitignore patterns found at: $GitignorePath"
        }
    }

    $ExcludePathPattern = $effectiveExcludePathPattern | Select-Object -Unique
}

$normalizedExcludePathPattern = @(
    $ExcludePathPattern | ForEach-Object { Normalize-RelativePath $_ }
)

Write-Host "Scanning for empty directories under: $resolvedRootPath"

if ($normalizedExcludePathPattern.Count -gt 0) {
    Write-Host "Excluded patterns: $($normalizedExcludePathPattern -join ', ')"
}

$directories = @(
    Get-DirectoryTreePostOrder -CurrentPath $resolvedRootPath -ResolvedRootPath $resolvedRootPath -Patterns $normalizedExcludePathPattern
)

foreach ($directory in $directories) {
    $directoryPath = Normalize-PathString -Path $directory.FullName
    $relativeDirectoryPath = Normalize-RelativePath (Get-RelativePath -BasePath $resolvedRootPath -TargetPath $directoryPath)

    if (Test-IsExcludedPath -RelativePath $relativeDirectoryPath -Patterns $normalizedExcludePathPattern) {
        continue
    }

    if ($null -ne (Get-ChildItem -LiteralPath $directoryPath -Force | Select-Object -First 1)) {
        continue
    }

    if ($PSCmdlet.ShouldProcess($relativeDirectoryPath, 'Remove empty directory')) {
        Remove-Item -LiteralPath $directoryPath -Force
        $removedDirectories.Add($relativeDirectoryPath)
        Write-Host "Removed: $relativeDirectoryPath"
    }
}

Write-Host ''

if ($removedDirectories.Count -eq 0) {
    Write-Host 'No empty directories found.'
    return
}

Write-Host "Removed empty directories: $($removedDirectories.Count)"
