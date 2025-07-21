namespace Deck.Core.Models;

/// <summary>
/// 端口检查结果
/// </summary>
public class PortCheckResult
{
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 协议类型
    /// </summary>
    public DeckProtocolType Protocol { get; set; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 错误信息（如果检查失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 端口冲突详细信息
/// </summary>
public class PortConflictInfo
{
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 协议类型
    /// </summary>
    public DeckProtocolType Protocol { get; set; }

    /// <summary>
    /// 是否有冲突
    /// </summary>
    public bool HasConflict { get; set; }

    /// <summary>
    /// 占用进程信息
    /// </summary>
    public ProcessInfo? OccupyingProcess { get; set; }

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime DetectionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 服务类型（如果可识别）
    /// </summary>
    public string? ServiceType { get; set; }

    /// <summary>
    /// 监听地址
    /// </summary>
    public string? ListenAddress { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public ConnectionState State { get; set; }

    /// <summary>
    /// 冲突严重程度
    /// </summary>
    public ConflictSeverity Severity { get; set; }
}

/// <summary>
/// 进程信息
/// </summary>
public class ProcessInfo
{
    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 可执行文件路径
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// 命令行参数
    /// </summary>
    public string? CommandLine { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 进程所有者
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// 是否为系统进程
    /// </summary>
    public bool IsSystemProcess { get; set; }

    /// <summary>
    /// 是否可以安全停止
    /// </summary>
    public bool CanBeStopped { get; set; } = true;
}

/// <summary>
/// 项目端口分配结果
/// </summary>
public class ProjectPortAllocation
{
    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 分配的端口映射
    /// </summary>
    public Dictionary<ProjectPortType, int> AllocatedPorts { get; set; } = new();

    /// <summary>
    /// 分配失败的端口类型
    /// </summary>
    public List<ProjectPortType> FailedAllocations { get; set; } = new();

    /// <summary>
    /// 分配建议（推荐的端口范围等）
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// 分配总结
    /// </summary>
    public string AllocationSummary { get; set; } = string.Empty;
}

/// <summary>
/// 端口解决建议
/// </summary>
public class PortResolutionSuggestion
{
    /// <summary>
    /// 建议类型
    /// </summary>
    public ResolutionType Type { get; set; }

    /// <summary>
    /// 建议描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 建议的命令
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// 替代端口（如果适用）
    /// </summary>
    public int? AlternativePort { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public SuggestionPriority Priority { get; set; } = SuggestionPriority.Medium;

    /// <summary>
    /// 风险评估
    /// </summary>
    public RiskLevel Risk { get; set; } = RiskLevel.Low;

    /// <summary>
    /// 是否可以自动执行
    /// </summary>
    public bool CanAutoExecute { get; set; }
}

/// <summary>
/// 进程停止结果
/// </summary>
public class ProcessStopResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 停止方法
    /// </summary>
    public StopMethod Method { get; set; }

    /// <summary>
    /// 执行的命令
    /// </summary>
    public string? ExecutedCommand { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; set; }
}

/// <summary>
/// 系统端口使用情况
/// </summary>
public class SystemPortUsage
{
    /// <summary>
    /// TCP端口使用情况
    /// </summary>
    public List<PortUsageInfo> TcpPorts { get; set; } = new();

    /// <summary>
    /// UDP端口使用情况
    /// </summary>
    public List<PortUsageInfo> UdpPorts { get; set; } = new();

    /// <summary>
    /// 统计信息
    /// </summary>
    public PortUsageStatistics Statistics { get; set; } = new();

    /// <summary>
    /// 扫描时间
    /// </summary>
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 端口使用信息
/// </summary>
public class PortUsageInfo
{
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 本地地址
    /// </summary>
    public string LocalAddress { get; set; } = string.Empty;

    /// <summary>
    /// 远程地址
    /// </summary>
    public string? RemoteAddress { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public ConnectionState State { get; set; }

    /// <summary>
    /// 进程信息
    /// </summary>
    public ProcessInfo? Process { get; set; }
}

/// <summary>
/// 端口使用统计
/// </summary>
public class PortUsageStatistics
{
    /// <summary>
    /// TCP监听端口总数
    /// </summary>
    public int TcpListeningPorts { get; set; }

    /// <summary>
    /// UDP监听端口总数
    /// </summary>
    public int UdpListeningPorts { get; set; }

    /// <summary>
    /// 活动连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 最常用的端口范围
    /// </summary>
    public Dictionary<string, int> PopularPortRanges { get; set; } = new();

    /// <summary>
    /// 系统进程占用的端口数
    /// </summary>
    public int SystemProcessPorts { get; set; }
}

/// <summary>
/// 端口验证结果
/// </summary>
public class PortValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 验证错误
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 验证警告
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 是否需要特权
    /// </summary>
    public bool RequiresPrivilege { get; set; }

    /// <summary>
    /// 建议的替代端口
    /// </summary>
    public List<int> SuggestedAlternatives { get; set; } = new();
}

/// <summary>
/// 项目端口类型枚举
/// </summary>
public enum ProjectPortType
{
    /// <summary>
    /// 开发服务器端口
    /// </summary>
    DevServer,

    /// <summary>
    /// 调试端口
    /// </summary>
    Debug,

    /// <summary>
    /// 热重载端口
    /// </summary>
    HotReload,

    /// <summary>
    /// 数据库端口
    /// </summary>
    Database,

    /// <summary>
    /// Redis缓存端口
    /// </summary>
    Redis,

    /// <summary>
    /// API端口
    /// </summary>
    Api,

    /// <summary>
    /// 前端端口
    /// </summary>
    Frontend,

    /// <summary>
    /// WebSocket端口
    /// </summary>
    WebSocket,

    /// <summary>
    /// 代理端口
    /// </summary>
    Proxy
}

/// <summary>
/// 连接状态枚举
/// </summary>
public enum ConnectionState
{
    Listen,
    Established,
    TimeWait,
    CloseWait,
    SynSent,
    SynReceived,
    Closing,
    Closed,
    Unknown
}

/// <summary>
/// 冲突严重程度枚举
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// 低 - 可以共存或易于解决
    /// </summary>
    Low,

    /// <summary>
    /// 中 - 可能影响功能
    /// </summary>
    Medium,

    /// <summary>
    /// 高 - 会阻止服务启动
    /// </summary>
    High,

    /// <summary>
    /// 严重 - 系统关键服务冲突
    /// </summary>
    Critical
}

/// <summary>
/// 解决方案类型枚举
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// 停止进程
    /// </summary>
    StopProcess,

    /// <summary>
    /// 使用替代端口
    /// </summary>
    UseAlternativePort,

    /// <summary>
    /// 修改配置
    /// </summary>
    ModifyConfiguration,

    /// <summary>
    /// 重启服务
    /// </summary>
    RestartService,

    /// <summary>
    /// 等待释放
    /// </summary>
    WaitForRelease,

    /// <summary>
    /// 手动干预
    /// </summary>
    ManualIntervention
}

/// <summary>
/// 建议优先级枚举
/// </summary>
public enum SuggestionPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// 风险级别枚举
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// 无风险
    /// </summary>
    None,

    /// <summary>
    /// 低风险
    /// </summary>
    Low,

    /// <summary>
    /// 中等风险
    /// </summary>
    Medium,

    /// <summary>
    /// 高风险
    /// </summary>
    High,

    /// <summary>
    /// 严重风险 - 不建议执行
    /// </summary>
    Critical
}

/// <summary>
/// 进程停止方法枚举
/// </summary>
public enum StopMethod
{
    /// <summary>
    /// 优雅停止
    /// </summary>
    Graceful,

    /// <summary>
    /// 强制停止
    /// </summary>
    Force,

    /// <summary>
    /// 终止信号
    /// </summary>
    Terminate,

    /// <summary>
    /// 杀死进程
    /// </summary>
    Kill
}