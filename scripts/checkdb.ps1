[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
# SELECT-only connectivity/server/TLS check; never provisions or migrates.
& (Join-Path $PSScriptRoot 'be.ps1') -Action check
