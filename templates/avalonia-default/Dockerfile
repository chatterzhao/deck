ARG DOCKER_REGISTRY
ARG DOTNET_VERSION
ARG AVALONIA_VERSION
ARG DEPENDENCIES

# =============================================================================
# Development Stage - 完整的开发环境
# =============================================================================
# 使用官方.NET SDK镜像作为基础
FROM ${DOCKER_REGISTRY}/dotnet/sdk:${DOTNET_VERSION} as development

# Install additional system dependencies
RUN apt-get update && apt-get install -y ${DEPENDENCIES} && rm -rf /var/lib/apt/lists/*

# Install .NET workloads based on configuration
# 平台限制与容器操作系统有关，不是宿主系统：
#  1. macOS，iOS workload - 只能在 macOS 上安装（需要 Xcode），在 Linux 容器中无法安装
#  2. Android workload - 可在 Linux 容器中安装 android
#  3. Windows，Linux workload 不需要安装，内置
ARG INSTALL_ANDROID_SUPPORT
ARG INSTALL_WASM_TOOLS
ARG INSTALL_WASM_EXPERIMENTAL
ARG INSTALL_WASI_EXPERIMENTAL

RUN WORKLOADS="" && \
    if [ "$INSTALL_ANDROID_SUPPORT" = "true" ]; then WORKLOADS="$WORKLOADS android"; fi && \
    if [ "$INSTALL_WASM_TOOLS" = "true" ]; then WORKLOADS="$WORKLOADS wasm-tools"; fi && \
    if [ "$INSTALL_WASM_EXPERIMENTAL" = "true" ]; then WORKLOADS="$WORKLOADS wasm-experimental"; fi && \
    if [ "$INSTALL_WASI_EXPERIMENTAL" = "true" ]; then WORKLOADS="$WORKLOADS wasi-experimental"; fi && \
    if [ -n "$WORKLOADS" ]; then dotnet workload install $WORKLOADS; fi

# Install Avalonia templates with specific version (optional)
ARG INSTALL_AVALONIA_TEMPLATES
RUN if [ "$INSTALL_AVALONIA_TEMPLATES" = "true" ]; then \
        dotnet new install Avalonia.ProjectTemplates::${AVALONIA_VERSION} || \
        dotnet new install Avalonia.ProjectTemplates; \
    fi

# Install .NET global tools based on environment variables
ARG INSTALL_DOTNET_WATCH
ARG INSTALL_DOTNET_FORMAT
ARG INSTALL_DOTNET_EF
ARG INSTALL_DOTNET_OUTDATED
ARG INSTALL_DOTNET_TRACE
ARG INSTALL_DOTNET_SERVE

RUN if [ "$INSTALL_DOTNET_WATCH" = "true" ]; then dotnet tool install --global dotnet-watch; fi && \
    if [ "$INSTALL_DOTNET_FORMAT" = "true" ]; then dotnet tool install --global dotnet-format; fi && \
    if [ "$INSTALL_DOTNET_EF" = "true" ]; then dotnet tool install --global dotnet-ef; fi && \
    if [ "$INSTALL_DOTNET_OUTDATED" = "true" ]; then dotnet tool install --global dotnet-outdated-tool; fi && \
    if [ "$INSTALL_DOTNET_TRACE" = "true" ]; then dotnet tool install --global dotnet-trace; fi && \
    if [ "$INSTALL_DOTNET_SERVE" = "true" ]; then dotnet tool install --global dotnet-serve; fi

# Configure global tools path
ENV PATH="$PATH:/root/.dotnet/tools"

# Install Android SDK and tools for mobile development (optional)
ARG ANDROID_SDK_VERSION
ARG ANDROID_NDK_VERSION  
ARG ANDROID_BUILD_TOOLS_VERSION

# Only set Android environment if Android support is enabled
RUN if [ "$INSTALL_ANDROID_SUPPORT" = "true" ]; then \
        export ANDROID_HOME=/opt/android-sdk && \
        export ANDROID_SDK_ROOT=$ANDROID_HOME && \
        mkdir -p $ANDROID_HOME/cmdline-tools && \
        wget -q https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O /tmp/cmdline-tools.zip && \
        unzip /tmp/cmdline-tools.zip -d $ANDROID_HOME/cmdline-tools && \
        mv $ANDROID_HOME/cmdline-tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest && \
        rm /tmp/cmdline-tools.zip && \
        yes | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses && \
        $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "platform-tools" "platforms;android-${ANDROID_SDK_VERSION}" "build-tools;${ANDROID_BUILD_TOOLS_VERSION}" && \
        $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "ndk;${ANDROID_NDK_VERSION}" "system-images;android-${ANDROID_SDK_VERSION};google_apis;x86_64"; \
    fi

# Set Android environment variables if Android support is enabled
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=$ANDROID_HOME
RUN if [ "$INSTALL_ANDROID_SUPPORT" = "true" ]; then \
        echo 'export PATH="$PATH:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$ANDROID_HOME/build-tools/'${ANDROID_BUILD_TOOLS_VERSION}'"' >> /etc/environment; \
    fi

# Install Node.js for web development (optional)
ARG INSTALL_NODEJS
RUN if [ "$INSTALL_NODEJS" = "true" ]; then \
        curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
        apt-get install -y nodejs; \
    fi

# Set up git (latest version)
RUN git --version

# Create cache directories with proper permissions
# 使用容器内专用缓存目录，避免与宿主环境冲突
RUN mkdir -p /opt/nuget-cache \
    && mkdir -p /opt/dotnet-cache \
    && mkdir -p /workspace/.nuget/packages \
    && mkdir -p /workspace/.dotnet \
    && chmod -R 755 /opt/nuget-cache \
    && chmod -R 755 /opt/dotnet-cache \
    && chmod -R 755 /workspace/.nuget \
    && chmod -R 755 /workspace/.dotnet

# Create dynamic startup script for development convenience
RUN echo '#!/bin/bash' > /usr/local/bin/avalonia-dev && \
    echo 'echo "🚀 Avalonia Development Environment"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "📁 Workspace: $(pwd)"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "🔷 .NET: $(dotnet --version)"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo ""' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "📦 Installed Features:"' >> /usr/local/bin/avalonia-dev && \
    echo 'if command -v android &> /dev/null || [ -d "/opt/android-sdk" ]; then echo "  ✅ Android SDK"; else echo "  ❌ Android SDK"; fi' >> /usr/local/bin/avalonia-dev && \
    echo 'if dotnet workload list | grep -q "wasm-tools"; then echo "  ✅ WebAssembly Tools"; else echo "  ❌ WebAssembly Tools"; fi' >> /usr/local/bin/avalonia-dev && \
    echo 'if command -v node &> /dev/null; then echo "  ✅ Node.js $(node --version)"; else echo "  ❌ Node.js"; fi' >> /usr/local/bin/avalonia-dev && \
    echo 'if dotnet new list | grep -q "avalonia"; then echo "  ✅ Avalonia Templates"; else echo "  ❌ Avalonia Templates"; fi' >> /usr/local/bin/avalonia-dev && \
    echo 'echo ""' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "🔧 Core Commands:"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet run       - Run the application"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet build     - Build the application"' >> /usr/local/bin/avalonia-dev && \
    echo 'if command -v dotnet-watch &> /dev/null; then echo "  dotnet watch     - Watch for file changes"; fi' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet test      - Run tests"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo ""' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "🖥️  Desktop Platforms:"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet publish -r linux-x64   - Build for Linux"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet publish -r win-x64     - Build for Windows"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet publish -r osx-x64     - Build for macOS Intel"' >> /usr/local/bin/avalonia-dev && \
    echo 'echo "  dotnet publish -r osx-arm64   - Build for macOS Apple Silicon"' >> /usr/local/bin/avalonia-dev && \
    echo 'if [ -d "/opt/android-sdk" ]; then' >> /usr/local/bin/avalonia-dev && \
    echo '  echo ""; echo "📱 Mobile Platforms:"; echo "  dotnet publish -f net9.0-android  - Build for Android"' >> /usr/local/bin/avalonia-dev && \
    echo 'fi' >> /usr/local/bin/avalonia-dev && \
    echo 'if dotnet workload list | grep -q "wasm-tools"; then' >> /usr/local/bin/avalonia-dev && \
    echo '  echo ""; echo "🌐 Web Platforms:"; echo "  dotnet publish -f net9.0-browser  - Build for Browser/WASM"' >> /usr/local/bin/avalonia-dev && \
    echo '  if command -v dotnet-serve &> /dev/null; then echo "  dotnet serve                      - Serve web app locally"; fi' >> /usr/local/bin/avalonia-dev && \
    echo 'fi' >> /usr/local/bin/avalonia-dev && \
    echo 'if dotnet new list | grep -q "avalonia"; then' >> /usr/local/bin/avalonia-dev && \
    echo '  echo ""; echo "🔧 Development Commands:"; echo "  dotnet new avalonia               - Create Avalonia app"' >> /usr/local/bin/avalonia-dev && \
    echo 'fi' >> /usr/local/bin/avalonia-dev && \
    echo 'echo ""' >> /usr/local/bin/avalonia-dev && \
    echo 'exec "$@"' >> /usr/local/bin/avalonia-dev && \
    chmod +x /usr/local/bin/avalonia-dev

# Set default environment variables
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_ENVIRONMENT=Development
ENV ASPNETCORE_ENVIRONMENT=Development

WORKDIR /workspace
EXPOSE 5000 9229

# Health check to ensure container is working
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || dotnet --info > /dev/null || exit 1

CMD ["avalonia-dev", "bash"]

# =============================================================================
# Production Stage - 可选的生产环境 (注释掉，需要时可启用)
# =============================================================================
# 注意：Avalonia是桌面应用，通常不需要生产容器部署
# 如需生产环境，取消以下注释并修改compose.yaml的target参数

# FROM ${DOCKER_REGISTRY}/dotnet/aspnet:${DOTNET_VERSION} as production
# WORKDIR /app
# 
# # 只复制运行时需要的文件
# COPY --from=development /workspace/bin/Release/net9.0/publish/ .
# 
# # 设置生产环境变量
# ENV DOTNET_ENVIRONMENT=Production
# ENV ASPNETCORE_ENVIRONMENT=Production
# 
# # 创建非root用户 (安全最佳实践)
# RUN groupadd -r appuser && useradd -r -g appuser appuser
# RUN chown -R appuser:appuser /app
# USER appuser
# 
# # 健康检查
# HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
#     CMD curl -f http://localhost:5000/health || exit 1
# 
# EXPOSE 5000
# ENTRYPOINT ["dotnet", "YourAppName.dll"]