# AOT发布和打包策略

## 概述

本文档详细描述了.NET Console版本deck工具的AOT（Ahead-of-Time）编译发布策略，包括本地构建配置、标准平台包创建和GitHub Actions自动化构建流程。AOT编译将提供更快的启动速度、更小的内存占用和无需运行时依赖的单文件可执行程序，同时为每个平台创建标准的安装包格式。

> **重要说明**：本文档中的所有路径和构建脚本都是相对于`deck-dotnet`目录执行的。在实际使用时，请确保在`deck-dotnet`目录下运行这些命令。

## AOT编译配置

### 项目文件配置

#### 主项目配置 (Deck.Console.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- AOT 编译配置 -->
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
    <TrimMode>full</TrimMode>
    <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
    
    <!-- 单文件发布配置 -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    
    <!-- 版本信息 -->
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <Version>1.0.0</Version>
    
    <!-- 应用程序信息 -->
    <AssemblyTitle>Deck - 甲板，容器化开发环境构建工具</AssemblyTitle>
    <AssemblyDescription>模板复用，助力开发快速起步的容器化开发环境构建工具</AssemblyDescription>
    <AssemblyCompany>Deck Team</AssemblyCompany>
    <AssemblyProduct>Deck</AssemblyProduct>
    <Copyright>Copyright © 2025 Deck Team</Copyright>
  </PropertyGroup>

  <!-- 条件编译符号 -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    <Optimize>true</Optimize>
  </PropertyGroup>

  <!-- NuGet 包引用 -->
  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="$(SystemCommandLineVersion)" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$(MicrosoftExtensionsVersion)" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="$(MicrosoftExtensionsVersion)" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="$(MicrosoftExtensionsVersion)" />
    <PackageReference Include="YamlDotNet" Version="$(YamlDotNetVersion)" />
    <PackageReference Include="System.Text.Json" Version="$(SystemTextJsonVersion)" />
  </ItemGroup>

  <!-- AOT 兼容性配置 -->
  <ItemGroup>
    <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
    <RdXmlFile Include="rd.xml" />
  </ItemGroup>

  <!-- 平台特定配置 -->
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'win-x64'">
    <DefineConstants>$(DefineConstants);WINDOWS</DefineConstants>
  </PropertyGroup>
  
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'linux-x64' OR '$(RuntimeIdentifier)' == 'linux-arm64'">
    <DefineConstants>$(DefineConstants);LINUX</DefineConstants>
  </PropertyGroup>
  
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'osx-x64' OR '$(RuntimeIdentifier)' == 'osx-arm64'">
    <DefineConstants>$(DefineConstants);MACOS</DefineConstants>
  </PropertyGroup>
</Project>
```

#### 全局构建配置 (Directory.Build.props)

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>NU1701</WarningsNotAsErrors>
    
    <!-- 代码分析 -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    
    <!-- AOT 全局设置 -->
    <IsAotCompatible>true</IsAotCompatible>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <EnableSingleFileAnalyzer>true</EnableSingleFileAnalyzer>
  </PropertyGroup>

  <!-- 包版本管理 -->
  <PropertyGroup>
    <MicrosoftExtensionsVersion>9.0.0</MicrosoftExtensionsVersion>
    <SystemTextJsonVersion>9.0.0</SystemTextJsonVersion>
    <SystemCommandLineVersion>2.0.0-beta4.22272.1</SystemCommandLineVersion>
    <YamlDotNetVersion>16.2.0</YamlDotNetVersion>
  </PropertyGroup>
</Project>
```

### AOT兼容性处理

#### JSON序列化配置

```csharp
// JsonSerializerContext.cs
using System.Text.Json.Serialization;

[JsonSerializable(typeof(DeckConfig))]
[JsonSerializable(typeof(TemplateConfig))]
[JsonSerializable(typeof(ContainerConfig))]
[JsonSerializable(typeof(SystemInfo))]
[JsonSerializable(typeof(ContainerInfo))]
[JsonSerializable(typeof(TemplateInfo))]
[JsonSerializable(typeof(ProjectInfo))]
[JsonSerializable(typeof(ComposeFile))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<string>))]
public partial class DeckJsonContext : JsonSerializerContext
{
}

// 在服务中使用
public class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = DeckJsonContext.Default,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    
    public async Task<DeckConfig> LoadConfigAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<DeckConfig>(json, JsonOptions)!;
    }
}
```

#### Trimmer配置文件

**TrimmerRoots.xml**
```xml
<linker>
  <assembly fullname="Deck.Console" />
  <assembly fullname="Deck.Core" />
  <assembly fullname="Deck.Services" />
  <assembly fullname="Deck.Infrastructure" />
  
  <!-- 保留System.CommandLine相关类型 -->
  <assembly fullname="System.CommandLine">
    <type fullname="*" />
  </assembly>
  
  <!-- 保留YamlDotNet相关类型 -->
  <assembly fullname="YamlDotNet">
    <type fullname="YamlDotNet.Serialization.*" />
  </assembly>
</linker>
```

**rd.xml**
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="Deck.Console" Dynamic="Required All" />
    <Assembly Name="Deck.Core" Dynamic="Required All" />
    <Assembly Name="Deck.Services" Dynamic="Required All" />
    <Assembly Name="Deck.Infrastructure" Dynamic="Required All" />
  </Application>
</Directives>
```

## 标准平台包格式

### 包格式说明

为了提供更好的用户体验，我们为每个平台创建标准的安装包：

- **Windows**: MSI 安装包 (`.msi`)
- **macOS**: DMG 磁盘镜像 (`.dmg`)  
- **Linux**: DEB 和 RPM 包 (`.deb`, `.rpm`)

### 打包工具要求

#### Windows MSI 打包
- **工具**: WiX Toolset v4
- **安装**: `dotnet tool install --global wix`
- **功能**: 自动创建开始菜单项、PATH环境变量、卸载程序

#### macOS DMG 打包
- **工具**: create-dmg 或 appdmg
- **安装**: `brew install create-dmg`
- **功能**: 应用程序拖拽安装、背景图片、许可协议

#### Linux 包打包
- **DEB**: `dpkg-deb` 工具
- **RPM**: `rpmbuild` 工具
- **功能**: 依赖管理、系统服务集成、桌面文件

### 包元数据配置

创建 `packaging/` 目录存储打包相关文件：

```
packaging/
├── windows/
│   ├── deck.wxs              # WiX 配置文件
│   └── banner.bmp            # 安装界面图片
├── macos/
│   ├── Info.plist           # 应用信息
│   ├── background.png       # DMG 背景
│   └── create-dmg.json      # DMG 配置
└── linux/
    ├── DEBIAN/
    │   ├── control          # DEB 包控制文件
    │   └── postinst         # 安装后脚本
    └── rpm/
        └── deck.spec        # RPM 规范文件
```

## 本地构建脚本

### Windows构建脚本 (build-windows.ps1)

```powershell
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
    $ExePath = "$PlatformOutputDir/deck.exe"
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
foreach ($Platform in $Platforms) {
    $PlatformOutputDir = "$OutputDir/$Platform"
    $MsiPath = "$OutputDir/deck-v$Version-$Platform.msi"
    
    # 使用 WiX 创建 MSI 包
    Write-Host "🔨 创建 $Platform MSI 包..." -ForegroundColor Blue
    wix build packaging/windows/deck.wxs `
        -d "Version=$Version" `
        -d "Platform=$Platform" `
        -d "SourceDir=$PlatformOutputDir" `
        -out $MsiPath
    
    if (Test-Path $MsiPath) {
        $MsiSize = (Get-Item $MsiPath).Length / 1MB
        Write-Host "📦 创建MSI包: $MsiPath ($([math]::Round($MsiSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Error "❌ $Platform MSI 创建失败"
        exit 1
    }
}

Write-Host "🎉 Windows 构建完成!" -ForegroundColor Green
Write-Host "📁 输出目录: $OutputDir" -ForegroundColor Cyan
```

### Linux/macOS构建脚本 (build-unix.sh)

```bash
#!/bin/bash

set -euo pipefail

# 参数解析
CONFIGURATION="${1:-Release}"
VERSION="${2:-1.0.0}"
CLEAN="${3:-false}"

echo "🚀 开始构建 Deck Unix 版本..."

# 设置变量
PROJECT_PATH="src/Deck.Console/Deck.Console.csproj"
OUTPUT_DIR="artifacts/unix"

# 检测平台
if [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORMS=("osx-x64" "osx-arm64")
    OS_NAME="macos"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORMS=("linux-x64" "linux-arm64")
    OS_NAME="linux"
else
    echo "❌ 不支持的操作系统: $OSTYPE"
    exit 1
fi

# 清理输出目录
if [[ "$CLEAN" == "true" ]] || [[ -d "$OUTPUT_DIR" ]]; then
    echo "🧹 清理输出目录..."
    rm -rf "$OUTPUT_DIR"
fi

# 恢复依赖
echo "📦 恢复 NuGet 包..."
dotnet restore "$PROJECT_PATH"

# 构建各平台版本
for PLATFORM in "${PLATFORMS[@]}"; do
    echo "🔨 构建 $PLATFORM 版本..."
    
    PLATFORM_OUTPUT_DIR="$OUTPUT_DIR/$PLATFORM"
    mkdir -p "$PLATFORM_OUTPUT_DIR"
    
    # AOT 发布
    dotnet publish "$PROJECT_PATH" \
        --configuration "$CONFIGURATION" \
        --runtime "$PLATFORM" \
        --self-contained true \
        --output "$PLATFORM_OUTPUT_DIR" \
        -p:Version="$VERSION" \
        -p:PublishAot=true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:InvariantGlobalization=true
    
    # 验证输出文件
    EXE_PATH="$PLATFORM_OUTPUT_DIR/deck"
    if [[ -f "$EXE_PATH" ]]; then
        FILE_SIZE=$(du -m "$EXE_PATH" | cut -f1)
        echo "✅ $PLATFORM 构建成功 (大小: ${FILE_SIZE} MB)"
        
        # 设置执行权限
        chmod +x "$EXE_PATH"
        
        # 测试可执行文件
        echo "🧪 测试 $PLATFORM 可执行文件..."
        if "$EXE_PATH" --version; then
            echo "✅ $PLATFORM 可执行文件测试通过"
        else
            echo "⚠️  $PLATFORM 可执行文件测试失败"
        fi
    else
        echo "❌ $PLATFORM 输出文件不存在: $EXE_PATH"
        exit 1
    fi
done

# 创建标准平台包
echo "📦 创建标准安装包..."
for PLATFORM in "${PLATFORMS[@]}"; do
    PLATFORM_OUTPUT_DIR="$OUTPUT_DIR/$PLATFORM"
    
    if [[ "$OS_NAME" == "macos" ]]; then
        # 创建 DMG 包
        DMG_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM.dmg"
        echo "🔨 创建 $PLATFORM DMG 包..."
        
        # 创建临时目录结构
        TEMP_DMG_DIR="$OUTPUT_DIR/dmg-temp"
        mkdir -p "$TEMP_DMG_DIR"
        cp "$PLATFORM_OUTPUT_DIR/deck" "$TEMP_DMG_DIR/"
        
        # 使用 create-dmg 创建 DMG
        create-dmg \
            --volname "Deck v$VERSION" \
            --volicon "packaging/macos/deck.icns" \
            --window-pos 200 120 \
            --window-size 600 300 \
            --icon-size 100 \
            --icon "deck" 175 120 \
            --hide-extension "deck" \
            --app-drop-link 425 120 \
            "$DMG_PATH" \
            "$TEMP_DMG_DIR"
        
        rm -rf "$TEMP_DMG_DIR"
        PACKAGE_PATH="$DMG_PATH"
    else
        # 创建 DEB 和 RPM 包
        DEB_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM.deb"
        RPM_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM.rpm"
        
        echo "🔨 创建 $PLATFORM DEB 包..."
        # 创建 DEB 包结构
        DEB_DIR="$OUTPUT_DIR/deb-temp"
        mkdir -p "$DEB_DIR/usr/local/bin"
        mkdir -p "$DEB_DIR/DEBIAN"
        
        cp "$PLATFORM_OUTPUT_DIR/deck" "$DEB_DIR/usr/local/bin/"
        cp "packaging/linux/DEBIAN/control" "$DEB_DIR/DEBIAN/"
        
        # 更新版本信息
        sed -i "s/{{VERSION}}/$VERSION/g" "$DEB_DIR/DEBIAN/control"
        sed -i "s/{{ARCHITECTURE}}/$(dpkg --print-architecture)/g" "$DEB_DIR/DEBIAN/control"
        
        dpkg-deb --build "$DEB_DIR" "$DEB_PATH"
        rm -rf "$DEB_DIR"
        
        echo "🔨 创建 $PLATFORM RPM 包..."
        # 使用 rpmbuild 创建 RPM 包
        rpmbuild -bb packaging/linux/rpm/deck.spec \
            --define "_version $VERSION" \
            --define "_sourcedir $PLATFORM_OUTPUT_DIR" \
            --define "_rpmdir $OUTPUT_DIR"
        
        PACKAGE_PATH="$DEB_PATH and $RPM_PATH"
    fi
    
    PACKAGE_SIZE=$(du -m "$DEB_PATH" 2>/dev/null || du -m "$DMG_PATH" 2>/dev/null | cut -f1)
    echo "📦 创建安装包: $PACKAGE_PATH (${PACKAGE_SIZE} MB)"
done

echo "🎉 Unix 构建完成!"
echo "📁 输出目录: $OUTPUT_DIR"
```

### 跨平台构建脚本 (build-all.sh)

```bash
#!/bin/bash

set -euo pipefail

VERSION="${1:-1.0.0}"
CONFIGURATION="${2:-Release}"

echo "🚀 开始跨平台构建 Deck v$VERSION..."

# 创建输出目录
OUTPUT_DIR="artifacts/release"
mkdir -p "$OUTPUT_DIR"

# 支持的平台
declare -A PLATFORMS=(
    ["windows-x64"]="win-x64"
    ["windows-arm64"]="win-arm64"
    ["linux-x64"]="linux-x64"
    ["linux-arm64"]="linux-arm64"
    ["macos-x64"]="osx-x64"
    ["macos-arm64"]="osx-arm64"
)

PROJECT_PATH="src/Deck.Console/Deck.Console.csproj"

# 恢复依赖
echo "📦 恢复 NuGet 包..."
dotnet restore "$PROJECT_PATH"

# 构建所有平台
for PLATFORM_NAME in "${!PLATFORMS[@]}"; do
    RUNTIME_ID="${PLATFORMS[$PLATFORM_NAME]}"
    echo "🔨 构建 $PLATFORM_NAME ($RUNTIME_ID)..."
    
    PLATFORM_OUTPUT_DIR="$OUTPUT_DIR/$PLATFORM_NAME"
    mkdir -p "$PLATFORM_OUTPUT_DIR"
    
    # AOT 发布
    dotnet publish "$PROJECT_PATH" \
        --configuration "$CONFIGURATION" \
        --runtime "$RUNTIME_ID" \
        --self-contained true \
        --output "$PLATFORM_OUTPUT_DIR" \
        -p:Version="$VERSION" \
        -p:PublishAot=true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:InvariantGlobalization=true
    
    # 确定可执行文件名
    if [[ "$RUNTIME_ID" == win-* ]]; then
        EXE_NAME="deck.exe"
    else
        EXE_NAME="deck"
    fi
    
    EXE_PATH="$PLATFORM_OUTPUT_DIR/$EXE_NAME"
    
    # 验证构建结果
    if [[ -f "$EXE_PATH" ]]; then
        FILE_SIZE=$(du -m "$EXE_PATH" | cut -f1)
        echo "✅ $PLATFORM_NAME 构建成功 (大小: ${FILE_SIZE} MB)"
        
        # 设置执行权限（非Windows平台）
        if [[ "$RUNTIME_ID" != win-* ]]; then
            chmod +x "$EXE_PATH"
        fi
    else
        echo "❌ $PLATFORM_NAME 构建失败: $EXE_PATH 不存在"
        exit 1
    fi
    
    # 创建压缩包
    if [[ "$RUNTIME_ID" == win-* ]]; then
        ZIP_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM_NAME.zip"
        (cd "$PLATFORM_OUTPUT_DIR" && zip -r "../$(basename "$ZIP_PATH")" .)
    else
        TAR_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM_NAME.tar.gz"
        tar -czf "$TAR_PATH" -C "$PLATFORM_OUTPUT_DIR" .
    fi
done

echo "🎉 跨平台构建完成!"
echo "📁 输出目录: $OUTPUT_DIR"
ls -la "$OUTPUT_DIR"
```

## GitHub Actions配置

### 主构建工作流 (.github/workflows/build.yml)

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  test:
    name: Test
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
      
    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
      with:
        file: coverage.cobertura.xml
        fail_ci_if_error: true

  build:
    name: Build ${{ matrix.os }}
    runs-on: ${{ matrix.os }}
    needs: test
    
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        include:
          - os: ubuntu-latest
            platforms: 'linux-x64,linux-arm64'
          - os: windows-latest
            platforms: 'win-x64,win-arm64'
          - os: macos-latest
            platforms: 'osx-x64,osx-arm64'
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build and publish
      shell: bash
      run: |
        IFS=',' read -ra PLATFORM_ARRAY <<< "${{ matrix.platforms }}"
        for platform in "${PLATFORM_ARRAY[@]}"; do
          echo "Building for $platform..."
          
          output_dir="artifacts/$platform"
          mkdir -p "$output_dir"
          
          dotnet publish src/Deck.Console/Deck.Console.csproj \
            --configuration Release \
            --runtime "$platform" \
            --self-contained true \
            --output "$output_dir" \
            -p:PublishAot=true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=true \
            -p:InvariantGlobalization=true
          
          # 创建压缩包
          if [[ "$platform" == win-* ]]; then
            (cd "$output_dir" && zip -r "../deck-$platform.zip" .)
          else
            tar -czf "artifacts/deck-$platform.tar.gz" -C "$output_dir" .
          fi
        done
        
    - name: Upload artifacts
      uses: actions/upload-artifact@v4
      with:
        name: deck-${{ matrix.os }}
        path: artifacts/deck-*
        retention-days: 30
```

### 发布工作流 (.github/workflows/release.yml)

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  create-release:
    name: Create Release
    runs-on: ubuntu-latest
    outputs:
      upload_url: ${{ steps.create_release.outputs.upload_url }}
      version: ${{ steps.get_version.outputs.version }}
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Get version
      id: get_version
      run: echo "version=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
      
    - name: Create Release
      id: create_release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: ${{ github.ref }}
        release_name: Deck v${{ steps.get_version.outputs.version }}
        draft: false
        prerelease: false
        body: |
          ## 🚀 Deck v${{ steps.get_version.outputs.version }}
          
          ### 📥 下载
          
          选择适合您系统的版本：
          
          #### Windows
          - [Windows x64](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-win-x64.msi)
          - [Windows ARM64](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-win-arm64.msi)
          
          #### Linux
          - [Linux x64 (DEB)](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-linux-x64.deb)
          - [Linux x64 (RPM)](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-linux-x64.rpm)
          - [Linux ARM64 (DEB)](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-linux-arm64.deb)
          - [Linux ARM64 (RPM)](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-linux-arm64.rpm)
          
          #### macOS
          - [macOS Intel](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-osx-x64.dmg)
          - [macOS Apple Silicon](../../releases/download/${{ github.ref_name }}/deck-v${{ steps.get_version.outputs.version }}-osx-arm64.dmg)
          
          ### 📦 安装说明
          
          #### Windows
          1. 下载 `.msi` 安装包
          2. 双击运行安装程序，按向导完成安装
          3. 运行 `deck --version` 验证安装
          
          #### macOS
          1. 下载 `.dmg` 文件
          2. 双击打开，将 Deck 拖拽到 Applications 文件夹
          3. 或通过 Terminal: `sudo installer -pkg /path/to/deck.dmg -target /`
          4. 运行 `deck --version` 验证安装
          
          #### Linux
          1. 下载对应的 `.deb` 或 `.rpm` 包
          2. Ubuntu/Debian: `sudo dpkg -i deck-vX.X.X-linux-x64.deb`
          3. CentOS/RHEL: `sudo rpm -ivh deck-vX.X.X-linux-x64.rpm`
          4. 运行 `deck --version` 验证安装
          
          ### 🔄 从Shell版本迁移
          
          如果您之前使用Shell版本的deck，请参考[迁移指南](../../wiki/Migration-Guide)。

  build-and-upload:
    name: Build and Upload ${{ matrix.name }}
    runs-on: ${{ matrix.os }}
    needs: create-release
    
    strategy:
      matrix:
        include:
          - name: Windows x64
            os: windows-latest
            runtime: win-x64
            artifact: deck-v${{ needs.create-release.outputs.version }}-win-x64.msi
          - name: Windows ARM64
            os: windows-latest
            runtime: win-arm64
            artifact: deck-v${{ needs.create-release.outputs.version }}-win-arm64.msi
          - name: Linux x64
            os: ubuntu-latest
            runtime: linux-x64
            artifact: deck-v${{ needs.create-release.outputs.version }}-linux-x64.deb
          - name: Linux ARM64
            os: ubuntu-latest
            runtime: linux-arm64
            artifact: deck-v${{ needs.create-release.outputs.version }}-linux-arm64.deb
          - name: macOS Intel
            os: macos-latest
            runtime: osx-x64
            artifact: deck-v${{ needs.create-release.outputs.version }}-osx-x64.dmg
          - name: macOS Apple Silicon
            os: macos-latest
            runtime: osx-arm64
            artifact: deck-v${{ needs.create-release.outputs.version }}-osx-arm64.dmg
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Publish
      run: |
        dotnet publish src/Deck.Console/Deck.Console.csproj \
          --configuration Release \
          --runtime ${{ matrix.runtime }} \
          --self-contained true \
          --output publish \
          -p:Version=${{ needs.create-release.outputs.version }} \
          -p:PublishAot=true \
          -p:PublishSingleFile=true \
          -p:PublishTrimmed=true \
          -p:InvariantGlobalization=true
          
    - name: Create package
      shell: bash
      run: |
        if [[ "${{ matrix.runtime }}" == win-* ]]; then
          # Windows: 创建 MSI 包
          wix build packaging/windows/deck.wxs \
            -d "Version=${{ needs.create-release.outputs.version }}" \
            -d "Platform=${{ matrix.runtime }}" \
            -d "SourceDir=publish" \
            -out "${{ matrix.artifact }}"
        elif [[ "${{ matrix.runtime }}" == osx-* ]]; then
          # macOS: 创建 DMG 包
          mkdir -p dmg-temp
          cp publish/deck dmg-temp/
          create-dmg \
            --volname "Deck v${{ needs.create-release.outputs.version }}" \
            --window-pos 200 120 \
            --window-size 600 300 \
            --icon-size 100 \
            --icon "deck" 175 120 \
            --hide-extension "deck" \
            --app-drop-link 425 120 \
            "${{ matrix.artifact }}" \
            dmg-temp
          rm -rf dmg-temp
        else
          # Linux: 创建 DEB 包
          mkdir -p deb-temp/usr/local/bin
          mkdir -p deb-temp/DEBIAN
          cp publish/deck deb-temp/usr/local/bin/
          cp packaging/linux/DEBIAN/control deb-temp/DEBIAN/
          sed -i "s/{{VERSION}}/${{ needs.create-release.outputs.version }}/g" deb-temp/DEBIAN/control
          dpkg-deb --build deb-temp "${{ matrix.artifact }}"
          rm -rf deb-temp
        fi
        
    - name: Upload Release Asset
      uses: actions/upload-release-asset@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        upload_url: ${{ needs.create-release.outputs.upload_url }}
        asset_path: ./${{ matrix.artifact }}
        asset_name: ${{ matrix.artifact }}
        asset_content_type: application/octet-stream
```

### 代码质量检查工作流 (.github/workflows/quality.yml)

```yaml
name: Code Quality

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  analyze:
    name: Analyze
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
        
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Run CodeQL Analysis
      uses: github/codeql-action/analyze@v3
      with:
        languages: csharp
        
    - name: Run SonarCloud Analysis
      uses: SonarSource/sonarcloud-github-action@master
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

## 性能优化和监控

### 构建性能优化

1. **并行构建**：使用`-m`参数启用并行构建
2. **增量构建**：利用构建缓存避免重复编译
3. **依赖缓存**：在CI中缓存NuGet包和构建输出

### 运行时性能监控

```csharp
// 性能监控中间件
public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    
    public void TrackCommandExecution(string command, TimeSpan duration, bool success)
    {
        _logger.LogInformation("Command: {Command}, Duration: {Duration}ms, Success: {Success}",
            command, duration.TotalMilliseconds, success);
    }
    
    public void TrackMemoryUsage()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        _logger.LogDebug("Memory usage: {MemoryMB} MB", memoryUsage / 1024 / 1024);
    }
}
```

## 发布检查清单

### 构建前检查
- [ ] 所有单元测试通过
- [ ] 代码质量检查通过
- [ ] AOT兼容性测试通过
- [ ] 跨平台兼容性验证
- [ ] 性能基准测试通过

### 发布后验证
- [ ] 所有平台的可执行文件正常运行
- [ ] 版本信息正确显示
- [ ] 基本功能测试通过
- [ ] 文件大小在预期范围内
- [ ] 启动时间符合性能要求

### 文档更新
- [ ] README.md更新安装说明
- [ ] CHANGELOG.md记录版本变更
- [ ] 迁移指南更新
- [ ] API文档更新

这个AOT发布策略确保了.NET Console版本的deck工具能够高效地构建、测试和发布到多个平台，同时保持高性能和用户友好的体验。