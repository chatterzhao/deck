#!/bin/bash

set -euo pipefail

VERSION="${1:-1.0.0}"
CONFIGURATION="${2:-Release}"

echo "🚀 开始跨平台构建 Deck v$VERSION..."

# 创建输出目录
OUTPUT_DIR="artifacts/release"
mkdir -p "$OUTPUT_DIR"

# 支持的平台
declare -A PLATFORMS
PLATFORMS["windows-x64"]="win-x64"
PLATFORMS["windows-arm64"]="win-arm64"
PLATFORMS["linux-x64"]="linux-x64"  
PLATFORMS["linux-arm64"]="linux-arm64"
PLATFORMS["macos-x64"]="osx-x64"
PLATFORMS["macos-arm64"]="osx-arm64"

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
echo "📁 输出目录: $OUTPUT_DIR"
echo ""
echo "📊 构建统计:"
for PLATFORM_NAME in "${!PLATFORMS[@]}"; do
    RUNTIME_ID="${PLATFORMS[$PLATFORM_NAME]}"
    if [[ "$RUNTIME_ID" == win-* ]]; then
        EXE_NAME="Deck.Console.exe"
    else
        EXE_NAME="Deck.Console"
    fi
    
    EXE_PATH="$OUTPUT_DIR/$PLATFORM_NAME/$EXE_NAME"
    if [[ -f "$EXE_PATH" ]]; then
        FILE_SIZE=$(du -m "$EXE_PATH" | cut -f1)
        echo "  📄 $PLATFORM_NAME: ${FILE_SIZE} MB"
    fi
done

echo ""
echo "💡 提示："
echo "  使用平台特定脚本创建安装包："
echo "  - Windows: .\build-windows.ps1 -Version $VERSION"
echo "  - Unix:    ./build-unix.sh Release $VERSION"