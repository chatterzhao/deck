# Deck

Deck（/dɛk/ "代克" [GitHub](https://github.com/chatterzhao/deck/releases) | [Gitee](https://gitee.com/zhaoquan/deck/releases/) ）是搭建容器化开发或生产环境的命令行工具，一个 `deck start` 命令自动尝试安装 podman 或 docker 并启动、根据 deck 官方模板快速构建镜像和启动容器，减少摩擦，助力容器化开发或生产快速起步。

使用 Deck 您主要需要管理配置文档，管理文档化，配置兼容 podman-compose 和 docker-compose，假如不使用 Deck 您的配置仍然有价值，或者您之前可用的配置，可以轻松迁移到 Deck ，让您专注于业务开发而非环境配置。

Deck .NET 版基于 .NET 9 构建，AOT，跨平台原生性能，支持 Windows、macOS 和 Linux 平台。

## Deck 主要特性

- **自动安装** - 自动尝试安装 podman 或 docker 并启动
- **模板复用** - 通过仓库维护模板文件，需要搭建环境时一个命令构建出一样的环境
- **快速起步** - 从零到运行开发环境，只需一个 deck start 命令
- **跨平台** - 本工具支持 Windows、macOS 和 Linux 平台使用
- **原生性能** - 本工具 AOT 编译，启动迅速，低资源占用
- **易扩展** - 支持自定义模板和项目特定配置，模板与工具可独立维护

## 快速开始

### 下载预编译版本

从 [Github Releases](https://github.com/chatterzhao/deck/releases) 或 [Gitee Releases](https://gitee.com/zhaoquan/deck/releases/) 页面下载适合您系统的版本：

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
2. 双击运行安装程序（双击安装包后，如果提示安全问题，问是否移动到“废纸篓”，不要点这个按钮，点其他的按钮，比如“确定”，然后打开“系统设置”中的“安全性和隐私”，滚到底，在安全区域，点击“仍要打开”）
3. 按向导完成安装
3. 安装完成后，在终端中运行 `deck --help` 验证
4. 终端执行 `deck-uninstall`

#### Linux
1. 下载对应的 `.deb` 或 `.rpm` 包
2. Ubuntu/Debian: `sudo dpkg -i deck-vX.X.X-linux-x64.deb`
3. CentOS/RHEL: `sudo rpm -ivh deck-vX.X.X-linux-x64.rpm`
4. 或通过包管理器安装（如 apt、yum）
5. 验证安装：`deck --help`
6. 卸载方法：终端执行命令 `rm -rf /usr/local/bin/deck`

### 开始使用 deck 命令

```bash
# 假如您还没有项目目录，则在终端执行命令或手工创建项目目录
# 在宿主机创建（不是容器内）
mkdir my-project
```
```bash
# 在终端执行命令进入您的项目目录
cd my-project
```

### 执行 deck start 命令
> 注意：deck start 命令必须在项目根目录（my-project）比如 `weichat/` 下运行，因为它会自动创建 .deck 目录，并自动把根目录挂在到容器的 workspace。此命令会自动尝试安装 podman 或 docker 并启动，如果您想手工安装或者自动安装失败，请先手工安装 [podman](https://podman.io) 和 [podman-compose](https://github.com/containers/podman-compose) 或 [docker](https://www.docker.com) 和 [docker-compose](https://docs.docker.com/compose/)
```bash
# 在终端应用执行命令启动交互式环境选择
deck start

# 或直接指定环境模板前缀进行筛选模板
# 建议模板命名是`xx-yy`，如`avalonia-default`，命令会自动取前缀xx，它的作用仅是筛选，当前缀没有命中，会显示所有模板
deck start nodejs
deck start python
deck start golang
```

Deck 三层目录 `templates -> custom -> images`和 `config.json` 文件的说明：
```
your-project/
└── .deck/                    # 工具管理目录（执行 deck start 命令后自动创建）
    ├── config.json           # 配置文件
    ├── templates/            # 远程模板（执行 deck start 命令后会从仓库拉取最新模板覆盖）
    │   ├── nodejs-default/
    │   ├── python-default/
    │   └── golang-default/
    ├── custom/               # 用户配置（可编辑）
    │   ├── nodejs-customized/
    │   └── python-api/
    └── images/               # 构建记录（不建议修改，最佳实践是永远与已构建镜像一致）
        ├── nodejs-customized-20250121-1430-dev/
        ├── nodejs-customized-20250121-1430-prod/
        └── python-api-20250120-0920/
```
- templates 目录存放官方模板。您可在 .deck/config.json 中设置您自己的仓库链接（注意：这个目录每次执行 deck start 命令都会从仓库拉取最新模板覆盖，所以你不应在这个目录进行模板配置，否则执行命令就丢失，应该复制到 custom 目录进行配置）。`.deck/templates` 目录在您项目建议添加 .gitignore 忽略
- custom 目录存放您的自定义模板。您可以在 custom 目录里创建新的模板，也可以复制 templates 目录里的模板来这里修改。`.deck/custom` 目录在您项目建议添加 .gitignore 忽略
- images 目录存放您基于 templates 或 custom 目录里某个配置构建镜像的配置。为了方便您与镜像或容器进行对照，知道当前镜像或容器的构建配置是什么，除 .env 里的运行时变量可修改外（运行时变量，每次启动容器会读取。建议开发或测试可这样直接修改，标准操作应该是 custom 修改好一个配置，构建后，images 不做任何修改，永远与构建后的镜像或容器能对应上）。假如您自定义的某个配置，当您构建镜像等测试没有问题，可以复制到 templates 目录，然后提交到 github 仓库，之后执行 deck start 将会自动从仓库获取到，这也是您内部团队共用模板的方式。`.deck/images` 目录建议参与 git 跟踪，因为这是最终与您镜像和容器对应对配置。注：如果您仅仅想通过文本与团队进行分享而不是镜像文件，请禁止在容器内部手工修改配置版本，比如 dotnet8 -> dotnet9 等，否则您别人用您的配置拉取的镜像，运行您的项目可能与您的不一致。
- config.json 用于存放配置模板的链接。如果您想更换模板为您自己的仓库，您需要修改链接，否则不需要修改

### deck 其他命令
> 可使用 podman 相关命令代替，本工具主要解决的是 start 命令相关的功能

```bash
# 查看帮助
deck --help
deck <command> --help             # 如 deck stat --help
# 系统管理
deck doctor                       # 系统诊断
deck clean                        # 智能清理（三层配置选择：Images/Custom/Templates）

# 环境管理
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

## 📚 使用指南

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

## 镜像与容器概念

- **镜像（Image）**: 静态的应用模板，类似于"光盘镜像"
- **容器（Container）**: 镜像的运行实例，类似于"正在运行的程序"
- **关系**: 一个镜像可以创建多个容器，删除镜像前需要先处理所有相关容器

**podman 命令：**

```bash
# 容器操作（操作运行实例）
podman ps -a                      # 查看所有容器（运行中+已停止）
podman stop [CONTAINER-name/ID]        # 停止容器
podman rm [CONTAINER-name/ID]          # 删除容器

# 镜像操作（操作静态模板）
podman images                     # 查看所有镜像
podman rmi [IMAGE-name/ID]        # 删除镜像（需要先删除相关容器）
podman build -t [IMAGE-NAME] .    # 构建镜像

# 清理构建缓存
podman builder prune -f

# 清理系统资源
podman system prune -f
```

## 容器开发最佳实践

### VS Code容器开发

1. **安装必要 VS Code 扩展**：
   - [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) - 容器开发支持

2. **Podman配置**：
   > 如果您安装是 docker 则不用配置，否则要配置，不然下一步连接时，会提示没有安装 docker
   如果使用Podman而非Docker，需要在VS Code设置中添加以下配置：
   ```json
   {
     "remote-containers.containerEngine": "podman",
     "dev.containers.dockerPath": "podman"
   }

3. **连接到运行中的容器**：
> 先把容器运行起来
   1. 点击VS Code左下角的`><`图标
   2. 选择"Attach to Running Container..."或"附加到正在运行的容器..."
   3. 将列出容器，选择目标容器
   4. 这个时候，一般 VS Code 左侧资源管理器还看不到打开的项目，你需要：
     - 点击“资源管理器”图标
     - 然后看到“已连接到远程。”下面有个按钮“打开文件夹”，点击它
     - 弹窗输入框会有"/root/"，把它删除，然后输入“workspace"，然后在现实的列表找到 “workspace”，点击它
     - 然后点输入框右侧的“确定”按钮
     - 这个时候资源管理器就现实项目了
     - 之后就正常进行开发

   5. 如果您还没有通过上一步打开项目，你可在终端中输入 "cd /workspace"，注意要有斜杠

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

# 运行测试
dotnet test

# 运行开发版本
dotnet run --project src/Deck.Console

# macOS 构建（本地）
# 构建后在 build/release 下按您系统平台找到 deck.console 即可复制路径到终端执行
./scripts/build.sh # 或 Windows ./scripts/build.ps1

# macOS 打包（本地）
./scripts/package.sh # 或 Windows ./scripts/package.ps1

# 使用 GitHub 发布
# 通过 tag 触发 GitHub Action
git tag vxx && git push origin vxx

# 删除本地和远程 tag
git tag -d vxx && git push --delete origin vxx
```

### 版本管理

本项目使用 Git tag 作为版本号的唯一来源，确保所有构建和分发包使用一致的版本号。

#### 发布流程（自动版本更新）

通过设置的 Git hooks，可以实现推送 tag 时自动更新版本号：
> 前置条件（先运行）：`./scripts/setup-hooks.sh`
1. 创建 Git tag：`git tag -a v1.2.3 -m "Release version 1.2.3"`
2. 推送 tag（自动更新版本号）：`git push origin v1.2.3`
3. 推送其他更改（如果需要）：`git push`

系统会自动：
- 检测到 tag 推送
- 更新项目中的版本号
- 提交更改（如果需要）

GitHub Actions 会自动验证版本一致性，构建所有平台的二进制文件，创建分发包，并发布到 GitHub Releases。

#### 手动发布流程（可选）

如果你不想使用自动版本更新，也可以手动执行：

1. 更新版本号：`./scripts/update-version.sh 1.2.3`
2. 提交更改：`git add . && git commit -m "chore: update version to 1.2.3" && git push`
3. 创建 Git tag：`git tag -a v1.2.3 -m "Release version 1.2.3"`
4. 推送更改和 tag：`git push && git push origin v1.2.3`

#### 版本号格式
版本号采用 `vX.Y.Z` 格式，遵循 [语义化版本控制规范](https://semver.org/lang/zh-CN/)：
- `v` 前缀是可选的
- `X` 是主版本号（重大变化）
- `Y` 是次版本号（新增功能）
- `Z` 是修订版本号（问题修复）

#### 自动版本提取
构建脚本会自动从 Git tag 提取版本号：
```bash
# 提取版本号并移除 'v' 前缀
TAG_VERSION="${GITHUB_REF_NAME#v}"
```

#### 版本管理工具
项目提供了以下脚本来管理版本：

1. **版本验证脚本** - 验证版本一致性：
   ```bash
   # 验证当前版本一致性
   ./scripts/validate-version.sh
   
   # 验证新版本是否比当前版本大
   ./scripts/validate-version.sh v1.2.3
   ```

2. **版本更新脚本** - 更新项目中的版本号：
   ```bash
   # 更新项目版本号
   ./scripts/update-version.sh 1.2.3
   ```

3. **Git hooks 设置** - 设置 Git hooks：
   ```bash
   # 设置 Git hooks（包括 pre-commit 和 pre-push）
   ./scripts/setup-hooks.sh
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