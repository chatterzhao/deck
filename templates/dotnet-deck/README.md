# Deck 项目开发模板

这是一个专门用于开发 Deck 项目本身的 .NET 开发环境模板。

如果您想参与 Deck 项目，请使用此模板。

## 模板特性

- 基于 .NET 9 SDK 的开发环境
- 预装开发工具 (vim, nano)
- 预配置的 .NET 全局工具
- 容器资源限制 (默认 4GB 内存, 2 CPU)
- 热重载支持
- 调试支持
- 共享内存配置 (512MB)
- DNS 配置优化

## 预装的 .NET 工具

- `dotnet-watch` - 文件变化自动重载
- `dotnet-format` - 代码格式化工具
- `dotnet-outdated` - NuGet 包更新检查
- `dotnet-trace` - 性能分析工具
- `dotnet-serve` - 静态文件服务器

## 预装的系统工具

- `vim` - 终端文本编辑器
- `nano` - 简单易用的文本编辑器
- `git` - 版本控制系统
- `curl` - 命令行下载工具
- `wget` - 网络下载工具

## 启动本配置的开发环境

### 方法一：使用Deck工具（推荐）

如果你已经安装了Deck工具，只需在Deck项目根目录下运行：
> 启动容器如果报 `something went wrong with the request: "listen tcp :5000: bind: address already in use`，请直接修改 .env 文件中的 `DEV_PORT=5000` 值，比如该为 5001

```bash
deck start
```

然后选择`dotnet-deck`模板，安装命令引导讲进行镜像构建并启动容器。

### 方法二：直接使用容器工具

如果你还没有安装Deck工具，或者想直接使用容器工具，请cd到本配置目录，然后执行：

```bash
# 对于 podman
podman-compose build # 构建镜像
podman-compose up -d # 启动容器

# 对于 docker
docker-compose build # 构建镜像
docker-compose up -d # 启动容器
```

## 使用 VS Code 进行开发

### 1. 安装必要的扩展

为了获得最佳的开发体验，请在VS Code中安装以下扩展：

- [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) - C#语言支持
- [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) - 容器开发支持
- [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) - 代码格式化支持

### 2. 连接到容器

如果你使用VS Code进行开发，可以通过以下步骤连接到容器：

通过点击VS Code左下角的`><`图标：
1. 选择 `Attach to Running Container...` 或 `附加到正在运行的容器...`
2. 选择"deck-dev"容器
3. 这个时候，一般 VS Code 左侧资源管理器还看不到打开的项目，你需要：
  - 点击“资源管理器”图标
  - 然后看到“已连接到远程。”下面有个按钮“打开文件夹”，点击它
  - 弹窗输入框会有"/root/"，把它删除，然后输入“workspace"，然后在现实的列表找到 “workspace”，点击它
  - 然后点输入框右侧的“确定”按钮
  - 这个时候资源管理器就现实项目了

4. 如果您还没有通过上一步打开项目，你可在终端中输入 "cd /workspace"，注意要有斜杠

或者
1. 打开VS Code
2. 按`Ctrl+Shift+P`（Windows/Linux）或`Cmd+Shift+P`（Mac）打开命令面板
3. 输入并选择"Dev Containers: Attach to Running Container..." 或 `附加到正在运行的容器...`
4. 选择"deck-dev"容器
5. 这个时候，一般 VS Code 左侧资源管理器还看不到打开的项目，你需要：
  - 点击“资源管理器”图标
  - 然后看到“已连接到远程。”下面有个按钮“打开文件夹”，点击它
  - 弹窗输入框会有"/root/"，把它删除，然后输入“workspace"，然后在现实的列表找到 “workspace”，点击它
  - 然后点输入框右侧的“确定”按钮
  - 这个时候资源管理器就现实项目了

6. 如果您还没有通过上一步打开项目，你可在终端中输入 "cd /workspace"，注意要有斜杠

> Dev Containers 扩展默认是为 Docker 设计的，如果您电脑安装的是 podman 还是会提示要安装 Docker，您可以通过下面的设置让它认 podman
> 
> 打开vscode settings.json 添加`"dev.containers.dockerPath": "podman"`

连接成功后，VS Code会自动加载项目目录，并提供以下开发优势：

- 完整的IntelliSense支持
- 代码导航和重构功能
- 断点调试支持
- 终端集成，可直接在VS Code中运行命令

### 3. 其他进入容器的方式

如果不想使用VS Code，也可以通过终端命令行进入容器：

```bash
# 对于 podman
podman exec -it deck-dev bash

# 对于 docker
docker exec -it deck-dev bash
```

或者直接在容器中运行Deck项目：

```bash
# 对于 podman
podman exec -it deck-dev deck-dev run

# 对于 docker
docker exec -it deck-dev deck-dev run
```

## 开发工作流程

### 1. 文件编辑

容器内预装了多种文本编辑器：

- `vim` - 功能强大的终端编辑器
- `nano` - 简单易用的编辑器

你也可以在宿主机上使用你喜欢的IDE编辑文件，因为项目目录已经挂载到容器中。

### 2. 运行和测试

进入容器后，您可以使用以下命令：

```bash
# 运行 Deck 项目（两种方式）
deck-dev run        # 使用模板提供的便捷命令
dotnet run --project src/Deck.Console  # 直接使用dotnet命令

# 运行测试
dotnet test

# 构建项目
dotnet build

# 格式化代码
dotnet format

# 检查过时的 NuGet 包
dotnet outdated

# 性能分析
dotnet trace
```

### 3. 实时开发

容器支持文件热重载，当你在宿主机上修改代码时，可以通过以下方式实时查看效果：

1. 使用`dotnet watch`命令运行项目：
   ```bash
   dotnet watch --project src/Deck.Console
   ```

2. 在宿主机上修改代码文件

3. 项目会自动重新编译和运行

### 4. 调试

VS Code提供了完整的调试支持：

1. 在代码中设置断点
2. 按F5或点击"开始调试"按钮
3. 选择".NET Core Launch"配置
4. 程序将在断点处暂停，你可以检查变量、调用堆栈等

容器已配置调试支持，你也可以：

1. 使用.NET调试器附加到运行中的进程
2. 设置断点进行调试
3. 使用`dotnet trace`进行性能分析

## 配置选项

### 构建时配置

| 环境变量 | 默认值 | 描述 |
|---------|--------|------|
| `UBUNTU_VERSION` | 22.04 | Ubuntu 版本 |
| `DOTNET_VERSION` | 9.0 | .NET 版本 |
| `DEPENDENCIES` | curl git wget build-essential ca-certificates vim nano | 系统依赖包 |

### 运行时配置

| 环境变量 | 默认值 | 描述 |
|---------|--------|------|
| `DEV_PORT` | 5000 | 开发服务器端口 |
| `DEBUG_PORT` | 9229 | 调试端口 |
| `MEMORY_LIMIT` | 4g | 内存限制 |
| `CPU_LIMIT` | 2 | CPU 限制 |
| `SHM_SIZE` | 512m | 共享内存大小 |

### 工具配置

| 环境变量 | 默认值 | 描述 |
|---------|--------|------|
| `INSTALL_DOTNET_WATCH` | true | 安装文件监视工具 |
| `INSTALL_DOTNET_FORMAT` | true | 安装代码格式化工具 |
| `INSTALL_DOTNET_OUTDATED` | true | 安装包更新检查工具 |
| `INSTALL_DOTNET_TRACE` | true | 安装性能分析工具 |
| `INSTALL_DOTNET_SERVE` | true | 安装静态文件服务器 |

## 目录结构

容器内的重要目录：

```
/workspace          # 挂载的项目根目录
/opt/nuget-cache    # NuGet 包缓存
/root/.dotnet/tools # .NET 全局工具
```

## 故障排除

### 构建问题

如果遇到构建问题，可以尝试清理构建缓存：

```bash
# 对于 Docker
docker builder prune -f

# 对于 Podman
podman builder prune -f
```

### 权限问题

如果遇到权限问题，请检查挂载目录的权限设置。

### 网络问题

模板配置了 Google 和 Cloudflare 的 DNS 服务器。如果仍然遇到网络问题，请检查您的网络配置。