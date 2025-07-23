#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 开始构建 Deck Windows 分发包..." -ForegroundColor Green

# 切换到项目根目录
Set-Location (Split-Path $PSScriptRoot -Parent)

# 设置变量
$DistDir = "dist/windows"
$BuildDir = "build/release"
$Platforms = @("windows-x64", "windows-arm64")
$RuntimeIds = @("win-x64", "win-arm64")

# 清理输出目录
if ($Clean -or (Test-Path $DistDir)) {
    Write-Host "🧹 清理分发目录..." -ForegroundColor Yellow
    Remove-Item -Path $DistDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 创建分发目录
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# 检查是否已有构建文件
$NeedBuild = $false
foreach ($Platform in $Platforms) {
    $PlatformBuildDir = "$BuildDir/$Platform"
    if (-not (Test-Path "$PlatformBuildDir/Deck.Console.exe")) {
        $NeedBuild = $true
        break
    }
}

if ($NeedBuild) {
    Write-Host "⚠️  未找到构建文件，先运行构建..." -ForegroundColor Yellow
    & "$PSScriptRoot/build.ps1" -Version $Version -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ 构建失败"
        exit 1
    }
}

# 复制构建文件到分发目录
Write-Host "📦 从构建文件创建分发包..." -ForegroundColor Blue

foreach ($Platform in $Platforms) {
    $PlatformBuildDir = "$BuildDir/$Platform"
    $PlatformDistDir = "$DistDir/$Platform"
    
    if (-not (Test-Path $PlatformBuildDir)) {
        Write-Warning "❌ 未找到平台构建: $PlatformBuildDir"
        continue
    }
    
    # 创建平台分发目录并复制文件
    New-Item -ItemType Directory -Path $PlatformDistDir -Force | Out-Null
    Copy-Item -Path "$PlatformBuildDir/*" -Destination $PlatformDistDir -Recurse -Force
    
    Write-Host "✅ $Platform 文件已复制到分发目录" -ForegroundColor Green
}

# 创建MSI安装包
Write-Host "📦 创建 MSI 安装包..." -ForegroundColor Blue

# 检查WiX是否安装
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host "📥 安装 WiX Toolset..." -ForegroundColor Yellow
    try {
        dotnet tool install --global wix
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "⚠️  WiX 安装失败，跳过MSI包创建"
            Write-Host "手动安装命令: dotnet tool install --global wix" -ForegroundColor Gray
        }
    }
    catch {
        Write-Warning "⚠️  WiX 安装失败，跳过MSI包创建"
        Write-Host "手动安装命令: dotnet tool install --global wix" -ForegroundColor Gray
    }
}

for ($i = 0; $i -lt $Platforms.Length; $i++) {
    $Platform = $Platforms[$i]
    $RuntimeId = $RuntimeIds[$i]
    $PlatformDistDir = "$DistDir/$Platform"
    $MsiPath = "$DistDir/deck-v$Version-$RuntimeId.msi"
    
    Write-Host "🔨 创建 $Platform MSI 包..." -ForegroundColor Blue
    
    # 检查WiX配置文件是否存在
    if (-not (Test-Path "packaging/windows/deck.wxs")) {
        Write-Warning "⚠️  WiX配置文件不存在: packaging/windows/deck.wxs"
        Write-Host "跳过 $Platform MSI 包创建" -ForegroundColor Gray
        continue
    }
    
    # 检查WiX命令是否可用
    if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
        Write-Warning "⚠️  WiX 工具未安装，跳过 $Platform MSI 包创建"
        continue
    }
    
    try {
        # 使用 WiX 创建 MSI 包
        wix build packaging/windows/deck.wxs `
            -d "Version=$Version" `
            -d "Platform=$RuntimeId" `
            -d "SourceDir=$PlatformDistDir" `
            -out $MsiPath
        
        if (Test-Path $MsiPath) {
            $MsiSize = [math]::Round((Get-Item $MsiPath).Length / 1MB, 2)
            Write-Host "📦 创建MSI包: $MsiPath ($MsiSize MB)" -ForegroundColor Green
        } else {
            Write-Warning "⚠️  $Platform MSI 创建失败 - 检查WiX配置"
        }
    }
    catch {
        Write-Warning "⚠️  $Platform MSI 创建失败: $($_.Exception.Message)"
    }
}

Write-Host "🎉 Windows 分发包构建完成!" -ForegroundColor Green
Write-Host "📁 分发目录: $DistDir" -ForegroundColor Cyan
Write-Host ""

Write-Host "📦 创建的分发包:" -ForegroundColor Cyan
Get-ChildItem $DistDir -Recurse | Where-Object { 
    -not $_.PSIsContainer -and ($_.Extension -eq ".msi" -or $_.Extension -eq ".exe") 
} | ForEach-Object {
    $Size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  📄 $($_.Name) ($Size MB)" -ForegroundColor Gray
}