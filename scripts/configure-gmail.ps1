[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repositoryRoot 'src/iCareX.Web/iCareX.Web.Api/iCareX.Web.Api.csproj'
$resultPath = Join-Path $repositoryRoot '.tmp-smtp-setup-result.json'
$securePassword = $null
$smtp = $null
$message = $null
$secretPayload = $null
$process = $null

function Write-SetupResult([bool]$Success, [string]$Reason) {
    @{ success = $Success; reason = $Reason; host = 'smtp.gmail.com'; port = 587; updatedAtUtc = [DateTime]::UtcNow.ToString('o') } |
        ConvertTo-Json | Set-Content -LiteralPath $resultPath -Encoding UTF8
}

try {
    Write-SetupResult $false 'waiting_for_input'
    Write-Host 'iCareX - Gmail SMTP setup' -ForegroundColor Green
    Write-Host 'Use a Google App Password, NOT your normal account password.'
    Write-Host 'Enable 2-Step Verification, then create an App Password:'
    Write-Host 'https://myaccount.google.com/apppasswords'
    Write-Host 'Credentials stay on this computer and are never printed.'
    Write-Host ''
    $senderAddress = (Read-Host 'Gmail / Google Workspace sender email').Trim()
    $parsedAddress = [System.Net.Mail.MailAddress]::new($senderAddress)
    if ($parsedAddress.Address -ne $senderAddress) { throw 'invalid_sender_address' }
    $securePassword = Read-Host 'Google App Password (hidden input, paste WITHOUT spaces)' -AsSecureString
    if ($securePassword.Length -ne 16) { throw 'app_password_must_be_16_characters_without_spaces' }
    Write-Host ''
    Write-Host 'A test email will be sent ONLY to the sender address entered above.'
    $consent = Read-Host 'Type SEND to test SMTP and save valid settings; anything else cancels'
    if ($consent -cne 'SEND') { Write-SetupResult $false 'cancelled'; return }

    Write-SetupResult $false 'testing_smtp'
    $smtp = [System.Net.Mail.SmtpClient]::new('smtp.gmail.com', 587)
    $smtp.EnableSsl = $true
    $smtp.Timeout = 15000
    $smtp.UseDefaultCredentials = $false
    $smtp.Credentials = [System.Net.NetworkCredential]::new($senderAddress, $securePassword)
    $message = [System.Net.Mail.MailMessage]::new($senderAddress, $senderAddress,
        'iCareX - SMTP connection test',
        'Gmail SMTP accepted this test message. iCareX can now send account verification emails after the API reloads its configuration. This message contains no verification token or password.')
    Write-Host 'Testing Gmail SMTP with STARTTLS...' -ForegroundColor Cyan
    $smtp.Send($message)

    # Merge settings through JSON stdin. No password in command-line arguments or temp files.
    $credential = [System.Net.NetworkCredential]::new('', $securePassword)
    $secretPayload = @{
        'AccountEmail:Host' = 'smtp.gmail.com'
        'AccountEmail:Port' = '587'
        'AccountEmail:EnableSsl' = 'true'
        'AccountEmail:UserName' = $senderAddress
        'AccountEmail:From' = $senderAddress
        'AccountEmail:Password' = $credential.Password
        'AccountEmail:PublicBaseUrl' = 'http://localhost:8443'
        'AccountEmail:DeliveryTimeoutSeconds' = '15'
    } | ConvertTo-Json -Compress
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Get-Command dotnet -ErrorAction Stop).Source
    $startInfo.Arguments = 'user-secrets --project "' + $projectPath + '" set'
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($startInfo)
    $process.StandardInput.WriteLine($secretPayload)
    $process.StandardInput.Close()
    $null = $process.StandardOutput.ReadToEnd()
    $null = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw 'secret_store_write_failed' }
    Write-SetupResult $true 'smtp_test_accepted_and_settings_saved'
    Write-Host 'SUCCESS: Gmail accepted the test email. Settings saved to .NET User Secrets.' -ForegroundColor Green
    Write-Host 'Restart the iCareX API, then sign in and resend the verification email.'
}
catch [System.Net.Mail.SmtpException] {
    Write-SetupResult $false ('smtp_failed_' + [int]$_.Exception.StatusCode)
    Write-Host 'SMTP failed. Check your App Password, 2-Step Verification, account policy, or network connection. Existing credentials were not replaced.' -ForegroundColor Red
}
catch {
    $knownReasons = @('invalid_sender_address', 'app_password_must_be_16_characters_without_spaces', 'secret_store_write_failed')
    $reason = if ($knownReasons -contains $_.Exception.Message) { $_.Exception.Message } else { 'setup_failed' }
    Write-SetupResult $false $reason
    Write-Host ('Setup did not complete: ' + $reason) -ForegroundColor Red
}
finally {
    $secretPayload = $null
    $credential = $null
    if ($securePassword) { $securePassword.Dispose() }
    if ($message) { $message.Dispose() }
    if ($smtp) { $smtp.Dispose() }
    if ($process) { $process.Dispose() }
    $null = Read-Host 'Press Enter to close'
}
