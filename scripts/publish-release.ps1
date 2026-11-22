# Publica release en GitHub (solo rama app-local).
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
if ($branch -ne "app-local") {
  throw "Debes estar en la rama app-local (actual: $branch)."
}
if (-not (Test-Path (Join-Path $PSScriptRoot "..\build-desktop.ps1"))) {
  throw "No se encuentra build-desktop.ps1; ¿estás en la raíz de app-local?"
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
git commit -m "release(app-local): ventory-v$Version"
git tag $tag
Write-Host "Tag local $tag creado. Publicar con:"
Write-Host "  git push origin app-local"
Write-Host "  git push origin $tag"
Write-Host ""
Write-Host "El push del tag dispara CI, construye Ventory.exe y crea GitHub Release."
Write-Host "Los clientes con github.token verán la actualización si su FileVersion es menor."