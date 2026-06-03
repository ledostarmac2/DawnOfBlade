<#
.SYNOPSIS
  Launches Dawn of Blade with the repo-local Godot/.NET toolchain.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $root '.tools\dotnet'
$godot = $env:GODOT_BIN
if (-not $godot) {
    $godot = Join-Path $root '.tools\godot\Godot_v4.2.2-stable_mono_win64\Godot_v4.2.2-stable_mono_win64.exe'
}

if (-not (Test-Path (Join-Path $dotnetRoot 'dotnet.exe')) -or -not (Test-Path $godot)) {
    Write-Error "Local toolchain is missing. Run: powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1"
}

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:PATH = "$dotnetRoot;$env:PATH"

$process = Start-Process -FilePath $godot -ArgumentList @('--path', $root) -WorkingDirectory $root -PassThru
Write-Host ("Dawn of Blade launched with PID {0}." -f $process.Id) -ForegroundColor Green
