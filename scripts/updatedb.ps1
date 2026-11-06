[CmdletBinding(SupportsShouldProcess = $true)]
param()
$ErrorActionPreference = 'Stop'
if ($PSCmdlet.ShouldProcess('Database configured by User Secrets/environment', 'Apply all pending EF migrations')) {
    & (Join-Path $PSScriptRoot 'be.ps1') -Action migrate
}
