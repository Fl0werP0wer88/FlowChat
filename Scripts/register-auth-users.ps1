#requires -Version 5.1
param(
    [string]$BaseUrl = 'http://localhost:5234',
    [string]$Route = 'api/users',
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$registerUserUri = '{0}/{1}' -f $BaseUrl.TrimEnd('/'), $Route.TrimStart('/')
$password = 'dRabina#098'
$organizations = @(
    'Northwind Labs',
    'Blue Orbit Studio',
    'Riverstone Systems',
    'Amberleaf Media',
    'Polar Grid Works'
)

$firstNames = @(
    'Anna',
    'Piotr',
    'Marta',
    'Jakub',
    'Zofia',
    'Tomasz',
    'Julia',
    'Mikolaj',
    'Natalia',
    'Kacper'
)

$lastNames = @(
    'Nowak',
    'Wojcik',
    'Mazur',
    'Krawczyk',
    'Szymczak',
    'Dudek',
    'Sikora',
    'Baran',
    'Lis',
    'Pawlak'
)

function Get-Users {
    $users = @()

    for ($i = 0; $i -lt 50; $i++) {
        $sequence = $i + 1
        $firstName = $firstNames[$i % $firstNames.Count]
        $lastName = $lastNames[[int][Math]::Floor($i / 5)]
        $organization = $organizations[$i % $organizations.Count]
        $sequenceToken = '{0:D2}' -f $sequence
        $friendlyUserId = ('{0}.{1}.{2}' -f $firstName, $lastName, $sequenceToken).ToLowerInvariant()
        $email = ('{0}.{1}{2}@seed.flowchat.local' -f $firstName, $lastName, $sequenceToken).ToLowerInvariant()

        $users += [PSCustomObject]@{
            FriendlyUserId = $friendlyUserId
            Email = $email
            Password = $password
            FirstName = $firstName
            LastName = $lastName
            Organization = $organization
        }
    }

    return $users
}

function Get-ErrorDetails {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    if ($null -eq $ErrorRecord.Exception.Response) {
        return $ErrorRecord.Exception.Message
    }

    try {
        $stream = $ErrorRecord.Exception.Response.GetResponseStream()
        if ($null -eq $stream) {
            return $ErrorRecord.Exception.Message
        }

        $reader = New-Object System.IO.StreamReader($stream)
        $content = $reader.ReadToEnd()
        $reader.Dispose()

        if ([string]::IsNullOrWhiteSpace($content)) {
            return $ErrorRecord.Exception.Message
        }

        return $content
    }
    catch {
        return $ErrorRecord.Exception.Message
    }
}

$users = Get-Users
$results = @()

Write-Host "RegisterUser endpoint: $registerUserUri"
Write-Host "Users to process: $($users.Count)"
Write-Host "Password for every user: $password"

for ($index = 0; $index -lt $users.Count; $index++) {
    $user = $users[$index]
    $position = '{0:D2}/50' -f ($index + 1)

    if ($DryRun) {
        Write-Host "[$position] DRY-RUN $($user.FriendlyUserId) <$($user.Email)> [$($user.Organization)]"
        continue
    }

    $payload = @{
        FriendlyUserId = $user.FriendlyUserId
        Email = $user.Email
        Password = $user.Password
        FirstName = $user.FirstName
        LastName = $user.LastName
        Organization = $user.Organization
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod `
            -Method Post `
            -Uri $registerUserUri `
            -ContentType 'application/json' `
            -Body $payload

        $results += [PSCustomObject]@{
            FriendlyUserId = $user.FriendlyUserId
            Email = $user.Email
            Success = $true
            ResponseId = $response.id
            Error = $null
        }

        Write-Host "[$position] CREATED  $($user.FriendlyUserId) <$($user.Email)>"
    }
    catch {
        $errorDetails = Get-ErrorDetails -ErrorRecord $_

        $results += [PSCustomObject]@{
            FriendlyUserId = $user.FriendlyUserId
            Email = $user.Email
            Success = $false
            ResponseId = $null
            Error = $errorDetails
        }

        Write-Warning "[$position] FAILED   $($user.FriendlyUserId) <$($user.Email)> :: $errorDetails"
    }
}

if ($DryRun) {
    return
}

$createdCount = @($results | Where-Object Success).Count
$failedCount = @($results | Where-Object { -not $_.Success }).Count

Write-Host ''
Write-Host 'Summary'
Write-Host "Created: $createdCount"
Write-Host "Failed: $failedCount"

if ($failedCount -gt 0) {
    Write-Host ''
    Write-Host 'Failed registrations'
    $results |
        Where-Object { -not $_.Success } |
        ForEach-Object {
            Write-Host "- $($_.FriendlyUserId) <$($_.Email)> :: $($_.Error)"
        }
}
