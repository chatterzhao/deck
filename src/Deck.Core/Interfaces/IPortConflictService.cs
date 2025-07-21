using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 端口冲突检测服务接口 - 提供跨平台端口管理和冲突解决
/// </summary>
public interface IPortConflictService
{
    /// <summary>
    /// 检查指定端口是否被占用
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="protocol">协议类型（TCP/UDP）</param>
    Task<PortCheckResult> CheckPortAsync(int port, DeckProtocolType protocol = DeckProtocolType.TCP);

    /// <summary>
    /// 批量检查多个端口状态
    /// </summary>
    /// <param name="ports">端口列表</param>
    /// <param name="protocol">协议类型</param>
    Task<List<PortCheckResult>> CheckPortsAsync(IEnumerable<int> ports, DeckProtocolType protocol = DeckProtocolType.TCP);

    /// <summary>
    /// 检测端口冲突并获取详细信息
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="protocol">协议类型</param>
    Task<PortConflictInfo> DetectPortConflictAsync(int port, DeckProtocolType protocol = DeckProtocolType.TCP);

    /// <summary>
    /// 为指定范围内自动分配可用端口
    /// </summary>
    /// <param name="preferredPort">优先使用的端口</param>
    /// <param name="startRange">搜索范围起始端口</param>
    /// <param name="endRange">搜索范围结束端口</param>
    /// <param name="protocol">协议类型</param>
    Task<int?> FindAvailablePortAsync(int? preferredPort = null, int startRange = 8000, int endRange = 9000, DeckProtocolType protocol = DeckProtocolType.TCP);

    /// <summary>
    /// 为项目自动分配端口（DEV_PORT、DEBUG_PORT等）
    /// </summary>
    /// <param name="projectType">项目类型</param>
    /// <param name="portType">端口类型</param>
    Task<ProjectPortAllocation> AllocateProjectPortsAsync(ProjectType projectType, params ProjectPortType[] portType);

    /// <summary>
    /// 获取端口冲突解决建议
    /// </summary>
    /// <param name="conflictInfo">端口冲突信息</param>
    Task<List<PortResolutionSuggestion>> GetResolutionSuggestionsAsync(PortConflictInfo conflictInfo);

    /// <summary>
    /// 尝试停止占用指定端口的进程
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="protocol">协议类型</param>
    /// <param name="force">是否强制停止</param>
    Task<ProcessStopResult> StopProcessUsingPortAsync(int port, DeckProtocolType protocol = DeckProtocolType.TCP, bool force = false);

    /// <summary>
    /// 获取系统所有端口使用情况
    /// </summary>
    Task<SystemPortUsage> GetSystemPortUsageAsync();

    /// <summary>
    /// 验证端口范围和权限
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="checkPrivileged">是否检查特权端口（1-1024）</param>
    Task<PortValidationResult> ValidatePortAsync(int port, bool checkPrivileged = true);
}