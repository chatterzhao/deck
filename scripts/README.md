# Deck 构建和分发脚本

## 📋 脚本环境要求和功能说明

| 脚本 | 执行脚本的宿主系统 | 依赖 | 功能 | 输出 | 使用场景 |
|------|------------|----------|------|------|----------|
| `build.sh` | Unix/Linux/macOS | .NET 9 SDK, bash | 构建所有6个平台二进制<br>**默认非AOT**，可用 `--aot` 启用 | `build/release/` | 开发调试<br>CI验证 |
| `build.ps1` | Windows | .NET 9 SDK, PowerShell 5.1+ | 构建所有6个平台二进制<br>**默认非AOT**，可用 `-Aot` 启用 | `build/release/` | 开发调试<br>CI验证 |
| `package.sh` | macOS | .NET 9 SDK, bash | 创建 macOS 分发包<br>**默认AOT**，可用 `--no-aot` 禁用 | `dist/macos/` | 用户分发 |
| `package.sh` | Linux | .NET 9 SDK, bash, `dpkg-deb`², `rpmbuild`³ | 创建 Linux 分发包<br>**默认AOT**，可用 `--no-aot` 禁用 | `dist/linux/` | 用户分发 |
| `package.ps1` | Windows | .NET 9 SDK, PowerShell 5.1+, `wix`⁴ | 创建 Windows 分发包<br>**默认AOT**，可用 `-NoAot` 禁用 | `dist/windows/` | 用户分发 |

### 工具安装命令

| 编号 | 工具 | 安装命令 | 说明 |
|------|------|----------|------|
| ² | dpkg-deb | 系统自带 | Linux DEB 包创建 |
| ³ | rpmbuild | `sudo apt-get install rpm` (Ubuntu)<br>`sudo yum install rpm-build` (CentOS) | Linux RPM 包创建 |
| ⁴ | wix | `dotnet tool install --global wix` | Windows MSI 包创建 |

### 关键区别

- **build 脚本**：编译源码 → 可执行文件（开发用，默认非AOT快速构建）
- **package 脚本**：打包文件 → 安装包（分发用，默认AOT优化构建）

## 📦 packaging 配置目录说明

`scripts/packaging/` 目录包含各平台安装包的**配置模板**，被 package 脚本使用：

```
scripts/packaging/
├── linux/                    # Linux 系统包配置
│   ├── DEBIAN/               # DEB 包配置 (Ubuntu/Debian)
│   │   ├── control           # 包元数据模板 (名称、版本、依赖等)
│   │   ├── postinst          # 安装后脚本 (创建链接、设置权限)
│   │   └── prerm             # 卸载前脚本 (清理链接)
│   └── rpm/                  # RPM 包配置 (CentOS/RHEL/Fedora)
│       └── deck.spec         # RPM 规格文件 (安装/卸载脚本等)
└── windows/                  # Windows 安装包配置
    └── deck.wxs              # WiX 配置文件 (MSI 包定义)
```

### 配置文件作用

| 文件 | 平台 | 作用 | 实现效果 |
|------|------|------|----------|
| `DEBIAN/control` | Ubuntu/Debian | DEB 包元数据定义 | 包名、版本、描述信息 |
| `DEBIAN/postinst` | Ubuntu/Debian | 安装后自动执行 | 创建 `/usr/bin/deck` 链接 |
| `DEBIAN/prerm` | Ubuntu/Debian | 卸载前自动执行 | 删除 `/usr/bin/deck` 链接 |
| `rpm/deck.spec` | CentOS/RHEL/Fedora | RPM 包完整定义 | 安装位置、权限、卸载清理 |
| `windows/deck.wxs` | Windows | MSI 安装包定义 | 安装目录、环境变量、快捷方式 |

### 使用流程

1. **package 脚本读取配置** → 根据当前系统选择对应配置文件
2. **动态替换变量** → 将 `{{VERSION}}` 等占位符替换为实际值  
3. **调用系统工具** → 使用 dpkg-deb、rpmbuild、wix 创建安装包
4. **生成标准安装包** → 用户可以双击安装，自动配置环境

**简单说**：这些配置让我们的可执行文件变成**专业的系统安装包**，用户安装后可以在任何地方直接运行 `deck` 命令！

### 应用卸载方法

| 平台 | 安装包格式 | 卸载命令 | 说明 |
|------|------------|----------|------|
| **Ubuntu/Debian** | `.deb` | `sudo dpkg -r deck` | 系统包管理器卸载 |
| **CentOS/RHEL/Fedora** | `.rpm` | `sudo rpm -e deck` | RPM 包管理器卸载 |
| **Windows** | `.msi` | 控制面板 → 程序和功能 → 卸载 | 图形界面卸载 |
| **Windows** | `.msi` | `msiexec /x {ProductCode}` | 命令行卸载 |
| **macOS** | `.pkg` | `sudo pkgutil --forget com.deck.deck` | 系统包管理器卸载 |

**自动清理功能**：
- **Linux**: 卸载时自动删除 `/usr/bin/deck` 符号链接（见 `rpm/deck.spec` 的 `%preun` 部分）
- **Windows**: MSI 卸载时自动清理注册表和环境变量
- **macOS**: PKG 卸载需要手动删除 `/usr/local/bin/deck` 文件，或使用 `sudo rm /usr/local/bin/deck`

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
    ├── deck-v1.0.0-intel.pkg
    └── deck-v1.0.0-apple-silicon.pkg
```

## 🚀 命令使用方法

### build.sh - Unix/Linux/macOS 跨平台构建

**功能**：构建所有支持平台的二进制文件

**语法**：
```bash
./scripts/build.sh [选项]
```

**参数**：
- `--version VERSION` - 版本号（默认：1.0.0）
- `--configuration CONFIG` - 构建配置（默认：Release）
- `--aot` - 启用AOT编译（默认：关闭）
- `--help` - 显示帮助信息

**示例**：
```bash
# 使用默认参数构建（非AOT，快速）
./scripts/build.sh

# 启用AOT优化构建
./scripts/build.sh --aot

# 指定版本号
./scripts/build.sh --version 1.2.0

# 指定版本号和配置
./scripts/build.sh --version 1.2.0 --configuration Debug

# 向后兼容的位置参数
./scripts/build.sh 1.2.0 Debug
```

**特性**：
- **默认非AOT**，开发友好的快速构建
- 可选AOT编译优化
- 支持 6 个平台交叉编译
- 自动文件大小统计和验证
- 兼容 macOS 旧版 bash

### build.ps1 - Windows 跨平台构建

**功能**：构建所有支持平台的二进制文件（Windows PowerShell 版本）

**语法**：
```powershell
.\scripts\build.ps1 [-Version <String>] [-Configuration <String>] [-Aot]
```

**参数**：
- `-Version` - 版本号（默认：1.0.0）
- `-Configuration` - 构建配置（默认：Release）
- `-Aot` - 启用AOT编译（默认：关闭）

**示例**：
```powershell
# 使用默认参数构建（非AOT，快速）
.\scripts\build.ps1

# 启用AOT优化构建
.\scripts\build.ps1 -Aot

# 指定版本号
.\scripts\build.ps1 -Version "1.2.0"

# 指定版本号和配置
.\scripts\build.ps1 -Version "1.2.0" -Configuration "Debug" -Aot
```

**特性**：
- 与 build.sh 功能完全一致
- **默认非AOT**，开发友好的快速构建
- 可选AOT编译优化
- 支持 6 个平台交叉编译
- PowerShell 原生错误处理

### package.sh - Unix/Linux 分发包创建

**功能**：为当前 Unix/Linux 系统创建对应格式的分发包

**语法**：
```bash
./scripts/package.sh [选项]
```

**参数**：
- `--configuration CONFIG` - 构建配置（默认：Release）
- `--version VERSION` - 版本号（默认：1.0.0）
- `--clean` - 清理构建目录
- `--no-aot` - 禁用AOT编译（默认：启用）
- `--help` - 显示帮助信息

**示例**：
```bash
# 使用默认参数创建分发包（AOT优化）
./scripts/package.sh

# 禁用AOT的快速打包
./scripts/package.sh --no-aot

# 指定版本号
./scripts/package.sh --version 1.2.0

# 启用清理模式
./scripts/package.sh --version 1.2.0 --clean

# 向后兼容的位置参数
./scripts/package.sh Release 1.2.0 true
```

**支持格式**：
- **macOS**: PKG 安装包（Intel 和 Apple Silicon 两个版本）
- **Linux**: TAR.GZ 压缩包（x64 和 ARM64 架构）

**特性**：
- **默认AOT**，生产级优化构建
- 可选快速构建模式
- 自动调用build脚本（如需要）

### package.ps1 - Windows 分发包创建

**功能**：创建 Windows MSI 安装包

**语法**：
```powershell
.\scripts\package.ps1 [-Configuration <String>] [-Version <String>] [-Clean] [-NoAot]
```

**参数**：
- `-Configuration` - 构建配置（默认：Release）
- `-Version` - 版本号（默认：1.0.0）
- `-Clean` - 清理输出目录开关
- `-NoAot` - 禁用AOT编译（默认：启用）

**示例**：
```powershell
# 使用默认参数（AOT优化）
.\scripts\package.ps1

# 禁用AOT的快速打包
.\scripts\package.ps1 -NoAot

# 指定版本号
.\scripts\package.ps1 -Version "1.2.0"

# 指定配置并启用清理
.\scripts\package.ps1 -Configuration "Debug" -Version "1.2.0" -Clean -NoAot
```

**特性**：
- **默认AOT**，生产级优化构建
- 可选快速构建模式
- 自动检测并调用 build.ps1 进行构建（如需要）
- 支持 Windows x64 和 ARM64 架构
- 自动安装 WiX Toolset（如未安装）
- 创建 MSI 安装包（如有 WiX 配置文件）

**输出格式**：MSI 安装包（x64 和 ARM64 架构）

## 🔍 故障排除

### AOT 编译失败
这是正常现象，常见原因：
- YamlDotNet 库不兼容 AOT 编译
- 跨平台编译环境限制
- 缺少原生工具链

**解决方案**：
- build脚本：AOT失败时会显示错误并退出
- package脚本：可使用 `--no-aot` / `-NoAot` 参数禁用AOT

### 分发包创建工具缺失
根据错误提示安装对应工具：
```bash
# macOS - 系统自带 pkgbuild，无需安装额外工具

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

1. **开发阶段**（快速构建，默认非AOT）：
   - Unix/Linux/macOS: 使用 `./scripts/build.sh` 进行快速构建和测试
   - Windows: 使用 `.\scripts\build.ps1` 进行快速构建和测试

2. **发布准备**（生产优化，默认AOT）：使用对应平台的 `package` 脚本创建分发包
   - Unix/Linux/macOS: `./scripts/package.sh`
   - Windows: `.\scripts\package.ps1`

3. **AOT控制**：
   - 开发构建启用AOT: `./scripts/build.sh --aot` / `.\scripts\build.ps1 -Aot`
   - 生产打包禁用AOT: `./scripts/package.sh --no-aot` / `.\scripts\package.ps1 -NoAot`

3. **版本管理**：始终明确指定版本号，避免使用默认值

4. **清理构建**：定期使用 `-Clean` 参数清理输出目录

5. **平台选择**：
   - 如果您在 Windows 上有 WSL 或 Git Bash，两套脚本都可以使用
   - 原生 Windows 用户推荐使用 PowerShell 脚本（.ps1）
   - Unix/Linux/macOS 用户使用 bash 脚本（.sh）