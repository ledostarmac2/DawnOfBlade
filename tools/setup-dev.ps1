# Bootstraps the local dev toolchain for DawnOfBlade into .tools/ (gitignored):
#   - .NET 8 SDK        (builds the net8.0 Godot game + xUnit tests)
#   - .NET 9 runtime    (host for the C# / C# Dev Kit language server in VS Code / Cursor)
#   - ripgrep           (Todo Tree and other extensions)
# Then prepends the vendored SDK to the user PATH, sets DOTNET_ROOT, and restores packages.
#
# Run once per machine:  powershell -ExecutionPolicy Bypass -File tools/setup-dev.ps1
# The matching absolute path in .vscode/settings.json (dotnetAcquisitionExtension.existingDotnetPath)
# must point at <repo>/.tools/dotnet/dotnet.exe for the C# Dev Kit to find the SDK.

param(
    [switch]$SkipPath
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$installDir = Join-Path $root ".tools\dotnet"
$ripgrepDir = Join-Path $root ".tools\ripgrep"
$ripgrepVersion = "14.1.1"

New-Item -ItemType Directory -Force -Path (Split-Path $installDir) | Out-Null
New-Item -ItemType Directory -Force -Path $ripgrepDir | Out-Null

$installScript = Join-Path $env:TEMP "dotnet-install.ps1"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing

Write-Host "Installing .NET 8 SDK to $installDir ..."
& $installScript -Channel 8.0 -InstallDir $installDir -Architecture x64

Write-Host "Installing .NET 9 runtime (host for the C# Dev Kit language server) ..."
& $installScript -Runtime dotnet -Channel 9.0 -InstallDir $installDir -Architecture x64 -SkipNonVersionedFiles

$dotnet = Join-Path $installDir "dotnet.exe"
if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe was not installed to $installDir"
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

Write-Host ""
Write-Host "Restoring NuGet packages ..."
& $dotnet restore (Join-Path $root "DawnOfBlade.sln")
$testProject = Join-Path $root "tests\DawnOfBlade.Tests.csproj"
if (Test-Path $testProject) {
    & $dotnet restore $testProject
}
