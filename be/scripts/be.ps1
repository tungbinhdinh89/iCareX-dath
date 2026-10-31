param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('restore','build','test','run','migrate','migration','check','provision','smoke','integration')]
    [string]$Action,
    [string]$Name = 'InitialCreate'
)
$ErrorActionPreference = 'Stop'
$backendRoot = Split-Path $PSScriptRoot -Parent
$originalLocation = Get-Location
$originalEnvironment = @{}
$databaseActions = @('migrate', 'migration', 'check', 'provision', 'smoke')
try {
    Set-Location $backendRoot
    $dotnetExecutable = if (Test-Path '.tools/dotnet/dotnet.exe') { Join-Path $backendRoot '.tools/dotnet/dotnet.exe' } else { 'dotnet' }
    if ($dotnetExecutable -ne 'dotnet') {
        $originalEnvironment['DOTNET_ROOT'] = $env:DOTNET_ROOT
        $originalEnvironment['PATH'] = $env:PATH
        $env:DOTNET_ROOT = Split-Path $dotnetExecutable -Parent
        $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    }
    if ($null -eq $env:ASPNETCORE_ENVIRONMENT) {
        $originalEnvironment['ASPNETCORE_ENVIRONMENT'] = $null
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
    }
    if ($Action -eq 'run' -and $null -eq $env:ASPNETCORE_URLS) {
        $originalEnvironment['ASPNETCORE_URLS'] = $null
        $env:ASPNETCORE_URLS = 'http://localhost:5080'
    }
    if ($databaseActions -contains $Action) {
        $userSecrets = & $dotnetExecutable user-secrets list --project src/VehicleCare.Api 2>&1
        if ($LASTEXITCODE -ne 0 -or -not ($userSecrets -match '^ConnectionStrings:VehicleCare\s*=')) {
            throw @"
Missing User Secret: ConnectionStrings:VehicleCare.
Configure it once with:
dotnet user-secrets --project be/src/VehicleCare.Api set "ConnectionStrings:VehicleCare" "<your-local-connection-string>"
"@
        }
        if ($userSecrets -match 'ConnectionStrings:VehicleCare\s*=\s*.*(Host=\.\.\.|Database=\.\.\.|Username=\.\.\.)') {
            throw @"
The User Secret ConnectionStrings:VehicleCare still contains placeholder values.
Set a real local PostgreSQL connection string with:
dotnet user-secrets --project be/src/VehicleCare.Api set "ConnectionStrings:VehicleCare" "<your-local-connection-string>"
"@
        }
    }
    switch ($Action) {
        'restore' { & $dotnetExecutable restore VehicleCare.slnx; if ($LASTEXITCODE -eq 0) { & $dotnetExecutable tool restore } }
        'build' { & $dotnetExecutable build VehicleCare.slnx -p:UseAppHost=false }
        'test' {
            & $dotnetExecutable test tests/VehicleCare.UnitTests -p:UseAppHost=false
            if ($LASTEXITCODE -eq 0) { & $dotnetExecutable test tests/VehicleCare.IntegrationTests --filter 'Category=Host' -p:UseAppHost=false }
        }
        'integration' { & $dotnetExecutable test tests/VehicleCare.IntegrationTests -p:UseAppHost=false }
        'run' { & $dotnetExecutable run --project src/VehicleCare.Api }
        'migration' {
            & $dotnetExecutable build src/VehicleCare.Api -p:UseAppHost=false
            if ($LASTEXITCODE -eq 0) {
                & $dotnetExecutable tool run dotnet-ef migrations add $Name --project src/VehicleCare.Infrastructure --startup-project src/VehicleCare.Api --output-dir Migrations --no-build
            }
        }
        'migrate' {
            & $dotnetExecutable build src/VehicleCare.Api -p:UseAppHost=false
            if ($LASTEXITCODE -eq 0) {
                & $dotnetExecutable tool run dotnet-ef database update --project src/VehicleCare.Infrastructure --startup-project src/VehicleCare.Api --no-build
            }
        }
        default { & $dotnetExecutable run --project tools/VehicleCare.DatabaseTool -- $Action }
    }
    if ($LASTEXITCODE -ne 0) { throw "Backend command failed (exit $LASTEXITCODE)." }
} finally {
    foreach ($key in $originalEnvironment.Keys) { [Environment]::SetEnvironmentVariable($key, $originalEnvironment[$key], 'Process') }
    Set-Location $originalLocation
}
