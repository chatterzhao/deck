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
    echo "🍎 检测到 macOS 环境，将创建 macOS 分发包"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORMS=("linux-x64" "linux-arm64")
    OS_NAME="linux"
    DIST_SUBDIR="$DIST_DIR/linux"
    echo "🐧 检测到 Linux 环境，将创建 Linux 分发包"
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
        # 创建 PKG 包
        if [[ "$PLATFORM" == "macos-x64" ]]; then
            PKG_NAME="deck-v$VERSION-intel.pkg"
        else
            PKG_NAME="deck-v$VERSION-apple-silicon.pkg"  
        fi
        PKG_PATH="$DIST_SUBDIR/$PKG_NAME"
        echo "🔨 创建 $PLATFORM PKG 包..."
        
        # 创建PKG包目录结构
        PKG_BUILD_DIR="$DIST_DIR/.pkg-temp-$PLATFORM"
        PKG_ROOT_DIR="$PKG_BUILD_DIR/root"
        PKG_SCRIPTS_DIR="$PKG_BUILD_DIR/scripts"
        
        rm -rf "$PKG_BUILD_DIR"
        mkdir -p "$PKG_ROOT_DIR/usr/local/bin"
        mkdir -p "$PKG_SCRIPTS_DIR"
        
        # 复制可执行文件到PKG根目录
        cp "$PLATFORM_BUILD_DIR/Deck.Console" "$PKG_ROOT_DIR/usr/local/bin/deck"
        chmod +x "$PKG_ROOT_DIR/usr/local/bin/deck"
        
        # 创建postinstall脚本
        cat > "$PKG_SCRIPTS_DIR/postinstall" << 'PKGEOF'
#!/bin/bash

set -e

INSTALL_PATH="/usr/local/bin"
BINARY_NAME="deck"

echo "🚀 配置 Deck 环境..."

# 确保二进制文件有执行权限
chmod +x "$INSTALL_PATH/$BINARY_NAME"

# 验证安装
if command -v deck >/dev/null 2>&1; then
    echo "✅ Deck 安装成功!"
    deck --version
else
    echo "⚠️  Deck 已安装，但可能需要重新启动终端或运行以下命令:"
    echo "   source ~/.zshrc  # 对于 zsh 用户"
    echo "   source ~/.bashrc # 对于 bash 用户"
    echo ""
    echo "或者手动运行: /usr/local/bin/deck --version"
fi

echo ""
echo "🎉 Deck 安装完成!"
echo "💡 如果 'deck' 命令不可用，请重新启动终端"
echo "   或检查 /usr/local/bin 是否在您的 PATH 环境变量中"

exit 0
PKGEOF
        chmod +x "$PKG_SCRIPTS_DIR/postinstall"
        
        # 创建卸载脚本到用户目录
        cat > "$PKG_ROOT_DIR/usr/local/bin/deck-uninstall" << 'UNINSTALLEOF'
#!/bin/bash

echo "🗑️  卸载 Deck..."

# 删除主程序
if [[ -f "/usr/local/bin/deck" ]]; then
    sudo rm /usr/local/bin/deck
    echo "✅ 已删除 /usr/local/bin/deck"
fi

# 删除卸载脚本自身
if [[ -f "/usr/local/bin/deck-uninstall" ]]; then
    sudo rm /usr/local/bin/deck-uninstall
    echo "✅ 已删除卸载脚本"
fi

# 忘记包记录
sudo pkgutil --forget com.deck.deck 2>/dev/null && echo "✅ 已清理包记录" || echo "⚠️  包记录清理失败（可能已清理）"

echo ""
echo "🎉 Deck 卸载完成!"

UNINSTALLEOF

        chmod +x "$PKG_ROOT_DIR/usr/local/bin/deck-uninstall"

        # 创建preinstall脚本（清理旧版本）
        cat > "$PKG_SCRIPTS_DIR/preinstall" << 'PKGEOF'
#!/bin/bash

set -e

INSTALL_PATH="/usr/local/bin"
BINARY_NAME="deck"

echo "🔍 检查现有 Deck 安装..."

# 如果存在旧版本，备份它
if [[ -f "$INSTALL_PATH/$BINARY_NAME" ]]; then
    echo "📦 发现现有版本，创建备份..."
    cp "$INSTALL_PATH/$BINARY_NAME" "$INSTALL_PATH/${BINARY_NAME}.backup.$(date +%Y%m%d%H%M%S)"
fi

exit 0
PKGEOF

        chmod +x "$PKG_SCRIPTS_DIR/preinstall"
        
        # 创建PKG包
        BUILD_NUMBER=${BUILD_NUMBER:-$(date +"%Y%m%d%H%M%S")}
        PACKAGE_ID="com.deck.deck"
        
        pkgbuild \
            --root "$PKG_ROOT_DIR" \
            --scripts "$PKG_SCRIPTS_DIR" \
            --identifier "$PACKAGE_ID" \
            --version "$VERSION" \
            --install-location "/" \
            "$PKG_PATH"
        
        # 清理临时目录
        rm -rf "$PKG_BUILD_DIR"
        
        if [[ -f "$PKG_PATH" ]]; then
            PKG_SIZE=$(du -m "$PKG_PATH" | cut -f1)
            echo "📦 创建PKG包: $PKG_PATH (${PKG_SIZE} MB)"
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
            echo "ℹ️  注意: 请确保 $INSTALL_DIR 在您的 PATH 环境变量中" 
            echo "   如果 'deck' 命令不可用，请将以下行添加到您的 shell 配置文件 ($SHELL_CONFIG):"
            echo "   export PATH=\"\$HOME/.local/bin:\$PATH\""
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
Comment=搭建容器化开发环境的命令行工具
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
rm -rf "$DIST_DIR"/.pkg-temp-* "$DIST_DIR"/.deb-temp

if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "🎉 AOT优化分发包构建完成!"
else
    echo "🎉 分发包构建完成!"
fi
echo "📁 分发目录: $DIST_SUBDIR"
echo ""
echo "📦 创建的安装包:"
find "$DIST_SUBDIR" -maxdepth 1 -type f \( -name "*.pkg" -o -name "*.tar.gz" \) -exec ls -lh {} \;

echo ""
echo "💡 提示："
if [[ "$ENABLE_AOT" == "true" ]]; then
    echo "  🔥 AOT优化分发包已完成"
    echo "  ⚡ 如需快速构建，请使用: $0 --no-aot"
else
    echo "  ⚡ 快速分发包已完成"
    echo "  🔥 生产环境推荐使用AOT优化: $0"
fi