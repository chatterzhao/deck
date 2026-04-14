# =============================================================================
# Base Development Image for Deck - Ubuntu ${UBUNTU_VERSION}
# =============================================================================

# Stage 1: Base Ubuntu system with common tools
ARG DOCKER_REGISTRY
ARG UBUNTU_VERSION
ARG UBUNTU_NAME

FROM ${DOCKER_REGISTRY}/ubuntu:${UBUNTU_VERSION} as ubuntu${UBUNTU_NAME}

# Define build arguments after FROM instruction to ensure they are in the correct scope
ARG DEPENDENCIES

# Prevent interactive dialogs during package installation
ENV DEBIAN_FRONTEND=noninteractive

# Install common system dependencies
RUN apt-get update && apt-get install -y ${DEPENDENCIES} && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r devuser && useradd -r -g devuser devuser

# Create workspace directory
WORKDIR /workspace

# Create development environment information script
RUN echo '#!/bin/bash' > /usr/local/bin/dev-info && \
    echo 'echo "🚀 Linux Base Development Environment"' >> /usr/local/bin/dev-info && \
    echo 'echo "📁 Workspace: $(pwd)"' >> /usr/local/bin/dev-info && \
    echo 'echo "🔄 Git: $(git --version)"' >> /usr/local/bin/dev-info && \
    echo 'echo "🌐 Curl: $(curl --version | head -1)"' >> /usr/local/bin/dev-info && \
    echo 'exec "$@"' >> /usr/local/bin/dev-info && \
    chmod +x /usr/local/bin/dev-info

CMD ["dev-info", "bash"]