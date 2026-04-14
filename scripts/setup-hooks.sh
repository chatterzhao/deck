#!/bin/bash
# 新建文件
#!/bin/bash

set -euo pipefail

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🔧 设置 Git hooks...${NC}"

# 获取当前目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 创建 hooks 目录（如果不存在）
mkdir -p "$PROJECT_ROOT/.git/hooks"

# 设置 pre-commit hook
cat > "$PROJECT_ROOT/.git/hooks/pre-commit" << 'EOF'
#!/bin/bash

# pre-commit 不再执行版本检查
echo -e "${GREEN}✅ pre-commit 检查通过（无需版本验证）${NC}"
EOF

# 设置 pre-push hook
cat > "$PROJECT_ROOT/.git/hooks/pre-push" << 'EOF'
#!/bin/bash

# 动态获取项目根目录
PROJECT_ROOT="$(git rev-parse --show-toplevel)"

# 检测 tag 推送并自动处理版本更新
while read local_ref local_sha remote_ref remote_sha; do
    if [[ "$remote_ref" =~ refs/tags/ ]]; then
        TAG_NAME="${remote_ref#refs/tags/}"
        
        echo -e "${YELLOW}🔍 检测到 tag 推送: $TAG_NAME${NC}"
        
        # 验证版本一致性
        echo -e "${YELLOW}🔍 验证版本一致性...${NC}"
        if ! "$PROJECT_ROOT/scripts/validate-version.sh" "v${TAG_NAME#v}"; then
            echo -e "${YELLOW}🔄 版本不一致，自动更新版本...${NC}"
            
            # 更新版本号
            echo -e "${YELLOW}📝 更新项目版本到 $TAG_NAME...${NC}"
            "$PROJECT_ROOT/scripts/update-version.sh" "${TAG_NAME#v}"
            
            # 自动提交版本更新
            echo -e "${YELLOW}💾 自动提交版本更新...${NC}"
            git add .
            git commit -m "chore: auto-update version to $TAG_NAME" --no-verify
            
            # 推送版本更新到所有远程仓库
            echo -e "${YELLOW}📤 推送版本更新到远程仓库...${NC}"
            if ! git push --no-verify all; then
                echo -e "${YELLOW}⚠️  git push all 失败，尝试使用 git push...${NC}"
                if ! git push --no-verify; then
                    echo -e "${RED}❌ 无法推送版本更新到远程仓库${NC}"
                    exit 1
                fi
            fi
            
            # 删除旧的 tag 并基于新提交创建新 tag
            echo -e "${YELLOW}🏷️  删除旧 tag 并创建指向新提交的新 tag...${NC}"
            git tag -d "$TAG_NAME" 2>/dev/null || true
            git tag "$TAG_NAME"
            
            # 推送新 tag 到所有远程仓库
            echo -e "${YELLOW}📤 推送更新后的 tag 到远程仓库...${NC}"
            if ! git push -f --no-verify all "$TAG_NAME"; then
                echo -e "${YELLOW}⚠️  git push all tag 失败，尝试使用 git push...${NC}"
                if ! git push -f --no-verify "$TAG_NAME"; then
                    echo -e "${RED}❌ 无法推送更新后的 tag 到远程仓库${NC}"
                    exit 1
                fi
            fi
            
            echo -e "${GREEN}✅ 版本已更新，tag 已更新并指向更新后的提交${NC}"
            echo -e "${YELLOW}💡 阻止原始 tag 推送操作，使用更新后的 tag${NC}"
            exit 0
        fi
        
        echo -e "${GREEN}✅ Tag 推送完成${NC}"
    fi
done
EOF

# 设置执行权限
chmod +x "$PROJECT_ROOT/.git/hooks/pre-commit"
chmod +x "$PROJECT_ROOT/.git/hooks/pre-push"

echo -e "${GREEN}✅ Git hooks 设置完成!${NC}"