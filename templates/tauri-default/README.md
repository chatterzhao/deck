# Tauri 桌面应用开发环境模板

## 🎯 模板功能概览

这个模板提供了一个功能完整的 Tauri 开发环境，包含以下核心能力：

### 🖥️ 桌面平台支持
- **Linux** - 原生开发和运行
- **Windows** - 交叉编译支持（mingw-w64）
- **macOS** - 交叉编译准备（需 macOS 最终构建）
- **多架构** - x86_64、ARM64 支持

### ⚡ 开发特性
- **Rust 工具链** - 最新稳定版（1.76.0）
- **Node.js 环境** - 现代前端支持（20.11.1）
- **Tauri CLI** - 官方开发工具（1.5.10）
- **热重载** - 前后端同步热重载
- **国内镜像优化** - Cargo、npm 中国镜像加速
- **持久化缓存** - Rust、Node.js 依赖缓存

### 🌐 前端支持
- **任意框架** - React、Vue、Svelte、Angular
- **构建工具** - Vite、Webpack、Rollup
- **包管理器** - npm、yarn、pnpm

## 🔧 可配置项（通过 .env 控制）

### 版本控制
```bash
RUST_VERSION=1.76.0             # Rust 工具链版本
NODEJS_VERSION=20.11.1          # Node.js 版本
TAURI_CLI_VERSION=1.5.10        # Tauri CLI 版本
UBUNTU_VERSION=22.04            # 基础系统版本
```

### 镜像源配置
```bash
DOCKER_REGISTRY=docker.m.daocloud.io                               # Docker 镜像源
APT_MIRROR=mirrors.ustc.edu.cn                                     # APT 软件源
CARGO_REGISTRY_SPARSE=sparse+https://mirrors.tuna.tsinghua.edu.cn/crates.io-index/  # Cargo 镜像
NPM_REGISTRY=https://registry.npmmirror.com                        # npm 镜像
NODEJS_MIRROR=https://mirrors.tuna.tsinghua.edu.cn/nodejs-release/ # Node.js 镜像
```

### 端口配置
```bash
DEV_PORT=1420                   # Tauri 开发端口
DEBUG_PORT=9229                 # 调试端口
VITE_PORT=1421                  # Vite 前端端口
```

### 资源限制
```bash
MEMORY_LIMIT=4g                 # 内存限制
CPU_LIMIT=2                     # CPU 限制
SHM_SIZE=512m                   # 共享内存大小
```

### 缓存路径
```bash
CARGO_CACHE_PATH=/usr/local/cargo/registry        # Cargo 缓存目录
NODE_MODULES_CACHE_PATH=/usr/local/node_modules_cache  # Node.js 缓存目录
```

## 📝 使用说明

### 如果默认配置满足需求
直接使用模板，预配置了国内镜像源和稳定的工具链版本。

### 如果需要自定义配置
1. 复制整个模板到 `custom` 目录
2. 创建或修改 `.env` 文件设置所需变量
3. 根据需要修改 `compose.yaml` 和 `Dockerfile`

### 快速开始
```bash
# 构建镜像并启动容器
deck start

# 进入开发容器
docker-compose exec tauri-dev bash

# 创建新项目（如果尚未存在）
tauri init

# 安装前端依赖
npm install

# 启动开发服务器
tauri dev

# 交叉编译 Windows 应用
cargo build --target x86_64-pc-windows-gnu
```
