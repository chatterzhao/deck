# Deck

Deck（/dɛk/ "代克"，甲板），容器化开发环境构建工具，模板复用，助力开发快速起步

Deck 通过模板为开发者提供标准化的开发环境基础，让您专注于业务开发而非环境配置。

Deck .NET 版基于 .NET 9 构建，跨平台原生性能，支持 Windows、macOS 和 Linux 平台。

## ✨ 主要特性

- 🔄 **模板复用** - 通过仓库维护模板文件，需要搭建环境时一个命令构建出一样的环境
- 🚀 **快速起步** - 从零到运行开发环境，只需一个 deck start 命令
- 📦 **环境隔离** - 基于容器技术，为每个项目创建独立的开发环境
- 🌍 **跨平台** - 本工具支持 Windows、macOS 和 Linux 平台使用
- ⚡ **原生性能** - 本工具 AOT 编译，启动迅速，低资源占用
- 🛠️ **易扩展** - 支持自定义模板和项目特定配置，模板与工具可独立维护

## 📥 安装

### 下载预编译版本

从 [Github Releases](https://github.com/chatterzhao/deck/releases)或 [Gitee Releases](https://gitee.com/zhaoquan/deck-shell/releases/) 页面下载适合您系统的版本：

- **Windows**: `deck-vX.X.X-win-x64.msi` 或 `deck-vX.X.X-win-arm64.msi`
- **Linux**: `deck-vX.X.X-linux-x64.deb` / `deck-vX.X.X-linux-x64.rpm` 或 ARM64 版本
- **macOS**: `deck-vX.X.X-osx-x64.dmg` 或 `deck-vX.X.X-osx-arm64.dmg`

### 安装步骤

#### Windows
1. 下载 `.msi` 安装包
2. 双击运行安装程序，按向导完成安装
3. 安装完成后，在终端中运行 `deck --version` 验证

#### macOS
1. 下载 `.dmg` 文件
2. 双击打开，将 Deck 应用拖拽到 Applications 文件夹
3. 验证安装：`deck --version`

#### Linux
1. 下载对应的 `.deb` 或 `.rpm` 包
2. Ubuntu/Debian: `sudo dpkg -i deck-vX.X.X-linux-x64.deb`
3. CentOS/RHEL: `sudo rpm -ivh deck-vX.X.X-linux-x64.rpm`
4. 或通过包管理器安装（如 apt、yum）
5. 验证安装：`deck --version`

### 通过源码构建

```bash
# 克隆仓库
git clone https://github.com/your-org/deck-dotnet.git
cd deck-dotnet

# 构建
dotnet build --configuration Release

# 发布（可选）
dotnet publish src/Deck.Console -c Release -o publish
```

## 🚀 快速开始

### 1. 初始化项目

```bash
# 在终端应用执行命令或手工创建项目目录
# 在宿主机创建（不是容器内）
mkdir my-project && cd my-project
```

### 2. 选择开发环境

```bash
# 在终端应用执行命令启动交互式环境选择
# 注意：deck start 命令必须在项目根目录（my-project）比如 `weichat/`下运行，因为它会自动创建 .deck 目录，并自动把命令根目录挂在到容器的 workspace
deck start

# 或直接指定环境模板
# 模板目录名是`nodejs-xx`，命令会自动取前缀，它的作用是筛选，当前缀没有命中，会显示所有模板
deck start nodejs
deck start python
deck start golang
```

### 3. 管理环境

```bash
# 查看当前状态 
deck images list

# 停止容器
deck stop [CONTAINER-NAME/ID]

# 智能清理环境
deck clean                        # 显示三层配置选择界面
deck images clean                 # 镜像配置清理（智能容器状态检测）
deck custom clean                 # 自定义配置清理
deck templates clean              # 模板清理

# 查看可用模板
deck templates list
```

## 📚 使用指南

### 基本概念

- **deck**: 基于三层配置体系构建镜像、启动容器的开发环境管理工具
- **Templates**: 远程模板库（只读，每次自动更新覆盖），提供最佳实践配置
- **Custom**: 用户自定义配置（可编辑），从Templates复制或手动创建
- **Images**: 构建记录（带时间戳），保存已构建镜像的配置快照
- **项目目录**: 执行 `deck start` 的目录，会自动创建 `.deck` 管理目录

### 三层配置工作流程

- **选择Images配置** → 智能容器管理（检测容器运行中/停止/不存在状态并处理）
- **选择Custom配置** → 构建新镜像流程（Custom→Images）
- **选择Templates配置** → 双工作流程：
  1. 创建可编辑配置（Templates→Custom，终止命令等待编辑）
  2. 直接构建启动（Templates→Custom→Images三步流程）

### 容器与镜像概念

- **镜像（Image）**: 静态的应用模板，类似于"光盘镜像"
- **容器（Container）**: 镜像的运行实例，类似于"正在运行的程序"
- **关系**: 一个镜像可以创建多个容器，删除镜像前需要先处理所有相关容器

**常用操作对比：**
```bash
# 容器操作（操作运行实例）
podman ps -a                      # 查看所有容器（运行中+已停止）
podman stop [CONTAINER-ID]        # 停止容器
podman rm [CONTAINER-ID]          # 删除容器

# 镜像操作（操作静态模板）
podman images                     # 查看所有镜像
podman rmi [IMAGE-ID]             # 删除镜像（需要先删除相关容器）
podman build -t [IMAGE-NAME] .    # 构建镜像
```

### 常用命令

```bash
# 查看帮助
deck --help
deck <command> --help

# 环境管理
deck start [env-type]             # 智能启动开发环境（显示三层配置选择）
deck stop [CONTAINER-NAME/ID]     # 停止指定容器（无参数时显示交互式列表）
deck restart [CONTAINER-NAME/ID]  # 重启指定容器（无参数时显示交互式列表）
deck logs [CONTAINER-NAME/ID] [-f] # 查看容器日志（无参数时显示交互式列表）
deck shell [CONTAINER-NAME/ID]    # 进入容器shell（无参数时显示交互式列表）
deck ps                           # 显示当前项目相关容器（比 podman ps -a 更智能）
deck rm [CONTAINER-NAME/ID]       # 删除容器（无参数时显示交互式列表）

# 配置管理
deck custom list                  # 列出用户自定义配置
deck custom clean                 # 清理自定义配置（交互式选择）

# 镜像三层管理（优化：统一管理Deck配置+Podman镜像+容器）
deck images list                  # 统一列出：Deck配置+Podman镜像+相关容器
deck images clean                 # 三层统一清理（不同选择不同清理逻辑）
deck images info [TARGET]         # 显示详细信息（无参数时三层交互式选择）
deck images help                  # 显示三层管理逻辑说明

# 模板管理
deck templates list               # 列出可用模板
deck templates update             # 更新远程模板
deck templates clean              # 提示使用update替代（templates每次start自动覆盖）

# 系统管理
deck doctor                       # 系统诊断
deck clean                        # 智能清理（三层配置选择：Images/Custom/Templates）
deck install podman               # 自动安装Podman
```

**🎯 .NET版本优化特性：**
- **交互式选择** - 所有需要容器名/配置名的命令都支持无参数交互式选择
- **三层统一管理** - `deck images` 系列统一管理Deck配置+Podman镜像+容器
- **智能清理逻辑** - 不同层级选择对应不同的清理策略和警告
- **智能命令建议** - `deck templates clean` 会提示更合适的替代命令
- **Podman命令提示** - 每次操作后显示等效的Podman命令，帮助学习
- **智能过滤** - `deck ps` 只显示当前项目相关容器，避免干扰
- **标准平台包** - MSI/DMG/DEB/RPM安装，无需手动配置PATH

**⚠️ 相比原版Shell实现，.NET版本放弃了以下命令：**
- `deck config create` - 建议直接从Templates复制到Custom目录手动编辑
- `deck config edit` - 建议使用编辑器直接编辑Custom目录中的配置文件

### 配置文件

Deck 在项目目录下自动创建 `.deck/config.yaml` 配置文件，支持完整的配置管理：

```yaml
# .deck/config.yaml
templates:
  repository:
    url: "https://github.com/your-org/your-templates.git"
    branch: "main"
  auto_update: true
  cache_expire: "24h"

container:
  engine: "podman"          # 或 "docker"
  auto_install: true

network:
  proxy:
    http: "http://proxy.company.com:8080"
    https: "https://proxy.company.com:8080"
```

**目录结构：**
```
your-project/
└── .deck/                    # 工具管理目录（自动创建）
    ├── config.yaml           # 配置文件
    ├── templates/            # 远程模板（只读，自动更新）
    │   ├── nodejs-default/
    │   ├── python-default/
    │   └── golang-default/
    ├── custom/               # 用户配置（可编辑）
    │   ├── nodejs-customized/
    │   └── python-api/
    └── images/               # 构建记录（带时间戳）
        ├── nodejs-app-20250121-1430/
        └── python-api-20250120-0920/
```

### 智能清理功能详解

**主清理命令 `deck clean`：**
```bash
$ deck clean
请选择要清理的类型：

Images list:
1. nodejs-app-20250121-1430
2. python-api-20250120-0920

Custom list:
3. nodejs-customized
4. python-api

Templates list:
5. nodejs-default
6. python-default

请输入序号（或按Enter取消）: 1
确认删除 nodejs-app-20250121-1430？ (y/n): y
```

**三层统一管理示例（.NET版本重要优化）：**

**`deck images list` - 三层统一展示：**
```bash
$ deck images list
当前项目镜像资源：

Deck Images配置 (.deck/images/)：
1. nodejs-app-20250121-1430 (构建时间: 2025-01-21 14:30)
2. python-api-20250120-0920 (构建时间: 2025-01-20 09:20)

Podman 镜像：
3. localhost/nodejs-app-20250121-1430:latest (大小: 1.2GB)
4. localhost/python-api-20250120-0920:latest (大小: 856MB)

相关容器：
5. nodejs-app-dev (运行中) 基于镜像#3 [ID: a1b2c3d4]
6. nodejs-app-test (已停止) 基于镜像#3 [ID: e5f6g7h8]
7. python-api-prod (运行中) 基于镜像#4 [ID: x9y8z7w6]

💡 等效Podman命令：
   podman images          # 查看所有镜像
   podman ps -a           # 查看所有容器
```

**`deck images clean` - 三层统一清理：**
```bash
$ deck images clean
请选择要清理的目标：

Deck Images配置：
1. nodejs-app-20250121-1430 配置目录
2. python-api-20250120-0920 配置目录

Podman 镜像：
3. localhost/nodejs-app-20250121-1430:latest (1.2GB)
4. localhost/python-api-20250120-0920:latest (856MB)

相关容器：
5. nodejs-app-dev (运行中) [ID: a1b2c3d4]
6. nodejs-app-test (已停止) [ID: e5f6g7h8]
7. python-api-prod (运行中) [ID: x9y8z7w6]

请输入序号: 3

⚠️ 警告：删除Podman镜像将同步删除以下内容：
- 镜像：localhost/nodejs-app-20250121-1430:latest
- 相关容器：nodejs-app-dev (运行中), nodejs-app-test (已停止)
- Deck配置：.deck/images/nodejs-app-20250121-1430/

请选择清理方式：
1. 强制删除镜像+所有相关容器+Deck配置目录
2. 删除镜像+容器+配置+构建缓存（⚠️ 不推荐，重建需重新下载）

请输入选项: 1
确认删除？这将无法撤销 (y/n): y

正在清理镜像相关资源...
✅ 已停止并删除容器：nodejs-app-dev (a1b2c3d4)
✅ 已删除容器：nodejs-app-test (e5f6g7h8)
✅ 已删除镜像：localhost/nodejs-app-20250121-1430:latest
✅ 已删除配置目录：.deck/images/nodejs-app-20250121-1430/

💡 等效Podman命令序列：
   podman rm -f a1b2c3d4 e5f6g7h8
   podman rmi localhost/nodejs-app-20250121-1430:latest
```

**`deck templates clean` - 智能提示替代方案：**
```bash
$ deck templates clean

💡 提示：templates 目录每次执行 deck start 时都会从远程仓库自动覆盖更新

建议使用以下命令替代：
- deck templates update  # 立即从仓库更新模板
- 直接执行 deck start   # 会自动更新并使用最新模板

清理 templates 目录意义不大，因为会被自动覆盖。

是否仍要继续清理操作？(y/n): n

💡 建议执行：deck templates update
```

### 交互式选择体验（.NET版本优化）

**无参数交互式选择示例：**
```bash
$ deck stop
请选择要停止的容器：

当前项目相关容器：
1. nodejs-app-dev (运行中) [ID: a1b2c3d4]
2. python-api-test (已停止) [ID: e5f6g7h8]

💡 提示：您也可以直接使用 Podman 命令：
   podman stop a1b2c3d4    # 停止 nodejs-app-dev
   podman ps -a            # 查看所有容器

请输入序号（或按 Enter 取消）: 1
正在停止容器 nodejs-app-dev...
✅ 已停止容器 nodejs-app-dev (a1b2c3d4)

等效的 Podman 命令：podman stop a1b2c3d4
```

**智能项目容器过滤：**
```bash
$ deck ps
当前项目相关容器：

运行中 (2)：
- nodejs-app-dev [ID: a1b2c3d4] 运行时间: 2小时
- python-api-prod [ID: e5f6g7h8] 运行时间: 1天

已停止 (1)：  
- nodejs-app-test [ID: x9y8z7w6] 退出码: 0

💡 查看所有容器：podman ps -a
💡 查看系统容器：podman ps -a --all
```

## 🛠️ 系统要求

- **容器引擎**: Docker 或 Podman（推荐）
- **操作系统**: 
  - Windows 10/11 (x64/ARM64)
  - macOS 10.15+ (Intel/Apple Silicon)
  - Linux (x64/ARM64)

## 🤝 贡献

我们欢迎社区贡献！请查看 [贡献指南](CONTRIBUTING.md) 了解如何参与项目开发。

### 开发环境搭建

```bash
# 克隆仓库
git clone https://github.com/chatterzhao/deck.git # 或 git clone https://gitee.com/zhaoquan/deck.git
cd deck

# 安装依赖
dotnet restore

# 构建
dotnet build

# 运行测试
dotnet test

# 运行开发版本
dotnet run --project src/Deck.Console
```

### 项目结构

```
src/
├── Deck.Console/          # 命令行界面
├── Deck.Core/             # 核心业务逻辑
├── Deck.Services/         # 业务服务层
└── Deck.Infrastructure/   # 基础设施层
tests/
├── Deck.Console.Tests/    # 控制台测试
└── Deck.Services.Tests/   # 服务层测试
```

## 📄 许可证

本项目基于 [MIT 许可证](LICENSE) 开源。

## 🔗 相关链接

- [官方文档](https://deck-tool.dev)
- [问题反馈](https://github.com/zhaoquan/deck/issues)
- [讨论区](https://github.com//deck-dotnet/discussions)
- [更新日志](CHANGELOG.md)

---

⭐ 如果这个项目对您有帮助，请给我们一个 Star！