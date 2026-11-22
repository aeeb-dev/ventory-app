# Publica release en GitHub (repo aeeb-dev/ventory-app, rama main).
# Uso: .\scripts\publish-release.ps1 -Version 1.0.1
param(
  [Parameter(Mandatory = $true)]
  [string]$Version
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
  throw "Version debe ser MAJOR.MINOR.PATCH (ej. 1.0.1)"
}

$branch = git branch --show-current
if ($branch -notin @("main", "app-local")) {
  throw "Debes estar en main (repo ventory-app) o app-local (actual: $branch)."
}
if (-not (Test-Path (Join-Path $PSScriptRoot "..\build-desktop.ps1"))) {
  throw "No se encuentra build-desktop.ps1 en la raíz del repo de escritorio."
}

$tag = "ventory-v$Version"
$existing = git tag --list $tag
if ($existing) {
  throw "El tag $tag ya existe."
}

$csproj = Join-Path $PSScriptRoot "..\ventory-tray\VentoryTray.csproj"
$c = Get-Content $csproj -Raw
$c = $c -replace '<(Version)>[^<]+</Version>', "<Version>$Version</Version>"
$c = $c -replace '<(AssemblyVersion)>[^<]+</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>"
$c = $c -replace '<(FileVersion)>[^<]+</FileVersion>', "<FileVersion>$Version.0</FileVersion>"
Set-Content $csproj $c -NoNewline

git add $csproj
git commit -m "release: ventory-v$Version"
git tag $tag
$remote = "origin"
if ((git remote) -contains "ventory-app") { $remote = "ventory-app" }
$pushRef = if ($remote -eq "ventory-app" -and $branch -eq "app-local") { "HEAD:main" } else { $branch }
Write-Host "Tag local $tag creado. Publicar en aeeb-dev/ventory-app:"
Write-Host "  git push $remote $pushRef"
Write-Host "  git push $remote $tag"
Write-Host ""
Write-Host "El push del tag dispara CI, construye Ventory.exe y crea GitHub Release publico."