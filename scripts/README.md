# Deck 构建和分发脚本

## 📋 脚本环境要求和功能说明

| 脚本 | 执行脚本的宿主系统 | 依赖 | 功能 | 输出 | 使用场景 |
|------|------------|----------|------|------|----------|
| `build.sh` | Unix/Linux/macOS | .NET 9 SDK, bash | 构建所有6个平台二进制 | `build/release/` | 开发调试<br>CI验证 |
| `build.ps1` | Windows | .NET 9 SDK, PowerShell 5.1+ | 构建所有6个平台二进制 | `build/release/` | 开发调试<br>CI验证 |
| `package.sh` | macOS | .NET 9 SDK, bash, `create-dmg`¹ | 创建 macOS 分发包 | `dist/macos/` | 用户分发 |
| `package.sh` | Linux | .NET 9 SDK, bash, `dpkg-deb`², `rpmbuild`³ | 创建 Linux 分发包 | `dist/linux/` | 用户分发 |
| `package.ps1` | Windows | .NET 9 SDK, PowerShell 5.1+, `wix`⁴ | 创建 Windows 分发包 | `dist/windows/` | 用户分发 |

### 工具安装命令

| 编号 | 工具 | 安装命令 | 说明 |
|------|------|----------|------|
| ¹ | create-dmg | `brew install create-dmg` | macOS DMG 包创建 |
| ² | dpkg-deb | 系统自带 | Linux DEB 包创建 |
| ³ | rpmbuild | `sudo apt-get install rpm` (Ubuntu)<br>`sudo yum install rpm-build` (CentOS) | Linux RPM 包创建 |
| ⁴ | wix | `dotnet tool install --global wix` | Windows MSI 包创建 |

### 关键区别

- **build 脚本**：编译源码 → 可执行文件（开发用）
- **package 脚本**：打包文件 → 安装包（分发用）

## 🚀 快速开始

### 刚克隆项目后的使用流程

用户**无需手动执行** `dotnet restore`，所有构建脚本都会自动处理依赖恢复：

```bash
# 1. 克隆项目
git clone <repository-url>
cd deck

# 2. 直接执行构建（脚本会自动 restore）
# macOS/Linux:
./scripts/build.sh

# Windows:
.\scripts\build.ps1

# 3. 创建分发包（可选）
# macOS/Linux:
./scripts/package.sh

# Windows:  
.\scripts\package.ps1
```

**重要提示**：所有脚本都内置了 `dotnet restore` 步骤，用户可以直接运行，无需额外准备。

## 📂 构建和分发目录树

### build 目录结构（开发构建）

```
build/
└── release/              # Release 配置构建输出
    ├── windows-x64/      # Windows Intel 64位
    │   └── Deck.Console.exe
    ├── windows-arm64/    # Windows ARM 64位
    │   └── Deck.Console.exe
    ├── linux-x64/        # Linux x86 64位
    │   └── Deck.Console
    ├── linux-arm64/      # Linux ARM 64位
    │   └── Deck.Console
    ├── macos-x64/        # macOS Intel
    │   └── Deck.Console
    └── macos-arm64/      # macOS Apple Silicon
        └── Deck.Console
```

### dist 目录结构（分发包）

```
dist/
├── windows/              # Windows 分发包
│   ├── win-x64/
│   │   └── Deck.Console.exe
│   ├── win-arm64/
│   │   └── Deck.Console.exe
│   ├── deck-v1.0.0-win-x64.msi
│   └── deck-v1.0.0-win-arm64.msi
├── linux/                # Linux 分发包
│   ├── deck-v1.0.0-amd64.deb
│   ├── deck-v1.0.0-arm64.deb
│   ├── deck-v1.0.0-amd64.rpm
│   └── deck-v1.0.0-arm64.rpm
└── macos/                # macOS 分发包
    ├── deck-v1.0.0-intel.dmg
    └── deck-v1.0.0-apple-silicon.dmg
```

## 🚀 命令使用方法

### build.sh - Unix/Linux/macOS 跨平台构建

**功能**：构建所有支持平台的二进制文件

**语法**：
```bash
./scripts/build.sh [VERSION] [CONFIGURATION]
```

**参数**：
- `VERSION` - 版本号（默认：1.0.0）
- `CONFIGURATION` - 构建配置（默认：Release）

**示例**：
```bash
# 使用默认参数构建
./scripts/build.sh

# 指定版本号
./scripts/build.sh 1.2.0

# 指定版本号和配置
./scripts/build.sh 1.2.0 Debug
```

**特性**：
- AOT 编译优先，失败时自动降级到标准发布
- 支持 6 个平台交叉编译
- 自动文件大小统计和验证
- 兼容 macOS 旧版 bash

### build.ps1 - Windows 跨平台构建

**功能**：构建所有支持平台的二进制文件（Windows PowerShell 版本）

**语法**：
```powershell
.\scripts\build.ps1 [-Version <String>] [-Configuration <String>]
```

**参数**：
- `-Version` - 版本号（默认：1.0.0）
- `-Configuration` - 构建配置（默认：Release）

**示例**：
```powershell
# 使用默认参数构建
.\scripts\build.ps1

# 指定版本号
.\scripts\build.ps1 -Version "1.2.0"

# 指定版本号和配置
.\scripts\build.ps1 -Version "1.2.0" -Configuration "Debug"
```

**特性**：
- 与 build.sh 功能完全一致
- AOT 编译优先，失败时自动降级
- 支持 6 个平台交叉编译
- PowerShell 原生错误处理

### package.sh - Unix/Linux 分发包创建

**功能**：为当前 Unix/Linux 系统创建对应格式的分发包

**语法**：
```bash
./scripts/package.sh [CONFIGURATION] [VERSION] [CLEAN]
```

**参数**：
- `CONFIGURATION` - 构建配置（默认：Release）
- `VERSION` - 版本号（默认：1.0.0）
- `CLEAN` - 是否清理（默认：false）

**示例**：
```bash
# 使用默认参数创建分发包
./scripts/package.sh

# 指定版本号
./scripts/package.sh Release 1.2.0

# 启用清理模式
./scripts/package.sh Release 1.2.0 true
```

**支持格式**：
- **macOS**: DMG 磁盘镜像（Intel 和 Apple Silicon 两个版本）
- **Linux**: DEB 和 RPM 包（x64 和 ARM64 架构）

### package.ps1 - Windows 分发包创建

**功能**：创建 Windows MSI 安装包

**语法**：
```powershell
.\scripts\package.ps1 [-Configuration <String>] [-Version <String>] [-Clean]
```

**参数**：
- `-Configuration` - 构建配置（默认：Release）
- `-Version` - 版本号（默认：1.0.0）
- `-Clean` - 清理输出目录开关

**示例**：
```powershell
# 使用默认参数
.\scripts\package.ps1

# 指定版本号
.\scripts\package.ps1 -Version "1.2.0"

# 指定配置并启用清理
.\scripts\package.ps1 -Configuration "Debug" -Version "1.2.0" -Clean
```

**特性**：
- 自动检测并调用 build.ps1 进行构建（如需要）
- 支持 Windows x64 和 ARM64 架构
- 自动安装 WiX Toolset（如未安装）
- 创建 MSI 安装包（如有 WiX 配置文件）

**输出格式**：MSI 安装包（x64 和 ARM64 架构）

## 🔍 故障排除

### AOT 编译失败
这是正常现象，脚本会自动降级到标准发布模式。常见原因：
- YamlDotNet 库不兼容 AOT 编译
- 跨平台编译环境限制
- 缺少原生工具链

### 分发包创建工具缺失
根据错误提示安装对应工具：
```bash
# macOS - 安装 create-dmg
brew install create-dmg

# Linux - 安装 RPM 构建工具
sudo apt-get install rpm          # Ubuntu/Debian
sudo yum install rpm-build        # CentOS/RHEL

# Windows - 安装 WiX Toolset
dotnet tool install --global wix
```

### 权限问题
```bash
# 为脚本添加执行权限
chmod +x scripts/*.sh
```

### 路径问题
所有脚本都会自动切换到项目根目录，可以从任何位置安全运行。

## 💡 最佳实践

1. **开发阶段**：
   - Unix/Linux/macOS: 使用 `./scripts/build.sh` 进行快速构建和测试
   - Windows: 使用 `.\scripts\build.ps1` 进行快速构建和测试

2. **发布准备**：使用对应平台的 `package` 脚本创建分发包
   - Unix/Linux/macOS: `./scripts/package.sh`
   - Windows: `.\scripts\package.ps1`

3. **版本管理**：始终明确指定版本号，避免使用默认值

4. **清理构建**：定期使用 `-Clean` 参数清理输出目录

5. **平台选择**：
   - 如果您在 Windows 上有 WSL 或 Git Bash，两套脚本都可以使用
   - 原生 Windows 用户推荐使用 PowerShell 脚本（.ps1）
   - Unix/Linux/macOS 用户使用 bash 脚本（.sh）