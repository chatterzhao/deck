# 贡献指南

感谢您对 Deck 项目的关注！我们欢迎任何形式的贡献。

## 如何贡献

### 报告问题

如果您发现了 Bug 或有功能建议，请通过以下方式提交：

- [Gitee Issues](https://gitee.com/zhaoquan/deck/issues)
- [GitHub Issues](https://github.com/chatterzhao/deck/issues)

提交问题时，请包含以下信息：

1. 问题的详细描述
2. 复现步骤
3. 期望行为和实际行为
4. 运行环境（操作系统、.NET 版本等）

### 提交代码

1. Fork 本仓库
2. 创建功能分支：`git checkout -b feature/my-feature`
3. 提交更改：`git commit -m 'feat: add some feature'`
4. 推送分支：`git push origin feature/my-feature`
5. 提交 Pull Request

### 开发环境搭建

```bash
# 克隆仓库
git clone https://gitee.com/zhaoquan/deck.git
# 或 git clone https://github.com/chatterzhao/deck.git
cd deck

# 安装依赖
dotnet restore

# 运行测试
dotnet test

# 运行开发版本
dotnet run --project src/Deck.Console

# 构建
./scripts/build.sh  # macOS/Linux
# 或 .\scripts\build.ps1  # Windows

# 打包
./scripts/package.sh  # macOS/Linux
# 或 .\scripts\package.ps1  # Windows
```

### 版本管理

本项目使用 Git tag 作为版本号的唯一来源，遵循 [语义化版本控制](https://semver.org/lang/zh-CN/)。

详细版本管理流程请参考 README.md 中的版本管理章节。

### GitFlow 分支管理

本项目采用 GitFlow 工作流：

```
develop          ← 开发分支（所有新功能合并到这里）
   │
   │  PR / Merge
   ▼
main             ← 生产分支（发布用的稳定代码）
   │
   │  git tag v1.x.x
   ▼
v1.x.x           ← 版本标签
```

**分支策略**：
- `develop` - 开发分支，用于集成所有新功能和修复
- `main` - 生产分支，仅包含稳定可发布的代码
- `feature/*` - 功能分支（可选，直接在 develop 开发也可）
- `v*` - 版本标签，用于发布

**发布流程**：

#### 阶段一：开发分支验证
功能开发完成后，合并到 `develop` 进行 CI 验证：

```bash
# 1. 确保在 develop 分支
git checkout develop
git merge feature/my-feature  # 合并功能分支（如果有）

# 2. 推送到远程
git push origin develop

# 3. 观察 CI 构建状态
gh run list --limit 5

# 4. 等待构建完成（约 10-15 分钟）
# - 观察是否全部 success
# - 如果失败，修复问题后重新推送
```

#### 阶段二：生产发布（管理者操作）
> ⚠️ **注意**：此阶段由项目管理者负责，贡献者无需执行。

`develop` 构建成功后，由管理者合并到 `main` 并打 tag：

```bash
# 1. 确保 develop 最新且 CI 通过
git checkout develop
git pull origin develop

# 2. 合并到 main
git checkout main
git pull origin main
git merge develop

# 3. 推送 main（触发 CI 构建）
git push origin main

# 4. 观察 main 的 CI 构建
gh run list --limit 5

# 5. 构建成功后，创建 tag
#    注意：版本号必须是 major.minor.patch 格式（如 1.0.2）
git tag v1.0.2 -m "Release version 1.0.2"

# 6. 推送 tag（触发正式发布）
git push origin v1.0.2

# 7. GitHub Actions 自动完成：
#    - 构建所有平台二进制
#    - 打包安装包（MSI/TAR.GZ/PKG）
#    - 创建 GitHub Release
#    - 上传构建产物
```

#### 版本号规范
- 使用 [语义化版本](https://semver.org/lang/zh-CN/)：`major.minor.patch`
- 示例：`v1.0.0`、`v1.0.1`、`v1.1.0`
- **不要**使用预发布版本如 `v1.0.0-alpha`（.NET 版本不支持）

**本地开发建议**：
```bash
# 同步最新代码
git checkout develop
git pull origin develop

# 创建功能分支（可选）
git checkout -b feature/my-feature develop

# 开发完成后，合并回 develop
git checkout develop
git merge feature/my-feature

# 推送并创建 PR
git push origin develop
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 仅运行单元测试（跳过集成测试）
dotnet test --filter "Category!=Integration"

# 运行特定测试
dotnet test --filter "FullyQualifiedName~TestClassName"
```

## 代码规范

- 遵循项目已有的代码风格（项目配置了 StyleCop 和 EditorConfig）
- 提交前请确保通过所有测试：`dotnet test`
- 提交信息遵循 [Conventional Commits](https://www.conventionalcommits.org/zh-hans/) 规范：
  - `feat:` 新功能
  - `fix:` Bug 修复
  - `docs:` 文档更新
  - `refactor:` 代码重构
  - `test:` 测试相关
  - `chore:` 构建/工具变更

## 许可证

通过向本项目贡献代码，您同意您的贡献将按照 [MIT License](LICENSE) 授权。
