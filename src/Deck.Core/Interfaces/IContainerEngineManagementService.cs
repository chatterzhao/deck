using System.Threading.Tasks;
using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 容器引擎管理服务接口，用于处理容器引擎的安装、检查和初始化
/// </summary>
public interface IContainerEngineManagementService
{
    /// <summary>
    /// 检查并处理容器引擎状态
    /// </summary>
    /// <returns>容器引擎信息</returns>
    Task<ContainerEngineInfo> CheckAndHandleContainerEngineAsync();
    
    /// <summary>
    /// 尝试安装容器引擎
    /// </summary>
    /// <returns>是否安装成功</returns>
    Task<bool> InstallContainerEngineAsync();
    
    /// <summary>
    /// 检查是否需要重新安装Podman（例如从brew安装的情况）
    /// </summary>
    /// <returns>是否需要重新安装</returns>
    Task<bool> CheckAndHandlePodmanReinstallationAsync();
    
    /// <summary>
    /// 初始化 Podman Machine（针对macOS和Windows）
    /// </summary>
    /// <returns>是否初始化成功</returns>
    Task<bool> InitializePodmanMachineAsync();
}