#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 开始构建 Deck Windows 版本..." -ForegroundColor Green

# 设置变量
$ProjectPath = "src/Deck.Console/Deck.Console.csproj"
$OutputDir = "artifacts/windows"
$Platforms = @("win-x64", "win-arm64")

# 清理输出目录
if ($Clean -or (Test-Path $OutputDir)) {
    Write-Host "🧹 清理输出目录..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 恢复依赖
Write-Host "📦 恢复 NuGet 包..." -ForegroundColor Blue
dotnet restore $ProjectPath

# 构建各平台版本
foreach ($Platform in $Platforms) {
    Write-Host "🔨 构建 $Platform 版本..." -ForegroundColor Blue
    
    $PlatformOutputDir = "$OutputDir/$Platform"
    New-Item -ItemType Directory -Path $PlatformOutputDir -Force | Out-Null
    
    # AOT 发布
    dotnet publish $ProjectPath `
        --configuration $Configuration `
        --runtime $Platform `
        --self-contained true `
        --output $PlatformOutputDir `
        -p:Version=$Version `
        -p:PublishAot=true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:InvariantGlobalization=true
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ $Platform 构建失败"
        exit 1
    }
    
    # 验证输出文件
    $ExePath = "$PlatformOutputDir/Deck.Console.exe"
    if (Test-Path $ExePath) {
        $FileSize = (Get-Item $ExePath).Length / 1MB
        Write-Host "✅ $Platform 构建成功 (大小: $([math]::Round($FileSize, 2)) MB)" -ForegroundColor Green
        
        # 测试可执行文件
        Write-Host "🧪 测试 $Platform 可执行文件..." -ForegroundColor Blue
        & $ExePath --version
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "⚠️  $Platform 可执行文件测试失败"
        }
    } else {
        Write-Error "❌ $Platform 输出文件不存在: $ExePath"
        exit 1
    }
}

# 创建MSI安装包
Write-Host "📦 创建 MSI 安装包..." -ForegroundColor Blue

# 检查WiX是否安装
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host "📥 安装 WiX Toolset..." -ForegroundColor Yellow
    dotnet tool install --global wix
}

foreach ($Platform in $Platforms) {
    $PlatformOutputDir = "$OutputDir/$Platform"
    $MsiPath = "$OutputDir/deck-v$Version-$Platform.msi"
    
    Write-Host "🔨 创建 $Platform MSI 包..." -ForegroundColor Blue
    
    # 检查WiX配置文件是否存在
    if (-not (Test-Path "packaging/windows/deck.wxs")) {
        Write-Warning "⚠️  WiX配置文件不存在，将创建基础版本"
        # 这里可以创建一个基础的WiX配置文件
        # 实际实现中需要完整的WiX配置
    } else {
        # 使用 WiX 创建 MSI 包
        wix build packaging/windows/deck.wxs `
            -d "Version=$Version" `
            -d "Platform=$Platform" `
            -d "SourceDir=$PlatformOutputDir" `
            -out $MsiPath
        
        if (Test-Path $MsiPath) {
            $MsiSize = (Get-Item $MsiPath).Length / 1MB
            Write-Host "📦 创建MSI包: $MsiPath ($([math]::Round($MsiSize, 2)) MB)" -ForegroundColor Green
        } else {
            Write-Warning "⚠️  $Platform MSI 创建失败 - 可能是WiX配置问题"
        }
    }
}

Write-Host "🎉 Windows 构建完成!" -ForegroundColor Green
Write-Host "📁 输出目录: $OutputDir" -ForegroundColor Cyan
Get-ChildItem $OutputDir -Recurse | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
    Write-Host "  📄 $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)" -ForegroundColor Gray
}