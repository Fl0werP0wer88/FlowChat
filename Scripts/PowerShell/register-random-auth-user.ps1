#requires -Version 5.1
param(
    [string]$BaseUrl = 'https://localhost:7236',
    [string]$Route = 'api/users',
    [string]$EmailDomain = 'seed.flowchat.local',
    [string]$Password = 'dRabina#098',
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$registerUserUri = '{0}/{1}' -f $BaseUrl.TrimEnd('/'), $Route.TrimStart('/')
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

function Enable-DevelopmentCertificateBypass {
    Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;

public static class TrustAllCertsForSingleUserRegistration
{
    public static bool IgnoreValidation(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }
}
"@

    [System.Net.ServicePointManager]::SecurityProtocol = `
        [System.Net.SecurityProtocolType]::Tls12 -bor `
        [System.Net.SecurityProtocolType]::Tls11 -bor `
        [System.Net.SecurityProtocolType]::Tls
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { param($sender, $certificate, $chain, $sslPolicyErrors) return $true }
}

function Get-ErrorDetails {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    $responseProperty = $ErrorRecord.Exception.PSObject.Properties['Response']
    if ($null -eq $responseProperty -or $null -eq $responseProperty.Value) {
        return $ErrorRecord.Exception.Message
    }

    try {
        $stream = $responseProperty.Value.GetResponseStream()
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

function New-RandomAuthUser {
    $id = [Guid]::NewGuid()
    $firstName = Get-Random -InputObject $firstNames
    $lastName = Get-Random -InputObject $lastNames
    $organization = Get-Random -InputObject $organizations
    $token = $id.ToString('N').Substring(0, 12)
    $friendlyUserId = ('{0}.{1}.{2}' -f $firstName, $lastName, $token).ToLowerInvariant()
    $email = ('{0}.{1}.{2}@{3}' -f $firstName, $lastName, $token, $EmailDomain.TrimStart('@')).ToLowerInvariant()

    return [PSCustomObject]@{
        Id = $id
        FriendlyUserId = $friendlyUserId
        Email = $email
        Password = $Password
        FirstName = $firstName
        LastName = $lastName
        Organization = $organization
    }
}

Enable-DevelopmentCertificateBypass

$user = New-RandomAuthUser
$payload = @{
    Id = $user.Id
    FriendlyUserId = $user.FriendlyUserId
    Email = $user.Email
    Password = $user.Password
    FirstName = $user.FirstName
    LastName = $user.LastName
    Organization = $user.Organization
} | ConvertTo-Json

Write-Host "RegisterUser endpoint: $registerUserUri"
Write-Host "Generated user:"
Write-Host "Id: $($user.Id)"
Write-Host "FriendlyUserId: $($user.FriendlyUserId)"
Write-Host "Email: $($user.Email)"
Write-Host "Password: $($user.Password)"
Write-Host "FirstName: $($user.FirstName)"
Write-Host "LastName: $($user.LastName)"
Write-Host "Organization: $($user.Organization)"

if ($DryRun) {
    Write-Host ''
    Write-Host 'DRY-RUN enabled. User was not registered.'
    return
}

try {
    $response = Invoke-RestMethod `
        -Method Put `
        -Uri $registerUserUri `
        -ContentType 'application/json' `
        -Body $payload

    Write-Host ''
    Write-Host "CREATED $($user.FriendlyUserId) <$($user.Email)>"

    if ($null -ne $response -and $null -ne $response.PSObject.Properties['id']) {
        Write-Host "ResponseId: $($response.id)"
    }

    Write-Host 'Email activation was not performed; account should remain unconfirmed.'
}
catch {
    $errorDetails = Get-ErrorDetails -ErrorRecord $_

    Write-Host ''
    Write-Warning "FAILED $($user.FriendlyUserId) <$($user.Email)> :: $errorDetails"
    exit 1
}
