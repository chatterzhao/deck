# Ubuntu 开发环境

这是 Deck 项目的 Ubuntu 基础开发环境模板。它提供了一个安装了常用开发工具的最小化 Ubuntu 系统。Ubuntu 版本可以通过 `UBUNTU_VERSION` 环境变量进行配置。

## 功能特性

- 可配置的 Ubuntu 基础系统（通过 `UBUNTU_VERSION` 变量）
- 常用开发工具（git、curl、wget、ssh、build-essential）
- 用于安全的非 root 用户
- 工作目录位于 `/workspace`
- 容器内缓存支持（APT缓存）
- 可通过 [.env](/templates/ubuntu/.env) 文件进行配置

## 组件

- **基础系统**: Ubuntu（版本通过 `UBUNTU_VERSION` 配置）
- **包管理器**: APT
- **预装工具**: 
  - git
  - curl
  - wget
  - ssh
  - build-essential
  - ca-certificates

## 使用方法

通过 Deck 使用此模板：

```bash
deck start ubuntu
```

或者创建可编辑的配置：

```bash
deck start ubuntu --editable
```

## 配置

您可以通过修改 [.env](/templates/ubuntu/.env) 文件来自定义此环境：

- `UBUNTU_VERSION`：Ubuntu 版本（默认：22.04）
- `UBUNTU_NAME`：Ubuntu 版本的简化标识符（如 2204），用于基础镜像名称
- `DOCKER_REGISTRY`：基础镜像的 Docker 仓库
- `DEPENDENCIES`：要安装的额外系统包
- `PROJECT_NAME`：容器名称
- `WORKSPACE_PATH`：挂载项目文件的路径
- `MEMORY_LIMIT`：容器内存限制（默认：2g）
- `CPU_LIMIT`：容器 CPU 限制（默认：2）
- `SHM_SIZE`：共享内存大小（默认：512m）

## 缓存

此模板支持容器内缓存，以提高构建和包安装速度：

- APT 包管理器缓存挂载到 `/var/cache/apt` 和 `/var/lib/apt`
- 缓存通过 Docker 命名卷持久化存储
- 不同项目间的缓存是隔离的，确保安全性

## 网络和端口

此模板默认不暴露特定端口。您可以修改 [compose.yaml](/templates/ubuntu/compose.yaml) 文件以根据需要添加端口映射。

## 卷

- 您的项目目录挂载到容器内的 `/workspace`
- APT 缓存通过命名卷持久化存储