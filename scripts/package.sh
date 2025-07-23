#!/bin/bash

set -euo pipefail

# 参数解析
CONFIGURATION="${1:-Release}"
VERSION="${2:-1.0.0}"
CLEAN="${3:-false}"

echo "🚀 开始构建 Deck 分发包..."

# 切换到项目根目录
cd "$(dirname "$0")/.."

# 设置变量
PROJECT_PATH="src/Deck.Console/Deck.Console.csproj"
DIST_DIR="dist"
BUILD_DIR="build/release"

# 检测平台和设置分发目录
if [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORMS=("macos-x64" "macos-arm64")
    OS_NAME="macos"
    DIST_SUBDIR="$DIST_DIR/macos"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORMS=("linux-x64" "linux-arm64")
    OS_NAME="linux"
    DIST_SUBDIR="$DIST_DIR/linux"
else
    echo "❌ 不支持的操作系统: $OSTYPE"
    exit 1
fi

# 创建分发目录
mkdir -p "$DIST_SUBDIR"

# 检查是否已有构建文件
if [[ ! -d "$BUILD_DIR" ]]; then
    echo "⚠️  未找到构建文件，先运行构建..."
    ./scripts/build.sh "$VERSION" "$CONFIGURATION"
fi

# 创建标准平台包
echo "📦 从构建文件创建分发包..."

for PLATFORM in "${PLATFORMS[@]}"; do
    PLATFORM_BUILD_DIR="$BUILD_DIR/$PLATFORM"
    
    if [[ ! -d "$PLATFORM_BUILD_DIR" ]]; then
        echo "❌ 未找到平台构建: $PLATFORM_BUILD_DIR"
        continue
    fi
    
    if [[ "$OS_NAME" == "macos" ]]; then
        # 创建 DMG 包
        if [[ "$PLATFORM" == "macos-x64" ]]; then
            DMG_NAME="deck-v$VERSION-intel.dmg"
        else
            DMG_NAME="deck-v$VERSION-apple-silicon.dmg"  
        fi
        DMG_PATH="$DIST_SUBDIR/$DMG_NAME"
        echo "🔨 创建 $PLATFORM DMG 包..."
        
        # 检查create-dmg是否安装
        if ! command -v create-dmg >/dev/null 2>&1; then
            echo "⚠️  create-dmg 未安装，跳过DMG包创建"
            echo "   安装方法: brew install create-dmg"
            continue
        fi
        
        # 创建临时目录结构
        TEMP_DMG_DIR="$DIST_DIR/.dmg-temp"
        mkdir -p "$TEMP_DMG_DIR"
        cp "$PLATFORM_BUILD_DIR/Deck.Console" "$TEMP_DMG_DIR/deck"
        
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
        if [[ "$PLATFORM" == "linux-x64" ]]; then
            ARCH_SUFFIX="amd64"
        else
            ARCH_SUFFIX="arm64"
        fi
        
        DEB_NAME="deck-v$VERSION-$ARCH_SUFFIX.deb"
        RPM_NAME="deck-v$VERSION-$ARCH_SUFFIX.rpm"
        DEB_PATH="$DIST_SUBDIR/$DEB_NAME"
        RPM_PATH="$DIST_SUBDIR/$RPM_NAME"
        
        echo "🔨 创建 $PLATFORM DEB 包..."
        
        # 检查dpkg-deb是否存在
        if command -v dpkg-deb >/dev/null 2>&1; then
            # 创建 DEB 包结构
            DEB_DIR="$DIST_DIR/.deb-temp"
            mkdir -p "$DEB_DIR/usr/local/bin"
            mkdir -p "$DEB_DIR/DEBIAN"
            
            cp "$PLATFORM_BUILD_DIR/Deck.Console" "$DEB_DIR/usr/local/bin/deck"
            
            # 创建基础的control文件
            if [[ -f "scripts/packaging/linux/DEBIAN/control" ]]; then
                cp "scripts/packaging/linux/DEBIAN/control" "$DEB_DIR/DEBIAN/"
                # 更新版本信息
                sed -i.bak "s/{{VERSION}}/$VERSION/g" "$DEB_DIR/DEBIAN/control"
                sed -i.bak "s/{{ARCHITECTURE}}/$(dpkg --print-architecture 2>/dev/null || echo 'amd64')/g" "$DEB_DIR/DEBIAN/control"
                rm "$DEB_DIR/DEBIAN/control.bak" 2>/dev/null || true
            fi
            
            # 复制安装后脚本
            if [[ -f "scripts/packaging/linux/DEBIAN/postinst" ]]; then
                cp "scripts/packaging/linux/DEBIAN/postinst" "$DEB_DIR/DEBIAN/"
                chmod +x "$DEB_DIR/DEBIAN/postinst"
            fi
            
            # 复制卸载前脚本
            if [[ -f "scripts/packaging/linux/DEBIAN/prerm" ]]; then
                cp "scripts/packaging/linux/DEBIAN/prerm" "$DEB_DIR/DEBIAN/"
                chmod +x "$DEB_DIR/DEBIAN/prerm"
            fi
            
            # 如果没有配置文件，创建基础control文件
            if [[ ! -f "scripts/packaging/linux/DEBIAN/control" ]]; then
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
        if command -v rpmbuild >/dev/null 2>&1 && [[ -f "scripts/packaging/linux/rpm/deck.spec" ]]; then
            echo "🔨 创建 $PLATFORM RPM 包..."
            rpmbuild -bb scripts/packaging/linux/rpm/deck.spec \
                --define "_version $VERSION" \
                --define "_sourcedir $PLATFORM_BUILD_DIR" \
                --define "_rpmdir $DIST_SUBDIR" || {
                    echo "⚠️  RPM包创建失败"
                }
        else
            echo "⚠️  rpmbuild 未安装或spec文件不存在，跳过RPM包创建"
        fi
    fi
done

# 清理临时文件
rm -rf "$DIST_DIR"/.dmg-temp "$DIST_DIR"/.deb-temp

echo "🎉 分发包构建完成!"
echo "📁 分发目录: $DIST_SUBDIR"
echo ""
echo "📦 创建的安装包:"
find "$DIST_SUBDIR" -maxdepth 1 -type f \( -name "*.dmg" -o -name "*.deb" -o -name "*.rpm" \) -exec ls -lh {} \;