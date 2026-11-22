$ErrorActionPreference = "Stop"
$install = Join-Path $env:LOCALAPPDATA "Ventory\Ventory.exe"
$repoExe = Join-Path $PSScriptRoot "Ventory\Ventory.exe"
$portable = Join-Path $PSScriptRoot "Ventory.exe"

if (-not (Test-Path $repoExe) -or -not (Test-Path $portable)) {
  & (Join-Path $PSScriptRoot "build-desktop.ps1")
}

if (Test-Path $install) {
  & $install @args
} elseif (Test-Path $repoExe) {
  & $repoExe @args
} else {
  & $portable @args
}