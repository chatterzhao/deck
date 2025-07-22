#!/bin/bash

# =============================================================================
# Deck .NET Console 多平台构建脚本 (Unix Shell)
# =============================================================================

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 图标定义
BUILD="🔨 [BUILD]"
SUCCESS="✅ [SUCCESS]"
ERROR="❌ [ERROR]"
WARN="⚠️  [WARN]"
INFO="ℹ️  [INFO]"

# 获取脚本目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# 默认配置
VERSION="${VERSION:-1.0.0}"
BUILD_DATE="${BUILD_DATE:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"
BUILD_COMMIT="${BUILD_COMMIT:-$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')}"

# 构建配置
BUILD_DIR="$PROJECT_ROOT/build"
DIST_DIR="$PROJECT_ROOT/dist"
SUPPORTED_PLATFORMS=("windows" "linux" "macos")
SUPPORTED_ARCHITECTURES=("x64" "arm64")

# 默认参数
PLATFORM=""
ARCHITECTURE=""
BUILD_ALL=false
CLEAN_ONLY=false
USE_AOT=false
NO_COMPRESS=false
SHOW_HELP=false

# 日志函数
log_info() {
    echo -e "${BLUE}${BUILD} $1${NC}"
}

log_success() {
    echo -e "${GREEN}${SUCCESS} $1${NC}"
}

log_error() {
    echo -e "${RED}${ERROR} $1${NC}"
}

log_warn() {
    echo -e "${YELLOW}${WARN} $1${NC}"
}

# 显示帮助信息
show_help() {
    cat << EOF
${CYAN}Deck .NET Console 多平台构建脚本${NC}

用法: $0 [选项] [平台] [架构]

选项:
  -h, --help          显示帮助信息
  -v, --version       指定版本号 (默认: ${VERSION})
  -c, --clean         清理构建目录
  -a, --all           构建所有平台和架构
  --aot               启用AOT编译 (实验性)
  --no-compress       不压缩输出文件

支持的平台:
  windows             构建Windows版本
  linux               构建Linux版本
  macos               构建macOS版本

支持的架构:
  x64                 构建x64架构
  arm64               构建ARM64架构

示例:
  $0 -a                        # 构建所有平台和架构
  $0 linux x64                 # 构建Linux x64版本
  $0 --platform macos --aot    # 构建macOS AOT版本
  $0 -c                        # 清理构建目录

EOF
}

# 清理构建目录
clean_build() {
    log_info "清理构建目录..."
    rm -rf "$BUILD_DIR"
    rm -rf "$DIST_DIR"
    log_success "构建目录已清理"
}

# 检查依赖
check_dependencies() {
    log_info "检查构建依赖..."
    
    # 检查.NET SDK
    if ! command -v dotnet >/dev/null 2>&1; then
        log_error "未找到 .NET SDK，请先安装 .NET 9"
        return 1
    fi
    
    local dotnet_version
    dotnet_version=$(dotnet --version)
    log_info "发现 .NET SDK: $dotnet_version"
    
    # 检查项目文件
    local project_file="$PROJECT_ROOT/src/Deck.Console/Deck.Console.csproj"
    if [[ ! -f "$project_file" ]]; then
        log_error "未找到项目文件: $project_file"
        return 1
    fi
    
    # 检查必需工具
    local missing_tools=()
    if ! command -v tar >/dev/null 2>&1; then
        missing_tools+=("tar")
    fi
    if ! command -v gzip >/dev/null 2>&1; then
        missing_tools+=("gzip")
    fi
    
    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        log_error "缺少必需工具: ${missing_tools[*]}"
        return 1
    fi
    
    log_success "依赖检查通过"
    return 0
}

# 获取运行时标识符
get_runtime_id() {
    local platform="$1"
    local architecture="$2"
    
    case "$platform" in
        "windows") echo "win-$architecture" ;;
        "linux")   echo "linux-$architecture" ;;
        "macos")   echo "osx-$architecture" ;;
        *)         echo "" ;;
    esac
}

# 构建单个目标
build_target() {
    local platform="$1"
    local architecture="$2"
    local use_aot="${3:-false}"
    
    local runtime_id
    runtime_id=$(get_runtime_id "$platform" "$architecture")
    if [[ -z "$runtime_id" ]]; then
        log_error "不支持的平台组合: $platform-$architecture"
        return 1
    fi
    
    local target_name="$platform-$architecture"
    if [[ "$use_aot" == "true" ]]; then
        target_name+="-aot"
    fi
    
    log_info "构建目标: $target_name (运行时: $runtime_id)"
    
    local output_dir="$BUILD_DIR/$target_name"
    mkdir -p "$output_dir"
    
    # 构建参数
    local publish_args=(
        "publish"
        "src/Deck.Console/Deck.Console.csproj"
        "--configuration" "Release"
        "--runtime" "$runtime_id"
        "--self-contained" "true"
        "--output" "$output_dir"
    )
    
    if [[ "$use_aot" == "true" ]]; then
        publish_args+=("-p:PublishAot=true")
        log_info "启用AOT编译 (实验性功能)"
    fi
    
    # 执行构建
    if dotnet "${publish_args[@]}"; then
        log_success "构建完成: $target_name"
        return 0
    else
        if [[ "$use_aot" == "true" ]]; then
            log_warn "AOT编译失败，这在当前版本是预期的 (YamlDotNet兼容性问题)"
        else
            log_error "构建失败: $target_name"
        fi
        return 1
    fi
}

# 创建压缩包
create_archive() {
    local platform="$1"
    local architecture="$2"
    local use_aot="${3:-false}"
    
    local target_name="$platform-$architecture"
    if [[ "$use_aot" == "true" ]]; then
        target_name+="-aot"
    fi
    
    local source_dir="$BUILD_DIR/$target_name"
    if [[ ! -d "$source_dir" ]]; then
        log_warn "跳过压缩，源目录不存在: $source_dir"
        return
    fi
    
    if [[ "$NO_COMPRESS" == "true" ]]; then
        log_info "跳过压缩，使用原始目录: $target_name"
        return
    fi
    
    mkdir -p "$DIST_DIR"
    
    local archive_name="deck-v$VERSION-$target_name"
    
    # 所有平台统一使用tar.gz格式
    local archive_file="$DIST_DIR/$archive_name.tar.gz"
    
    log_info "创建压缩包: $archive_name.tar.gz"
    
    # 进入构建目录进行打包
    pushd "$BUILD_DIR" > /dev/null
    
    if tar -czf "$archive_file" "$target_name"; then
        # 计算SHA256校验和
        local hash_file="$archive_file.sha256"
        if command -v sha256sum >/dev/null 2>&1; then
            sha256sum "$(basename "$archive_file")" > "$hash_file" 2>/dev/null || true
        elif command -v shasum >/dev/null 2>&1; then
            shasum -a 256 "$(basename "$archive_file")" > "$hash_file" 2>/dev/null || true
        fi
        
        log_success "压缩包已创建: $(basename "$archive_file")"
    else
        log_warn "压缩失败: $archive_name.tar.gz"
    fi
    
    popd > /dev/null
}

# 检测当前平台
detect_current_platform() {
    case "$(uname -s)" in
        Linux*)     echo "linux" ;;
        Darwin*)    echo "macos" ;;
        CYGWIN*|MINGW*) echo "windows" ;;
        *)          echo "linux" ;;
    esac
}

# 检测当前架构
detect_current_architecture() {
    case "$(uname -m)" in
        x86_64|x64)     echo "x64" ;;
        arm64|aarch64)  echo "arm64" ;;
        *)              echo "x64" ;;
    esac
}

# 显示构建摘要
show_build_summary() {
    echo ""
    log_info "构建摘要"
    echo ""
    
    echo -e "${CYAN}版本信息:${NC}"
    echo "  版本: v$VERSION"
    echo "  构建时间: $BUILD_DATE"
    echo "  Git提交: $BUILD_COMMIT"
    echo ""
    
    if [[ -d "$BUILD_DIR" ]]; then
        echo -e "${CYAN}构建产物:${NC}"
        for dir in "$BUILD_DIR"/*; do
            if [[ -d "$dir" ]]; then
                echo -e "  ${GREEN}$(basename "$dir")${NC}"
            fi
        done
        echo ""
    fi
    
    if [[ -d "$DIST_DIR" ]]; then
        echo -e "${CYAN}发布包:${NC}"
        for file in "$DIST_DIR"/*; do
            if [[ -f "$file" && ! "$file" =~ \.sha256$ ]]; then
                local size
                if command -v du >/dev/null 2>&1; then
                    size=$(du -h "$file" | cut -f1)
                    echo -e "  ${GREEN}$(basename "$file") ($size)${NC}"
                else
                    echo -e "  ${GREEN}$(basename "$file")${NC}"
                fi
            fi
        done
        echo ""
    fi
    
    echo -e "${CYAN}后续步骤:${NC}"
    echo "  1. 测试构建产物"
    echo "  2. 发布到GitHub Releases"
    echo ""
}

# 解析命令行参数
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                SHOW_HELP=true
                shift
                ;;
            -v|--version)
                VERSION="$2"
                shift 2
                ;;
            -c|--clean)
                CLEAN_ONLY=true
                shift
                ;;
            -a|--all)
                BUILD_ALL=true
                shift
                ;;
            --aot)
                USE_AOT=true
                shift
                ;;
            --no-compress)
                NO_COMPRESS=true
                shift
                ;;
            --platform)
                PLATFORM="$2"
                shift 2
                ;;
            --architecture)
                ARCHITECTURE="$2"
                shift 2
                ;;
            windows|linux|macos)
                PLATFORM="$1"
                shift
                ;;
            x64|arm64)
                ARCHITECTURE="$1"
                shift
                ;;
            *)
                log_error "未知参数: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# 主函数
main() {
    parse_arguments "$@"
    
    if [[ "$SHOW_HELP" == "true" ]]; then
        show_help
        return 0
    fi
    
    echo ""
    echo -e "${CYAN}🚀 Deck .NET Console 构建脚本 v$VERSION${NC}"
    echo ""
    
    # 仅清理
    if [[ "$CLEAN_ONLY" == "true" ]]; then
        clean_build
        return 0
    fi
    
    # 检查依赖
    if ! check_dependencies; then
        exit 1
    fi
    
    # 清理并重新创建构建目录
    clean_build
    mkdir -p "$BUILD_DIR"
    
    # 确定构建目标
    local build_targets=()
    
    if [[ "$BUILD_ALL" == "true" ]]; then
        log_info "构建所有平台和架构..."
        for platform in "${SUPPORTED_PLATFORMS[@]}"; do
            for architecture in "${SUPPORTED_ARCHITECTURES[@]}"; do
                build_targets+=("$platform:$architecture")
            done
        done
    elif [[ -n "$PLATFORM" && -n "$ARCHITECTURE" ]]; then
        build_targets+=("$PLATFORM:$ARCHITECTURE")
    elif [[ -n "$PLATFORM" ]]; then
        for architecture in "${SUPPORTED_ARCHITECTURES[@]}"; do
            build_targets+=("$PLATFORM:$architecture")
        done
    else
        # 默认构建当前平台
        local current_platform
        local current_arch
        current_platform=$(detect_current_platform)
        current_arch=$(detect_current_architecture)
        
        log_info "构建当前平台: $current_platform-$current_arch"
        build_targets+=("$current_platform:$current_arch")
    fi
    
    # 执行构建
    local success_count=0
    local total_count=${#build_targets[@]}
    
    for target in "${build_targets[@]}"; do
        IFS=':' read -r platform architecture <<< "$target"
        
        # 标准构建
        if build_target "$platform" "$architecture" "false"; then
            ((success_count++))
            create_archive "$platform" "$architecture" "false"
        fi
        
        # AOT构建 (实验性)
        if [[ "$USE_AOT" == "true" ]]; then
            log_info "尝试AOT构建 (实验性)..."
            if build_target "$platform" "$architecture" "true"; then
                create_archive "$platform" "$architecture" "true"
            fi
        fi
    done
    
    # 显示结果
    echo ""
    if [[ $success_count -eq $total_count ]]; then
        log_success "所有构建成功完成! ($success_count/$total_count)"
    elif [[ $success_count -gt 0 ]]; then
        log_warn "部分构建成功完成 ($success_count/$total_count)"
    else
        log_error "所有构建失败"
        exit 1
    fi
    
    show_build_summary
    log_success "构建脚本执行完成!"
}

# 脚本入口点
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi