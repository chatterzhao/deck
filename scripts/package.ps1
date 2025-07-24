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
    
    # Create MSI package using WiX
    $MsiPath = "$DistDir/Deck-v$Version-$RuntimeId.msi"
    $WixSource = "scripts/packaging/windows/deck.wxs"
    
    if (Test-Path $WixSource) {
        Write-Host "Creating MSI package using WiX..." -ForegroundColor Blue
        
        # Convert paths to absolute paths for WiX
        $AbsWixSource = Resolve-Path $WixSource
        $AbsMsiPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($MsiPath)
        $AbsSourceDir = Resolve-Path $PlatformBuildDir
        
        Write-Host "WiX Source: $AbsWixSource" -ForegroundColor Gray
        Write-Host "Output MSI: $AbsMsiPath" -ForegroundColor Gray
        Write-Host "Source Dir: $AbsSourceDir" -ForegroundColor Gray
        
        # Check if WiX is available
        if (Get-Command wix -ErrorAction SilentlyContinue) {
            Write-Host "WiX toolset found, proceeding with MSI creation..." -ForegroundColor Green
            
            # Verify source directory contains executable
            $ExePath = Join-Path $AbsSourceDir "Deck.Console.exe"
            if (-not (Test-Path $ExePath)) {
                Write-Host "Error: Deck.Console.exe not found in source directory: $AbsSourceDir" -ForegroundColor Red
                Write-Host "Contents of source directory:" -ForegroundColor Yellow
                Get-ChildItem $AbsSourceDir | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }
                throw "Missing executable for MSI packaging"
            }
            
            Write-Host "Building MSI with WiX v4..." -ForegroundColor Blue
            Write-Host "Command: wix build `"$AbsWixSource`" -d `"Version=$Version`" -d `"SourceDir=$AbsSourceDir`" -o `"$AbsMsiPath`"" -ForegroundColor Gray
            
            # Build MSI using WiX v4 syntax with detailed output
            $WixOutput = wix build "$AbsWixSource" `
                -d "Version=$Version" `
                -d "SourceDir=$AbsSourceDir" `
                -o "$AbsMsiPath" 2>&1
                
            Write-Host "WiX build output:" -ForegroundColor Gray
            $WixOutput | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
                
            if ($LASTEXITCODE -eq 0 -and (Test-Path $MsiPath)) {
                $MsiSize = [math]::Round((Get-Item $MsiPath).Length / 1MB, 2)
                Write-Host "✅ Created MSI package: $MsiPath ($MsiSize MB)" -ForegroundColor Green
            } else {
                Write-Host "❌ MSI creation failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
                Write-Host "WiX build failed. This is required for Windows installation packages." -ForegroundColor Red
                throw "MSI package creation failed. Cannot proceed without proper Windows installer."
            }
        } else {
            Write-Host "❌ WiX command not found in PATH" -ForegroundColor Red
            Write-Host "Available commands:" -ForegroundColor Yellow
            Get-Command *wix* -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.Name) - $($_.Source)" -ForegroundColor Gray }
            Write-Host "WiX Toolset is required for creating MSI packages." -ForegroundColor Red
            throw "WiX Toolset not found. Please install WiX Toolset v4 to create MSI packages."
        }
    } else {
        Write-Host "❌ WiX source file not found at $WixSource" -ForegroundColor Red
        Write-Host "MSI packaging requires the WiX source file for Windows installers." -ForegroundColor Red
        throw "WiX source file missing: $WixSource. Cannot create MSI package."
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
    -not $_.PSIsContainer -and ($_.Extension -eq ".msi" -or $_.Extension -eq ".zip" -or $_.Name -eq "Deck.bat") 
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