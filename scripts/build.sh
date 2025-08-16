#!/bin/bash

set -euo pipefail

# 默认参数
VERSION="1.0.0"
CONFIGURATION="Release"
ENABLE_AOT="false"  # 开发构建默认关闭AOT

# 参数解析
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --configuration|-c)
            CONFIGURATION="$2"
            shift 2
            ;;
        --aot)
            ENABLE_AOT="true"
            shift
            ;;
        --help|-h)
            echo "用法: $0 [选项]"
            echo "选项:"
            echo "  --version VERSION        设置版本号 (默认: 1.0.0)"
            echo "  --configuration CONFIG   设置配置 (默认: Release)"
            echo "  --aot                   启用AOT编译 (开发模式默认关闭)"
            echo "  --help                  显示此帮助信息"
            echo ""
            echo "示例:"
            echo "  $0                      # 开发构建，快速编译"
            echo "  $0 --aot                # 开发构建，启用AOT优化"
            echo "  $0 --version 1.2.3      # 指定版本号"
            exit 0
            ;;
        *)
            # 向后兼容：位置参数
            if [[ -z "${VERSION_SET:-}" ]]; then
                VERSION="$1"
                VERSION_SET="true"
            elif [[ -z "${CONFIG_SET:-}" ]]; then
                CONFIGURATION="$1"
                CONFIG_SET="true"
            else
                echo "❌ 未知参数: $1"
                echo "使用 --help 查看帮助信息"
                exit 1
            fi
            shift
            ;;
    esac
done

if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "⚠️  注意：AOT模式下仅支持构建当前宿主系统平台的二进制文件"
    echo "🚀 开始跨平台构建 Deck v$VERSION (AOT优化)..."
else
    echo "⚠️  注意：开发模式下将构建所有平台的二进制文件"
    echo "🚀 开始跨平台构建 Deck v$VERSION (开发模式)..."
fi

echo ""
echo "⚠️  跨平台构建说明:"
echo "   虽然此脚本可以在单一环境中构建所有平台的二进制文件，"
echo "   但为了获得最佳兼容性，建议在目标平台上进行构建。"
echo "   特别是使用AOT编译时，应在相应的目标平台上构建。"
echo ""

# 切换到项目根目录
cd "$(dirname "$0")/.."

# 创建输出目录
BUILD_DIR="build/release"
mkdir -p "$BUILD_DIR"

# 检测宿主架构
HOST_ARCH=$(uname -m)

# 根据AOT和宿主系统选择平台
if [[ "$ENABLE_AOT" == "true" ]]; then
    # AOT模式：只构建当前宿主系统支持的平台
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "🔥 AOT模式：仅构建 macOS 平台（当前宿主系统）"
        PLATFORM_NAMES=("macos-x64" "macos-arm64")
        RUNTIME_IDS=("osx-x64" "osx-arm64")
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "🔥 AOT模式：构建 Linux 平台（当前宿主系统: $HOST_ARCH）"
        # 根据宿主架构决定构建目标
        if [[ "$HOST_ARCH" == "x86_64" ]]; then
            echo "⚠️  检测到 x86_64 宿主，跳过 ARM64 交叉编译（需要额外工具链）"
            PLATFORM_NAMES=("linux-x64")
            RUNTIME_IDS=("linux-x64")
        elif [[ "$HOST_ARCH" == "aarch64" || "$HOST_ARCH" == "arm64" ]]; then
            echo "⚠️  检测到 ARM64 宿主，跳过 x86_64 交叉编译（需要额外工具链）"
            PLATFORM_NAMES=("linux-arm64")
            RUNTIME_IDS=("linux-arm64")
        else
            echo "⚠️  未知宿主架构 $HOST_ARCH，仅构建 x86_64"
            PLATFORM_NAMES=("linux-x64")
            RUNTIME_IDS=("linux-x64")
        fi
    elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
        echo "🔥 AOT模式：仅构建 Windows 平台（当前宿主系统）"
        PLATFORM_NAMES=("windows-x64" "windows-arm64")
        RUNTIME_IDS=("win-x64" "win-arm64")
    else
        echo "❌ 不支持的宿主系统进行AOT编译: $OSTYPE"
        exit 1
    fi
else
    # 标准模式：构建所有平台
    PLATFORM_NAMES=("windows-x64" "windows-arm64" "linux-x64" "linux-arm64" "macos-x64" "macos-arm64")
    RUNTIME_IDS=("win-x64" "win-arm64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")
fi

PROJECT_PATH="src/Deck.Console/Deck.Console.csproj"

# 恢复依赖
echo "📦 恢复 NuGet 包..."
dotnet restore "$PROJECT_PATH"

# 构建所有平台
for i in "${!PLATFORM_NAMES[@]}"; do
    PLATFORM_NAME="${PLATFORM_NAMES[$i]}"
    RUNTIME_ID="${RUNTIME_IDS[$i]}"
    echo "🔨 构建 $PLATFORM_NAME ($RUNTIME_ID)..."
    
    PLATFORM_OUTPUT_DIR="$BUILD_DIR/$PLATFORM_NAME"
    mkdir -p "$PLATFORM_OUTPUT_DIR"
    
    # 根据配置选择构建模式
    if [[ "$ENABLE_AOT" == "true" ]]; then
        echo "🔥 使用AOT编译: $PLATFORM_NAME"
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
    else
        echo "⚡ 使用标准编译: $PLATFORM_NAME"
        dotnet publish "$PROJECT_PATH" \
            --configuration "$CONFIGURATION" \
            --runtime "$RUNTIME_ID" \
            --self-contained true \
            --output "$PLATFORM_OUTPUT_DIR" \
            -p:Version="$VERSION" \
            -p:PublishSingleFile=true
    fi
    
    # 确定可执行文件名
    if [[ "$RUNTIME_ID" == win-* ]]; then
        EXE_NAME="Deck.Console.exe"
    else
        EXE_NAME="Deck.Console"
    fi
    
    EXE_PATH="$PLATFORM_OUTPUT_DIR/$EXE_NAME"
    
    # 验证构建结果
    if [[ -f "$EXE_PATH" ]]; then
        FILE_SIZE=$(du -m "$EXE_PATH" | cut -f1)
        if [[ "$ENABLE_AOT" == "true" ]]; then
            echo "✅ $PLATFORM_NAME AOT构建成功 (大小: ${FILE_SIZE} MB)"
        else
            echo "✅ $PLATFORM_NAME 构建成功 (大小: ${FILE_SIZE} MB)"
        fi
        
        # 设置执行权限（非Windows平台）
        if [[ "$RUNTIME_ID" != win-* ]]; then
            chmod +x "$EXE_PATH"
        fi
    else
        echo "❌ $PLATFORM_NAME 构建失败: $EXE_PATH 不存在"
        exit 1
    fi
done

if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "🎉 跨平台AOT构建完成!"
else
    echo "🎉 跨平台构建完成!"
fi
echo "📁 构建目录: $BUILD_DIR"
echo ""
echo "📊 构建统计:"
for i in "${!PLATFORM_NAMES[@]}"; do
    PLATFORM_NAME="${PLATFORM_NAMES[$i]}"
    RUNTIME_ID="${RUNTIME_IDS[$i]}"
    
    if [[ "$RUNTIME_ID" == win-* ]]; then
        EXE_NAME="Deck.Console.exe"
    else
        EXE_NAME="Deck.Console"
    fi
    
    EXE_PATH="$BUILD_DIR/$PLATFORM_NAME/$EXE_NAME"
    if [[ -f "$EXE_PATH" ]]; then
        FILE_SIZE=$(du -m "$EXE_PATH" | cut -f1)
        echo "  📄 $PLATFORM_NAME: ${FILE_SIZE} MB"
    fi
done

echo ""
echo "💡 提示："
if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "  🔥 AOT优化构建已完成，文件位于: $BUILD_DIR/"
else
    echo "  ⚡ 开发构建已完成，文件位于: $BUILD_DIR/"
    echo "  🔥 如需AOT优化构建，请使用: $0 --aot"
fi
echo "  📦 创建生产分发包请使用："
echo "    - macOS/Linux: ./scripts/package.sh"
echo "    - Windows:     .\\scripts\\package.ps1"