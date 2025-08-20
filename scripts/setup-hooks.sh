// 新建文件
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

# 检测 tag 推送并更新版本
while read local_ref local_sha remote_ref remote_sha; do
    if [[ "$remote_ref" =~ refs/tags/ ]]; then
        TAG_NAME="${remote_ref#refs/tags/}"
        
        # 验证版本一致性
        echo -e "${YELLOW}🔍 验证版本一致性...${NC}"
        "$PROJECT_ROOT/scripts/validate-version.sh"
        
        # 更新版本号
        echo -e "${YELLOW}🔄 更新项目版本到 $TAG_NAME...${NC}"
        "$PROJECT_ROOT/scripts/update-version.sh" "$TAG_NAME"
        
        # 自动提交版本更新
        echo -e "${YELLOW}💾 自动提交版本更新...${NC}"
        git add .
        git commit -m "chore: auto-update version to $TAG_NAME" --no-verify
    fi
done
EOF

# 设置执行权限
chmod +x "$PROJECT_ROOT/.git/hooks/pre-commit"
chmod +x "$PROJECT_ROOT/.git/hooks/pre-push"

echo -e "${GREEN}✅ Git hooks 设置完成!${NC}"
