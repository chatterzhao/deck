#!/bin/bash

set -euo pipefail

VERSION="${1:-1.0.0}"
CONFIGURATION="${2:-Release}"

echo "🚀 开始跨平台构建 Deck v$VERSION..."

# 切换到项目根目录
cd "$(dirname "$0")/.."

# 创建输出目录
BUILD_DIR="build/release"
mkdir -p "$BUILD_DIR"

# 支持的平台（使用数组而不是关联数组）
PLATFORM_NAMES=("windows-x64" "windows-arm64" "linux-x64" "linux-arm64" "macos-x64" "macos-arm64")
RUNTIME_IDS=("win-x64" "win-arm64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")

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
    
    # AOT 发布 (如果失败则使用标准发布)
    if ! dotnet publish "$PROJECT_PATH" \
        --configuration "$CONFIGURATION" \
        --runtime "$RUNTIME_ID" \
        --self-contained true \
        --output "$PLATFORM_OUTPUT_DIR" \
        -p:Version="$VERSION" \
        -p:PublishAot=true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:InvariantGlobalization=true 2>/dev/null; then
        
        echo "⚠️  AOT编译失败，使用标准发布: $PLATFORM_NAME"
        dotnet publish "$PROJECT_PATH" \
            --configuration "$CONFIGURATION" \
            --runtime "$RUNTIME_ID" \
            --self-contained true \
            --output "$PLATFORM_OUTPUT_DIR" \
            -p:Version="$VERSION"
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
        echo "✅ $PLATFORM_NAME 构建成功 (大小: ${FILE_SIZE} MB)"
        
        # 设置执行权限（非Windows平台）
        if [[ "$RUNTIME_ID" != win-* ]]; then
            chmod +x "$EXE_PATH"
        fi
    else
        echo "❌ $PLATFORM_NAME 构建失败: $EXE_PATH 不存在"
        exit 1
    fi
done

echo "🎉 跨平台构建完成!"
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
echo "  🔨 开发构建已完成，文件位于: $BUILD_DIR/"
echo "  📦 创建分发包请使用："
echo "    - macOS/Linux: ./scripts/package.sh $VERSION"
echo "    - Windows:     .\\scripts\\package.ps1 -Version $VERSION"