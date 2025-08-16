# =============================================================================
# .NET Development Image for Deck - Based on Ubuntu
# =============================================================================

# Build arguments
ARG DOCKER_REGISTRY
ARG UBUNTU_VERSION
ARG UBUNTU_NAME
ARG DOTNET_VERSION
ARG DOTNET_VERSION_NAME

FROM localhost/ubuntu${UBUNTU_NAME}:${UBUNTU_VERSION} as dotnet${DOTNET_VERSION_NAME}

# Define build arguments after FROM instruction to ensure they are in the correct scope
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

# Install curl and other essential dependencies before trying to use them
# This ensures that critical tools are available even if they're not in the base image
# Explicitly installing key dependencies to ensure build reliability
# Add Microsoft package repository and install .NET SDK
# Use the script-based installation method which works across architectures
RUN apt-get update && apt-get install -y curl wget apt-transport-https software-properties-common && \
    curl -SL -o dotnet.tar.gz https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.304/dotnet-sdk-9.0.304-linux-arm64.tar.gz && \
    mkdir -p /usr/share/dotnet && \
    tar -zxf dotnet.tar.gz -C /usr/share/dotnet && \
    rm dotnet.tar.gz && \
    ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm -rf /var/lib/apt/lists/*

# Set .NET environment variables
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="$PATH:/usr/share/dotnet"

# Install additional system dependencies specific to .NET
RUN apt-get update && apt-get install -y ${DEPENDENCIES} && rm -rf /var/lib/apt/lists/*

# Install .NET workloads based on configuration

RUN WORKLOADS="" && \
    if [ "$INSTALL_ANDROID_SUPPORT" = "true" ]; then WORKLOADS="$WORKLOADS android"; fi && \
    if [ "$INSTALL_WASM_TOOLS" = "true" ]; then WORKLOADS="$WORKLOADS wasm-tools"; fi && \
    if [ "$INSTALL_WASM_EXPERIMENTAL" = "true" ]; then WORKLOADS="$WORKLOADS wasm-experimental"; fi && \
    if [ "$INSTALL_WASI_EXPERIMENTAL" = "true" ]; then WORKLOADS="$WORKLOADS wasi-experimental"; fi && \
    if [ -n "$WORKLOADS" ]; then dotnet workload install $WORKLOADS; fi

# Install .NET global tools based on environment variables

RUN if [ "$INSTALL_DOTNET_WATCH" = "true" ]; then dotnet tool install --global dotnet-watch; fi && \
    if [ "$INSTALL_DOTNET_FORMAT" = "true" ]; then dotnet tool install --global dotnet-format; fi && \
    if [ "$INSTALL_DOTNET_EF" = "true" ]; then dotnet tool install --global dotnet-ef; fi && \
    if [ "$INSTALL_DOTNET_OUTDATED" = "true" ]; then dotnet tool install --global dotnet-outdated-tool; fi && \
    if [ "$INSTALL_DOTNET_TRACE" = "true" ]; then dotnet tool install --global dotnet-trace; fi && \
    if [ "$INSTALL_DOTNET_SERVE" = "true" ]; then dotnet tool install --global dotnet-serve; fi

# Configure global tools path
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

# Create cache directories with proper permissions
RUN mkdir -p /opt/nuget-cache \
    && mkdir -p /opt/dotnet-cache \
    && mkdir -p /workspace/.nuget/packages \
    && mkdir -p /workspace/.dotnet \
    && chmod -R 755 /opt/nuget-cache \
    && chmod -R 755 /opt/dotnet-cache \
    && chmod -R 755 /workspace/.nuget \
    && chmod -R 755 /workspace/.dotnet

# Create dynamic startup script for development convenience
RUN echo '#!/bin/bash' > /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "🚀 .NET Development Environment"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "📁 Workspace: $(pwd)"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "🔷 .NET: $(/usr/share/dotnet/dotnet --version)"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo "" >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "📦 Installed Features:"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if /usr/share/dotnet/dotnet workload list 2>/dev/null | grep -q "android"; then echo "  ✅ Android Workload"; else echo "  ❌ Android Workload"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if /usr/share/dotnet/dotnet workload list 2>/dev/null | grep -q "wasm-tools"; then echo "  ✅ WebAssembly Tools"; else echo "  ❌ WebAssembly Tools"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-ef > /dev/null 2>&1; then echo "  ✅ Entity Framework Tools"; else echo "  ❌ Entity Framework Tools"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-watch > /dev/null 2>&1; then echo "  ✅ Watch Tool"; else echo "  ❌ Watch Tool"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo "" >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "🔧 Core Commands:"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet run       - Run the application"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet build     - Build the application"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-watch > /dev/null 2>&1; then echo "  dotnet watch     - Watch for file changes"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet test      - Run tests"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-ef > /dev/null 2>&1; then echo "  dotnet ef        - Entity Framework commands"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-format > /dev/null 2>&1; then echo "  dotnet format    - Format code"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-outdated-tool > /dev/null 2>&1; then echo "  dotnet outdated  - Check for outdated packages"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if command -v dotnet-trace > /dev/null 2>&1; then echo "  dotnet trace     - Collect traces"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo "" >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "🖥️  Platforms:"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet publish -r linux-x64   - Build for Linux"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet publish -r win-x64     - Build for Windows"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet publish -r osx-x64     - Build for macOS Intel"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'echo "  dotnet publish -r osx-arm64   - Build for macOS Apple Silicon"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if /usr/share/dotnet/dotnet workload list 2>/dev/null | grep -q "android"; then' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo '  echo "  dotnet publish -f net9.0-android  - Build for Android"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if /usr/share/dotnet/dotnet workload list 2>/dev/null | grep -q "wasm-tools"; then' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo '  echo "  dotnet publish -f net9.0-browser  - Build for Browser/WASM"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo '  if command -v dotnet-serve > /dev/null 2>&1; then echo "  dotnet serve                      - Serve web app locally"; fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo "" >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'if [ "$#" -eq 0 ]; then' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo '  exec bash' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'else' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo '  exec /usr/share/dotnet/dotnet "$@"' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    echo 'fi' >> /usr/local/bin/dotnet${DOTNET_VERSION_NAME} && \
    chmod +x /usr/local/bin/dotnet${DOTNET_VERSION_NAME}

WORKDIR /workspace
EXPOSE 5000 9229

CMD ["dotnet${DOTNET_VERSION_NAME}", "bash"]