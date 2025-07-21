namespace Deck.Core.Models;

/// <summary>
/// Docker Compose 文件验证结果
/// </summary>
public class ComposeValidationResult
{
    /// <summary>
    /// 是否通过验证
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Compose 文件版本
    /// </summary>
    public string? ComposeVersion { get; set; }

    /// <summary>
    /// 验证的文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 解析错误（语法错误等）
    /// </summary>
    public List<ComposeValidationError> ParseErrors { get; set; } = new();

    /// <summary>
    /// 配置警告
    /// </summary>
    public List<ComposeValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// 服务验证结果
    /// </summary>
    public List<ServiceValidationResult> ServiceResults { get; set; } = new();

    /// <summary>
    /// 网络配置验证结果
    /// </summary>
    public List<ComposeNetworkValidationResult> NetworkResults { get; set; } = new();

    /// <summary>
    /// 卷配置验证结果
    /// </summary>
    public List<VolumeValidationResult> VolumeResults { get; set; } = new();

    /// <summary>
    /// 环境变量检查结果
    /// </summary>
    public List<EnvironmentValidationResult> EnvironmentResults { get; set; } = new();

    /// <summary>
    /// 端口冲突检查
    /// </summary>
    public List<PortConflictResult> PortConflicts { get; set; } = new();

    /// <summary>
    /// 依赖检查结果
    /// </summary>
    public List<DependencyValidationResult> DependencyResults { get; set; } = new();

    /// <summary>
    /// 验证摘要信息
    /// </summary>
    public ComposeValidationSummary Summary { get; set; } = new();

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 验证耗时（毫秒）
    /// </summary>
    public long ValidationTimeMs { get; set; }
}

/// <summary>
/// Compose 验证错误
/// </summary>
public class ComposeValidationError
{
    /// <summary>
    /// 错误类型
    /// </summary>
    public ComposeErrorType Type { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误位置（行号）
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// 错误位置（列号）
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// 相关的服务名称
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? FixSuggestion { get; set; }
}

/// <summary>
/// Compose 验证警告
/// </summary>
public class ComposeValidationWarning
{
    /// <summary>
    /// 警告类型
    /// </summary>
    public ComposeWarningType Type { get; set; }

    /// <summary>
    /// 警告消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 相关的服务名称
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// 建议的改进措施
    /// </summary>
    public string? Recommendation { get; set; }

    /// <summary>
    /// 警告严重程度
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Warning;
}

/// <summary>
/// 服务验证结果
/// </summary>
public class ServiceValidationResult
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 镜像验证结果
    /// </summary>
    public ImageValidationResult? ImageValidation { get; set; }

    /// <summary>
    /// 端口配置验证
    /// </summary>
    public List<PortValidationResult> PortValidations { get; set; } = new();

    /// <summary>
    /// 卷挂载验证
    /// </summary>
    public List<VolumeValidationResult> VolumeValidations { get; set; } = new();

    /// <summary>
    /// 环境变量验证
    /// </summary>
    public List<EnvironmentValidationResult> EnvironmentValidations { get; set; } = new();

    /// <summary>
    /// 健康检查配置验证
    /// </summary>
    public HealthCheckValidationResult? HealthCheckValidation { get; set; }

    /// <summary>
    /// 服务验证错误
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 服务验证警告
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 镜像验证结果
/// </summary>
public class ImageValidationResult
{
    /// <summary>
    /// 镜像名称
    /// </summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// 镜像是否存在
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 镜像标签验证
    /// </summary>
    public bool HasValidTag { get; set; }

    /// <summary>
    /// 镜像大小（如果已拉取）
    /// </summary>
    public long? ImageSize { get; set; }

    /// <summary>
    /// 镜像创建时间
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// 是否为官方镜像
    /// </summary>
    public bool IsOfficialImage { get; set; }

    /// <summary>
    /// 安全扫描结果
    /// </summary>
    public SecurityScanResult? SecurityScan { get; set; }
}

/// <summary>
/// 端口验证结果
/// </summary>
public class PortValidationResult
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
    public ProtocolType Protocol { get; set; }

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 端口是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 冲突的进程信息
    /// </summary>
    public string? ConflictingProcess { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 卷验证结果
/// </summary>
public class VolumeValidationResult
{
    /// <summary>
    /// 卷名称或路径
    /// </summary>
    public string VolumeName { get; set; } = string.Empty;

    /// <summary>
    /// 挂载点
    /// </summary>
    public string MountPoint { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 源路径是否存在（对于 bind mount）
    /// </summary>
    public bool SourceExists { get; set; }

    /// <summary>
    /// 权限检查结果
    /// </summary>
    public bool HasValidPermissions { get; set; }

    /// <summary>
    /// 卷类型
    /// </summary>
    public VolumeType Type { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 卷类型枚举
/// </summary>
public enum VolumeType
{
    NamedVolume,
    BindMount,
    TmpfsMount,
    AnonymousVolume
}

/// <summary>
/// 环境变量验证结果
/// </summary>
public class EnvironmentValidationResult
{
    /// <summary>
    /// 环境变量名称
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// 是否定义
    /// </summary>
    public bool IsDefined { get; set; }

    /// <summary>
    /// 是否有默认值
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// 是否敏感信息
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 建议的配置方式
    /// </summary>
    public string? Recommendation { get; set; }
}

/// <summary>
/// Compose网络验证结果
/// </summary>
public class ComposeNetworkValidationResult
{
    /// <summary>
    /// 网络名称
    /// </summary>
    public string NetworkName { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 网络是否存在
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 网络驱动类型
    /// </summary>
    public string? Driver { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 健康检查验证结果
/// </summary>
public class HealthCheckValidationResult
{
    /// <summary>
    /// 是否配置了健康检查
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// 健康检查配置是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 测试命令
    /// </summary>
    public string? TestCommand { get; set; }

    /// <summary>
    /// 间隔时间
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// 超时时间
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int? Retries { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 端口冲突结果
/// </summary>
public class PortConflictResult
{
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 协议类型
    /// </summary>
    public ProtocolType Protocol { get; set; }

    /// <summary>
    /// 是否冲突
    /// </summary>
    public bool HasConflict { get; set; }

    /// <summary>
    /// 冲突的服务列表
    /// </summary>
    public List<string> ConflictingServices { get; set; } = new();

    /// <summary>
    /// 占用端口的进程信息
    /// </summary>
    public string? OccupyingProcess { get; set; }

    /// <summary>
    /// 解决建议
    /// </summary>
    public string? Resolution { get; set; }
}

/// <summary>
/// 依赖验证结果
/// </summary>
public class DependencyValidationResult
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 依赖的服务列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 是否有循环依赖
    /// </summary>
    public bool HasCircularDependency { get; set; }

    /// <summary>
    /// 循环依赖路径
    /// </summary>
    public List<string> CircularDependencyPath { get; set; } = new();

    /// <summary>
    /// 缺失的依赖服务
    /// </summary>
    public List<string> MissingDependencies { get; set; } = new();

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }
}

/// <summary>
/// 安全扫描结果
/// </summary>
public class SecurityScanResult
{
    /// <summary>
    /// 是否扫描完成
    /// </summary>
    public bool IsScanned { get; set; }

    /// <summary>
    /// 高危漏洞数量
    /// </summary>
    public int HighVulnerabilities { get; set; }

    /// <summary>
    /// 中危漏洞数量
    /// </summary>
    public int MediumVulnerabilities { get; set; }

    /// <summary>
    /// 低危漏洞数量
    /// </summary>
    public int LowVulnerabilities { get; set; }

    /// <summary>
    /// 总漏洞数量
    /// </summary>
    public int TotalVulnerabilities => HighVulnerabilities + MediumVulnerabilities + LowVulnerabilities;

    /// <summary>
    /// 扫描工具
    /// </summary>
    public string? ScanTool { get; set; }

    /// <summary>
    /// 扫描时间
    /// </summary>
    public DateTime? ScanTime { get; set; }
}

/// <summary>
/// Compose 验证摘要
/// </summary>
public class ComposeValidationSummary
{
    /// <summary>
    /// 总服务数
    /// </summary>
    public int TotalServices { get; set; }

    /// <summary>
    /// 有效服务数
    /// </summary>
    public int ValidServices { get; set; }

    /// <summary>
    /// 总错误数
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// 总警告数
    /// </summary>
    public int TotalWarnings { get; set; }

    /// <summary>
    /// 端口冲突数
    /// </summary>
    public int PortConflicts { get; set; }

    /// <summary>
    /// 缺失镜像数
    /// </summary>
    public int MissingImages { get; set; }

    /// <summary>
    /// 整体健康分数 (0-100)
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// 推荐的改进措施
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Compose 错误类型枚举
/// </summary>
public enum ComposeErrorType
{
    SyntaxError,
    InvalidVersion,
    MissingService,
    InvalidImageName,
    PortConflict,
    VolumeError,
    NetworkError,
    EnvironmentError,
    DependencyError,
    PermissionError
}

/// <summary>
/// Compose 警告类型枚举
/// </summary>
public enum ComposeWarningType
{
    DeprecatedSyntax,
    PerformanceIssue,
    SecurityConcern,
    BestPracticeViolation,
    MissingHealthCheck,
    UnoptimizedImage,
    ExposedSensitiveData
}

/// <summary>
/// 验证严重程度枚举
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}