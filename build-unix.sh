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
    EXE_PATH="$PLATFORM_OUTPUT_DIR/Deck.Console"
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
        
        # 检查create-dmg是否安装
        if ! command -v create-dmg >/dev/null 2>&1; then
            echo "⚠️  create-dmg 未安装，跳过DMG包创建"
            echo "   安装方法: brew install create-dmg"
            continue
        fi
        
        # 创建临时目录结构
        TEMP_DMG_DIR="$OUTPUT_DIR/dmg-temp"
        mkdir -p "$TEMP_DMG_DIR"
        cp "$PLATFORM_OUTPUT_DIR/Deck.Console" "$TEMP_DMG_DIR/deck"
        
        # 使用 create-dmg 创建 DMG
        create-dmg \
            --volname "Deck v$VERSION" \
            --window-pos 200 120 \
            --window-size 600 300 \
            --icon-size 100 \
            --icon "deck" 175 120 \
            --hide-extension "deck" \
            --app-drop-link 425 120 \
            "$DMG_PATH" \
            "$TEMP_DMG_DIR" 2>/dev/null || {
                echo "⚠️  DMG创建失败，可能需要macOS特定工具"
            }
        
        rm -rf "$TEMP_DMG_DIR"
        
        if [[ -f "$DMG_PATH" ]]; then
            DMG_SIZE=$(du -m "$DMG_PATH" | cut -f1)
            echo "📦 创建DMG包: $DMG_PATH (${DMG_SIZE} MB)"
        fi
    else
        # 创建 DEB 和 RPM 包
        DEB_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM.deb"
        RPM_PATH="$OUTPUT_DIR/deck-v$VERSION-$PLATFORM.rpm"
        
        echo "🔨 创建 $PLATFORM DEB 包..."
        
        # 检查dpkg-deb是否存在
        if command -v dpkg-deb >/dev/null 2>&1; then
            # 创建 DEB 包结构
            DEB_DIR="$OUTPUT_DIR/deb-temp"
            mkdir -p "$DEB_DIR/usr/local/bin"
            mkdir -p "$DEB_DIR/DEBIAN"
            
            cp "$PLATFORM_OUTPUT_DIR/Deck.Console" "$DEB_DIR/usr/local/bin/deck"
            
            # 创建基础的control文件
            if [[ -f "packaging/linux/DEBIAN/control" ]]; then
                cp "packaging/linux/DEBIAN/control" "$DEB_DIR/DEBIAN/"
                # 更新版本信息
                sed -i.bak "s/{{VERSION}}/$VERSION/g" "$DEB_DIR/DEBIAN/control"
                sed -i.bak "s/{{ARCHITECTURE}}/$(dpkg --print-architecture 2>/dev/null || echo 'amd64')/g" "$DEB_DIR/DEBIAN/control"
                rm "$DEB_DIR/DEBIAN/control.bak" 2>/dev/null || true
            else
                # 创建基础control文件
                cat > "$DEB_DIR/DEBIAN/control" << EOF
Package: deck
Version: $VERSION
Section: base
Priority: optional
Architecture: amd64
Maintainer: Deck Team <deck@example.com>
Description: 容器化开发环境构建工具
 Deck - 甲板，容器化开发环境构建工具，模板复用，助力开发快速起步
EOF
            fi
            
            dpkg-deb --build "$DEB_DIR" "$DEB_PATH"
            rm -rf "$DEB_DIR"
            
            if [[ -f "$DEB_PATH" ]]; then
                DEB_SIZE=$(du -m "$DEB_PATH" | cut -f1)
                echo "📦 创建DEB包: $DEB_PATH (${DEB_SIZE} MB)"
            fi
        else
            echo "⚠️  dpkg-deb 未安装，跳过DEB包创建"
        fi
        
        # 创建RPM包
        if command -v rpmbuild >/dev/null 2>&1 && [[ -f "packaging/linux/rpm/deck.spec" ]]; then
            echo "🔨 创建 $PLATFORM RPM 包..."
            rpmbuild -bb packaging/linux/rpm/deck.spec \
                --define "_version $VERSION" \
                --define "_sourcedir $PLATFORM_OUTPUT_DIR" \
                --define "_rpmdir $OUTPUT_DIR" || {
                    echo "⚠️  RPM包创建失败"
                }
        else
            echo "⚠️  rpmbuild 未安装或spec文件不存在，跳过RPM包创建"
        fi
    fi
done

echo "🎉 Unix 构建完成!"
echo "📁 输出目录: $OUTPUT_DIR"
find "$OUTPUT_DIR" -maxdepth 2 -type f \( -name "*.dmg" -o -name "*.deb" -o -name "*.rpm" -o -name "Deck.Console" \) -exec ls -lh {} \;