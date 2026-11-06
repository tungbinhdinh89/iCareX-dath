[CmdletBinding()]
param([switch]$Database)
$ErrorActionPreference = 'Stop'
# Default: list migrations offline and compare the EF model with the snapshot.
# -Database additionally compares migration history against the configured DB.
& (Join-Path $PSScriptRoot 'be.ps1') -Action check-migration -Database:$Database
