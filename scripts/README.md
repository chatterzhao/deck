# Deck .NET Console 构建脚本

这个目录包含用于构建和发布 Deck .NET Console 应用程序的脚本。

## 文件说明

- `build.sh` - Unix/Linux/macOS 构建脚本
- `build.ps1` - Windows PowerShell 构建脚本  
- `README.md` - 本说明文件

## 快速开始

### Unix/Linux/macOS

```bash
# 构建当前平台
./scripts/build.sh

# 构建所有平台
./scripts/build.sh -a

# 构建特定平台
./scripts/build.sh linux x64
./scripts/build.sh macos arm64

# 启用AOT编译 (实验性)
./scripts/build.sh --aot

# 清理构建目录
./scripts/build.sh -c
```

### Windows PowerShell

```powershell
# 构建当前平台
.\scripts\build.ps1

# 构建所有平台
.\scripts\build.ps1 -All

# 构建特定平台
.\scripts\build.ps1 -Platform linux -Architecture x64
.\scripts\build.ps1 windows arm64

# 启用AOT编译 (实验性)
.\scripts\build.ps1 -Aot

# 清理构建目录
.\scripts\build.ps1 -Clean
```

## 支持的平台

| 平台 | 架构 | 运行时标识符 | 状态 |
|------|------|-------------|------|
| Windows | x64 | win-x64 | ✅ 支持 |
| Windows | ARM64 | win-arm64 | ✅ 支持 |
| Linux | x64 | linux-x64 | ✅ 支持 |
| Linux | ARM64 | linux-arm64 | ✅ 支持 |
| macOS | x64 (Intel) | osx-x64 | ✅ 支持 |
| macOS | ARM64 (Apple Silicon) | osx-arm64 | ✅ 支持 |

## 输出目录结构

```
build/           # 构建输出目录
├── windows-x64/
├── linux-x64/
├── macos-arm64/
└── ...

dist/            # 发布包目录
├── deck-v1.0.0-windows-x64.zip
├── deck-v1.0.0-linux-x64.tar.gz
├── deck-v1.0.0-macos-arm64.tar.gz
└── *.sha256     # SHA256校验和文件
```

## AOT 编译说明

⚠️ **AOT编译目前为实验性功能**

由于某些依赖库（如YamlDotNet）的AOT兼容性问题，AOT编译可能会失败。这是已知问题，在MVP版本中可以接受。

### 已知的AOT限制

1. **YamlDotNet**: 不完全兼容AOT编译
2. **反射代码**: 某些动态代码可能无法在AOT模式下工作
3. **JSON序列化**: 已配置使用源生成器，但可能仍有兼容性问题

### 启用AOT编译

```bash
# Unix/Linux/macOS
./scripts/build.sh --aot

# Windows
.\scripts\build.ps1 -Aot
```

## 环境变量

可以通过环境变量自定义构建：

```bash
# 设置版本号
export VERSION="1.1.0"

# 设置构建时间
export BUILD_DATE="2025-01-01T00:00:00Z"

# 设置Git提交哈希
export BUILD_COMMIT="abc1234"

./scripts/build.sh
```

## 依赖要求

### 所有平台
- .NET 9 SDK
- Git (用于获取提交哈希)

### Unix/Linux/macOS额外要求
- tar
- gzip

### Windows额外要求
- PowerShell 5.1+ (Windows 10自带)

## 故障排除

### 常见问题

1. **构建失败：未找到.NET SDK**
   - 安装 .NET 9 SDK
   - 确保 `dotnet` 命令在PATH中

2. **AOT编译失败**
   - 这是预期的行为，使用标准构建即可
   - AOT编译问题将在未来版本中解决

3. **权限错误 (Unix)**
   - 确保脚本有执行权限：`chmod +x scripts/build.sh`

4. **压缩失败**
   - 检查是否安装了tar/gzip (Unix) 或PowerShell (Windows)
   - 使用 `--no-compress` 跳过压缩

### 调试构建问题

1. 启用详细输出：
   ```bash
   # Unix
   ./scripts/build.sh linux x64 2>&1 | tee build.log
   
   # Windows  
   .\scripts\build.ps1 -Platform linux -Architecture x64 -Verbose
   ```

2. 检查构建日志：
   - Unix: 查看控制台输出
   - Windows: 使用 `-Verbose` 参数

3. 手动构建测试：
   ```bash
   dotnet publish src/Deck.Console/Deck.Console.csproj \
     --configuration Release \
     --runtime linux-x64 \
     --self-contained true \
     --output ./test-build
   ```

## 与GitHub Actions集成

这些脚本与项目的GitHub Actions工作流集成：

- `.github/workflows/build.yml` - 自动构建和测试
- `.github/workflows/release.yml` - 发布工作流
- `.github/workflows/code-quality.yml` - 代码质量检查

本地构建脚本与CI/CD流程使用相同的构建逻辑，确保一致性。