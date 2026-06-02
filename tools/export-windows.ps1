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

& $godot --headless --path $repo --export-release "Windows Desktop" $output

# IMPORTANT: do NOT post-process this .exe with rcedit. The preset uses
# binary_format/embed_pck=true, so the game data (.pck) is appended to the end of the
# executable. rcedit rewrites the PE resource section and strips that appended data,
# producing the "Couldn't load project data at path '.' / .pck file missing" launcher
# error. To give the .exe a custom Windows icon, set it in the Godot export preset
# (Application > Icon, pointing at the .ico) so Godot bakes it in during export, or switch
# the preset to embed_pck=false and ship the sibling DawnOfBlade.pck.
# ($rcedit / $icon / build-transparent-icon are intentionally unused for that future path.)
exit $LASTEXITCODE
