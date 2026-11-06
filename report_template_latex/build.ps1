# Build this self-contained source package using TeX Live. No Python required.
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$latexmkCommand = Get-Command latexmk.exe -ErrorAction SilentlyContinue
if (-not $latexmkCommand) {
    $texBin = 'C:\texlive\2026\bin\windows'
    if (Test-Path -LiteralPath (Join-Path $texBin 'latexmk.exe')) {
        $env:Path = $texBin + ';' + $env:Path
        $latexmkCommand = Get-Command latexmk.exe
    } else {
        throw 'TeX Live / latexmk is required. Add its bin directory to PATH.'
    }
}
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss.fffffffZ') + '-build'
$runRoot = Join-Path $projectRoot (Join-Path 'logs\main' $runId)
$diagnostics = Join-Path $runRoot 'diagnostics'
$buildRoot = Join-Path $projectRoot 'build'
$distRoot = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Force -Path $diagnostics,$buildRoot,$distRoot | Out-Null
$consoleLog = Join-Path $runRoot 'console.log'
@(
    '[START UTC] ' + [DateTime]::UtcNow.ToString('o')
    '[CWD] ' + $projectRoot
    '[ENGINE] pdflatex'
    '[COMMAND] latexmk -pdf -synctex=1 -interaction=nonstopmode -file-line-error -halt-on-error -outdir=build main.tex'
) | Set-Content -Encoding UTF8 -LiteralPath $consoleLog
$oldLocale = $env:LC_ALL
$oldLang = $env:LANG
Push-Location -LiteralPath $projectRoot
try {
    $env:LC_ALL = 'C'
    $env:LANG = 'C'
    # Merge and retain all compiler output; do not filter warnings/errors.
    $ErrorActionPreference = 'Continue'
    & $latexmkCommand.Source -pdf -synctex=1 -interaction=nonstopmode -file-line-error -halt-on-error -outdir=build main.tex 2>&1 |
        ForEach-Object {
            $outputLine = $_.ToString()
            Add-Content -Encoding UTF8 -LiteralPath $consoleLog -Value $outputLine
            Write-Output $outputLine
        }
    $resultCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    Get-ChildItem -LiteralPath $buildRoot -File |
        Where-Object { $_.Extension -in '.log','.blg','.fls','.fdb_latexmk' } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $diagnostics }
    if ($resultCode -eq 0) {
        Copy-Item -LiteralPath (Join-Path $buildRoot 'main.pdf') -Destination (Join-Path $distRoot 'main.pdf')
    }
    @(('[RETURN CODE] ' + $resultCode), ('[END UTC] ' + [DateTime]::UtcNow.ToString('o'))) |
        Add-Content -Encoding UTF8 -LiteralPath $consoleLog
    Write-Output ('Log: ' + $consoleLog)
    if ($resultCode -ne 0) { throw ('LaTeX build failed with exit code ' + $resultCode) }
    Write-Output ('PDF: ' + (Join-Path $distRoot 'main.pdf'))
} finally {
    $env:LC_ALL = $oldLocale
    $env:LANG = $oldLang
    Pop-Location
}
