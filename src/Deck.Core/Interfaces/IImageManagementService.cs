using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 镜像管理服务接口 - 对应 deck-shell 的 image-mgmt.sh
/// 处理 Docker/Podman 镜像的构建、管理和容器生命周期
/// </summary>
public interface IImageManagementService
{
    /// <summary>
    /// 构建镜像
    /// </summary>
    Task<BuildResult> BuildImageAsync(string configPath, BuildOptions options);

    /// <summary>
    /// 获取镜像信息
    /// </summary>
    Task<ImageInfo?> GetImageInfoAsync(string imageName);

    /// <summary>
    /// 检查容器状态
    /// </summary>
    Task<ContainerStatus> GetContainerStatusAsync(string containerName);

    /// <summary>
    /// 智能启动容器 - 实现 deck-shell 的智能启动逻辑
    /// 1. 检查容器是否运行 → 直接进入
    /// 2. 检查容器是否存在但停止 → 启动现有容器
    /// 3. 检查镜像是否存在 → 创建新容器
    /// 4. 镜像不存在 → 构建镜像并启动
    /// </summary>
    Task<ContainerStartResult> SmartStartContainerAsync(string configPath, StartOptions options);

    /// <summary>
    /// 停止容器
    /// </summary>
    Task<bool> StopContainerAsync(string containerName);

    /// <summary>
    /// 重启容器
    /// </summary>
    Task<bool> RestartContainerAsync(string containerName);

    /// <summary>
    /// 获取容器日志
    /// </summary>
    Task<ContainerLogs> GetContainerLogsAsync(string containerName, LogOptions options);

    /// <summary>
    /// 进入容器 shell
    /// </summary>
    Task<bool> EnterContainerShellAsync(string containerName, ShellOptions options);
}