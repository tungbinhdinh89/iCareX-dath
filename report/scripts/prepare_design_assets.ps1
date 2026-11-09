# Copy the supplied design exports and extract exact, unscaled regions.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$reportRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$workspaceRoot = (Resolve-Path (Join-Path $reportRoot '..')).Path
$inputRoot = Join-Path $workspaceRoot 'Design'
$outputRoot = Join-Path $reportRoot 'figures\design'
[System.IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$assetEntries = New-Object System.Collections.Generic.List[object]
$sourceMap = [ordered]@{
    'component-library.png' = 'Pasted 2026-11-07 at 6.53.49 AM.png'
    'design-foundations.png' = 'Pasted 2026-11-07 at 6.54.30 AM.png'
    'login.png' = 'Pasted 2026-11-07 at 8.41.53 AM.png'
    'landing.png' = 'Pasted 2026-11-07 at 8.42.33 AM.png'
    'register.png' = 'Pasted 2026-11-07 at 8.43.16 AM.png'
    'vehicle-overview.png' = 'Pasted 2026-11-07 at 8.43.58 AM.png'
    'fuel-create.png' = 'Pasted 2026-11-07 at 8.44.23 AM.png'
    'notifications.png' = 'Pasted 2026-11-07 at 8.44.42 AM.png'
}
foreach ($assetName in $sourceMap.Keys) {
    $sourcePath = Join-Path $inputRoot $sourceMap[$assetName]
    $targetPath = Join-Path $outputRoot $assetName
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    $bitmap = [System.Drawing.Bitmap]::FromFile($sourcePath)
    try {
        $assetEntries.Add([ordered]@{
            asset = 'figures/design/' + $assetName
            source = 'Design/' + $sourceMap[$assetName]
            operation = 'byte-for-byte copy'
            source_sha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
            asset_sha256 = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
            source_width = $bitmap.Width
            source_height = $bitmap.Height
            width = $bitmap.Width
            height = $bitmap.Height
        })
    } finally { $bitmap.Dispose() }
}
$crops = @(
    @{ Name='logo-light.png'; Source='design-foundations.png'; X=150; Y=1028; Width=270; Height=90 },
    @{ Name='logo-dark.png'; Source='design-foundations.png'; X=518; Y=1032; Width=252; Height=82 },
    @{ Name='symbol-light.png'; Source='design-foundations.png'; X=175; Y=720; Width=232; Height=232 },
    @{ Name='symbol-dark.png'; Source='design-foundations.png'; X=460; Y=700; Width=268; Height=268 },
    @{ Name='controls.png'; Source='component-library.png'; X=110; Y=450; Width=3380; Height=570 },
    @{ Name='status-chips.png'; Source='component-library.png'; X=110; Y=1080; Width=3380; Height=320 },
    @{ Name='status-badges.png'; Source='component-library.png'; X=145; Y=1205; Width=1230; Height=85 },
    @{ Name='component-cards.png'; Source='component-library.png'; X=110; Y=2145; Width=3380; Height=665 },
    @{ Name='list-feedback.png'; Source='component-library.png'; X=110; Y=4380; Width=3380; Height=460 },
    @{ Name='field-focus.png'; Source='component-library.png'; X=975; Y=760; Width=830; Height=260 },
    @{ Name='field-error.png'; Source='component-library.png'; X=1810; Y=760; Width=830; Height=260 },
    @{ Name='card-vehicle.png'; Source='component-library.png'; X=110; Y=2145; Width=810; Height=665 },
    @{ Name='card-odometer.png'; Source='component-library.png'; X=968; Y=2145; Width=810; Height=665 },
    @{ Name='card-reminder.png'; Source='component-library.png'; X=1826; Y=2145; Width=810; Height=665 },
    @{ Name='card-expenses.png'; Source='component-library.png'; X=2684; Y=2145; Width=806; Height=665 },
    @{ Name='feedback-loading.png'; Source='component-library.png'; X=110; Y=4380; Width=810; Height=460 },
    @{ Name='feedback-empty.png'; Source='component-library.png'; X=968; Y=4380; Width=810; Height=460 },
    @{ Name='feedback-error.png'; Source='component-library.png'; X=1826; Y=4380; Width=810; Height=460 },
    @{ Name='feedback-success.png'; Source='component-library.png'; X=2684; Y=4380; Width=806; Height=460 },
    @{ Name='nav-overview.png'; Source='vehicle-overview.png'; X=54; Y=215; Width=48; Height=48 },
    @{ Name='nav-vehicles.png'; Source='vehicle-overview.png'; X=54; Y=319; Width=48; Height=48 },
    @{ Name='nav-fuel.png'; Source='vehicle-overview.png'; X=54; Y=423; Width=48; Height=48 },
    @{ Name='nav-expenses.png'; Source='vehicle-overview.png'; X=54; Y=527; Width=48; Height=48 },
    @{ Name='nav-maintenance.png'; Source='vehicle-overview.png'; X=54; Y=631; Width=48; Height=48 },
    @{ Name='nav-inbox.png'; Source='vehicle-overview.png'; X=54; Y=735; Width=48; Height=48 },
    @{ Name='nav-reports.png'; Source='vehicle-overview.png'; X=54; Y=839; Width=48; Height=48 },
    @{ Name='nav-settings.png'; Source='vehicle-overview.png'; X=54; Y=943; Width=48; Height=48 }
)
foreach ($crop in $crops) {
    $sourcePath = Join-Path $inputRoot $sourceMap[$crop.Source]
    $targetPath = Join-Path $outputRoot $crop.Name
    $bitmap = [System.Drawing.Bitmap]::FromFile($sourcePath)
    try {
        if ($crop.X -lt 0 -or $crop.Y -lt 0 -or
            ($crop.X + $crop.Width) -gt $bitmap.Width -or
            ($crop.Y + $crop.Height) -gt $bitmap.Height) { throw ('Crop outside image: ' + $crop.Name) }
        $rectangle = New-Object System.Drawing.Rectangle($crop.X, $crop.Y, $crop.Width, $crop.Height)
        $region = $bitmap.Clone($rectangle, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try { $region.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png) } finally { $region.Dispose() }
        $assetEntries.Add([ordered]@{
            asset = 'figures/design/' + $crop.Name
            source = 'Design/' + $sourceMap[$crop.Source]
            operation = 'exact pixel crop; no resampling, recoloring or content editing'
            source_sha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
            asset_sha256 = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
            source_width = $bitmap.Width
            source_height = $bitmap.Height
            crop_xywh = @($crop.X, $crop.Y, $crop.Width, $crop.Height)
            width = $crop.Width
            height = $crop.Height
        })
    } finally { $bitmap.Dispose() }
}
$manifest = [ordered]@{
    revision_date = '2026-11-07'
    authority = 'User-supplied design screenshots; screenshot fallback explicitly authorized'
    figma_file = 'dhuYBdCuQxXFoNb6OpOjdW'
    figma_node = '35:451'
    mcp_file_read = $false
    note = 'Screenshot text is reference material; DEC07 and the agreed account requirements remain authoritative.'
    assets = @($assetEntries.ToArray())
}
$manifestPath = Join-Path $reportRoot 'evidence\design-assets.json'
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 7) + [Environment]::NewLine, $utf8)
Write-Output ('Prepared ' + $assetEntries.Count + ' exact design assets.')
Write-Output ('Manifest: ' + $manifestPath)
