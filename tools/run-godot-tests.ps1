<#
.SYNOPSIS
  Runs the in-engine (headless Godot) test scene and returns its exit code.

.DESCRIPTION
  The pure-C# logic is covered by `dotnet test`. This script covers behavior that only exists once
  the Godot runtime is booted: real Node instantiation, _Ready, and the brain -> Node3D transform
  bridge. It builds the C# assembly, then launches Godot headless on test/HeadlessTests.tscn, which
  prints PASS/FAIL lines and quits with an exit code equal to the failure count.

  Set $env:GODOT_BIN to use a specific Godot binary; otherwise the project-local download under
  tools/godot is used.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $root '.tools\dotnet'
$dotnet = Join-Path $dotnetRoot 'dotnet.exe'

$godot = $env:GODOT_BIN
if (-not $godot) {
    $godot = Join-Path $root '.tools\godot\Godot_v4.2.2-stable_mono_win64\Godot_v4.2.2-stable_mono_win64_console.exe'
}
if (-not (Test-Path $dotnet) -or -not (Test-Path $godot)) {
    Write-Error "Local toolchain is missing. Run: powershell -ExecutionPolicy Bypass -File tools\setup-dev.ps1"
}

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:PATH = "$dotnetRoot;$env:PATH"

Write-Host "Building C# assembly..." -ForegroundColor Cyan
& $dotnet build (Join-Path $root 'DawnOfBlade.csproj') --nologo | Out-Host
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed." }

Write-Host "Running headless Godot tests..." -ForegroundColor Cyan
& $godot --headless --path $root "res://test/HeadlessTests.tscn"
$code = $LASTEXITCODE
Write-Host ("Godot test runner exit code: {0}" -f $code) -ForegroundColor Cyan
exit $code
