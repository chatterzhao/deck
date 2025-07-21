namespace Deck.Core.Models;

/// <summary>
/// 容器信息 - 对应 deck-shell 的容器管理
/// </summary>
public class ContainerInfo
{
    /// <summary>
    /// 容器ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 容器名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 容器状态
    /// </summary>
    public ContainerStatus Status { get; set; }

    /// <summary>
    /// 镜像名称
    /// </summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// 镜像ID
    /// </summary>
    public string ImageId { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 停止时间
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// 端口映射
    /// </summary>
    public Dictionary<string, string> PortMappings { get; set; } = new();

    /// <summary>
    /// 环境变量
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>
    /// 挂载点
    /// </summary>
    public List<MountInfo> Mounts { get; set; } = new();

    /// <summary>
    /// 网络信息
    /// </summary>
    public List<NetworkInfo> Networks { get; set; } = new();

    /// <summary>
    /// 容器引擎类型
    /// </summary>
    public ContainerEngineType EngineType { get; set; }

    /// <summary>
    /// 项目类型（用于三层管理）
    /// </summary>
    public ProjectType? ProjectType { get; set; }

    /// <summary>
    /// 项目根目录
    /// </summary>
    public string? ProjectRoot { get; set; }

    /// <summary>
    /// 容器标签
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>
    /// CPU 使用情况
    /// </summary>
    public ContainerResourceUsage? ResourceUsage { get; set; }
}

/// <summary>
/// 挂载信息
/// </summary>
public class MountInfo
{
    /// <summary>
    /// 源路径
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 目标路径
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// 挂载模式
    /// </summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// 挂载类型
    /// </summary>
    public MountType Type { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool ReadOnly { get; set; }
}

/// <summary>
/// 挂载类型枚举
/// </summary>
public enum MountType
{
    Bind,
    Volume,
    Tmpfs
}

/// <summary>
/// 网络信息
/// </summary>
public class NetworkInfo
{
    /// <summary>
    /// 网络名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IPAddress { get; set; }

    /// <summary>
    /// 网关
    /// </summary>
    public string? Gateway { get; set; }

    /// <summary>
    /// 网络模式
    /// </summary>
    public NetworkMode Mode { get; set; }

    /// <summary>
    /// 端口映射
    /// </summary>
    public List<PortMapping> Ports { get; set; } = new();
}

/// <summary>
/// 网络模式枚举
/// </summary>
public enum NetworkMode
{
    Bridge,
    Host,
    None,
    Container,
    Custom
}

/// <summary>
/// 端口映射
/// </summary>
public class PortMapping
{
    /// <summary>
    /// 主机端口
    /// </summary>
    public int HostPort { get; set; }

    /// <summary>
    /// 容器端口
    /// </summary>
    public int ContainerPort { get; set; }

    /// <summary>
    /// 协议类型
    /// </summary>
    public DeckProtocolType Protocol { get; set; } = DeckProtocolType.TCP;

    /// <summary>
    /// 主机IP
    /// </summary>
    public string? HostIP { get; set; }
}

/// <summary>
/// 协议类型枚举
/// </summary>
public enum DeckProtocolType
{
    TCP,
    UDP,
    SCTP
}

/// <summary>
/// 容器资源使用情况
/// </summary>
public class ContainerResourceUsage
{
    /// <summary>
    /// CPU使用率 (0.0 - 100.0)
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// 内存使用量 (bytes)
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// 内存限制 (bytes)
    /// </summary>
    public long MemoryLimitBytes { get; set; }

    /// <summary>
    /// 内存使用率 (0.0 - 100.0)
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// 网络接收字节数
    /// </summary>
    public long NetworkRxBytes { get; set; }

    /// <summary>
    /// 网络发送字节数
    /// </summary>
    public long NetworkTxBytes { get; set; }

    /// <summary>
    /// 磁盘读取字节数
    /// </summary>
    public long DiskReadBytes { get; set; }

    /// <summary>
    /// 磁盘写入字节数
    /// </summary>
    public long DiskWriteBytes { get; set; }

    /// <summary>
    /// 进程数
    /// </summary>
    public int ProcessCount { get; set; }

    /// <summary>
    /// 运行时长
    /// </summary>
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// 容器列表选项 - 用于交互式选择
/// </summary>
public class ContainerListOptions
{
    /// <summary>
    /// 是否显示所有容器（包括已停止的）
    /// </summary>
    public bool ShowAll { get; set; } = true;

    /// <summary>
    /// 按项目类型过滤
    /// </summary>
    public ProjectType? FilterByProjectType { get; set; }

    /// <summary>
    /// 按状态过滤
    /// </summary>
    public ContainerStatus? FilterByStatus { get; set; }

    /// <summary>
    /// 按容器引擎过滤
    /// </summary>
    public ContainerEngineType? FilterByEngine { get; set; }

    /// <summary>
    /// 名称过滤模式
    /// </summary>
    public string? NamePattern { get; set; }

    /// <summary>
    /// 是否显示资源使用情况
    /// </summary>
    public bool ShowResourceUsage { get; set; }

    /// <summary>
    /// 排序方式
    /// </summary>
    public ContainerSortBy SortBy { get; set; } = ContainerSortBy.Created;

    /// <summary>
    /// 是否降序排序
    /// </summary>
    public bool Descending { get; set; } = true;
}

/// <summary>
/// 容器排序方式
/// </summary>
public enum ContainerSortBy
{
    Name,
    Created,
    Status,
    Image,
    Size,
    CpuUsage,
    MemoryUsage
}