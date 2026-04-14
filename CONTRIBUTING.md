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
