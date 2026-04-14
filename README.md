# Deck

Deck（/dɛk/ "代克"）是搭建容器化开发环境的命令行工具，模板复用，助力开发快速起步

Deck 通过模板为开发者提供标准化的开发环境基础，让您专注于业务开发而非环境配置。

Deck .NET 版基于 .NET 9 构建，AOT，跨平台原生性能，支持 Windows、macOS 和 Linux 平台。

## ✨ 主要特性

- 🔄 **模板复用** - 通过仓库维护模板文件，需要搭建环境时一个命令构建出一样的环境
- 🚀 **快速起步** - 从零到运行开发环境，只需一个 deck start 命令
- 📦 **环境隔离** - 基于容器技术，为每个项目创建独立的开发环境
- 🌍 **跨平台** - 本工具支持 Windows、macOS 和 Linux 平台使用
- ⚡ **原生性能** - 本工具 AOT 编译，启动迅速，低资源占用
- 🛠️ **易扩展** - 支持自定义模板和项目特定配置，模板与工具可独立维护

## 📥 安装

### 下载预编译版本

从 [Github Releases](https://github.com/chatterzhao/deck/releases)或 [Gitee Releases](https://gitee.com/zhaoquan/deck/releases/) 页面下载适合您系统的版本：

- **Windows**: `deck-vX.X.X-win-x64.msi` 或 `deck-vX.X.X-win-arm64.msi`
- **Linux**: `deck-vX.X.X-linux-x64.deb` / `deck-vX.X.X-linux-x64.rpm` 或 ARM64 版本
- **macOS**: `deck-vX.X.X-osx-x64.pkg` 或 `deck-vX.X.X-osx-arm64.pkg`

### 安装或卸载步骤

#### Windows
1. 下载 `.msi` 安装包
2. 双击运行安装程序，按向导完成安装
3. 安装完成后，在终端中运行 `deck --help` 验证
4. 卸载方法：控制面板 → 程序和功能 → 找到 `Deck` 并卸载

#### macOS
1. 下载 `.pkg` 安装包
2. 双击运行安装程序，按向导完成安装
3. 安装完成后，在终端中运行 `deck --help` 验证
4. 终端执行`deck-uninstall`

#### Linux
1. 下载对应的 `.deb` 或 `.rpm` 包
2. Ubuntu/Debian: `sudo dpkg -i deck-vX.X.X-linux-x64.deb`
3. CentOS/RHEL: `sudo rpm -ivh deck-vX.X.X-linux-x64.rpm`
4. 或通过包管理器安装（如 apt、yum）
5. 验证安装：`deck --help`
6. 卸载方法：终端执行命令`rm -rf /usr/local/bin/deck`

### 通过源码构建

```bash
# 克隆仓库
git clone https://github.com/your-org/deck-dotnet.git
cd deck-dotnet

# macOS 构建
./scripts/build.sh # 如果是 Windows 执行：./scripts/build.ps1

# macOS 打包
./scripts/package.sh # 如果是 Windows 执行：./scripts/package.ps1
```

## 🚀 快速开始

### 1. 初始化项目

```bash
# 假如还没有项目目录，则创建
# 在终端应用执行命令或手工创建项目目录
# 在宿主机创建（不是容器内）
mkdir my-project
```
```bash
# 在终端执行命令进入项目目录
cd my-project
```

### 2. 选择开发环境
> 注意：deck start 命令必须在项目根目录（my-project）比如 `weichat/`下运行，因为它会自动创建 .deck 目录，并自动把根目录挂在到容器的 workspace
```bash
# 在终端应用执行命令启动交互式环境选择
deck start

# 或直接指定环境模板
# 我们要求的模板命名是`xx-yy`，如`avalonia-default`，命令会自动取前缀xx，它的作用是筛选，当前缀没有命中，会显示所有模板
deck start nodejs
deck start python
deck start golang
```


## 📚 使用指南

### 容器与镜像概念

- **镜像（Image）**: 静态的应用模板，类似于"光盘镜像"
- **容器（Container）**: 镜像的运行实例，类似于"正在运行的程序"
- **关系**: 一个镜像可以创建多个容器，删除镜像前需要先处理所有相关容器

**podman 命令：**
> start 命令会自动检查是否已安装 podman，没有则自动安装。您也可以手动安装 podman。
```bash
# 容器操作（操作运行实例）
podman ps -a                      # 查看所有容器（运行中+已停止）
podman stop [CONTAINER-name/ID]        # 停止容器
podman rm [CONTAINER-name/ID]          # 删除容器

# 镜像操作（操作静态模板）
podman images                     # 查看所有镜像
podman rmi [IMAGE-name/ID]        # 删除镜像（需要先删除相关容器）
podman build -t [IMAGE-NAME] .    # 构建镜像
```

### 基本概念

- **deck**: 命令行工具名称
- **创建.deck目录**: 在您项目根目录执行 `deck start` 命令，自动创建 `.deck` 目录结构
- **目录结构**: .deck/Images|Custom|Templates｜config.json
- **Templates**: 模板目录（只读），每次执行 start 命令时自动覆盖式更新，可通过 config.json 配置自定义模板仓库URL
- **Custom**: 用户自定义配置目录（可编辑），start 命令自动从 Templates 复制（您也可以手动创建，格式需与 Templates 保持一致，如`avalonia-desktop-zhangsan/.env|compose.yaml|Dockerfile|README.md`，比如你在设计一个模板或修改一个模板时）
- **Images**: 构建记录目录（带时间戳），目录名与镜像和容器名相同，命令会按这个目录名去找镜像和容器，所以目录名不可修改，它的另外一个作用时保存已构建的镜像配置，这样才知道这个镜像都是什么配置，由start命令自动管理（不过.env 文件的运行时变量部分可以在启动容器前修改）
- **config.json**: 配置文件，用于配置远程模板仓库 URL

### deck start 命令的交互式选项
> 输入环境序号时，命令会判断这个序号属于哪个配置目录（images|custom|templates 三类配置）进入对应的流程。
- **选择的是 Images 配置** → 智能容器管理（检测容器运行状态并处理，不会构建镜像）
- **选择的是 Custom 配置** → 使用选中的配置构建新镜像（从 Custom 目录复制配置到 Images 目录，并自动重命名，并构建镜像，启动容器）
- **选择的是 Templates 配置** → 双工作流程，进行二次选择：
  1. 创建可编辑配置（选择这个，将执行从 Templates 目录复制配置到 Custom 目录，终止命令等待编辑，您需要打开这个目录的文件进行编辑，之后重新执行 deck start 命令，这次你输入的序号需要看是你刚修改的这个配置的序号）
  2. 直接构建启动（如果你选择的是这个，将执行从Templates 目录复制配置到 Custom 目录，并立即又从 custom复制到 Images 目录，并立即开始构建镜像，启动容器）

### deck 常用命令

```bash
# 查看帮助
deck --help
deck <command> --help             # 如 deck stat --help
# 系统管理
deck doctor                       # 系统诊断
deck clean                        # 智能清理（三层配置选择：Images/Custom/Templates）

# 环境管理
deck start [env-type]             # 参数可为空，为空时列出所有技术栈的配置
deck stop [CONTAINER-NAME/ID]     # 停止指定容器（无容器ID时显示交互式列表，可以输入序号选择）
deck restart [CONTAINER-NAME/ID]  # 重启指定容器（无容器ID时显示交互式列表，可以输入序号选择）
deck logs [CONTAINER-NAME/ID] [-f] # 查看容器日志（无容器ID时显示交互式列表，可以输入序号选择）
deck shell [CONTAINER-NAME/ID]    # 进入容器shell（无容器ID时显示交互式列表）
deck ps                           # 显示当前项目相关容器
deck rm [CONTAINER-NAME/ID]       # 删除容器（无容器ID时显示交互式列表，可以输入序号选择）

# 配置管理


# 镜像三层管理（优化：统一管理Deck配置+Podman镜像+容器）
deck images list                  # 分类列出（类似start 列出可选环境那种效果）：images 目录的所有目录的目录名+Podman与这些目录名相同的镜像+与这些目录名相同的容器
deck images clean                 # 三层统一清理（列出所有可选的images目录的目录名，输入需要清理的序号，让用户输入y/n 确认，一次性清理与目录名相同的容器，镜像，配置目录）
deck images info [TARGET]         # 显示详细信息（无target时三层交互式选择）
deck images help                  # 显示 images 命令的说明

# 自定义配置管理
deck custom list                  # 列出custom目录下所有配置目录的目录名(等价 ls)
deck custom clean                 # 清理自定义配置目录（交互式选择,，可以输入序号选择）

# 模板管理
deck templates list               # 列出templates 目录下的目录的目录名（也就时配置名）
deck templates update             # 从仓库更新模板（覆盖式）
deck templates clean              # 提示使用update替代（templates每次start自动覆盖，所以清理意义不大）
```

**🎯 Deck .NET版本优化特性：**
- **交互式选择** - 所有需要容器名/配置名的命令都支持无参数交互式选择
- **三层统一管理** - `deck images` 系列统一管理Deck配置+Podman镜像+容器
- **智能清理逻辑** - 不同层级选择对应不同的清理策略和警告
- **智能命令建议** - `deck templates clean` 会提示更合适的替代命令
- **Podman命令提示** - 每次操作后显示等效的Podman命令，帮助学习
- **智能过滤** - `deck ps` 只显示当前项目相关容器，避免干扰
- **标准平台包** - MSI/PKG/TAR.GZ安装，无需手动配置PATH

**⚠️ 相比原版Deck Shell实现，.NET版本放弃了以下命令：**
- `deck config create` - 建议直接从Templates复制到Custom目录手动编辑
- `deck config edit` - 建议使用编辑器直接编辑Custom目录中的配置文件

### 配置文件

Deck 在项目目录下自动创建 `.deck/config.json` 配置文件，url将被用于同步模板：

```json
// .deck/config.json
{
  "remoteTemplates": {
    "repository": "https://gitee.com/zhaoquan/deck.git",
    "branch": "main",
    "cacheTtl": "24h",
    "autoUpdate": true
  }
}
```

**模板同步网络配置：**
- Deck 只负责测试模板仓库的连接性（如 GitHub、Gitee）
- 如果模板同步失败，工具会提供以下解决方案：
  1. 检查网络连接
  2. 手动修改 `.deck/config.json` 更换仓库地址
  3. 使用本地模板（如果已下载）
  4. 在 `.deck/templates/` 目录下手动创建模板


**您项目根目录下的 .deck 目录结构：**
> 执行 deck start 命令后会自动在执行命令的根目录创建 .deck 目录，后面启动容器时，会自动把根目录(这里也就是`your-project`)挂在到容器的 workspace。
```
your-project/
└── .deck/                    # 工具管理目录（自动创建）
    ├── config.json           # 配置文件
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
你输入类型是镜像，将同时清理images下的目录、同名镜像、同名容器，请谨慎操作，确认删除 nodejs-app-20250121-1430？ (y/n): y
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
5. nodejs-app-20250121-1430-dev (运行中) 基于镜像#3-开发[ID: a1b2c3d4]
6. nodejs-app-20250121-1430-test (已停止) 基于镜像#3-测试[ID: e5f6g7h8]
7. nodejs-app-20250121-1430-prod (运行中) 基于镜像#3-生产 [ID: x9y8z7w6]

💡 部分等效Podman命令：
   cd .deck/images ls     # 查看images 目录下的目录名
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
- 如果这个镜像对应有生产容器，含未运行（xx-prod），则这个命令终止，引导提醒有生产容器，超级危险操作，谨慎操作，确定后告诉用什么方式先删除生产容器
- 镜像：localhost/nodejs-app-20250121-1430:latest
- 相关容器：nodejs-app-20250121-1430-dev (运行中), nodejs-app-20250121-1430-test (已停止)，
- Deck配置：.deck/images/nodejs-app-20250121-1430/

请选择清理方式：
1. 强制删除镜像+所有相关容器+Deck配置目录（无生产容器含未运行）
2. 删除镜像+容器+配置+构建缓存（⚠️ 不推荐，重建需重新下载，无生产容器含未运行）

请输入选项: 1
确认删除？这将无法撤销 (y/n): y

正在清理镜像相关资源...
✅ 已停止并删除容器：nodejs-app-20250121-1430-dev (a1b2c3d4)
✅ 已删除容器：nodejs-app-20250121-1430-test (e5f6g7h8)
✅ 已删除镜像：localhost/nodejs-app-20250121-1430:latest
✅ 已删除配置目录：.deck/images/nodejs-app-20250121-1430/

💡 等效Podman命令序列：
   podman rm -f a1b2c3d4 e5f6g7h8
   podman rmi localhost/nodejs-app-20250121-1430:latest
```

**`deck templates clean` - 智能提示替代方案：**
```bash
$ deck templates clean

💡 提示：命令已终止。templates 目录每次执行 deck start 时都会从远程仓库自动覆盖更新，清理无意义，建议使用以下命令替代：
- deck templates update  # 立即从仓库更新模板
- deck start   # 会自动更新并使用最新模板
```

### 交互式选择体验（.NET版本优化）

**无参数交互式选择示例：**
```bash
$ deck stop

💡 提示：您也可以直接使用 Podman 命令：
   podman stop a1b2c3d4    # 停止 nodejs-app-dev
   podman ps -a            # 查看所有容器

请选择要停止的容器：

当前项目相关容器：
1. nodejs-app-20250121-1430-dev (运行中) [ID: a1b2c3d4]
2. nodejs-app-20250121-1430-test (已停止) [ID: e5f6g7h8]


请输入序号（或按 Enter 取消）: 1
正在停止容器 nodejs-app-20250121-1430-dev...
✅ 已停止容器 nodejs-app-20250121-1430-dev (a1b2c3d4)

等效的 Podman 命令：podman stop a1b2c3d4
```

**智能项目容器过滤：**
```bash
$ deck ps
💡 提示：您也可以直接使用 Podman 命令：
💡 查看所有容器：podman ps -a
💡 查看系统容器：podman ps -a --all

当前项目相关容器：

运行中 (2)：
- nodejs-app-20250121-1430-dev [ID: a1b2c3d4] 运行时间: 2小时
- nodejs-app-20250121-1430-prod [ID: e5f6g7h8] 运行时间: 1天

已停止 (1)：  
- nodejs-app-20250121-1430-test [ID: x9y8z7w6] 退出码: 0
```

## 🛠️ 系统要求

- **容器引擎**: Docker 或 Podman（推荐）
- **操作系统**: 
  - Windows 10/11 (x64/ARM64)
  - macOS 10.15+ (Intel/Apple Silicon)
  - Linux (x64/ARM64)

## 🤝 贡献

我们欢迎社区贡献！

### 开发环境搭建

```bash
# 克隆仓库
git clone https://gitee.com/zhaoquan/deck.git # 或 git clone https://gitee.com/zhaoquan/deck.git
cd deck

# 安装依赖
dotnet restore

# macOS 构建
.scripts/build.sh # 或 Windows .scripts/build.ps1

# macOS 打包
.scripts/package.sh # 或 Windows .scripts/package.ps1

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

本项目基于 [MIT License](LICENSE) 开源。

Copyright © 2025 Deck Team

## 🔗 相关链接

- [问题反馈](https://gitee.com/zhaoquan/deck/issues)
- [讨论区](https://github.com/chatterzhao/deck/discussions)

---

⭐ 如果这个项目对您有帮助，请给我们一个 Star！