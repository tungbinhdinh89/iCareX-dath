$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$slidesRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$workspaceRoot = (Resolve-Path (Join-Path $slidesRoot '..\..')).Path
$sourceRoot = Join-Path $workspaceRoot 'report_template_latex\figures\design'
$assetRoot = Join-Path $slidesRoot 'assets'
[System.IO.Directory]::CreateDirectory($assetRoot) | Out-Null
$entries = New-Object System.Collections.Generic.List[object]
$names = @('logo-light.png','logo-dark.png','symbol-dark.png','vehicle-overview.png','fuel-create.png','notifications.png','status-badges.png','card-expenses.png','card-reminder.png')
foreach ($name in $names) {
    $sourcePath = Join-Path $sourceRoot $name
    $targetPath = Join-Path $assetRoot $name
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    $entries.Add([ordered]@{asset=('assets/' + $name); source=('report_template_latex/figures/design/' + $name); operation='exact copy'; sha256=(Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()})
}
$crops = @(
    @{Name='demo-vehicle.png'; Source='vehicle-overview.png'; X=490; Y=180; W=2340; H=1530},
    @{Name='demo-fuel.png'; Source='fuel-create.png'; X=540; Y=710; W=1510; H=790},
    @{Name='demo-inbox.png'; Source='notifications.png'; X=1410; Y=600; W=1410; H=1500}
)
foreach ($crop in $crops) {
    $sourcePath = Join-Path $sourceRoot $crop.Source
    $targetPath = Join-Path $assetRoot $crop.Name
    $image = [System.Drawing.Bitmap]::FromFile($sourcePath)
    try {
        $rect = New-Object System.Drawing.Rectangle($crop.X,$crop.Y,$crop.W,$crop.H)
        $part = $image.Clone($rect,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try { $part.Save($targetPath,[System.Drawing.Imaging.ImageFormat]::Png) } finally { $part.Dispose() }
    } finally { $image.Dispose() }
    $entries.Add([ordered]@{asset=('assets/' + $crop.Name); source=('report_template_latex/figures/design/' + $crop.Source); operation='exact pixel crop'; crop_xywh=@($crop.X,$crop.Y,$crop.W,$crop.H); sha256=(Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()})
}
$fontSource = Join-Path $workspaceRoot 'src\iCareX.Web\iCareX.Web.Client\public\fonts'
$fontTarget = Join-Path $assetRoot 'fonts'
[System.IO.Directory]::CreateDirectory($fontTarget) | Out-Null
foreach ($fontName in @('be-vietnam-pro-400.ttf','be-vietnam-pro-600.ttf')) {
    $fontPath = Join-Path $fontTarget $fontName
    Copy-Item -LiteralPath (Join-Path $fontSource $fontName) -Destination $fontPath -Force
    $entries.Add([ordered]@{asset=('assets/fonts/' + $fontName); source=('src/iCareX.Web/iCareX.Web.Client/public/fonts/' + $fontName); operation='exact copy'; sha256=(Get-FileHash -LiteralPath $fontPath -Algorithm SHA256).Hash.ToLowerInvariant()})
}
$entries.ToArray() | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $slidesRoot 'assets-manifest.json') -Encoding UTF8
Write-Output ('Prepared ' + $entries.Count + ' assets from the approved design screenshots.')
