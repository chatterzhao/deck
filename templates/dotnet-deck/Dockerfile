# =============================================================================
# Deck 项目开发环境 - 基于 .NET 模板
# =============================================================================

# 构建参数
ARG DOCKER_REGISTRY
ARG UBUNTU_VERSION
ARG UBUNTU_NAME
ARG DOTNET_VERSION
ARG DOTNET_VERSION_NAME

# 基于 dotnet 模板镜像
FROM localhost/dotnet${DOTNET_VERSION_NAME}:${DOTNET_VERSION} as deck-dev

# 定义构建参数
ARG DEPENDENCIES
ARG INSTALL_ANDROID_SUPPORT
ARG INSTALL_WASM_TOOLS
ARG INSTALL_WASM_EXPERIMENTAL
ARG INSTALL_WASI_EXPERIMENTAL
ARG INSTALL_DOTNET_WATCH
ARG INSTALL_DOTNET_FORMAT
ARG INSTALL_DOTNET_EF
ARG INSTALL_DOTNET_OUTDATED
ARG INSTALL_DOTNET_TRACE
ARG INSTALL_DOTNET_SERVE

# 安装额外的开发工具（vim和nano已经在DEPENDENCIES中）
RUN apt-get update && apt-get install -y ${DEPENDENCIES} && rm -rf /var/lib/apt/lists/*

# 创建 Deck 开发环境信息脚本
RUN echo '#!/bin/bash' > /usr/local/bin/deck-dev && \
    echo 'echo "🚀 Deck 项目开发环境"' >> /usr/local/bin/deck-dev && \
    echo 'echo "📁 Workspace: $(pwd)"' >> /usr/local/bin/deck-dev && \
    echo 'echo "🔷 .NET: $(/usr/share/dotnet/dotnet --version)"' >> /usr/local/bin/deck-dev && \
    echo "" >> /usr/local/bin/deck-dev && \
    echo 'echo "📦 已安装的功能:"' >> /usr/local/bin/deck-dev && \
    echo 'if /usr/share/dotnet/dotnet workload list 2>/dev/null | grep -q "android"; then echo "  ✅ Android 工作负载"; else echo "  ❌ Android 工作负载"; fi' >> /usr/local/bin/deck-dev && \
    echo 'if /usr/share/dotnet/dotnet workload list 2>/dev/null | grep -q "wasm-tools"; then echo "  ✅ WebAssembly 工具"; else echo "  ❌ WebAssembly 工具"; fi' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-ef > /dev/null 2>&1; then echo "  ✅ Entity Framework 工具"; else echo "  ❌ Entity Framework 工具"; fi' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-watch > /dev/null 2>&1; then echo "  ✅ 监视工具"; else echo "  ❌ 监视工具"; fi' >> /usr/local/bin/deck-dev && \
    echo "" >> /usr/local/bin/deck-dev && \
    echo 'echo "🔧 核心命令:"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  dotnet run --project src/Deck.Console    - 运行 Deck"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  dotnet build                            - 构建项目"' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-watch > /dev/null 2>&1; then echo "  dotnet watch                            - 监视文件变化"; fi' >> /usr/local/bin/deck-dev && \
    echo 'echo "  dotnet test                             - 运行测试"' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-ef > /dev/null 2>&1; then echo "  dotnet ef                               - Entity Framework 命令"; fi' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-format > /dev/null 2>&1; then echo "  dotnet format                           - 格式化代码"; fi' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-outdated-tool > /dev/null 2>&1; then echo "  dotnet outdated                         - 检查过时的包"; fi' >> /usr/local/bin/deck-dev && \
    echo 'if command -v dotnet-trace > /dev/null 2>&1; then echo "  dotnet trace                            - 收集跟踪信息"; fi' >> /usr/local/bin/deck-dev && \
    echo "" >> /usr/local/bin/deck-dev && \
    echo 'echo "🖥️  Deck 项目结构:"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  src/Deck.Console        - 命令行界面"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  src/Deck.Core           - 核心接口和模型"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  src/Deck.Services       - 业务服务实现"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  src/Deck.Infrastructure - 基础设施实现"' >> /usr/local/bin/deck-dev && \
    echo 'echo "  tests/                  - 测试项目"' >> /usr/local/bin/deck-dev && \
    echo "" >> /usr/local/bin/deck-dev && \
    echo 'if [ "$#" -eq 0 ]; then' >> /usr/local/bin/deck-dev && \
    echo '  exec bash' >> /usr/local/bin/deck-dev && \
    echo 'else' >> /usr/local/bin/deck-dev && \
    echo '  # 检查是否是要运行Deck项目' >> /usr/local/bin/deck-dev && \
    echo '  if [ "$1" = "run" ] && [ "$#" -eq 1 ]; then' >> /usr/local/bin/deck-dev && \
    echo '    exec /usr/share/dotnet/dotnet run --project src/Deck.Console' >> /usr/local/bin/deck-dev && \
    echo '  else' >> /usr/local/bin/deck-dev && \
    echo '    # 将参数传递给dotnet命令' >> /usr/local/bin/deck-dev && \
    echo '    exec /usr/share/dotnet/dotnet "$@"' >> /usr/local/bin/deck-dev && \
    echo '  fi' >> /usr/local/bin/deck-dev && \
    echo 'fi' >> /usr/local/bin/deck-dev && \
    chmod +x /usr/local/bin/deck-dev

WORKDIR /workspace
EXPOSE 5000 9229

CMD ["deck-dev"]