# 模板目录说明

Deck（/dɛk/ "代克" [GitHub](https://github.com/chatterzhao/deck/releases) | [Gitee](https://gitee.com/zhaoquan/deck/releases/) ）是搭建容器化开发或生产环境的命令行工具，一个 `deck start` 命令自动尝试安装 podman 或 docker 并启动、根据 deck 官方模板快速构建镜像和启动容器，减少摩擦，助力容器化开发或生产快速起步
> start 命令会自动安装 podman 或 docker，如果您想手工安装或者自动安装失败，请先手工安装 [podman](https://podman.io) 和 [podman-compose](https://github.com/containers/podman-compose) 或 [docker](https://www.docker.com) 和 [docker-compose](https://docs.docker.com/compose/)

## 模板兼容性说明

Deck 工具不要求新格式，兼容 `podman-compose build` 或 `docker-compose build` 命令的配置即满足 deck 工具的模板要求：

```bash
# 您可以不使用 deck 而使用 podman-compose 或 docker-compose 进行测试，但是注意要先 cd 到模板目录
cd xx
podman-compose build
podman-compose up
```

> 注意：deck 的使用是要先安装 deck 工具（ [GitHub](https://github.com/chatterzhao/deck/releases) | [Gitee](https://gitee.com/zhaoquan/deck/releases/) ），必须在您项目的`根目录`而不是在模板目录执行 `deck start` 命令。执行后，它会在执行命令所在路径生成 `.deck` 目录，并自动从 deck 工具仓库拉取官方模板到该目录（需联网），之后你可以看到 `templates` 目录，如果要修改，可以手工复制或创建某个模板到 `custom` 目录，然后修改为自己的。注意不能直接修改 `templates` 目录下的配置，因为每次执行 `deck start` 命令都会从仓库拉取最新模板覆盖。

---

## 🧱 模板设计推荐模式

目前支持两种主流的模板设计模式，适用于不同的使用场景和维护需求：
> 设计模式仅仅是建议，不是要求

### 模板设计模式对比

| 特性 | 一体化模板 | 分层复用模板 |
|------|------------|----------------|
| **结构** | 单层，所有内容包含在一个模板中 | 多层，如 `linux -> dotnet -> avalonia` |
| **构建方式** | 一次构建完成 | 逐层构建（从基础层开始） |
| **复用性** | 不可复用 | 高度复用，基础层可被多个子模板共享 |
| **维护难度** | 简单 | 稍复杂，但升级只需改某一层 |
| **构建速度** | 每次修改都需要重新构建整个镜像 | 利用缓存，仅修改层需重新构建 |
| **适用场景** | 快速原型开发、独立项目 | 多项目共享技术栈、需要频繁升级基础环境 |
| **扩展性** | 有限 | 高，支持组合式模板设计 |

### 1. 一体化模板（Monolithic Template）
> 构建后一个镜像
**特点**：
- 一个模板包含完整的开发环境配置。
- 不依赖其他模板，完全自包含。

**适用场景**：
- 快速原型开发。
- 独立项目，不需要复用基础层。
- 不需要频繁升级基础环境的项目。

**优点**：
- 配置简单，易于理解。
- 快速启动，无需多层构建。

**缺点**：
- 重复构建成本高。
- 不利于复用。

### 2. 分层复用模板（Layered Reusable Template）
> 构建后多个镜像，但存储层面是共享的，存储空间并没有增加：在存储层面，Podman/Docker 会共享相同的底层镜像层，节省磁盘空间。如项目层的镜像，通过 Docker 的分层文件系统机制，高层镜像实际上包含了底层镜像的所有内容，是一个完整的、可独立运行的镜像，但存储上它没有重复包含底层镜像

**特点**：
- 建议的分层：基础层->技术栈层->项目层，如：`ubuntu -> dotnet -> avalonia`
- 每一层专注于特定的功能，可被复用
- 构建时需从最基础层开始逐层构建

**适用场景**：
- 多个项目共享相同技术栈（如多个 .NET 项目）。
- 需要频繁升级基础环境（如 .NET SDK）。
- 希望提升模板的可维护性和一致性。
- 个人电脑，涉猎广泛

**优点**：
- 提高复用性，减少重复构建。
- 加快构建速度（利用构建缓存）。
- 易于维护和升级。

**缺点**：
- 初期配置耗时，越底层的越需要仔细配置，这样复用时才会被选用

**分层复用模板的两种构建方式**：
- 分层构建
- 统一构建，通过 compose.yaml 配置来实现

---

## 📐 分层复用结构示意图

```
┌────────────────────────────┐
│     基础层（Base Layer）     │
│     ubuntu / alpine / ...   │
│  - 基础系统（apt, yum）     │
│  - 常用工具（git, curl）    │
│  - 非root用户配置           │
└──────────────┬─────────────┘
               │
┌──────────────▼─────────────┐
│    技术栈层（Tech Layer）   │
│    dotnet / java / python  │
│  - SDK / 运行时            │
│  - 开发工具（dotnet-watch）│
│  - 环境变量配置            │
└──────────────┬─────────────┘
               │
┌──────────────▼─────────────┐
│  项目层（Project Layer）   │
│ avalonia / beeware / webapi│
│  - 项目依赖安装            │
│  - 启动脚本                │
│  - 特定配置                │
└────────────────────────────┘
```

---

## 📂 模板目录结构说明

每个模板目录下应包含以下标准文件：

```
templates/
└── <template-name>/
    ├── .env                 # 环境变量配置文件，解决仅仅修改变量就能解决问题的场景（非必须）
    ├── Dockerfile           # 容器构建文件
    ├── compose.yaml         # 容器编排文件
    └── README.md            # 模板说明文档（非必须）
```

### 📄 文件说明

#### 1. `.env`
- 用于定义环境变量，分构建变量和运行时变量，如 `DOTNET_VERSION=9.0`、`WORKSPACE_PATH=/workspace`。
- 使用 `${VARIABLE:-default}` 语法在 [compose.yaml](/templates/dotnet/compose.yaml) 中引用。

#### 2. [Dockerfile](/templates/dotnet/Dockerfile)
- 定义镜像构建过程。
- 使用 `ARG` 声明构建参数。

#### 3. [compose.yaml](/templates/dotnet/compose.yaml)
- 定义服务、卷、环境变量、构建参数等。

#### 4. `README.md`
- 模板说明文档。
- 包含模板功能、构建方式、使用方法、依赖关系等信息。