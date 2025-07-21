using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 系统检测服务接口 - 对应 deck-shell 的 system-detect.sh
/// 检测操作系统、架构、容器引擎、项目类型等
/// </summary>
public interface ISystemDetectionService
{
    /// <summary>
    /// 获取系统信息
    /// </summary>
    Task<SystemInfo> GetSystemInfoAsync();

    /// <summary>
    /// 检测容器引擎
    /// </summary>
    Task<ContainerEngineInfo> DetectContainerEngineAsync();

    /// <summary>
    /// 检测项目类型
    /// </summary>
    Task<ProjectTypeInfo> DetectProjectTypeAsync(string projectPath);

    /// <summary>
    /// 检查系统要求
    /// </summary>
    Task<SystemRequirementsResult> CheckSystemRequirementsAsync();
}