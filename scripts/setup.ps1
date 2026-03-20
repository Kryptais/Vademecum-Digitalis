<#
Simple setup script for the repository.
- pins SDK via global.json (already in repo)
- checks dotnet installation and version
- checks and installs missing MAUI workloads (android, maui-windows)
- optional: checks adb/emulator presence and suggests steps
Usage: run from repo root in an elevated PowerShell if workloads need to be installed.
#>

Write-Host "Running repository setup checks..."

function ExitWith($msg, $code=1) {
    Write-Error $msg
    exit $code
}

# dotnet check
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { ExitWith "dotnet CLI not found. Install .NET 9 SDK: https://aka.ms/dotnet/download" }

$info = dotnet --info 2>&1
Write-Host $info

# check SDK version in global.json
$globalJson = Join-Path (Get-Location) 'global.json'
if (Test-Path $globalJson) {
    $g = Get-Content $globalJson | Out-String | ConvertFrom-Json
    $required = $g.sdk.version
    $installed = (dotnet --info | Select-String 'Version:\s+([0-9.]+)' -AllMatches).Matches[0].Groups[1].Value
    if ($installed -ne $required) {
        Write-Warning "Pinned SDK version in global.json: $required; installed: $installed. Building should still work but consider installing $required for reproducibility."
    }
}

# required workloads
$requiredWorkloads = @('android','maui-windows')
$installed = dotnet workload list | Select-String 'Installierte Workload-ID' -Context 0,10 | Out-String
foreach ($w in $requiredWorkloads) {
    if (-not (dotnet workload list | Select-String "^$w\b")) {
        Write-Host "Workload '$w' not found. Installing..."
        dotnet workload install $w
        if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to install workload $w - you may need to run setup as administrator or install Visual Studio workloads." }
    } else {
        Write-Host "Workload '$w' already installed."
    }
}

# optional: check adb/emulator
if (-not (Get-Command adb -ErrorAction SilentlyContinue)) {
    Write-Warning "adb not found in PATH. Android emulator/device targets will not work until platform-tools are installed and added to PATH."
} else {
    Write-Host "adb found:" (Get-Command adb).Source
}

# final build check (windows target)
Write-Host "Running a quick build check (windows target)..."
Set-Location -Path src/VademecumDigitalis
dotnet build -f net9.0-windows10.0.19041.0
if ($LASTEXITCODE -ne 0) { ExitWith "Build failed. See output above." }

Write-Host "Setup complete. You can now open the project in VS Code and press F5 (select 'Run MAUI (Windows) - via Task')."
