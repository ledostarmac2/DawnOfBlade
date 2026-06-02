$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $repo ".tools\dotnet"
$godot = Join-Path $repo ".tools\godot\Godot_v4.2.2-stable_mono_win64\Godot_v4.2.2-stable_mono_win64_console.exe"
$rcedit = Join-Path $repo ".tools\rcedit\rcedit-x64.exe"
$output = Join-Path $repo "exports\windows\DawnOfBlade.exe"
$icon = Join-Path $repo "assets\branding\dawn_of_blade_icon_transparent.ico"

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:PATH = "$dotnetRoot;$env:PATH"

& (Join-Path $PSScriptRoot "build-transparent-icon.ps1")
& $godot --headless --path $repo --export-release "Windows Desktop" $output
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $rcedit $output `
    --set-icon $icon `
    --set-file-version "1.0.0.0" `
    --set-product-version "1.0.0.0" `
    --set-version-string ProductName "Dawn of Blade" `
    --set-version-string FileDescription "Dawn of Blade"
exit $LASTEXITCODE
