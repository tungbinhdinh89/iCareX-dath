param([int]$Scale = 900)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$reportRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$renderRun = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss.fffffffZ')
$renderRoot = Join-Path (Join-Path $reportRoot 'rendered') $renderRun
[System.IO.Directory]::CreateDirectory($renderRoot) | Out-Null
$pdfPath = Join-Path $reportRoot 'dist/main.pdf'
& pdftoppm -scale-to $Scale -png $pdfPath (Join-Path $renderRoot 'page')
if ($LASTEXITCODE -ne 0) { throw 'pdftoppm failed' }
$pageFiles = @(Get-ChildItem -LiteralPath $renderRoot -File -Filter 'page-*.png' | Sort-Object Name)
$cellWidth = 355
$cellHeight = 525
$labelFont = New-Object System.Drawing.Font('Arial', 11)
try {
  for ($offset = 0; $offset -lt $pageFiles.Count; $offset += 12) {
    $canvas = New-Object System.Drawing.Bitmap(($cellWidth * 3), ($cellHeight * 4))
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
      $graphics.Clear([System.Drawing.Color]::LightGray)
      $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
      for ($slot = 0; $slot -lt 12 -and ($offset + $slot) -lt $pageFiles.Count; $slot++) {
        $pic = [System.Drawing.Image]::FromFile($pageFiles[$offset + $slot].FullName)
        try {
          $ratio = [Math]::Min(($cellWidth - 12) / $pic.Width, ($cellHeight - 30) / $pic.Height)
          $x = ($slot % 3) * $cellWidth + 6
          $y = [Math]::Floor($slot / 3) * $cellHeight
          $graphics.DrawString(('PDF page ' + ($offset + $slot + 1)), $labelFont, [System.Drawing.Brushes]::Black, $x, ($y + 3))
          $graphics.DrawImage($pic, [int]$x, [int]($y + 25), [int]($pic.Width * $ratio), [int]($pic.Height * $ratio))
        } finally { $pic.Dispose() }
      }
      $sheetName = 'contact-{0:00}.png' -f ([int]($offset / 12) + 1)
      $canvas.Save((Join-Path $renderRoot $sheetName), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $canvas.Dispose() }
  }
} finally { $labelFont.Dispose() }
Write-Output ('Rendered ' + $pageFiles.Count + ' PDF pages in ' + $renderRoot)
