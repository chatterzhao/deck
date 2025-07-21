# Avalonia 跨平台开发环境模板

## 🎯 模板功能概览

这个模板提供了一个功能完整的 Avalonia 开发环境，包含以下核心能力：

### 🖥️ 多平台支持
- **Linux 桌面** - 直接运行和调试（X11 GUI 支持），内置环境
- **Windows** - 交叉编译目标，内置环境
- **macOS** - 需要 macOS 系统安装 Xcode 最新版
- **iOS** - 需要在 macOS 上安装 Xcode 最新版
- **Android** - 根据需要在 .env 配置改为 true，默认false
- **WebAssembly** - 浏览器应用支持,根据需要在 .env 配置改为 true，默认false

### ⚡ 开发特性
- **.NET 9.0 SDK** - 最新工具链
- **热重载** - `dotnet watch` 自动重编译
- **调试支持** - 内置调试器（端口 9229）
- **缓存优化** - NuGet 和工具持久化缓存
- **GUI 转发** - X11 支持容器内运行桌面应用

## 🔧 可配置项（通过 .env 控制）

### 版本控制
```bash
DOTNET_VERSION=9.0              # .NET SDK 版本
AVALONIA_VERSION=11.3.2         # Avalonia 框架版本
ANDROID_SDK_VERSION=35          # Android SDK 版本
ANDROID_NDK_VERSION=25.2.9519653 # Android NDK 版本
```

### 端口配置
```bash
DEV_PORT=5000                   # 开发服务器端口
DEBUG_PORT=9229                 # 调试端口
WEB_PORT=8080                   # Web 应用端口
ANDROID_DEBUG_PORT=5037         # Android 调试桥端口
```

### 资源限制
```bash
MEMORY_LIMIT=4g                 # 内存限制
CPU_LIMIT=2                     # CPU 限制
SHM_SIZE=512m                   # 共享内存大小
```

### 镜像源配置
```bash
DOCKER_REGISTRY=mcr.microsoft.com  # 微软镜像源
```

## 📝 使用说明

### 如果默认配置满足需求
直接使用模板，所有配置都有合理的默认值。

### 如果需要自定义配置
1. 复制整个模板到 `custom` 目录
2. 修改 `.env` 文件设置所需变量
3. 根据需要修改 `compose.yaml` 和 `Dockerfile`

### 快速开始
```bash
# 构建镜像并启动容器
deck start

# 进入开发容器
docker-compose exec avalonia-dev bash

# 创建新项目
dotnet new avalonia -n MyApp
cd MyApp

# 启动热重载开发
dotnet watch run
```