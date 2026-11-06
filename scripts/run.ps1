[CmdletBinding()]
param(
    [ValidateSet('Both', 'Frontend', 'Backend')]
    [string]$Only = 'Both',
    [ValidateRange(1, 65535)][int]$FrontendPort = 8443,
    [ValidateRange(1, 65535)][int]$BackendPort = 5080,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$clientRoot = Join-Path $repositoryRoot 'src/iCareX.Web/iCareX.Web.Client'
$apiRoot = Join-Path $repositoryRoot 'src/iCareX.Web/iCareX.Web.Api'
$previousLocation = Get-Location
$environmentNames = @('ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS', 'ICAREX_API_URL', 'PORT', 'DOTNET_ROOT', 'PATH')
$savedEnvironment = @{}
foreach ($key in $environmentNames) { $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
$ownedProcesses = @()
$logOffsets = @{}
$logRoot = Join-Path ([IO.Path]::GetTempPath()) ('icarex-run-' + [Guid]::NewGuid().ToString('N'))

function Test-Endpoint([string]$Url) {
    try { return (Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200 }
    catch { return $false }
}
function Write-ServiceLogs {
    foreach ($service in $script:ownedProcesses) {
        foreach ($log in @($service.Out, $service.Err)) {
            if (Test-Path -LiteralPath $log) {
                $lines = @(Get-Content -LiteralPath $log)
                $offset = if ($script:logOffsets.ContainsKey($log)) { $script:logOffsets[$log] } else { 0 }
                for ($i = $offset; $i -lt $lines.Count; $i++) { Write-Host "[$($service.Name)] $($lines[$i])" }
                $script:logOffsets[$log] = $lines.Count
            }
        }
    }
}
function Start-ServiceProcess([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$Directory, [string]$Probe) {
    if (Test-Endpoint $Probe) {
        Write-Host "$Name already responds at $Probe; reusing it (will not stop it on exit)."
        return
    }
    $stdout = Join-Path $logRoot "$Name.stdout.log"
    $stderr = Join-Path $logRoot "$Name.stderr.log"
    $process = Start-Process -FilePath $Executable -ArgumentList $Arguments -WorkingDirectory $Directory -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $script:ownedProcesses += [pscustomobject]@{ Name = $Name; Process = $process; Out = $stdout; Err = $stderr }
    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    while ([DateTime]::UtcNow -lt $deadline) {
        Write-ServiceLogs
        if ($process.HasExited) { throw "$Name exited with code $($process.ExitCode). Logs: $logRoot" }
        if (Test-Endpoint $Probe) { Write-Host "$Name ready: $Probe"; return }
        Start-Sleep -Milliseconds 400
    }
    throw "$Name did not respond within 45 seconds. Logs: $logRoot"
}

try {
    Set-Location -LiteralPath $repositoryRoot
    if ($Only -eq 'Both' -and $FrontendPort -eq $BackendPort) { throw 'FrontendPort and BackendPort must differ.' }
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    $dotnetExecutable = if ($dotnetCommand) { $dotnetCommand.Source } else { Join-Path $repositoryRoot 'tools/.local/dotnet/dotnet.exe' }
    if (-not $dotnetCommand -and $Only -ne 'Frontend') {
        if (-not (Test-Path -LiteralPath $dotnetExecutable)) { throw 'Install .NET 10 or tools/.local/dotnet.' }
        $env:DOTNET_ROOT = Split-Path $dotnetExecutable -Parent
        $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    }
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "http://localhost:$BackendPort"
    $env:ICAREX_API_URL = "http://localhost:$BackendPort"
    $env:PORT = [string]$FrontendPort
    New-Item -ItemType Directory -Path $logRoot | Out-Null
    if ($Only -ne 'Frontend') {
        if (-not $NoBuild -and -not (Test-Endpoint "$env:ASPNETCORE_URLS/health/live")) {
            & $dotnetExecutable build (Join-Path $apiRoot 'iCareX.Web.Api.csproj') '-p:UseAppHost=false'
            if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
        }
        $apiDll = Join-Path $apiRoot 'bin/Debug/net10.0/iCareX.Web.Api.dll'
        if (-not (Test-Path -LiteralPath $apiDll)) { throw 'Backend DLL missing. Run without -NoBuild.' }
        Start-ServiceProcess 'Backend' $dotnetExecutable @(('"' + $apiDll + '"')) $apiRoot "$env:ASPNETCORE_URLS/health/live"
    }
    if ($Only -ne 'Backend') {
        $node = (Get-Command node -ErrorAction Stop).Source
        $vite = Join-Path $clientRoot 'node_modules/vite/bin/vite.js'
        if (-not (Test-Path -LiteralPath $vite)) { throw 'Frontend dependencies missing. Run pnpm install from the repository root.' }
        Start-ServiceProcess 'Frontend' $node @(('"' + $vite + '"'), '--config', 'vite.config.ts') $clientRoot "http://localhost:$FrontendPort/@vite/client"
        Write-Host "Client UI: http://localhost:$FrontendPort/"
        Write-Host "An existing frontend keeps its original API proxy. Restart it if BackendPort has changed."
    }
    if ($Only -ne 'Frontend') {
        Write-Host "API: $env:ASPNETCORE_URLS | OpenAPI: $env:ASPNETCORE_URLS/openapi/v1.json"
        Write-Host "Database readiness: $env:ASPNETCORE_URLS/health/ready (separate from process liveness)."
    }
    Write-Host "Logs: $logRoot"
    if ($ownedProcesses.Count -eq 0) { Write-Host 'Both requested services already run; nothing started.'; return }
    Write-Host 'Press Ctrl+C to stop services started by this script.'
    while ($true) {
        Write-ServiceLogs
        foreach ($service in $ownedProcesses) {
            if ($service.Process.HasExited) { throw "$($service.Name) exited with code $($service.Process.ExitCode)." }
        }
        Start-Sleep -Milliseconds 500
    }
} finally {
    foreach ($service in $ownedProcesses) {
        if (-not $service.Process.HasExited) {
            & taskkill.exe /PID $service.Process.Id /T /F 2>$null | Out-Null
        }
    }
    foreach ($key in $environmentNames) { [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key], 'Process') }
    Set-Location -LiteralPath $previousLocation
}
