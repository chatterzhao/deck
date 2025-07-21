# =============================================================================
# Build Arguments - All values come from .env file
# =============================================================================
ARG DOCKER_REGISTRY
ARG UBUNTU_VERSION
FROM ${DOCKER_REGISTRY}/ubuntu:${UBUNTU_VERSION}

ARG RUST_VERSION
ARG NODEJS_VERSION
ARG TAURI_CLI_VERSION
ARG DEPENDENCIES

# 网络配置参数
ARG APT_MIRROR
ARG CARGO_REGISTRY_INDEX
ARG CARGO_REGISTRY_SPARSE
ARG CARGO_FALLBACK_REGISTRY
ARG NPM_REGISTRY
ARG NPM_FALLBACK_REGISTRY
ARG NODEJS_MIRROR
ARG NODEJS_FALLBACK

# =============================================================================
# 配置软件包镜像源 (中国优化)
# =============================================================================
RUN sed -i "s|archive.ubuntu.com|${APT_MIRROR}|g" /etc/apt/sources.list && \
    sed -i "s|security.ubuntu.com|${APT_MIRROR}|g" /etc/apt/sources.list

# =============================================================================
# Install System Dependencies
# =============================================================================
RUN apt-get update && apt-get install -y ${DEPENDENCIES} && rm -rf /var/lib/apt/lists/*

# =============================================================================
# 配置 Rust 安装
# =============================================================================
# 设置 Rust 镜像源配置
RUN mkdir -p /root/.cargo && \
    echo "[source.crates-io]" > /root/.cargo/config.toml && \
    echo "replace-with = 'mirror'" >> /root/.cargo/config.toml && \
    echo "[source.mirror]" >> /root/.cargo/config.toml && \
    echo "registry = \"${CARGO_REGISTRY_INDEX}\"" >> /root/.cargo/config.toml

# 安装 Rust 带备用机制
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y \
    --default-toolchain ${RUST_VERSION} \
    --profile minimal \
    --component clippy,rustfmt || \
    (echo "从官方源安装失败，尝试备用源..." && \
     curl --proto '=https' --tlsv1.2 -sSf https://forge.rust-lang.org/infra/channel-layout.html | sh -s -- -y \
     --default-toolchain ${RUST_VERSION} \
     --profile minimal \
     --component clippy,rustfmt)

ENV PATH="/root/.cargo/bin:${PATH}"

# =============================================================================
# 配置 Node.js 安装
# =============================================================================
# 设置 npm 镜像源配置
RUN mkdir -p /root/.config && \
    echo "registry=${NPM_REGISTRY}" > /root/.npmrc

# 安装 Node.js 带备用机制
RUN curl -fsSL https://deb.nodesource.com/setup_$(echo ${NODEJS_VERSION} | cut -d. -f1).x | bash - && \
    apt-get install -y nodejs || \
    (echo "从 NodeSource 安装失败，尝试备用源..." && \
     curl -fsSL https://nodejs.org/dist/v${NODEJS_VERSION}/node-v${NODEJS_VERSION}-linux-x64.tar.xz | tar -xJ -C /usr/local --strip-components=1)

# 配置 npm 优化设置
RUN npm config set fund false && \
    npm config set audit-level moderate && \
    npm config set registry ${NPM_REGISTRY}

# =============================================================================
# Install Development Tools
# =============================================================================
# Install Tauri CLI
RUN cargo install tauri-cli --version ${TAURI_CLI_VERSION} --locked

# Install useful development tools
RUN cargo install cargo-watch --locked
RUN npm install -g @tauri-apps/cli yarn pnpm

# =============================================================================
# Configure Cross-compilation
# =============================================================================
# Add cross-compilation targets
RUN rustup target add x86_64-pc-windows-gnu \
    && rustup target add x86_64-unknown-linux-gnu \
    && rustup target add aarch64-unknown-linux-gnu

# Install cross-compilation dependencies
RUN apt-get update && apt-get install -y \
    gcc-mingw-w64-x86-64 \
    gcc-aarch64-linux-gnu \
    && rm -rf /var/lib/apt/lists/*

# =============================================================================
# Create Workspace and Set Permissions
# =============================================================================
WORKDIR /workspace

# Create cache directories with proper permissions
RUN mkdir -p /usr/local/cargo/registry \
    && mkdir -p /usr/local/node_modules_cache \
    && chmod -R 755 /usr/local/cargo \
    && chmod -R 755 /usr/local/node_modules_cache

# =============================================================================
# Runtime Configuration
# =============================================================================
# Expose development ports
EXPOSE 1420 9229 1421

# Set default environment variables
ENV RUST_LOG=info
ENV NODE_ENV=development
ENV CARGO_TARGET_DIR=/workspace/target

# Create startup script for development convenience
RUN echo '#!/bin/bash' > /usr/local/bin/tauri-dev && \
    echo 'echo "🚀 Tauri Development Environment"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "📁 Workspace: $(pwd)"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "🦀 Rust: $(rustc --version)"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "📦 Node.js: $(node --version)"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "⚡ Tauri CLI: $(tauri --version)"' >> /usr/local/bin/tauri-dev && \
    echo 'echo ""' >> /usr/local/bin/tauri-dev && \
    echo 'echo "🔧 Available commands:"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "  tauri dev    - Start development server"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "  tauri build  - Build application"' >> /usr/local/bin/tauri-dev && \
    echo 'echo "  cargo watch  - Watch for file changes"' >> /usr/local/bin/tauri-dev && \
    echo 'echo ""' >> /usr/local/bin/tauri-dev && \
    echo 'exec "$@"' >> /usr/local/bin/tauri-dev && \
    chmod +x /usr/local/bin/tauri-dev

# =============================================================================
# Health Check
# =============================================================================
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:1420/health || exit 1

CMD ["tauri-dev", "bash"]