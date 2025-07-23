#!/usr/bin/env pwsh

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$Aot = $false
)

$ErrorActionPreference = "Stop"

if ($Aot) {
    Write-Host "Building Deck v$Version with AOT optimization..." -ForegroundColor Green
} else {
    Write-Host "Building Deck v$Version (Development mode)..." -ForegroundColor Green
}

# Change to project root
Set-Location (Split-Path $PSScriptRoot -Parent)

# Create output directory
$BuildDir = "build/release"
New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null

# Select platforms based on AOT and host system
if ($Aot) {
    # AOT mode: only build for current host system
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        Write-Host "AOT mode: Building for Windows platforms only" -ForegroundColor Yellow
        $PlatformNames = @("windows-x64", "windows-arm64")
        $RuntimeIds = @("win-x64", "win-arm64")
    } elseif ($IsLinux) {
        Write-Host "AOT mode: Building for Linux platforms only" -ForegroundColor Yellow  
        $PlatformNames = @("linux-x64", "linux-arm64")
        $RuntimeIds = @("linux-x64", "linux-arm64")
    } elseif ($IsMacOS) {
        Write-Host "AOT mode: Building for macOS platforms only" -ForegroundColor Yellow
        $PlatformNames = @("macos-x64", "macos-arm64") 
        $RuntimeIds = @("osx-x64", "osx-arm64")
    } else {
        Write-Error "Unsupported host system for AOT compilation"
        exit 1
    }
} else {
    # Standard mode: build all platforms
    $PlatformNames = @("windows-x64", "windows-arm64", "linux-x64", "linux-arm64", "macos-x64", "macos-arm64")
    $RuntimeIds = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
}

$ProjectPath = "src/Deck.Console/Deck.Console.csproj"

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Blue
dotnet restore $ProjectPath

# Build all platforms
for ($i = 0; $i -lt $PlatformNames.Length; $i++) {
    $PlatformName = $PlatformNames[$i]
    $RuntimeId = $RuntimeIds[$i]
    Write-Host "Building $PlatformName ($RuntimeId)..." -ForegroundColor Blue
    
    $PlatformOutputDir = "$BuildDir/$PlatformName"
    New-Item -ItemType Directory -Path $PlatformOutputDir -Force | Out-Null
    
    # Choose build mode based on configuration
    if ($Aot) {
        Write-Host "Using AOT compilation: $PlatformName" -ForegroundColor Yellow
        dotnet publish $ProjectPath `
            --configuration $Configuration `
            --runtime $RuntimeId `
            --self-contained true `
            --output $PlatformOutputDir `
            -p:PublishAot=true `
            -p:Version=$Version `
            -p:AssemblyVersion=$Version.0 `
            -p:FileVersion=$Version.0

        if ($LASTEXITCODE -ne 0) {
            Write-Error "AOT build failed for $PlatformName. Error: Cannot find advapi32.lib - Windows SDK components missing."
            Write-Host "To fix this issue:" -ForegroundColor Yellow
            Write-Host "1. Install Visual Studio with 'C++ build tools' workload" -ForegroundColor Yellow
            Write-Host "2. Install Windows SDK (latest version)" -ForegroundColor Yellow
            Write-Host "3. Or run without AOT: .\scripts\package.ps1 -NoAot" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "Using standard compilation: $PlatformName" -ForegroundColor Blue
        dotnet publish $ProjectPath `
            --configuration $Configuration `
            --runtime $RuntimeId `
            --self-contained true `
            --output $PlatformOutputDir `
            -p:Version=$Version `
            -p:AssemblyVersion=$Version.0 `
            -p:FileVersion=$Version.0

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Standard build failed for $PlatformName"
            exit 1
        }
    }
    
    # Check output
    $MainExe = if ($RuntimeId.StartsWith("win-")) { "Deck.Console.exe" } else { "Deck.Console" }
    if (Test-Path "$PlatformOutputDir/$MainExe") {
        $Size = [math]::Round((Get-Item "$PlatformOutputDir/$MainExe").Length / 1MB, 2)
        Write-Host "Built: $MainExe ($Size MB)" -ForegroundColor Green
    } else {
        Write-Error "Build output not found: $PlatformOutputDir/$MainExe"
        exit 1
    }
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $BuildDir/" -ForegroundColor Gray

Write-Host ""
Write-Host "Tips:" -ForegroundColor Cyan
if ($Aot) {
    Write-Host "  AOT optimized build completed, files are in: $BuildDir/" -ForegroundColor Gray
} else {
    Write-Host "  Development build completed, files are in: $BuildDir/" -ForegroundColor Gray
    Write-Host "  For AOT optimized build, use: .\scripts\build.ps1 -Aot" -ForegroundColor Gray
}
Write-Host "  To create production packages, use:" -ForegroundColor Gray
Write-Host "    - Windows:     .\scripts\package.ps1 -Version $Version" -ForegroundColor Gray
Write-Host "    - macOS/Linux: ./scripts/package.sh Release $Version" -ForegroundColor Gray