[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')]
    [string]$Name
)
$ErrorActionPreference = 'Stop'
if ($PSCmdlet.ShouldProcess('iCareX.Infrastructure/Migrations', "Add migration $Name")) {
    & (Join-Path $PSScriptRoot 'be.ps1') -Action migration -Name $Name
}
