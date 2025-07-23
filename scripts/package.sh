#!/bin/bash

set -euo pipefail

# 默认参数
CONFIGURATION="Release"
VERSION="1.0.0"
ENABLE_AOT="true"  # 生产打包默认启用AOT

# 参数解析
while [[ $# -gt 0 ]]; do
    case $1 in
        --configuration|-c)
            CONFIGURATION="$2"
            shift 2
            ;;
        --version)
            VERSION="$2"
            shift 2
            ;;
        --no-aot)
            ENABLE_AOT="false"
            shift
            ;;
        --help|-h)
            echo "用法: $0 [选项]"
            echo "选项:"
            echo "  --configuration CONFIG   设置配置 (默认: Release)"
            echo "  --version VERSION        设置版本号 (默认: 1.0.0)"
            echo "  --no-aot                禁用AOT编译 (生产模式默认启用)"
            echo "  --help                  显示此帮助信息"
            echo ""
            echo "说明:"
            echo "  • 每次运行会自动清理分发目录，确保输出干净"
            echo ""
            echo "示例:"
            echo "  $0                      # 生产打包，AOT优化，自动清理"
            echo "  $0 --no-aot             # 生产打包，快速构建"
            echo "  $0 --version 1.2.3      # 指定版本号"
            exit 0
            ;;
        *)
            # 向后兼容：位置参数
            if [[ -z "${CONFIG_SET:-}" ]]; then
                CONFIGURATION="$1"
                CONFIG_SET="true"
            elif [[ -z "${VERSION_SET:-}" ]]; then
                VERSION="$1"
                VERSION_SET="true"
            else
                echo "❌ 未知参数: $1"
                echo "使用 --help 查看帮助信息"
                exit 1
            fi
            shift
            ;;
    esac
done

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

# 清理并创建分发目录（默认清理）
echo "🧹 清理分发目录..."
rm -rf "$DIST_SUBDIR"
mkdir -p "$DIST_SUBDIR"

# 重新构建以确保使用正确的编译模式
echo "🔨 重新构建以确保编译模式正确..."
if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "🔥 使用AOT编译进行构建..."
    ./scripts/build.sh --version "$VERSION" --configuration "$CONFIGURATION" --aot
else
    echo "⚡ 使用标准编译进行构建..."
    ./scripts/build.sh --version "$VERSION" --configuration "$CONFIGURATION"
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
        
        # 创建 macOS .app 应用程序束
        APP_NAME="Deck.app"
        APP_DIR="$TEMP_DMG_DIR/$APP_NAME"
        CONTENTS_DIR="$APP_DIR/Contents"
        MACOS_DIR="$CONTENTS_DIR/MacOS"
        RESOURCES_DIR="$CONTENTS_DIR/Resources"
        
        # 创建应用程序目录结构
        mkdir -p "$MACOS_DIR"
        mkdir -p "$RESOURCES_DIR"
        
        # 创建 Info.plist
        cat > "$CONTENTS_DIR/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>Deck</string>
    <key>CFBundleIdentifier</key>
    <string>com.deck.developer-tools</string>
    <key>CFBundleName</key>
    <string>Deck</string>
    <key>CFBundleDisplayName</key>
    <string>Deck 开发工具</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>DECK</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.developer-tools</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
</dict>
</plist>
EOF

        # 复制实际的二进制文件到 Resources
        cp "$PLATFORM_BUILD_DIR/Deck.Console" "$RESOURCES_DIR/deck-binary"
        chmod +x "$RESOURCES_DIR/deck-binary"
        
        # 创建主执行文件
        cat > "$MACOS_DIR/Deck" << 'EOF'
#!/bin/bash

# 获取应用程序路径
APP_DIR="$(dirname "$(dirname "$(realpath "$0")")")"
RESOURCES_DIR="$APP_DIR/Resources"
DECK_BINARY="$RESOURCES_DIR/deck-binary"

# 检查是否是在 Applications 中运行
if [[ "$APP_DIR" == "/Applications/Deck.app/Contents" ]]; then
    APP_IN_APPLICATIONS=true
    CONFIG_FILE="/Applications/.deck-configured"
else
    APP_IN_APPLICATIONS=false
    CONFIG_FILE="$(dirname "$APP_DIR")/.deck-configured"
fi

# 检查是否是首次运行
if [[ ! -f "$CONFIG_FILE" ]]; then
    # 打开终端窗口显示配置界面
    osascript << APPLESCRIPT
tell application "Terminal"
    activate
    do script "
echo '🚀 欢迎使用 Deck 开发工具!'
echo '========================='
echo ''
echo '正在进行初始化配置...'
echo ''

# 尝试配置命令行访问
echo '📦 正在配置命令行访问...'
if sudo ln -sf '$DECK_BINARY' /usr/local/bin/deck 2>/dev/null; then
    echo '✅ 命令行配置成功!'
    echo ''
    echo '🎉 安装完成!'
    echo ''
    echo '现在您可以：'
    echo '• 在 VS Code 终端中使用: deck --help'
    echo '• 在任何终端中使用: deck start python'
    echo '• 在启动台中双击此应用图标直接运行'
    echo ''
    echo '💡 这是一个终端工具，主要在命令行中使用。'
else
    echo '⚠️  需要管理员权限配置命令行访问'
    echo ''
    echo '请手动运行以下命令完成配置:'
    echo 'sudo ln -sf $DECK_BINARY /usr/local/bin/deck'
    echo ''
    echo '配置完成后，您就可以在 VS Code 等终端中使用 deck 命令了。'
    echo ''
    echo '💡 或者您也可以直接在启动台双击此应用使用。'
fi

echo ''
echo '📚 获取更多帮助:'
echo '• GitHub:  https://github.com/your-org/deck'
echo '• Gitee:   https://gitee.com/your-org/deck'
echo '• 使用指南: https://github.com/your-org/deck/wiki'
echo ''
echo '💡 提示: 复制上面的链接到浏览器查看详细使用方法'
echo ''
read -p '按回车键关闭...'

# 标记为已配置
touch '$CONFIG_FILE'
exit 0
"
end tell
APPLESCRIPT
else
    # 已配置，直接在终端中运行deck
    osascript << APPLESCRIPT
tell application "Terminal"
    activate
    do script "$DECK_BINARY"
end tell
APPLESCRIPT
fi
EOF
        
        # 设置执行权限
        chmod +x "$MACOS_DIR/Deck"
        
        # 删除已存在的DMG文件以避免冲突
        rm -f "$DMG_PATH"
        
        # 使用 create-dmg 创建 DMG
        create-dmg \
            --volname "Deck v$VERSION" \
            --window-pos 200 120 \
            --window-size 600 300 \
            --icon-size 100 \
            --icon "$APP_NAME" 175 120 \
            --hide-extension "$APP_NAME" \
            --app-drop-link 425 120 \
            "$DMG_PATH" \
            "$TEMP_DMG_DIR" 2>/dev/null || {
                echo "⚠️  DMG创建失败，可能需要macOS特定工具"
            }
        
        # 清理临时目录
        rm -rf "$TEMP_DMG_DIR"
        
        # 清理create-dmg产生的临时文件（模式: rw.*.dmg）
        find "$DIST_SUBDIR" -name "rw.*.dmg" -delete 2>/dev/null || true
        
        if [[ -f "$DMG_PATH" ]]; then
            DMG_SIZE=$(du -m "$DMG_PATH" | cut -f1)
            echo "📦 创建DMG包: $DMG_PATH (${DMG_SIZE} MB)"
        fi
    else
        # 创建 Linux 安装包
        if [[ "$PLATFORM" == "linux-x64" ]]; then
            ARCH_SUFFIX="x64"
        else
            ARCH_SUFFIX="arm64"
        fi
        
        INSTALLER_DIR="$DIST_SUBDIR/Deck-Installer-$ARCH_SUFFIX"
        mkdir -p "$INSTALLER_DIR"
        
        echo "🔨 创建 $PLATFORM 安装程序..."
        
        # 复制主程序
        cp "$PLATFORM_BUILD_DIR/Deck.Console" "$INSTALLER_DIR/deck-binary"
        chmod +x "$INSTALLER_DIR/deck-binary"
        
        # 创建安装脚本
        cat > "$INSTALLER_DIR/install.sh" << 'EOF'
#!/bin/bash

set -e

echo ""
echo "🚀 欢迎使用 Deck 开发工具!"
echo "========================="
echo ""
echo "正在进行初始化配置..."
echo ""

# 获取脚本目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DECK_BINARY="$SCRIPT_DIR/deck-binary"
INSTALL_DIR="$HOME/.local/bin"
INSTALLED_BINARY="$INSTALL_DIR/deck"
CONFIG_FILE="$HOME/.local/share/deck/.deck-configured"

# 检查是否首次运行
if [[ ! -f "$CONFIG_FILE" ]]; then
    # 创建安装目录
    mkdir -p "$INSTALL_DIR"
    mkdir -p "$(dirname "$CONFIG_FILE")"
    
    # 复制程序到用户目录
    cp "$DECK_BINARY" "$INSTALLED_BINARY"
    chmod +x "$INSTALLED_BINARY"
    
    echo "📦 正在配置环境变量..."
    
    # 检查是否已在PATH中
    if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
        # 添加到shell配置文件
        SHELL_CONFIG=""
        if [[ -n "$ZSH_VERSION" ]]; then
            SHELL_CONFIG="$HOME/.zshrc"
        elif [[ -n "$BASH_VERSION" ]]; then
            SHELL_CONFIG="$HOME/.bashrc"
        else
            # 尝试检测默认shell
            if [[ "$SHELL" == */zsh ]]; then
                SHELL_CONFIG="$HOME/.zshrc"
            elif [[ "$SHELL" == */bash ]]; then
                SHELL_CONFIG="$HOME/.bashrc"
            else
                SHELL_CONFIG="$HOME/.profile"
            fi
        fi
        
        if [[ -n "$SHELL_CONFIG" ]]; then
            echo "" >> "$SHELL_CONFIG"
            echo "# Added by Deck installer" >> "$SHELL_CONFIG"
            echo "export PATH=\"\$HOME/.local/bin:\$PATH\"" >> "$SHELL_CONFIG"
            echo "✅ 环境变量配置成功! (添加到 $SHELL_CONFIG)"
        else
            echo "⚠️  请手动添加 $INSTALL_DIR 到 PATH 环境变量"
        fi
        
        # 为当前会话更新PATH
        export PATH="$INSTALL_DIR:$PATH"
    else
        echo "✅ 环境变量已存在!"
    fi
    
    echo ""
    echo "📦 创建桌面快捷方式..."
    
    # 创建桌面应用文件
    DESKTOP_FILE="$HOME/.local/share/applications/deck.desktop"
    mkdir -p "$(dirname "$DESKTOP_FILE")"
    cat > "$DESKTOP_FILE" << DESKTOP_EOF
[Desktop Entry]
Name=Deck 开发工具
Comment=开发环境容器化工具
Exec=$INSTALLED_BINARY
Icon=terminal
Terminal=true
Type=Application
Categories=Development;
StartupNotify=false
DESKTOP_EOF
    
    echo "✅ 桌面快捷方式创建成功!"
    echo ""
    echo "🎉 安装完成!"
    echo ""
    echo "现在您可以："
    echo "• 在 VS Code 终端中使用: deck --help"
    echo "• 在任何终端中使用: deck start python"
    echo "• 在应用程序菜单中找到 Deck 开发工具"
    echo ""
    echo "💡 这是一个终端工具，主要在命令行中使用。"
    echo ""
    echo "📚 获取更多帮助:"
    echo "• GitHub:  https://github.com/your-org/deck"
    echo "• Gitee:   https://gitee.com/your-org/deck"
    echo "• 使用指南: https://github.com/your-org/deck/wiki"
    echo ""
    echo "💡 提示: 复制上面的链接到浏览器查看详细使用方法"
    echo ""
    echo "注意: 您可能需要重新打开终端窗口以使环境变量生效"
    echo ""
    
    # 标记为已配置
    touch "$CONFIG_FILE"
    
    read -p "按回车键关闭..."
    exit 0
fi

# 后续运行：直接执行deck功能
exec "$INSTALLED_BINARY" "$@"
EOF
        
        chmod +x "$INSTALLER_DIR/install.sh"
        
        # 创建 TAR.GZ 包
        TAR_NAME="Deck-v$VERSION-linux-$ARCH_SUFFIX.tar.gz"
        TAR_PATH="$DIST_SUBDIR/$TAR_NAME"
        
        if tar -czf "$TAR_PATH" -C "$DIST_SUBDIR" "$(basename "$INSTALLER_DIR")" 2>/dev/null; then
            if [[ -f "$TAR_PATH" ]]; then
                TAR_SIZE=$(du -m "$TAR_PATH" | cut -f1)
                echo "📦 创建TAR.GZ包: $TAR_PATH (${TAR_SIZE} MB)"
            fi
        else
            echo "⚠️  $PLATFORM TAR.GZ 创建失败"
        fi
    fi
done

# 清理临时文件
rm -rf "$DIST_DIR"/.dmg-temp "$DIST_DIR"/.deb-temp

# 最终清理create-dmg产生的临时文件
find "$DIST_SUBDIR" -name "rw.*.dmg" -delete 2>/dev/null || true

if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "🎉 AOT优化分发包构建完成!"
else
    echo "🎉 分发包构建完成!"
fi
echo "📁 分发目录: $DIST_SUBDIR"
echo ""
echo "📦 创建的安装包:"
find "$DIST_SUBDIR" -maxdepth 1 -type f \( -name "*.dmg" -o -name "*.tar.gz" \) -exec ls -lh {} \;

echo ""
echo "💡 提示："
if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "  🔥 AOT优化分发包已完成"
    echo "  ⚡ 如需快速构建，请使用: $0 --no-aot"
else
    echo "  ⚡ 快速分发包已完成"
    echo "  🔥 生产环境推荐使用AOT优化: $0"
fi