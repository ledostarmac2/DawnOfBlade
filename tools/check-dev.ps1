<#
.SYNOPSIS
  Runs the repeatable local validation suite for Dawn of Blade.

.DESCRIPTION
  Uses only the repo-local toolchain under .tools/: the pinned .NET SDK and Godot 4.2.2 .NET.
  This covers pure C# xUnit tests and the in-engine Godot headless test scene.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $root '.tools\dotnet'
$dotnet = Join-Path $dotnetRoot 'dotnet.exe'
$godot = Join-Path $root '.tools\godot\Godot_v4.2.2-stable_mono_win64\Godot_v4.2.2-stable_mono_win64_console.exe'

if (-not (Test-Path $dotnet) -or -not (Test-Path $godot)) {
    Write-Error "Local toolchain is missing. Run: powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1"
}

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:PATH = "$dotnetRoot;$env:PATH"

function Invoke-Step([string]$Name, [scriptblock]$Action) {
    Write-Host ""
    Write-Host "== $Name ==" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

Invoke-Step 'Restore' {
    & $dotnet restore (Join-Path $root 'DawnOfBlade.sln') --nologo
}

Invoke-Step 'xUnit suite' {
    & $dotnet test (Join-Path $root 'DawnOfBlade.sln') --nologo
}

Invoke-Step 'Godot engine tests' {
    powershell -ExecutionPolicy Bypass -File (Join-Path $root 'tools\run-godot-tests.ps1')
}

Write-Host ""
Write-Host "All local validation checks passed." -ForegroundColor Green
