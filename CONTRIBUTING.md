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
1. 在 `develop` 分支完成开发和测试
2. 创建 PR 合并到 `main` 分支
3. CI 自动构建、测试、打包
4. 测试通过后，合并到 `main`
5. 创建 tag `git tag v1.x.x` 触发正式发布
6. GitHub Actions 自动创建 Release 并上传安装包

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
