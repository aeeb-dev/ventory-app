param(
  [string]$ProductVersion = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$proj = Join-Path $root "ventory-tray\VentoryTray.csproj"
$outDir = Join-Path $root "Ventory"
$portableExe = Join-Path $root "Ventory.exe"
$stagePortable = Join-Path $root ".publish-portable"
$iconDst = Join-Path $root "ventory-tray\icon-inv-favicon.ico"

$versionProps = @()
if ($ProductVersion -match '^(\d+)\.(\d+)\.(\d+)$') {
  $fileVer = "$ProductVersion.0"
  $versionProps = @(
    "-p:Version=$ProductVersion",
    "-p:AssemblyVersion=$fileVer",
    "-p:FileVersion=$fileVer",
    "-p:InformationalVersion=$ProductVersion"
  )
  Write-Host "Version embebida: $ProductVersion ($fileVer)"
}

$iconSrc = $iconDst
if (-not (Test-Path $iconSrc)) {
  $iconSrc = Join-Path $root "..\public\icon-inv-favicon.ico"
}
if (-not (Test-Path $iconSrc)) {
  throw "No se encontró icon-inv-favicon.ico (ventory-tray o public)."
}
$srcFull = (Resolve-Path -LiteralPath $iconSrc).Path
$dstFull = [System.IO.Path]::GetFullPath($iconDst)
if ($srcFull -ne $dstFull) {
  Copy-Item -LiteralPath $iconSrc -Destination $iconDst -Force
}

if (Test-Path $stagePortable) { Remove-Item $stagePortable -Recurse -Force }
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
if (Test-Path $portableExe) { Remove-Item $portableExe -Force -ErrorAction SilentlyContinue }
$zipPath = Join-Path $root "Ventory-desktop.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "1/2 Carpeta Ventory ..."
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:DebugType=none `
  -p:DebugSymbols=false `
  @versionProps `
  -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item $iconDst (Join-Path $outDir "icon-inv-favicon.ico") -Force
Copy-Item (Join-Path $root "packaging\LEEME.txt") $outDir -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root "packaging\update.json") $outDir -Force

Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath -Force

Write-Host "2/2 Ventory.exe portable ..."
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=none `
  -p:DebugSymbols=false `
  @versionProps `
  -o $stagePortable

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Move-Item (Join-Path $stagePortable "Ventory.exe") $portableExe -Force
Remove-Item $stagePortable -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Listo: $portableExe"