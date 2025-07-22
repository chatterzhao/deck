# Deck .NET Console 构建指南

本文档说明如何构建和发布 Deck .NET Console 应用程序，基于 AOT (Ahead-of-Time) 编译策略。

## 快速开始

### 所有平台构建

```bash
# 构建所有平台（标准发布）
./build-all.sh 1.0.0 Release
```

### Windows 平台构建

```powershell
# Windows PowerShell
.\build-windows.ps1 -Version "1.0.0" -Configuration Release
```

### Unix/Linux/macOS 平台构建

```bash
# Unix/Linux/macOS
./build-unix.sh Release 1.0.0 false
```

## 构建策略

### AOT编译状态

⚠️ **当前AOT编译状态**：由于YamlDotNet库的AOT兼容性问题，完整的AOT编译暂时无法使用。构建脚本会尝试AOT编译，如果失败会自动回退到标准发布。

这是已知问题，在MVP版本中是可以接受的。

### 标准平台包

构建脚本会创建以下格式的平台包：

- **Windows**: MSI 安装包 (需要 WiX Toolset)
- **macOS**: DMG 磁盘镜像 (需要 create-dmg)
- **Linux**: DEB 和 RPM 包 (需要 dpkg-deb 和 rpmbuild)

### 工具依赖

#### Windows
- .NET 9 SDK
- WiX Toolset: `dotnet tool install --global wix`

#### macOS
- .NET 9 SDK
- create-dmg: `brew install create-dmg`

#### Linux
- .NET 9 SDK
- dpkg-deb (通常系统自带)
- rpmbuild (可选): `sudo apt install rpm` 或 `sudo yum install rpm-build`

## 输出结构

```
artifacts/
├── windows/
│   ├── win-x64/
│   │   └── Deck.Console.exe
│   ├── win-arm64/
│   │   └── Deck.Console.exe
│   ├── deck-v1.0.0-win-x64.msi
│   └── deck-v1.0.0-win-arm64.msi
├── unix/
│   ├── osx-x64/
│   │   └── Deck.Console
│   ├── osx-arm64/
│   │   └── Deck.Console
│   ├── deck-v1.0.0-osx-x64.dmg
│   ├── deck-v1.0.0-osx-arm64.dmg
│   ├── deck-v1.0.0-linux-x64.deb
│   └── deck-v1.0.0-linux-arm64.deb
└── release/
    ├── windows-x64/
    ├── windows-arm64/
    ├── linux-x64/
    ├── linux-arm64/
    ├── macos-x64/
    └── macos-arm64/
```

## CI/CD 集成

构建脚本与GitHub Actions工作流集成：

- `.github/workflows/build.yml` - 多平台构建和测试
- `.github/workflows/release.yml` - 标签触发的自动发布
- `.github/workflows/code-quality.yml` - 代码质量检查

## 故障排除

### 常见问题

1. **AOT编译失败**
   - 这是预期行为，脚本会自动回退到标准发布
   - 原因：YamlDotNet库不完全支持AOT

2. **MSI/DMG/DEB包创建失败**
   - 检查是否安装了相应的打包工具
   - 在CI/CD环境中，某些打包工具可能不可用

3. **权限错误**
   - 确保构建脚本有执行权限：`chmod +x *.sh`

### 手动验证构建

```bash
# 测试可执行文件
./artifacts/unix/osx-arm64/Deck.Console --version
./artifacts/unix/osx-arm64/Deck.Console --help

# 检查文件大小
ls -lh ./artifacts/unix/osx-arm64/Deck.Console
```

## 开发环境

构建需要：
- .NET 9 SDK
- Git
- 平台特定的打包工具（可选）

详细的AOT编译配置请参考 `aot-publishing-strategy.md` 文档。