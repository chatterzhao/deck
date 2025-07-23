#!/usr/bin/env pwsh

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 开始跨平台构建 Deck v$Version..." -ForegroundColor Green

# 切换到项目根目录
Set-Location (Split-Path $PSScriptRoot -Parent)

# 创建输出目录
$BuildDir = "build/release"
New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null

# 支持的平台
$PlatformNames = @("windows-x64", "windows-arm64", "linux-x64", "linux-arm64", "macos-x64", "macos-arm64")
$RuntimeIds = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")

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
    
    # AOT 发布 (如果失败则使用标准发布)
    $AotSuccess = $true
    try {
        dotnet publish $ProjectPath `
            --configuration $Configuration `
            --runtime $RuntimeId `
            --self-contained true `
            --output $PlatformOutputDir `
            -p:Version=$Version `
            -p:PublishAot=true `
            -p:PublishSingleFile=true `
            -p:PublishTrimmed=true `
            -p:InvariantGlobalization=true 2>$null
            
        if ($LASTEXITCODE -ne 0) {
            $AotSuccess = $false
        }
    }
    catch {
        $AotSuccess = $false
    }
    
    if (-not $AotSuccess) {
        Write-Host "⚠️  AOT编译失败，使用标准发布: $PlatformName" -ForegroundColor Yellow
        dotnet publish $ProjectPath `
            --configuration $Configuration `
            --runtime $RuntimeId `
            --self-contained true `
            --output $PlatformOutputDir `
            -p:Version=$Version
            
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
        Write-Host "✅ $PlatformName 构建成功 (大小: $FileSize MB)" -ForegroundColor Green
        
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

Write-Host "🎉 跨平台构建完成!" -ForegroundColor Green
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
Write-Host "  🔨 开发构建已完成，文件位于: $BuildDir/" -ForegroundColor Gray
Write-Host "  📦 创建分发包请使用:" -ForegroundColor Gray
Write-Host "    - Windows:     .\scripts\package.ps1 -Version $Version" -ForegroundColor Gray
Write-Host "    - macOS/Linux: ./scripts/package.sh Release $Version" -ForegroundColor Gray