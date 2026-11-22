# Publica AppLocal\Ventory\ (rápido) y Ventory.exe autocontenido en AppLocal\ (para compartir).
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$proj = Join-Path $root "ventory-tray\VentoryTray.csproj"
$outDir = Join-Path $root "Ventory"
$portableExe = Join-Path $root "Ventory.exe"
$stagePortable = Join-Path $root ".publish-portable"
$stageFolder = Join-Path $root ".publish-folder"
$iconDst = Join-Path $root "ventory-tray\icon-inv-favicon.ico"
$iconSrc = $iconDst
if (-not (Test-Path $iconSrc)) {
  $iconSrc = Join-Path $root "..\public\icon-inv-favicon.ico"
}
if (-not (Test-Path $iconSrc)) {
  throw "No se encontró icon-inv-favicon.ico en ventory-tray ni en public/ del repo principal."
}
Copy-Item $iconSrc $iconDst -Force

foreach ($d in @($stagePortable, $stageFolder)) {
  if (Test-Path $d) { Remove-Item $d -Recurse -Force }
}
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
if (Test-Path $portableExe) { Remove-Item $portableExe -Force -ErrorAction SilentlyContinue }

Write-Host "1/2 Carpeta Ventory (arranque rápido tras instalación en AppData) ..."
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:DebugType=none `
  -p:DebugSymbols=false `
  -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item $iconDst (Join-Path $outDir "icon-inv-favicon.ico") -Force
Copy-Item (Join-Path $root "packaging\LEEME.txt") $outDir -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root "packaging\update.json") $outDir -Force

$zipPath = Join-Path $root "Ventory-desktop.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath -Force

Write-Host "2/2 Ventory.exe portable (un archivo para compartir; se instala solo en 1er uso) ..."
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=none `
  -p:DebugSymbols=false `
  -o $stagePortable

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Move-Item (Join-Path $stagePortable "Ventory.exe") $portableExe -Force
Remove-Item $stagePortable -Recurse -Force -ErrorAction SilentlyContinue

$installPath = Join-Path $env:LOCALAPPDATA "Ventory"
Write-Host ""
Write-Host "Listo."
Write-Host "  Compartir:     $portableExe"
Write-Host "  Desde repo:    $outDir\Ventory.exe"
Write-Host "  Tras 1er uso:  $installPath\Ventory.exe"