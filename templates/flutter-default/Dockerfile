ARG DOCKER_REGISTRY
ARG UBUNTU_VERSION
FROM ${DOCKER_REGISTRY}/ubuntu:${UBUNTU_VERSION}

ARG FLUTTER_VERSION
ARG DART_VERSION
ARG DEPENDENCIES
ARG APT_MIRROR
ARG FLUTTER_DOWNLOAD_URL
ARG FLUTTER_MIRROR_URL
ARG FLUTTER_FALLBACK_URL
ARG ANDROID_SDK_VERSION
ARG JAVA_VERSION
ARG PUB_HOSTED_URL
ARG FLUTTER_STORAGE_BASE_URL

# 使用更快的 APT 镜像源
RUN sed -i "s|archive.ubuntu.com|${APT_MIRROR}|g" /etc/apt/sources.list && \
    sed -i "s|security.ubuntu.com|${APT_MIRROR}|g" /etc/apt/sources.list

# 安装系统依赖
RUN apt-get update && apt-get install -y ${DEPENDENCIES} openjdk-${JAVA_VERSION}-jdk && rm -rf /var/lib/apt/lists/*

# 配置 Flutter 镜像源环境变量
ENV PUB_HOSTED_URL=${PUB_HOSTED_URL}
ENV FLUTTER_STORAGE_BASE_URL=${FLUTTER_STORAGE_BASE_URL}

# 安装 Flutter 带备用机制
RUN git clone ${FLUTTER_DOWNLOAD_URL} -b stable --depth 1 /flutter || \
    (echo "从 GitHub 下载失败，尝试备用源..." && \
     curl -L ${FLUTTER_FALLBACK_URL}/v${FLUTTER_VERSION}/flutter_linux_${FLUTTER_VERSION}-stable.tar.xz | tar -xJ -C / && \
     mv /flutter_${FLUTTER_VERSION} /flutter)

ENV PATH="/flutter/bin:${PATH}"

# 配置 Flutter 不发送分析数据
RUN flutter config --no-analytics

# 预下载依赖包
RUN flutter precache --no-android || flutter precache

# 设置 Android SDK 环境
ENV ANDROID_SDK_ROOT=/android-sdk
ENV ANDROID_HOME=/android-sdk
ENV PATH="${ANDROID_SDK_ROOT}/tools:${ANDROID_SDK_ROOT}/platform-tools:${PATH}"

# 创建 Android SDK 目录
RUN mkdir -p ${ANDROID_SDK_ROOT}

# 设置 Java 环境
ENV JAVA_HOME=/usr/lib/jvm/java-${JAVA_VERSION}-openjdk-x64

# 验证安装
RUN flutter doctor --version

WORKDIR /workspace
EXPOSE 3000 9229 8080

CMD ["bash"]