# =============================================================================
# Deck .NET Console 多平台构建脚本 (Windows PowerShell)
# =============================================================================

param(
    [string]$Platform = "",
    [string]$Architecture = "",
    [switch]$All = $false,
    [switch]$Clean = $false,
    [switch]$Aot = $false,
    [switch]$NoCompress = $false,
    [string]$Version = "1.0.0",
    [switch]$Help = $false
)

# 构建配置
$SupportedPlatforms = @("windows", "linux", "macos")
$SupportedArchitectures = @("x64", "arm64")
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$BuildDir = Join-Path $ProjectRoot "build"
$DistDir = Join-Path $ProjectRoot "dist"

# 输出帮助信息
function Show-Help {
    Write-Host "Deck .NET Console 多平台构建脚本" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "用法: .\build.ps1 [选项] [平台] [架构]" -ForegroundColor White
    Write-Host ""
    Write-Host "选项:" -ForegroundColor Yellow
    Write-Host "  -Help           显示帮助信息"
    Write-Host "  -Version        指定版本号 (默认: 1.0.0)"
    Write-Host "  -Clean          清理构建目录"
    Write-Host "  -All            构建所有平台和架构"
    Write-Host "  -Aot            启用AOT编译 (实验性)"
    Write-Host "  -NoCompress     不压缩输出文件"
    Write-Host ""
    Write-Host "支持的平台:" -ForegroundColor Yellow
    Write-Host "  windows         构建Windows版本"
    Write-Host "  linux           构建Linux版本"
    Write-Host "  macos           构建macOS版本"
    Write-Host ""
    Write-Host "支持的架构:" -ForegroundColor Yellow
    Write-Host "  x64             构建x64架构"
    Write-Host "  arm64           构建ARM64架构"
    Write-Host ""
    Write-Host "示例:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -All                    # 构建所有平台和架构"
    Write-Host "  .\build.ps1 windows x64            # 构建Windows x64版本"
    Write-Host "  .\build.ps1 -Platform linux -Aot   # 构建Linux AOT版本"
    Write-Host "  .\build.ps1 -Clean                 # 清理构建目录"
    Write-Host ""
}

# 日志函数
function Write-Info {
    param([string]$Message)
    Write-Host "🔨 [BUILD] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ [SUCCESS] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ [ERROR] $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  [WARN] $Message" -ForegroundColor Yellow
}

# 清理构建目录
function Clear-BuildDirectory {
    Write-Info "清理构建目录..."
    if (Test-Path $BuildDir) {
        Remove-Item $BuildDir -Recurse -Force
    }
    if (Test-Path $DistDir) {
        Remove-Item $DistDir -Recurse -Force
    }
    Write-Success "构建目录已清理"
}

# 检查依赖
function Test-Dependencies {
    Write-Info "检查构建依赖..."
    
    # 检查.NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Info "发现 .NET SDK: $dotnetVersion"
    }
    catch {
        Write-Error "未找到 .NET SDK，请先安装 .NET 9"
        return $false
    }
    
    # 检查项目文件
    $projectFile = Join-Path $ProjectRoot "src\Deck.Console\Deck.Console.csproj"
    if (-not (Test-Path $projectFile)) {
        Write-Error "未找到项目文件: $projectFile"
        return $false
    }
    
    Write-Success "依赖检查通过"
    return $true
}

# 获取运行时标识符
function Get-RuntimeId {
    param([string]$Platform, [string]$Architecture)
    
    switch ($Platform) {
        "windows" { return "win-$Architecture" }
        "linux"   { return "linux-$Architecture" }  
        "macos"   { return "osx-$Architecture" }
        default   { return $null }
    }
}

# 构建单个目标
function Build-Target {
    param(
        [string]$Platform,
        [string]$Architecture,
        [bool]$UseAot = $false
    )
    
    $runtimeId = Get-RuntimeId -Platform $Platform -Architecture $Architecture
    if ($null -eq $runtimeId) {
        Write-Error "不支持的平台组合: $Platform-$Architecture"
        return $false
    }
    
    $targetName = "$Platform-$Architecture"
    if ($UseAot) { $targetName += "-aot" }
    
    Write-Info "构建目标: $targetName (运行时: $runtimeId)"
    
    $outputDir = Join-Path $BuildDir $targetName
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    
    # 构建参数
    $publishArgs = @(
        "publish"
        "src\Deck.Console\Deck.Console.csproj"
        "--configuration", "Release"
        "--runtime", $runtimeId
        "--self-contained", "true"
        "--output", $outputDir
    )
    
    if ($UseAot) {
        $publishArgs += "-p:PublishAot=true"
        Write-Info "启用AOT编译 (实验性功能)"
    }
    
    # 执行构建
    try {
        $process = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -WorkingDirectory $ProjectRoot -Wait -PassThru -NoNewWindow
        if ($process.ExitCode -ne 0) {
            if ($UseAot) {
                Write-Warning "AOT编译失败，这在当前版本是预期的 (YamlDotNet兼容性问题)"
                return $false
            } else {
                Write-Error "构建失败，退出代码: $($process.ExitCode)"
                return $false
            }
        }
        
        Write-Success "构建完成: $targetName"
        return $true
    }
    catch {
        Write-Error "构建异常: $($_.Exception.Message)"
        return $false
    }
}

# 创建压缩包
function New-Archive {
    param(
        [string]$Platform,
        [string]$Architecture,
        [bool]$UseAot = $false
    )
    
    $targetName = "$Platform-$Architecture"
    if ($UseAot) { $targetName += "-aot" }
    
    $sourceDir = Join-Path $BuildDir $targetName
    if (-not (Test-Path $sourceDir)) {
        Write-Warning "跳过压缩，源目录不存在: $sourceDir"
        return
    }
    
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
    
    $archiveName = "deck-v$Version-$targetName"
    
    if ($NoCompress) {
        Write-Info "跳过压缩，使用原始目录: $targetName"
        return
    }
    
    # Windows和macOS使用zip，Linux使用tar.gz
    if ($Platform -eq "linux") {
        # 在Windows上也使用PowerShell压缩，但命名为.tar.gz以保持一致性
        $archiveFile = Join-Path $DistDir "$archiveName.tar.gz"
        Write-Info "创建压缩包: $archiveName.tar.gz"
    } else {
        $archiveFile = Join-Path $DistDir "$archiveName.zip"
        Write-Info "创建压缩包: $archiveName.zip"
    }
    
    try {
        # 使用PowerShell的Compress-Archive
        Compress-Archive -Path "$sourceDir\*" -DestinationPath $archiveFile -Force
        
        # 计算SHA256校验和
        $hash = Get-FileHash -Path $archiveFile -Algorithm SHA256
        $hashFile = "$archiveFile.sha256"
        "$($hash.Hash.ToLower())  $(Split-Path $archiveFile -Leaf)" | Out-File -FilePath $hashFile -Encoding utf8
        
        Write-Success "压缩包已创建: $(Split-Path $archiveFile -Leaf)"
    }
    catch {
        Write-Warning "压缩失败: $($_.Exception.Message)"
    }
}

# 显示构建摘要
function Show-BuildSummary {
    Write-Info "构建摘要"
    Write-Host ""
    
    Write-Host "版本信息:" -ForegroundColor Cyan
    Write-Host "  版本: v$Version"
    Write-Host "  构建时间: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')"
    Write-Host ""
    
    if (Test-Path $BuildDir) {
        Write-Host "构建产物:" -ForegroundColor Cyan
        Get-ChildItem $BuildDir | ForEach-Object {
            Write-Host "  $($_.Name)" -ForegroundColor Green
        }
        Write-Host ""
    }
    
    if (Test-Path $DistDir) {
        Write-Host "发布包:" -ForegroundColor Cyan
        Get-ChildItem $DistDir -File | ForEach-Object {
            $size = [math]::Round($_.Length / 1MB, 2)
            Write-Host "  $($_.Name) ($size MB)" -ForegroundColor Green
        }
        Write-Host ""
    }
    
    Write-Host "后续步骤:" -ForegroundColor Cyan
    Write-Host "  1. 测试构建产物"
    Write-Host "  2. 发布到GitHub Releases"
    Write-Host ""
}

# 主函数
function Main {
    if ($Help) {
        Show-Help
        return
    }
    
    Write-Host ""
    Write-Host "🚀 Deck .NET Console 构建脚本 v$Version" -ForegroundColor Cyan
    Write-Host ""
    
    # 仅清理
    if ($Clean) {
        Clear-BuildDirectory
        return
    }
    
    # 检查依赖
    if (-not (Test-Dependencies)) {
        exit 1
    }
    
    # 清理并重新创建构建目录
    Clear-BuildDirectory
    New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null
    
    $buildTargets = @()
    
    # 确定构建目标
    if ($All) {
        Write-Info "构建所有平台和架构..."
        foreach ($plt in $SupportedPlatforms) {
            foreach ($arch in $SupportedArchitectures) {
                $buildTargets += @{ Platform = $plt; Architecture = $arch }
            }
        }
    } elseif ($Platform -and $Architecture) {
        $buildTargets += @{ Platform = $Platform; Architecture = $Architecture }
    } elseif ($Platform) {
        foreach ($arch in $SupportedArchitectures) {
            $buildTargets += @{ Platform = $Platform; Architecture = $arch }
        }
    } else {
        # 默认构建当前平台
        $currentPlatform = "windows"
        $currentArch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
        if ($currentArch -eq "x86") { $currentArch = "x64" } # 强制使用x64
        
        Write-Info "构建当前平台: $currentPlatform-$currentArch"
        $buildTargets += @{ Platform = $currentPlatform; Architecture = $currentArch }
    }
    
    # 执行构建
    $successCount = 0
    $totalCount = $buildTargets.Count
    
    foreach ($target in $buildTargets) {
        $success = Build-Target -Platform $target.Platform -Architecture $target.Architecture -UseAot:$false
        if ($success) {
            $successCount++
            
            # 创建压缩包
            New-Archive -Platform $target.Platform -Architecture $target.Architecture -UseAot:$false
        }
        
        # 如果启用AOT，尝试AOT构建（实验性）
        if ($Aot) {
            Write-Info "尝试AOT构建 (实验性)..."
            $aotSuccess = Build-Target -Platform $target.Platform -Architecture $target.Architecture -UseAot:$true
            if ($aotSuccess) {
                New-Archive -Platform $target.Platform -Architecture $target.Architecture -UseAot:$true
            }
        }
    }
    
    # 显示结果
    Write-Host ""
    if ($successCount -eq $totalCount) {
        Write-Success "所有构建成功完成! ($successCount/$totalCount)"
    } elseif ($successCount -gt 0) {
        Write-Warning "部分构建成功完成 ($successCount/$totalCount)"
    } else {
        Write-Error "所有构建失败"
        exit 1
    }
    
    Show-BuildSummary
    Write-Success "构建脚本执行完成!"
}

# 执行主函数
Main