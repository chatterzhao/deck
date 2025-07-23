#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$NoAot = $false
)

$ErrorActionPreference = "Stop"

if ($NoAot) {
    Write-Host "Building Deck Windows distribution packages (Fast build)..." -ForegroundColor Green
} else {
    Write-Host "Building Deck Windows distribution packages (AOT optimized)..." -ForegroundColor Green
}

# Change to project root
Set-Location (Split-Path $PSScriptRoot -Parent)

# Set variables
$DistDir = "dist/windows"
$BuildDir = "build/release"
$Platforms = @("windows-x64", "windows-arm64")
$RuntimeIds = @("win-x64", "win-arm64")

# Clean and create distribution directory
Write-Host "Cleaning distribution directory..." -ForegroundColor Yellow
Remove-Item -Path $DistDir -Recurse -Force -ErrorAction SilentlyContinue

# Create distribution directory
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Rebuild to ensure correct compilation mode
Write-Host "Rebuilding to ensure correct compilation mode..." -ForegroundColor Blue
if ($NoAot) {
    Write-Host "Using standard compilation for build..." -ForegroundColor Blue
    & "$PSScriptRoot/build.ps1" -Version $Version -Configuration $Configuration
} else {
    Write-Host "Using AOT compilation for build..." -ForegroundColor Yellow
    & "$PSScriptRoot/build.ps1" -Version $Version -Configuration $Configuration -Aot
}
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Create Windows installer packages
Write-Host "Creating distribution packages from build files..." -ForegroundColor Blue

foreach ($Platform in $Platforms) {
    $RuntimeId = $RuntimeIds[$Platforms.IndexOf($Platform)]
    $PlatformBuildDir = "$BuildDir/$Platform"
    
    if (-not (Test-Path $PlatformBuildDir)) {
        Write-Host "Platform build not found: $PlatformBuildDir" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "Creating $Platform installer..." -ForegroundColor Blue
    
    # Create installer directory structure
    $InstallerDir = "$DistDir/Deck-Installer-$Platform"
    New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null
    
    # Copy main executable
    $MainExe = if ($Platform -eq "windows-x64" -or $Platform -eq "windows-arm64") { "Deck.Console.exe" } else { "Deck.Console" }
    Copy-Item -Path "$PlatformBuildDir/$MainExe" -Destination "$InstallerDir/deck-binary.exe" -Force
    
    # Create installer batch script
    $InstallScript = @"
@echo off
setlocal enabledelayedexpansion

echo.
echo Welcome to Deck Development Tool!
echo =================================
echo.
echo Performing initial setup...
echo.

set "CURRENT_DIR=%~dp0"
set "DECK_BINARY=%CURRENT_DIR%deck-binary.exe"
set "INSTALL_DIR=%USERPROFILE%\.local\bin"
set "INSTALLED_BINARY=%INSTALL_DIR%\deck.exe"
set "CONFIG_FILE=%USERPROFILE%\.local\share\deck\.deck-configured"

REM Check if first run
if not exist "%CONFIG_FILE%" (
    REM Create install directory
    if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
    if not exist "%USERPROFILE%\.local\share\deck" mkdir "%USERPROFILE%\.local\share\deck"
    
    REM Copy program to user directory
    copy "%DECK_BINARY%" "%INSTALLED_BINARY%" >nul
    
    echo Setting up environment variables...
    
    REM Add to PATH using registry
    for /f "tokens=2*" %%a in ('reg query "HKCU\Environment" /v PATH 2^>nul') do set "USER_PATH=%%b"
    if not defined USER_PATH set "USER_PATH="
    
    REM Check if already in PATH
    echo !USER_PATH! | findstr /i "%INSTALL_DIR%" >nul
    if errorlevel 1 (
        if defined USER_PATH (
            set "NEW_PATH=!USER_PATH!;%INSTALL_DIR%"
        ) else (
            set "NEW_PATH=%INSTALL_DIR%"
        )
        reg add "HKCU\Environment" /v PATH /t REG_EXPAND_SZ /d "!NEW_PATH!" /f >nul
        echo Environment variable configured successfully!
    ) else (
        echo Environment variable already exists!
    )
    
    echo.
    echo Installation completed!
    echo.
    echo You can now use:
    echo • In VS Code terminal: deck --help
    echo • In any terminal: deck start python
    echo • Double-click this installer to run directly
    echo.
    echo Note: You may need to restart your terminal for PATH changes to take effect
    echo.
    
    REM Mark as configured
    echo. > "%CONFIG_FILE%"
    
    pause
    exit /b 0
)

REM Subsequent runs: execute deck functionality directly
"%INSTALLED_BINARY%" %*
"@
    
    Set-Content -Path "$InstallerDir/install.bat" -Value $InstallScript -Encoding UTF8
    
    # Create ZIP package
    $ZipPath = "$DistDir/Deck-v$Version-$RuntimeId.zip"
    
    if (Get-Command Compress-Archive -ErrorAction SilentlyContinue) {
        Compress-Archive -Path "$InstallerDir/*" -DestinationPath $ZipPath -Force
        Write-Host "Created ZIP package: $ZipPath" -ForegroundColor Green
    } else {
        Write-Host "Warning: Compress-Archive not available, skipping ZIP creation" -ForegroundColor Yellow
    }
}

# Clean up temporary directories
Remove-Item -Path "$DistDir/Deck-Installer-*" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
if ($NoAot) {
    Write-Host "Fast distribution package build completed!" -ForegroundColor Green
} else {
    Write-Host "AOT optimized distribution package build completed!" -ForegroundColor Green
}
Write-Host "Distribution directory: $DistDir" -ForegroundColor Gray
Write-Host ""
Write-Host "Created packages:" -ForegroundColor Gray
Get-ChildItem $DistDir -Recurse | Where-Object { 
    -not $_.PSIsContainer -and ($_.Extension -eq ".zip" -or $_.Name -eq "Deck.bat") 
} | ForEach-Object {
    $Size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name) ($Size MB)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Tips:" -ForegroundColor Cyan
if ($NoAot) {
    Write-Host "  Fast build completed" -ForegroundColor Gray
    $productionCmd = ".\scripts\package.ps1 -Version " + $Version
    Write-Host "  For production AOT build, use: $productionCmd" -ForegroundColor Gray
} else {
    Write-Host "  AOT optimized build completed" -ForegroundColor Gray
    $fastCmd = ".\scripts\package.ps1 -NoAot -Version " + $Version
    Write-Host "  For fast build, use: $fastCmd" -ForegroundColor Gray
}