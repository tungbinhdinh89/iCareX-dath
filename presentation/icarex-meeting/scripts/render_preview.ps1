param([int]$Scale = 1600)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$slidesRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runName = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss.fffffffZ')
$output = Join-Path (Join-Path $slidesRoot 'rendered') $runName
[System.IO.Directory]::CreateDirectory($output) | Out-Null
pdftoppm -scale-to $Scale -png (Join-Path $slidesRoot 'dist\main.pdf') (Join-Path $output 'slide')
if ($LASTEXITCODE -ne 0) { throw 'PDF rendering failed.' }
$pages = @(Get-ChildItem -LiteralPath $output -File -Filter 'slide-*.png' | Sort-Object Name)
$font = New-Object System.Drawing.Font('Arial',12)
try {
    for ($offset=0; $offset -lt $pages.Count; $offset+=6) {
        $sheet = New-Object System.Drawing.Bitmap(1280,1140)
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        try {
            $graphics.Clear([System.Drawing.Color]::LightGray)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            for ($slot=0; $slot -lt 6 -and $offset+$slot -lt $pages.Count; $slot++) {
                $pic = [System.Drawing.Image]::FromFile($pages[$offset+$slot].FullName)
                try {
                    $x = ($slot % 2)*640+6
                    $y = [Math]::Floor($slot/2)*380
                    $ratio = [Math]::Min(628/$pic.Width,354/$pic.Height)
                    $graphics.DrawString(('Slide ' + ($offset+$slot+1)),$font,[System.Drawing.Brushes]::Black,$x,($y+1))
                    $graphics.DrawImage($pic,[int]$x,[int]($y+22),[int]($pic.Width*$ratio),[int]($pic.Height*$ratio))
                } finally { $pic.Dispose() }
            }
            $sheet.Save((Join-Path $output ('contact-{0:00}.png' -f ([int]($offset/6)+1))),[System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $graphics.Dispose(); $sheet.Dispose() }
    }
} finally { $font.Dispose() }
Write-Output ('Rendered ' + $pages.Count + ' slides: ' + $output)
