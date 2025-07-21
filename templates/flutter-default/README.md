# Flutter 跨平台开发环境模板

## 🎯 模板功能概览

这个模板提供了一个功能完整的 Flutter 开发环境，包含以下核心能力：

### 📱 多平台支持
- **Android** - 完整 SDK 支持（API 33，Java 17）
- **iOS** - 开发支持（需 macOS 最终构建）
- **Linux 桌面** - 原生开发和运行
- **Windows/macOS** - 交叉编译支持
- **Web/PWA** - 浏览器应用支持

### ⚡ 开发特性
- **Flutter SDK** - 最新稳定版（3.19.6）
- **Dart SDK** - 配套版本（3.3.4）
- **热重载** - 即时代码更新和 UI 刷新
- **国内镜像优化** - 清华大学等镜像源加速
- **持久化缓存** - Flutter SDK、Pub 包、Android SDK

## 🔧 可配置项（通过 .env 控制）

### 版本控制
```bash
FLUTTER_VERSION=3.19.6          # Flutter SDK 版本
DART_VERSION=3.3.4              # Dart SDK 版本  
ANDROID_SDK_VERSION=33          # Android SDK 版本
JAVA_VERSION=17                 # Java 版本
UBUNTU_VERSION=22.04            # 基础系统版本
```

### 镜像源配置
```bash
DOCKER_REGISTRY=docker.m.daocloud.io           # Docker 镜像源
APT_MIRROR=mirrors.ustc.edu.cn                 # APT 软件源
PUB_HOSTED_URL=https://mirrors.tuna.tsinghua.edu.cn/dart-pub  # Pub 包源
FLUTTER_STORAGE_BASE_URL=https://mirrors.tuna.tsinghua.edu.cn/flutter  # Flutter 镜像
```

### 端口配置
```bash
DEV_PORT=3000                   # 开发服务器端口
DEBUG_PORT=9229                 # 调试端口
HOT_RELOAD_PORT=8080           # 热重载端口
```

### 下载源配置
```bash
FLUTTER_DOWNLOAD_URL=https://github.com/flutter/flutter.git  # Flutter 源码
FLUTTER_MIRROR_URL=https://storage.flutter-io.cn            # 中国镜像
FLUTTER_FALLBACK_URL=https://github.com/flutter/flutter/releases/download  # 备用源
```

## 📝 使用说明

### 如果默认配置满足需求
直接使用模板，预配置了国内镜像源和合理的版本组合。

### 如果需要自定义配置
1. 复制整个模板到 `custom` 目录
2. 创建或修改 `.env` 文件设置所需变量
3. 根据需要修改 `compose.yaml` 和 `Dockerfile`

### 快速开始
```bash
# 构建镜像并启动容器
deck start

# 进入开发容器
docker-compose exec flutter-dev bash

# 检查环境配置
flutter doctor

# 创建新项目
flutter create my_app && cd my_app

# Web 开发
flutter run -d web-server --web-port 3000

# Android 构建
flutter build apk
```