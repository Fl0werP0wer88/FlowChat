#requires -Version 5.1
param(
    [string]$BaseUrl = 'https://localhost:7236',
    [string]$Route = 'api/users',
    [string]$NotificationBaseUrl = 'https://localhost:7206',
    [string]$NotificationRoute = 'api/notifications',
    [string]$UserProfileBaseUrl = 'https://localhost:7148',
    [string]$UserProfileConfirmRoute = 'api/userprofiles/email-verification/confirm',
    [int]$ActivationDelaySeconds = 10,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$registerUserUri = '{0}/{1}' -f $BaseUrl.TrimEnd('/'), $Route.TrimStart('/')
$notificationsUri = '{0}/{1}' -f $NotificationBaseUrl.TrimEnd('/'), $NotificationRoute.TrimStart('/')
$confirmEmailUri = '{0}/{1}' -f $UserProfileBaseUrl.TrimEnd('/'), $UserProfileConfirmRoute.TrimStart('/')
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

function Enable-DevelopmentCertificateBypass {
    Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;

public static class TrustAllCerts
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

function Get-ActivationLinkFromBody {
    param(
        [AllowNull()]
        [string]$Body
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return $null
    }

    $match = [regex]::Match($Body, 'https?://[^\s"''<>]+', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        return $null
    }

    return $match.Value.TrimEnd('.', ',', ';', ')', ']', '>')
}

function Get-QueryParameterValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UriString,
        [Parameter(Mandatory = $true)]
        [string]$ParameterName
    )

    try {
        Add-Type -AssemblyName System.Web
        $uri = [System.Uri]$UriString
        $query = [System.Web.HttpUtility]::ParseQueryString($uri.Query)
        $value = $query[$ParameterName]

        if ([string]::IsNullOrWhiteSpace($value)) {
            return $null
        }

        return $value.Trim()
    }
    catch {
        return $null
    }
}

function Get-LatestEmailVerificationNotification {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Notifications,
        [Parameter(Mandatory = $true)]
        [string]$Email
    )

    return @(
        $Notifications |
            Where-Object {
                $_.Email -eq $Email -and ($_.Type -eq 'EmailVerification' -or $_.Type -eq 1)
            } |
            Sort-Object CreatedDate -Descending
    ) | Select-Object -First 1
}

function Invoke-CurlRequest {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('GET', 'POST')]
        [string]$Method,
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [string]$Body
    )

    $curlArgs = @(
        '--silent',
        '--show-error',
        '--location',
        '--write-out',
        "`n%{http_code}",
        '--request',
        $Method
    )

    if ($Uri.StartsWith('https://', [System.StringComparison]::OrdinalIgnoreCase)) {
        $curlArgs += '--insecure'
    }

    if ($Method -eq 'POST') {
        $curlArgs += @(
            '--header',
            'Content-Type: application/json',
            '--data',
            $Body
        )
    }

    $curlArgs += $Uri

    $rawResponse = & curl.exe @curlArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "curl request failed for '$Uri': $($rawResponse -join [Environment]::NewLine)"
    }

    $rawText = ($rawResponse -join [Environment]::NewLine).Trim()
    $statusCodeText = [regex]::Match($rawText, '(\d{3})\s*$').Groups[1].Value

    if ([string]::IsNullOrWhiteSpace($statusCodeText)) {
        throw "Unable to determine HTTP status code for '$Uri'. Response: $rawText"
    }

    $statusCode = [int]$statusCodeText
    $responseBody = $rawText.Substring(0, $rawText.Length - $statusCodeText.Length).TrimEnd()

    return [PSCustomObject]@{
        StatusCode = $statusCode
        Body = $responseBody
    }
}

function Invoke-JsonPostRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string]$Body
    )

    return Invoke-RestMethod `
        -Method Post `
        -Uri $Uri `
        -ContentType 'application/json' `
        -Body $Body
}

Enable-DevelopmentCertificateBypass

$users = Get-Users
$results = @()
$activationResults = @()

Write-Host "RegisterUser endpoint: $registerUserUri"
Write-Host "GetNotifications endpoint: $notificationsUri"
Write-Host "Confirm email endpoint: $confirmEmailUri"
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
    Write-Host ''
    Write-Host "DRY-RUN would wait $ActivationDelaySeconds seconds, fetch notifications, extract activation links and confirm emails."
    return
}

$createdCount = @($results | Where-Object Success).Count
$failedCount = @($results | Where-Object { -not $_.Success }).Count

Write-Host ''
Write-Host 'Registration summary'
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

Write-Host ''
Write-Host "Waiting $ActivationDelaySeconds seconds before fetching notifications..."
Start-Sleep -Seconds $ActivationDelaySeconds

try {
    $notificationsHttpResponse = Invoke-CurlRequest -Method GET -Uri $notificationsUri
    if ($notificationsHttpResponse.StatusCode -lt 200 -or $notificationsHttpResponse.StatusCode -ge 300) {
        throw "Notification API returned HTTP $($notificationsHttpResponse.StatusCode). Body: $($notificationsHttpResponse.Body)"
    }

    $notificationsResponse = $notificationsHttpResponse.Body | ConvertFrom-Json
    $notifications = @($notificationsResponse.Notifications)
}
catch {
    $errorDetails = Get-ErrorDetails -ErrorRecord $_
    throw "Failed to fetch notifications from '$notificationsUri'. $errorDetails"
}

Write-Host "Fetched notifications: $($notifications.Count)"

for ($index = 0; $index -lt $users.Count; $index++) {
    $user = $users[$index]
    $position = '{0:D2}/50' -f ($index + 1)
    $notification = Get-LatestEmailVerificationNotification -Notifications $notifications -Email $user.Email

    if ($null -eq $notification) {
        $activationResults += [PSCustomObject]@{
            Email = $user.Email
            FriendlyUserId = $user.FriendlyUserId
            LinkOpened = $false
            Confirmed = $false
            Error = 'No email verification notification found.'
        }

        Write-Warning "[$position] NOT FOUND $($user.Email) :: no email verification notification found"
        continue
    }

    $activationLink = Get-ActivationLinkFromBody -Body $notification.Body
    if ([string]::IsNullOrWhiteSpace($activationLink)) {
        $activationResults += [PSCustomObject]@{
            Email = $user.Email
            FriendlyUserId = $user.FriendlyUserId
            LinkOpened = $false
            Confirmed = $false
            Error = 'No activation link found in notification body.'
        }

        Write-Warning "[$position] NO LINK   $($user.Email) :: activation link not found in body"
        continue
    }

    $linkOpened = $false
    $confirmed = $false
    $errorMessage = $null

    try {
        $activationLinkResponse = Invoke-CurlRequest -Method GET -Uri $activationLink
        if ($activationLinkResponse.StatusCode -ge 200 -and $activationLinkResponse.StatusCode -lt 400) {
            $linkOpened = $true
        }
        else {
            $errorMessage = "Link GET returned HTTP $($activationLinkResponse.StatusCode)"
        }
    }
    catch {
        $errorMessage = "Link GET failed: $(Get-ErrorDetails -ErrorRecord $_)"
    }

    $token = Get-QueryParameterValue -UriString $activationLink -ParameterName 'token'
    if ([string]::IsNullOrWhiteSpace($token)) {
        if ([string]::IsNullOrWhiteSpace($errorMessage)) {
            $errorMessage = 'Activation link does not contain token query parameter.'
        }
    }
    else {
        try {
            $confirmPayload = @{ Token = $token } | ConvertTo-Json
            $null = Invoke-JsonPostRequest -Uri $confirmEmailUri -Body $confirmPayload

            $confirmed = $true
        }
        catch {
            $confirmError = Get-ErrorDetails -ErrorRecord $_
            if ([string]::IsNullOrWhiteSpace($errorMessage)) {
                $errorMessage = "Token confirmation failed: $confirmError"
            }
            else {
                $errorMessage = "$errorMessage | Token confirmation failed: $confirmError"
            }
        }
    }

    $activationResults += [PSCustomObject]@{
        Email = $user.Email
        FriendlyUserId = $user.FriendlyUserId
        LinkOpened = $linkOpened
        Confirmed = $confirmed
        Error = $errorMessage
    }

    if ($confirmed) {
        Write-Host "[$position] ACTIVATED $($user.FriendlyUserId) <$($user.Email)>"
    }
    elseif ($linkOpened) {
        Write-Warning "[$position] PARTIAL  $($user.FriendlyUserId) <$($user.Email)> :: $errorMessage"
    }
    else {
        Write-Warning "[$position] FAILED   $($user.FriendlyUserId) <$($user.Email)> :: $errorMessage"
    }
}

$activatedCount = @($activationResults | Where-Object Confirmed).Count
$activationFailedCount = @($activationResults | Where-Object { -not $_.Confirmed }).Count

Write-Host ''
Write-Host 'Activation summary'
Write-Host "Activated: $activatedCount"
Write-Host "Not activated: $activationFailedCount"

if ($activationFailedCount -gt 0) {
    Write-Host ''
    Write-Host 'Activation failures'
    $activationResults |
        Where-Object { -not $_.Confirmed } |
        ForEach-Object {
            Write-Host "- $($_.FriendlyUserId) <$($_.Email)> :: $($_.Error)"
        }
}
