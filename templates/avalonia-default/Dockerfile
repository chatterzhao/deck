# Define build arguments for base image reference
ARG DOCKER_REGISTRY
ARG UBUNTU_VERSION
ARG UBUNTU_NAME
ARG DOTNET_VERSION
ARG DOTNET_VERSION_NAME
ARG AVALONIA_VERSION

FROM localhost/dotnet${DOTNET_VERSION_NAME}:${DOTNET_VERSION} as avalonia_base

# Define build arguments after FROM instruction to ensure they are in the correct scope
ARG DEPENDENCIES
ARG INSTALL_AVALONIA_TEMPLATES
ARG INSTALL_ANDROID_SUPPORT
ARG ANDROID_SDK_VERSION
ARG ANDROID_NDK_VERSION  
ARG ANDROID_BUILD_TOOLS_VERSION

# Install additional system dependencies specific to Avalonia
RUN apt-get update && apt-get install -y ${DEPENDENCIES} && rm -rf /var/lib/apt/lists/*

# Install Avalonia templates with specific version (optional)
RUN if [ "$INSTALL_AVALONIA_TEMPLATES" = "true" ]; then \
        dotnet new install Avalonia.Templates::${AVALONIA_VERSION} || \
        dotnet new install Avalonia.Templates; \
    fi

# Install Android SDK and tools for mobile development (optional)
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
RUN SCRIPT_NAME="avalonia${AVALONIA_VERSION_NAME}" && \
    echo '#!/bin/bash' > /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "🚀 Avalonia Development Environment"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "📁 Workspace: $(pwd)"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "🔷 .NET: $(dotnet --version)"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo ""' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "📦 Installed Features:"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if command -v android &> /dev/null || [ -d "/opt/android-sdk" ]; then echo "  ✅ Android SDK"; else echo "  ❌ Android SDK"; fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if dotnet workload list | grep -q "wasm-tools"; then echo "  ✅ WebAssembly Tools"; else echo "  ❌ WebAssembly Tools"; fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if command -v node &> /dev/null; then echo "  ✅ Node.js $(node --version)"; else echo "  ❌ Node.js"; fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if dotnet new list | grep -q "avalonia"; then echo "  ✅ Avalonia Templates"; else echo "  ❌ Avalonia Templates"; fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo ""' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "🔧 Core Commands:"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet run       - Run the application"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet build     - Build the application"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if command -v dotnet-watch &> /dev/null; then echo "  dotnet watch     - Watch for file changes"; fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet test      - Run tests"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo ""' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "🖥️  Desktop Platforms:"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet publish -r linux-x64   - Build for Linux"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet publish -r win-x64     - Build for Windows"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet publish -r osx-x64     - Build for macOS Intel"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'echo "  dotnet publish -r osx-arm64   - Build for macOS Apple Silicon"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if [ -d "/opt/android-sdk" ]; then' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo '  echo ""; echo "📱 Mobile Platforms:"; echo "  dotnet publish -f net9.0-android  - Build for Android"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if dotnet workload list | grep -q "wasm-tools"; then' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo '  echo ""; echo "🌐 Web Platforms:"; echo "  dotnet publish -f net9.0-browser  - Build for Browser/WASM"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo '  if command -v dotnet-serve &> /dev/null; then echo "  dotnet serve                      - Serve web app locally"; fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'if dotnet new list | grep -q "avalonia"; then' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo '  echo ""; echo "🔧 Development Commands:"; echo "  dotnet new avalonia               - Create Avalonia app"' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'fi' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo ""' >> /usr/local/bin/${SCRIPT_NAME} && \
    echo 'exec "$@"' >> /usr/local/bin/${SCRIPT_NAME} && \
    chmod +x /usr/local/bin/${SCRIPT_NAME}

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

CMD ["/usr/local/bin/avalonia${AVALONIA_VERSION_NAME}", "bash"]

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