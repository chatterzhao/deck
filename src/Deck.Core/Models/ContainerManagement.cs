namespace Deck.Core.Models;

/// <summary>
/// 镜像信息
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// 镜像ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 镜像名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 镜像标签
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// 镜像大小 (bytes)
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 是否存在
    /// </summary>
    public bool Exists { get; set; }
}

/// <summary>
/// 容器状态枚举
/// </summary>
public enum ContainerStatus
{
    /// <summary>
    /// 不存在
    /// </summary>
    NotExists,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// 暂停中
    /// </summary>
    Paused,

    /// <summary>
    /// 异常状态
    /// </summary>
    Error,

    /// <summary>
    /// 已创建但未启动
    /// </summary>
    Created,

    /// <summary>
    /// 已退出
    /// </summary>
    Exited,

    /// <summary>
    /// 重启中
    /// </summary>
    Restarting,

    /// <summary>
    /// 移除中
    /// </summary>
    Removing,

    /// <summary>
    /// 死亡状态
    /// </summary>
    Dead,

    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown
}

/// <summary>
/// 构建选项
/// </summary>
public class BuildOptions
{
    /// <summary>
    /// 是否强制重新构建
    /// </summary>
    public bool ForceRebuild { get; set; }

    /// <summary>
    /// 是否使用缓存
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// 构建参数
    /// </summary>
    public Dictionary<string, string> BuildArgs { get; set; } = new();

    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// 启动选项
/// </summary>
public class StartOptions
{
    /// <summary>
    /// 是否强制重新创建容器
    /// </summary>
    public bool ForceRecreate { get; set; }

    /// <summary>
    /// 是否在后台运行
    /// </summary>
    public bool Detached { get; set; } = true;

    /// <summary>
    /// 端口映射覆盖
    /// </summary>
    public Dictionary<string, string> PortOverrides { get; set; } = new();

    /// <summary>
    /// 环境变量覆盖
    /// </summary>
    public Dictionary<string, string> EnvOverrides { get; set; } = new();

    /// <summary>
    /// 是否附加到容器输出
    /// </summary>
    public bool Attach { get; set; }
}

/// <summary>
/// 构建结果
/// </summary>
public class BuildResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 镜像ID
    /// </summary>
    public string? ImageId { get; set; }

    /// <summary>
    /// 镜像名称
    /// </summary>
    public string? ImageName { get; set; }

    /// <summary>
    /// 构建日志
    /// </summary>
    public List<string> BuildLogs { get; set; } = new();

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 构建时间（毫秒）
    /// </summary>
    public long BuildTimeMs { get; set; }
}

/// <summary>
/// 容器启动结果
/// </summary>
public class ContainerStartResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 容器ID
    /// </summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// 启动模式
    /// </summary>
    public StartMode Mode { get; set; }

    /// <summary>
    /// 端口映射
    /// </summary>
    public Dictionary<string, string> PortMappings { get; set; } = new();

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 启动日志
    /// </summary>
    public List<string> StartupLogs { get; set; } = new();
}

/// <summary>
/// 启动模式枚举 - 对应 deck-shell 的智能启动逻辑
/// </summary>
public enum StartMode
{
    /// <summary>
    /// 直接进入已运行容器
    /// </summary>
    AttachedToRunning,

    /// <summary>
    /// 启动现有容器
    /// </summary>
    StartedExisting,

    /// <summary>
    /// 创建新容器
    /// </summary>
    CreatedNew,

    /// <summary>
    /// 构建镜像并启动
    /// </summary>
    BuiltAndStarted,

    /// <summary>
    /// 新建容器
    /// </summary>
    New,

    /// <summary>
    /// 恢复已停止的容器
    /// </summary>
    Resume
}

/// <summary>
/// 日志选项
/// </summary>
public class LogOptions
{
    /// <summary>
    /// 是否跟随日志
    /// </summary>
    public bool Follow { get; set; }

    /// <summary>
    /// 显示时间戳
    /// </summary>
    public bool ShowTimestamps { get; set; } = true;

    /// <summary>
    /// 日志行数限制
    /// </summary>
    public int? TailLines { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? Since { get; set; }
}

/// <summary>
/// Shell 选项
/// </summary>
public class ShellOptions
{
    /// <summary>
    /// Shell 类型
    /// </summary>
    public string Shell { get; set; } = "/bin/bash";

    /// <summary>
    /// 工作目录
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// 是否分配 TTY
    /// </summary>
    public bool AllocateTty { get; set; } = true;

    /// <summary>
    /// 是否交互式
    /// </summary>
    public bool Interactive { get; set; } = true;
}

/// <summary>
/// 容器日志
/// </summary>
public class ContainerLogs
{
    /// <summary>
    /// 日志内容
    /// </summary>
    public List<LogEntry> Entries { get; set; } = new();

    /// <summary>
    /// 是否截断
    /// </summary>
    public bool IsTruncated { get; set; }

    /// <summary>
    /// 总行数
    /// </summary>
    public int TotalLines { get; set; }
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 日志内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 日志类型（stdout/stderr）
    /// </summary>
    public LogType Type { get; set; }
}

/// <summary>
/// 日志类型枚举
/// </summary>
public enum LogType
{
    Stdout,
    Stderr
}