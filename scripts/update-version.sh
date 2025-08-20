#!/bin/bash

set -euo pipefail

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🔄 更新项目版本...${NC}"

# 获取当前目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 切换到项目根目录
cd "$PROJECT_ROOT"

# 获取新版本号
if [[ $# -eq 0 ]]; then
    echo -e "${RED}❌ 请提供新版本号作为参数${NC}"
    echo "例如: $0 1.2.3"
    exit 1
fi

NEW_VERSION="$1"
echo -e "🆕 设置新版本为: $NEW_VERSION"

# 移除可能的 'v' 前缀
NEW_VERSION_CLEAN="${NEW_VERSION#v}"

# 更新 Directory.Build.props 中的版本号
echo -e "${YELLOW}📝 更新 Directory.Build.props...${NC}"
sed -i.bak "s/<Version[^>]*>[^<]*<\/Version>/<Version>$NEW_VERSION_CLEAN<\/Version>/" Directory.Build.props
rm Directory.Build.props.bak

# 验证更新
UPDATED_VERSION=$(grep -o '<Version>[^<]*</Version>' Directory.Build.props | sed 's/<Version>\(.*\)<\/Version>/\1/')
if [[ "$UPDATED_VERSION" == "$NEW_VERSION_CLEAN" ]]; then
    echo -e "${GREEN}✅ Directory.Build.props 版本更新成功${NC}"
else
    echo -e "${RED}❌ Directory.Build.props 版本更新失败${NC}"
    exit 1
fi

# 更新 RPM spec 文件中的版本
echo -e "${YELLOW}📝 更新 RPM spec 文件...${NC}"
if [[ -f "scripts/packaging/linux/rpm/deck.spec" ]]; then
    # 更新 Version 字段
    sed -i.bak "s/Version:.*/Version:        $NEW_VERSION_CLEAN/" scripts/packaging/linux/rpm/deck.spec
    
    # 更新 changelog 条目 (使用简单格式避免 sed 问题)
    CURRENT_DATE=$(date "+%a %b %d %Y")
    echo "* $CURRENT_DATE Deck Team <deck@example.com> - $NEW_VERSION_CLEAN-1" >> scripts/packaging/linux/rpm/deck.spec
    echo "- Update to version $NEW_VERSION_CLEAN" >> scripts/packaging/linux/rpm/deck.spec
    echo "" >> scripts/packaging/linux/rpm/deck.spec
    
    rm scripts/packaging/linux/rpm/deck.spec.bak
    echo -e "${GREEN}✅ RPM spec 文件更新成功${NC}"
else
    echo -e "${YELLOW}⚠️  RPM spec 文件不存在，跳过更新${NC}"
fi

# 更新 README.md 中的示例文件名
echo -e "${YELLOW}📝 更新 README.md 中的示例文件名...${NC}"
if [[ -f "scripts/README.md" ]]; then
    # 更新 MSI 示例文件名
    sed -i.bak "s/deck-v{VERSION}-win-x64\.msi/deck-v$NEW_VERSION_CLEAN-win-x64.msi/g" scripts/README.md
    sed -i.bak "s/deck-v{VERSION}-win-arm64\.msi/deck-v$NEW_VERSION_CLEAN-win-arm64.msi/g" scripts/README.md
    
    # 更新 DEB 示例文件名
    sed -i.bak "s/deck-v{VERSION}-amd64\.deb/deck-v$NEW_VERSION_CLEAN-amd64.deb/g" scripts/README.md
    sed -i.bak "s/deck-v{VERSION}-arm64\.deb/deck-v$NEW_VERSION_CLEAN-arm64.deb/g" scripts/README.md
    
    # 更新 RPM 示例文件名
    sed -i.bak "s/deck-v{VERSION}-amd64\.rpm/deck-v$NEW_VERSION_CLEAN-amd64.rpm/g" scripts/README.md
    sed -i.bak "s/deck-v{VERSION}-arm64\.rpm/deck-v$NEW_VERSION_CLEAN-arm64.rpm/g" scripts/README.md
    
    # 更新 PKG 示例文件名
    sed -i.bak "s/deck-v{VERSION}-intel\.pkg/deck-v$NEW_VERSION_CLEAN-intel.pkg/g" scripts/README.md
    sed -i.bak "s/deck-v{VERSION}-apple-silicon\.pkg/deck-v$NEW_VERSION_CLEAN-apple-silicon.pkg/g" scripts/README.md
    
    rm scripts/README.md.bak
    echo -e "${GREEN}✅ README.md 示例文件名更新成功${NC}"
else
    echo -e "${YELLOW}⚠️  README.md 文件不存在，跳过更新${NC}"
fi

echo -e "${GREEN}🎉 版本更新完成!${NC}"
echo -e "${YELLOW}💡 建议执行以下操作:${NC}"
echo -e "   1. 检查修改的文件: git diff"
echo -e "   2. 提交更改: git add . && git commit -m \"chore: update version to $NEW_VERSION_CLEAN\""
echo -e "   3. 创建新 tag: git tag -a v$NEW_VERSION_CLEAN -m \"Release version $NEW_VERSION_CLEAN\""
echo -e "   4. 推送更改: git push && git push origin v$NEW_VERSION_CLEAN"