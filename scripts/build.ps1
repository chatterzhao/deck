#!/usr/bin/env pwsh

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$Aot = $false
)

$ErrorActionPreference = "Stop"

if ($Aot) {
    Write-Host "🚀 开始跨平台构建 Deck v$Version (AOT优化)..." -ForegroundColor Green
} else {
    Write-Host "🚀 开始跨平台构建 Deck v$Version (开发模式)..." -ForegroundColor Green
}

# 切换到项目根目录
Set-Location (Split-Path $PSScriptRoot -Parent)

# 创建输出目录
$BuildDir = "build/release"
New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null

# 根据AOT和宿主系统选择平台
if ($Aot) {
    # AOT模式：只构建当前宿主系统支持的平台
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        Write-Host "🔥 AOT模式：仅构建 Windows 平台（当前宿主系统）" -ForegroundColor Yellow
        $PlatformNames = @("windows-x64", "windows-arm64")
        $RuntimeIds = @("win-x64", "win-arm64")
    } elseif ($IsLinux) {
        Write-Host "🔥 AOT模式：仅构建 Linux 平台（当前宿主系统）" -ForegroundColor Yellow  
        $PlatformNames = @("linux-x64", "linux-arm64")
        $RuntimeIds = @("linux-x64", "linux-arm64")
    } elseif ($IsMacOS) {
        Write-Host "🔥 AOT模式：仅构建 macOS 平台（当前宿主系统）" -ForegroundColor Yellow
        $PlatformNames = @("macos-x64", "macos-arm64") 
        $RuntimeIds = @("osx-x64", "osx-arm64")
    } else {
        Write-Error "❌ 不支持的宿主系统进行AOT编译"
        exit 1
    }
} else {
    # 标准模式：构建所有平台
    $PlatformNames = @("windows-x64", "windows-arm64", "linux-x64", "linux-arm64", "macos-x64", "macos-arm64")
    $RuntimeIds = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
}

$ProjectPath = "src/Deck.Console/Deck.Console.csproj"

# 恢复依赖
Write-Host "📦 恢复 NuGet 包..." -ForegroundColor Blue
dotnet restore $ProjectPath

# 构建所有平台
for ($i = 0; $i -lt $PlatformNames.Length; $i++) {
    $PlatformName = $PlatformNames[$i]
    $RuntimeId = $RuntimeIds[$i]
    Write-Host "🔨 构建 $PlatformName ($RuntimeId)..." -ForegroundColor Blue
    
    $PlatformOutputDir = "$BuildDir/$PlatformName"
    New-Item -ItemType Directory -Path $PlatformOutputDir -Force | Out-Null
    
    # 根据配置选择构建模式
    if ($Aot) {
        Write-Host "🔥 使用AOT编译: $PlatformName" -ForegroundColor Yellow
        dotnet publish $ProjectPath `
            --configuration $Configuration `
            --runtime $RuntimeId `
            --self-contained true `
            --output $PlatformOutputDir `
            -p:Version=$Version `
            -p:PublishAot=true `
            -p:PublishSingleFile=true `
            -p:PublishTrimmed=true `
            -p:InvariantGlobalization=true
            
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ $PlatformName AOT构建失败"
            exit 1
        }
    } else {
        Write-Host "⚡ 使用标准编译: $PlatformName" -ForegroundColor Blue
        dotnet publish $ProjectPath `
            --configuration $Configuration `
            --runtime $RuntimeId `
            --self-contained true `
            --output $PlatformOutputDir `
            -p:Version=$Version `
            -p:PublishSingleFile=true
            
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ $PlatformName 构建失败"
            exit 1
        }
    }
    
    # 确定可执行文件名
    if ($RuntimeId -like "win-*") {
        $ExeName = "Deck.Console.exe"
    } else {
        $ExeName = "Deck.Console"
    }
    
    $ExePath = "$PlatformOutputDir/$ExeName"
    
    # 验证构建结果
    if (Test-Path $ExePath) {
        $FileSize = [math]::Round((Get-Item $ExePath).Length / 1MB, 2)
        if ($Aot) {
            Write-Host "✅ $PlatformName AOT构建成功 (大小: $FileSize MB)" -ForegroundColor Green
        } else {
            Write-Host "✅ $PlatformName 构建成功 (大小: $FileSize MB)" -ForegroundColor Green
        }
        
        # 设置执行权限（非Windows平台在Unix系统上需要，这里只是标记）
        if ($RuntimeId -notlike "win-*") {
            # PowerShell 在 Windows 上运行时无法设置 Unix 权限，这个在目标系统上处理
            Write-Host "  💡 注意: $ExeName 在目标系统上可能需要执行权限" -ForegroundColor Gray
        }
    } else {
        Write-Error "❌ $PlatformName 构建失败: $ExePath 不存在"
        exit 1
    }
}

if ($Aot) {
    Write-Host "🎉 跨平台AOT构建完成!" -ForegroundColor Green
} else {
    Write-Host "🎉 跨平台构建完成!" -ForegroundColor Green
}
Write-Host "📁 构建目录: $BuildDir" -ForegroundColor Cyan
Write-Host ""

Write-Host "📊 构建统计:" -ForegroundColor Cyan
for ($i = 0; $i -lt $PlatformNames.Length; $i++) {
    $PlatformName = $PlatformNames[$i]
    $RuntimeId = $RuntimeIds[$i]
    
    if ($RuntimeId -like "win-*") {
        $ExeName = "Deck.Console.exe"
    } else {
        $ExeName = "Deck.Console"
    }
    
    $ExePath = "$BuildDir/$PlatformName/$ExeName"
    if (Test-Path $ExePath) {
        $FileSize = [math]::Round((Get-Item $ExePath).Length / 1MB, 2)
        Write-Host "  📄 $PlatformName`: $FileSize MB" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "💡 提示:" -ForegroundColor Cyan
if ($Aot) {
    Write-Host "  🔥 AOT优化构建已完成，文件位于: $BuildDir/" -ForegroundColor Gray
} else {
    Write-Host "  ⚡ 开发构建已完成，文件位于: $BuildDir/" -ForegroundColor Gray
    Write-Host "  🔥 如需AOT优化构建，请使用: .\scripts\build.ps1 -Aot" -ForegroundColor Gray
}
Write-Host "  📦 创建生产分发包请使用:" -ForegroundColor Gray
Write-Host "    - Windows:     .\scripts\package.ps1 -Version $Version" -ForegroundColor Gray
Write-Host "    - macOS/Linux: ./scripts/package.sh Release $Version" -ForegroundColor Gray