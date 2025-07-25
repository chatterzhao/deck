using Deck.Core.Models;
using System.Diagnostics;

namespace Deck.Core.Interfaces;

/// <summary>
/// 容器引擎接口，提供统一的容器操作抽象
/// </summary>
public interface IContainerEngine
{
    /// <summary>
    /// 容器引擎类型
    /// </summary>
    ContainerEngineType Type { get; }
    
    /// <summary>
    /// 检查容器是否存在
    /// </summary>
    Task<bool> ContainerExistsAsync(string containerName);
    
    /// <summary>
    /// 获取容器信息
    /// </summary>
    Task<ContainerInfo?> GetContainerInfoAsync(string containerName);
    
    /// <summary>
    /// 获取所有容器列表
    /// </summary>
    Task<List<ContainerInfo>> GetAllContainersAsync();
    
    /// <summary>
    /// 获取运行中的容器列表
    /// </summary>
    Task<List<ContainerInfo>> GetRunningContainersAsync();
    
    /// <summary>
    /// 启动容器
    /// </summary>
    Task<StartContainerResult> StartContainerAsync(string containerName, StartOptions? options = null);
    
    /// <summary>
    /// 停止容器
    /// </summary>
    Task<StopContainerResult> StopContainerAsync(string containerName, bool force = false);
    
    /// <summary>
    /// 重启容器
    /// </summary>
    Task<RestartContainerResult> RestartContainerAsync(string containerName);
    
    /// <summary>
    /// 检查容器是否正在运行
    /// </summary>
    Task<bool> IsContainerRunningAsync(string containerName);
    
    /// <summary>
    /// 执行命令在容器内
    /// </summary>
    Task<ShellExecutionResult> ExecuteInContainerAsync(string containerName, string command, ShellOptions? options = null);
    
    /// <summary>
    /// 获取容器日志
    /// </summary>
    Task<ContainerLogsResult> GetContainerLogsAsync(string containerName, int tailLines = 100);
}