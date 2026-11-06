param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('restore', 'build', 'test', 'run', 'migrate', 'migration', 'check-migration', 'check', 'provision', 'smoke', 'integration')]
    [string]$Action,
    [string]$Name,
    [switch]$Database
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$previousLocation = Get-Location
$previousEnvironment = @{}
$apiProject = 'src/iCareX.Web/iCareX.Web.Api/iCareX.Web.Api.csproj'
$infrastructureProject = 'src/iCareX.Infrastructure/iCareX.Infrastructure.csproj'
$unitProject = 'src/iCareX.UnitTests/iCareX.UnitTests.csproj'
$integrationProject = 'src/iCareX.IntegrationTests/iCareX.IntegrationTests.csproj'
$databaseTool = 'tools/iCareX.DatabaseTool/iCareX.DatabaseTool.csproj'
$installedDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$localDotnet = Join-Path $repositoryRoot 'tools/.local/dotnet/dotnet.exe'
$dotnetExecutable = if ($installedDotnet) { $installedDotnet.Source } elseif (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { throw 'Install .NET 10 or use tools/.local/dotnet.' }

function Invoke-Dotnet([string[]]$CommandArguments) {
    & $dotnetExecutable @CommandArguments
    if ($LASTEXITCODE -ne 0) { throw "Backend command failed (exit $LASTEXITCODE)." }
}
try {
    Set-Location -LiteralPath $repositoryRoot
    if (-not $installedDotnet) {
        $previousEnvironment['DOTNET_ROOT'] = $env:DOTNET_ROOT
        $previousEnvironment['PATH'] = $env:PATH
        $env:DOTNET_ROOT = Split-Path $dotnetExecutable -Parent
        $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    }
    if (-not $env:ASPNETCORE_ENVIRONMENT) {
        $previousEnvironment['ASPNETCORE_ENVIRONMENT'] = $env:ASPNETCORE_ENVIRONMENT
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
    }
    if ($Action -eq 'run' -and -not $env:ASPNETCORE_URLS) {
        $previousEnvironment['ASPNETCORE_URLS'] = $env:ASPNETCORE_URLS
        $env:ASPNETCORE_URLS = 'http://localhost:5080'
    }
    switch ($Action) {
        'restore' {
            Invoke-Dotnet @('restore', 'iCareX.slnx')
            Invoke-Dotnet @('tool', 'restore')
        }
        'build' { Invoke-Dotnet @('build', 'iCareX.slnx', '-p:UseAppHost=false') }
        'test' {
            Invoke-Dotnet @('test', $unitProject, '-p:UseAppHost=false')
            Invoke-Dotnet @('test', $integrationProject, '--filter', 'Category=Host', '-p:UseAppHost=false')
        }
        'integration' { Invoke-Dotnet @('test', $integrationProject, '-p:UseAppHost=false') }
        'run' { Invoke-Dotnet @('run', '--project', $apiProject, '--no-launch-profile') }
        'migration' {
            if ([string]::IsNullOrWhiteSpace($Name) -or $Name -notmatch '^[A-Za-z][A-Za-z0-9_]*$') { throw 'Supply a migration Name, e.g. AddVehicleIndex.' }
            Invoke-Dotnet @('tool', 'restore')
            Invoke-Dotnet @('build', $apiProject, '-p:UseAppHost=false')
            Invoke-Dotnet @('tool', 'run', 'dotnet-ef', 'migrations', 'add', $Name, '--project', $infrastructureProject, '--startup-project', $apiProject, '--output-dir', 'Migrations', '--no-build')
        }
        'migrate' {
            Invoke-Dotnet @('tool', 'restore')
            Invoke-Dotnet @('build', $apiProject, '-p:UseAppHost=false')
            Invoke-Dotnet @('tool', 'run', 'dotnet-ef', 'database', 'update', '--project', $infrastructureProject, '--startup-project', $apiProject, '--no-build')
        }
        'check-migration' {
            Invoke-Dotnet @('tool', 'restore')
            Invoke-Dotnet @('build', $apiProject, '-p:UseAppHost=false')
            $efProjects = @('--project', $infrastructureProject, '--startup-project', $apiProject, '--no-build')
            # List first so a pending-model exit code does not hide migration history.
            $listArguments = @('tool', 'run', 'dotnet-ef', 'migrations', 'list') + $efProjects
            if (-not $Database) { $listArguments += '--no-connect' }
            Invoke-Dotnet $listArguments
            Invoke-Dotnet (@('tool', 'run', 'dotnet-ef', 'migrations', 'has-pending-model-changes') + $efProjects)
        }
        default { Invoke-Dotnet @('run', '--project', $databaseTool, '--', $Action) }
    }
} finally {
    foreach ($environmentKey in $previousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($environmentKey, $previousEnvironment[$environmentKey], 'Process')
    }
    Set-Location -LiteralPath $previousLocation
}
