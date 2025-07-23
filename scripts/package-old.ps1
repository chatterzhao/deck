#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$NoAot = $false
)

$ErrorActionPreference = "Stop"

if ($NoAot) {
    Write-Host "🚀 开始构建 Deck Windows 分发包 (快速构建)..." -ForegroundColor Green
} else {
    Write-Host "🚀 开始构建 Deck Windows 分发包 (AOT优化)..." -ForegroundColor Green
}

# 切换到项目根目录
Set-Location (Split-Path $PSScriptRoot -Parent)

# 设置变量
$DistDir = "dist/windows"
$BuildDir = "build/release"
$Platforms = @("windows-x64", "windows-arm64")
$RuntimeIds = @("win-x64", "win-arm64")

# 清理并创建分发目录（默认清理）
Write-Host "🧹 清理分发目录..." -ForegroundColor Yellow
Remove-Item -Path $DistDir -Recurse -Force -ErrorAction SilentlyContinue

# 创建分发目录
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# 重新构建以确保使用正确的编译模式
Write-Host "🔨 重新构建以确保编译模式正确..." -ForegroundColor Blue
if ($NoAot) {
    Write-Host "⚡ 使用标准编译进行构建..." -ForegroundColor Blue
    & "$PSScriptRoot/build.ps1" -Version $Version -Configuration $Configuration
} else {
    Write-Host "🔥 使用AOT编译进行构建..." -ForegroundColor Yellow
    & "$PSScriptRoot/build.ps1" -Version $Version -Configuration $Configuration -Aot
}
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ 构建失败"
    exit 1
}

# 创建 Windows 安装包
Write-Host "📦 从构建文件创建分发包..." -ForegroundColor Blue

foreach ($Platform in $Platforms) {
    $PlatformBuildDir = "$BuildDir/$Platform"
    
    if (-not (Test-Path $PlatformBuildDir)) {
        Write-Warning "❌ 未找到平台构建: $PlatformBuildDir"
        continue
    }
    
    # 创建安装程序目录
    $InstallerDir = "$DistDir/Deck-Installer-$Platform"
    New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null
    
    # 复制主程序
    $MainExe = if ($Platform -eq "windows-x64" -or $Platform -eq "windows-arm64") { "Deck.Console.exe" } else { "Deck.Console" }
    Copy-Item -Path "$PlatformBuildDir/$MainExe" -Destination "$InstallerDir/deck-binary.exe" -Force
    
    # 创建主启动程序 (带桌面图标创建功能)
    $MainLauncher = @"
@echo off
setlocal enabledelayedexpansion

:: 获取当前目录
set "CURRENT_DIR=%~dp0"
set "DECK_BINARY=%CURRENT_DIR%deck-binary.exe"
set "INSTALL_DIR=%USERPROFILE%\AppData\Local\Deck"
set "INSTALLED_BINARY=%INSTALL_DIR%\deck.exe"
set "CONFIG_FILE=%INSTALL_DIR%\.deck-configured"

:: 检查是否首次运行
if not exist "%CONFIG_FILE%" (
    echo.
    echo 🚀 欢迎使用 Deck 开发工具!
    echo =========================
    echo.
    echo 正在进行初始化配置...
    echo.
    
    :: 创建安装目录
    if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
    
    :: 复制程序到用户目录
    copy "%DECK_BINARY%" "%INSTALLED_BINARY%" >nul
    
    :: 添加到PATH环境变量
    echo 📦 正在配置环境变量...
    
    :: 获取当前用户PATH
    for /f "tokens=2*" %%A in ('reg query "HKCU\Environment" /v PATH 2^>nul') do set "CURRENT_PATH=%%B"
    
    :: 检查是否已在PATH中
    echo !CURRENT_PATH! | findstr /i "%INSTALL_DIR%" >nul
    if !errorlevel! neq 0 (
        :: 添加到PATH
        if "!CURRENT_PATH!"=="" (
            set "NEW_PATH=%INSTALL_DIR%"
        ) else (
            set "NEW_PATH=!CURRENT_PATH!;%INSTALL_DIR%"
        )
        reg add "HKCU\Environment" /v PATH /t REG_EXPAND_SZ /d "!NEW_PATH!" /f >nul
        echo ✅ 环境变量配置成功!
        
        :: 通知系统更新环境变量
        powershell -Command "[Environment]::SetEnvironmentVariable('Path', [Environment]::GetEnvironmentVariable('Path', 'User'), 'User')" >nul 2>&1
    ) else (
        echo ✅ 环境变量已存在!
    )
    
    echo.
    echo 📦 创建桌面快捷方式...
    
    :: 创建桌面快捷方式
    powershell -Command "$WScript = New-Object -ComObject WScript.Shell; $Shortcut = $WScript.CreateShortcut('%USERPROFILE%\Desktop\Deck 开发工具.lnk'); $Shortcut.TargetPath = '%INSTALLED_BINARY%'; $Shortcut.WorkingDirectory = '%USERPROFILE%'; $Shortcut.Description = 'Deck 开发环境工具'; $Shortcut.Save()" >nul 2>&1
    
    echo ✅ 桌面快捷方式创建成功!
    echo.
    echo 🎉 安装完成!
    echo.
    echo 现在您可以：
    echo • 在 VS Code 终端中使用: deck --help
    echo • 在 PowerShell 中使用: deck start python
    echo • 双击桌面快捷方式直接运行
    echo.
    echo 💡 这是一个终端工具，主要在命令行中使用。
    echo.
    echo 📚 获取更多帮助:
    echo • GitHub:  https://github.com/your-org/deck
    echo • Gitee:   https://gitee.com/your-org/deck
    echo • 使用指南: https://github.com/your-org/deck/wiki
    echo.
    echo 💡 提示: 复制上面的链接到浏览器查看详细使用方法
    echo.
    echo 注意: 您可能需要重新打开终端窗口以使环境变量生效
    echo.
    
    :: 标记为已配置
    echo configured > "%CONFIG_FILE%"
    
    pause
    exit /b 0
)

:: 后续运行：直接执行deck功能
"%INSTALLED_BINARY%" %*
"@
    
    # 写入启动脚本
    $MainLauncher | Out-File -FilePath "$InstallerDir/Deck.bat" -Encoding UTF8
    
    Write-Host "✅ $Platform 安装程序已创建: $InstallerDir" -ForegroundColor Green
}

# 创建ZIP分发包
Write-Host "📦 创建ZIP分发包..." -ForegroundColor Blue

for ($i = 0; $i -lt $Platforms.Length; $i++) {
    $Platform = $Platforms[$i]
    $RuntimeId = $RuntimeIds[$i]
    $InstallerDir = "$DistDir/Deck-Installer-$Platform"
    $ZipPath = "$DistDir/Deck-v$Version-$RuntimeId.zip"
    
    if (Test-Path $InstallerDir) {
        try {
            # 创建ZIP包
            Compress-Archive -Path "$InstallerDir/*" -DestinationPath $ZipPath -Force
            
            if (Test-Path $ZipPath) {
                $ZipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
                Write-Host "📦 创建ZIP包: $ZipPath ($ZipSize MB)" -ForegroundColor Green
            }
        }
        catch {
            Write-Warning "⚠️  $Platform ZIP 创建失败: $($_.Exception.Message)"
        }
    }
}

if ($NoAot) {
    Write-Host "🎉 Windows 分发包构建完成!" -ForegroundColor Green
} else {
    Write-Host "🎉 Windows AOT优化分发包构建完成!" -ForegroundColor Green
}
Write-Host "📁 分发目录: $DistDir" -ForegroundColor Cyan
Write-Host ""

Write-Host "📦 创建的分发包:" -ForegroundColor Cyan
Get-ChildItem $DistDir -Recurse | Where-Object { 
    -not $_.PSIsContainer -and ($_.Extension -eq ".zip" -or $_.Name -eq "Deck.bat") 
} | ForEach-Object {
    $Size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  📄 $($_.Name) ($Size MB)" -ForegroundColor Gray
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