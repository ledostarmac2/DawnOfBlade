# Bootstraps the local dev toolchain for DawnOfBlade into .tools/ (gitignored):
#   - .NET 8 SDK        (builds the net8.0 Godot game + xUnit tests)
#   - Godot 4.2.2 .NET  (runs and exports the project)
#   - ripgrep           (Todo Tree and other extensions)
# Then prepends the vendored SDK to the user PATH, sets DOTNET_ROOT, and restores packages.
# Windows places machine PATH entries before user PATH entries in some shells, so the
# workspace tasks and editor settings call the vendored dotnet.exe explicitly.
#
# Run once per machine:  powershell -ExecutionPolicy Bypass -File tools/setup-dev.ps1
# VS Code's .NET Install Tool manages the separate runtime used by C# extensions.

param(
    [switch]$SkipPath
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$installDir = Join-Path $root ".tools\dotnet"
$godotRoot = Join-Path $root ".tools\godot"
$godotVersion = "4.2.2-stable"
$godotFolder = "Godot_v$godotVersion" + "_mono_win64"
$godotDir = Join-Path $godotRoot $godotFolder
$ripgrepDir = Join-Path $root ".tools\ripgrep"
$ripgrepVersion = "14.1.1"

New-Item -ItemType Directory -Force -Path (Split-Path $installDir) | Out-Null
New-Item -ItemType Directory -Force -Path $godotRoot | Out-Null
New-Item -ItemType Directory -Force -Path $ripgrepDir | Out-Null

$installScript = Join-Path $env:TEMP "dotnet-install.ps1"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing

Write-Host "Installing .NET 8 SDK to $installDir ..."
& $installScript -Channel 8.0 -InstallDir $installDir -Architecture x64

$dotnet = Join-Path $installDir "dotnet.exe"
if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe was not installed to $installDir"
}

$godotExe = Join-Path $godotDir "$godotFolder.exe"
$godotConsole = Join-Path $godotDir ($godotFolder + "_console.exe")
if (-not (Test-Path $godotExe) -or -not (Test-Path $godotConsole)) {
    Write-Host "Installing Godot $godotVersion .NET to $godotDir ..."
    $zip = Join-Path $env:TEMP "$godotFolder.zip"
    $url = "https://github.com/godotengine/godot-builds/releases/download/$godotVersion/$godotFolder.zip"
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    $extractDir = Join-Path $env:TEMP $godotFolder
    if (Test-Path $extractDir) {
        Remove-Item -Recurse -Force $extractDir
    }
    Expand-Archive -Path $zip -DestinationPath $extractDir
    if (Test-Path $godotDir) {
        Remove-Item -Recurse -Force $godotDir
    }
    Move-Item -Path (Join-Path $extractDir $godotFolder) -Destination $godotDir
    Remove-Item $zip -Force -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $extractDir -ErrorAction SilentlyContinue
}

$rg = Join-Path $ripgrepDir "rg.exe"
if (-not (Test-Path $rg)) {
    Write-Host "Installing ripgrep $ripgrepVersion to $ripgrepDir ..."
    $zip = Join-Path $env:TEMP "ripgrep-$ripgrepVersion.zip"
    $url = "https://github.com/BurntSushi/ripgrep/releases/download/$ripgrepVersion/ripgrep-$ripgrepVersion-x86_64-pc-windows-msvc.zip"
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    $extractDir = Join-Path $env:TEMP "ripgrep-$ripgrepVersion"
    if (Test-Path $extractDir) {
        Remove-Item -Recurse -Force $extractDir
    }
    Expand-Archive -Path $zip -DestinationPath $extractDir
    Copy-Item (Join-Path $extractDir "ripgrep-$ripgrepVersion-x86_64-pc-windows-msvc\rg.exe") $rg -Force
    Remove-Item $zip -Force -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $extractDir -ErrorAction SilentlyContinue
}

if (-not $SkipPath) {
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($userPath -notlike "*$installDir*") {
        [Environment]::SetEnvironmentVariable("Path", "$installDir;$userPath", "User")
        Write-Host "Prepended $installDir to user PATH."
    }
    [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $installDir, "User")
    Write-Host "Set DOTNET_ROOT to $installDir. Restart VS Code / Cursor / Godot to pick up the change."
}

Write-Host ""
Write-Host "Installed tools:"
Write-Host "  SDKs:"
& $dotnet --list-sdks | ForEach-Object { Write-Host "    $_" }
& $dotnet --list-runtimes | ForEach-Object { Write-Host "  Runtime: $_" }
Write-Host "  ripgrep: $rg"
Write-Host "  Godot: $godotExe"

Write-Host ""
Write-Host "Restoring NuGet packages ..."
& $dotnet restore (Join-Path $root "DawnOfBlade.sln")
$testProject = Join-Path $root "tests\DawnOfBlade.Tests.csproj"
if (Test-Path $testProject) {
    & $dotnet restore $testProject
}
