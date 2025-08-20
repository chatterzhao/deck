#!/bin/bash

set -euo pipefail

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🔍 验证版本一致性...${NC}"

# 获取当前目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 切换到项目根目录
cd "$PROJECT_ROOT"

# 获取最新的 Git tag 作为期望版本
EXPECTED_VERSION=$(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//' || echo "1.0.0")
echo -e "🏷️  最新的 Git tag 版本: v$EXPECTED_VERSION"

# 从 Directory.Build.props 提取当前配置的版本
CONFIGURED_VERSION=$(grep -o '<Version>[^<]*</Version>' Directory.Build.props | sed 's/<Version>\(.*\)<\/Version>/\1/')
echo -e "⚙️  Directory.Build.props 配置版本: $CONFIGURED_VERSION"

# 检查版本是否匹配
if [[ "$EXPECTED_VERSION" != "$CONFIGURED_VERSION" ]]; then
    echo -e "${RED}❌ 版本不匹配!${NC}"
    echo -e "${RED}   Git tag 版本: $EXPECTED_VERSION${NC}"
    echo -e "${RED}   配置文件版本: $CONFIGURED_VERSION${NC}"
    echo -e "${YELLOW}💡 请更新 Directory.Build.props 中的版本号与最新的 Git tag 保持一致${NC}"
    exit 1
else
    echo -e "${GREEN}✅ 版本匹配!${NC}"
fi

# 如果有新的 tag 参数传入，验证它是否比现有版本大
if [[ $# -gt 0 ]]; then
    NEW_VERSION="$1"
    echo -e "🆕 检查新版本: $NEW_VERSION"
    
    # 移除可能的 'v' 前缀
    NEW_VERSION_CLEAN="${NEW_VERSION#v}"
    
    # 比较版本号
    if [[ "$(printf '%s\n' "$EXPECTED_VERSION" "$NEW_VERSION_CLEAN" | sort -V | head -n1)" != "$EXPECTED_VERSION" ]] || [[ "$EXPECTED_VERSION" == "$NEW_VERSION_CLEAN" ]]; then
        echo -e "${GREEN}✅ 新版本 $NEW_VERSION_CLEAN 大于或等于当前版本 $EXPECTED_VERSION${NC}"
    else
        echo -e "${RED}❌ 新版本 $NEW_VERSION_CLEAN 小于当前版本 $EXPECTED_VERSION${NC}"
        echo -e "${YELLOW}💡 Git tag 版本应该比最新的 tag 版本大${NC}"
        exit 1
    fi
fi

echo -e "${GREEN}🎉 版本验证通过!${NC}"