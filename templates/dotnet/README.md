# .NET 开发环境模板

该模板提供了一个基于 Ubuntu 模板的完整 .NET 开发环境。它包含了 .NET SDK 和 .NET 开发所需的通用工具。

## 功能特性

- 基于 Ubuntu 22.04
- .NET SDK 9.0
- 支持多平台（Linux、Windows、macOS、Android、WebAssembly）
- 常用 .NET 工具（dotnet-watch、dotnet-format 等）
- 可配置的工作负载（Android、WebAssembly 等）

## 使用方法

通过 Deck 使用此模板：

```bash
deck start dotnet
```

## 配置

您可以通过修改 [.env](/templates/dotnet/.env) 文件来自定义环境：

- `UBUNTU_NAME`：Ubuntu 版本的简化标识符（如 2204），用于基础镜像名称
- `DOTNET_VERSION`：要安装的 .NET 版本（如 9.0）
- `DOTNET_VERSION_NAME`：.NET 版本的简化标识符（如 90），用于镜像标签等场景
- `INSTALL_ANDROID_SUPPORT`：启用 Android 开发支持（默认：false）
- `INSTALL_WASM_TOOLS`：启用 WebAssembly 工具（默认：true）
- `INSTALL_WASM_EXPERIMENTAL`：启用 WebAssembly 实验性功能（默认：false）
- `INSTALL_WASI_EXPERIMENTAL`：启用 WASI 实验性功能（默认：false）
- 资源限制（内存、CPU）

## 已安装的工具

默认情况下，安装了以下工具：
- dotnet-watch（文件监视器）
- dotnet-format（代码格式化工具）

可选工具（默认禁用）：
- dotnet-ef（Entity Framework 工具）
- dotnet-outdated-tool（包分析器）
- dotnet-trace（性能分析器）
- dotnet-serve（静态文件服务器）

通过在 [.env](/templates/dotnet/.env) 文件中将相应的环境变量设置为 `true` 来启用这些工具。

## 工作负载

该模板支持安装各种 .NET 工作负载：
- Android 开发（默认禁用）
- WebAssembly 工具（默认启用）
- WebAssembly 实验性功能（默认禁用）
- WASI 实验性功能（默认禁用）

通过在 [.env](/templates/dotnet/.env) 文件中将相应的环境变量设置为 `true` 来启用这些工作负载。

## 端口

默认暴露以下端口：
- 5000：开发服务器
- 9229：调试
- 8080：Web 应用程序
- 8443：HTTPS

可以通过 [.env](/templates/dotnet/.env) 文件中的环境变量自定义这些端口。

## 镜像命名规范

生成的镜像将遵循以下命名规范：
> 如果冒号右侧没有内容，则默认添加 `latest`，如 `localhost/dotnet90:latest`
- 镜像名称：`localhost/dotnet${DOTNET_VERSION_NAME}:${DOTNET_VERSION}`
- 例如：`localhost/dotnet90:9.0`