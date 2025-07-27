namespace Deck.Core.Models;

/// <summary>
/// 网络服务类型枚举
/// </summary>
public enum NetworkServiceType
{
    /// <summary>
    /// GitHub服务
    /// </summary>
    GitHub,

    /// <summary>
    /// Docker Hub
    /// </summary>
    DockerHub,

    /// <summary>
    /// Quay.io容器仓库
    /// </summary>
    QuayIo,

    /// <summary>
    /// 阿里云容器仓库
    /// </summary>
    AliyunRegistry,

    /// <summary>
    /// 腾讯云容器仓库
    /// </summary>
    TencentRegistry,

    /// <summary>
    /// 网易云容器仓库
    /// </summary>
    NetEaseRegistry,

    /// <summary>
    /// 中国科技大学镜像
    /// </summary>
    UstcRegistry,

    /// <summary>
    /// 清华大学镜像
    /// </summary>
    TsinghuaRegistry,

    /// <summary>
    /// 通用HTTP连通性测试
    /// </summary>
    HttpConnectivity,

    /// <summary>
    /// DNS解析测试
    /// </summary>
    DnsResolution
}

/// <summary>
/// 容器仓库类型枚举
/// </summary>
public enum ContainerRegistryType
{
    /// <summary>
    /// Docker Hub官方
    /// </summary>
    DockerHub,

    /// <summary>
    /// Quay.io
    /// </summary>
    QuayIo,

    /// <summary>
    /// 阿里云容器仓库
    /// </summary>
    AliyunRegistry,

    /// <summary>
    /// 腾讯云容器仓库
    /// </summary>
    TencentRegistry,

    /// <summary>
    /// 网易云容器仓库
    /// </summary>
    NetEaseRegistry,

    /// <summary>
    /// 中国科技大学镜像
    /// </summary>
    UstcRegistry,

    /// <summary>
    /// 清华大学镜像
    /// </summary>
    TsinghuaRegistry,

    /// <summary>
    /// 自定义仓库
    /// </summary>
    Custom
}

/// <summary>
/// 连通性状态枚举
/// </summary>
public enum ConnectivityStatus
{
    /// <summary>
    /// 连接成功
    /// </summary>
    Connected,

    /// <summary>
    /// 连接失败
    /// </summary>
    Failed,

    /// <summary>
    /// 连接缓慢
    /// </summary>
    Slow,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout,

    /// <summary>
    /// DNS解析失败
    /// </summary>
    DnsFailure,

    /// <summary>
    /// 代理错误
    /// </summary>
    ProxyError,

    /// <summary>
    /// 证书错误
    /// </summary>
    CertificateError
}

/// <summary>
/// 网络连通性检测结果
/// </summary>
public class NetworkConnectivityResult
{
    /// <summary>
    /// 是否连通
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 整体连通性状态
    /// </summary>
    public ConnectivityStatus OverallStatus { get; set; }

    /// <summary>
    /// 服务检测结果列表
    /// </summary>
    public List<ServiceConnectivityResult> ServiceResults { get; set; } = new();

    /// <summary>
    /// 网络类型（有线/无线/移动网络）
    /// </summary>
    public string NetworkType { get; set; } = string.Empty;

    /// <summary>
    /// 本地IP地址
    /// </summary>
    public string? LocalIPAddress { get; set; }

    /// <summary>
    /// 公网IP地址
    /// </summary>
    public string? PublicIPAddress { get; set; }

    /// <summary>
    /// 地理位置信息
    /// </summary>
    public GeolocationInfo? Geolocation { get; set; }

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 总检测耗时（毫秒）
    /// </summary>
    public long TotalElapsedMs { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 建议信息
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 服务连通性检测结果
/// </summary>
public class ServiceConnectivityResult
{
    /// <summary>
    /// 服务类型
    /// </summary>
    public NetworkServiceType ServiceType { get; set; }

    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 测试URL
    /// </summary>
    public string TestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 连通性状态
    /// </summary>
    public ConnectivityStatus Status { get; set; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// HTTP状态码
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细错误信息
    /// </summary>
    public string? DetailedError { get; set; }

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否使用了代理
    /// </summary>
    public bool UsedProxy { get; set; }

    /// <summary>
    /// 代理信息
    /// </summary>
    public string? ProxyInfo { get; set; }
}

/// <summary>
/// 地理位置信息
/// </summary>
public class GeolocationInfo
{
    /// <summary>
    /// 国家代码
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// 国家名称
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    /// 地区/省份
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// ISP供应商
    /// </summary>
    public string? ISP { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    public string? TimeZone { get; set; }
}

/// <summary>
/// 网络Fallback策略
/// </summary>
public class NetworkFallbackStrategy
{
    /// <summary>
    /// 策略类型
    /// </summary>
    public FallbackStrategyType StrategyType { get; set; }

    /// <summary>
    /// 策略描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 推荐操作
    /// </summary>
    public List<FallbackAction> RecommendedActions { get; set; } = new();

    /// <summary>
    /// 替代服务列表
    /// </summary>
    public List<AlternativeService> AlternativeServices { get; set; } = new();

    /// <summary>
    /// 是否启用离线模式
    /// </summary>
    public bool EnableOfflineMode { get; set; }

    /// <summary>
    /// 预期恢复时间
    /// </summary>
    public TimeSpan? EstimatedRecoveryTime { get; set; }

    /// <summary>
    /// 严重程度
    /// </summary>
    public FallbackSeverity Severity { get; set; }
}

/// <summary>
/// Fallback策略类型枚举
/// </summary>
public enum FallbackStrategyType
{
    /// <summary>
    /// 使用镜像服务
    /// </summary>
    UseMirrorService,

    /// <summary>
    /// 配置代理
    /// </summary>
    ConfigureProxy,

    /// <summary>
    /// 启用离线模式
    /// </summary>
    EnableOfflineMode,

    /// <summary>
    /// 等待网络恢复
    /// </summary>
    WaitForRecovery,

    /// <summary>
    /// 使用本地缓存
    /// </summary>
    UseLocalCache,

    /// <summary>
    /// 重试连接
    /// </summary>
    RetryConnection
}

/// <summary>
/// Fallback严重程度枚举
/// </summary>
public enum FallbackSeverity
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,

    /// <summary>
    /// 警告
    /// </summary>
    Warning,

    /// <summary>
    /// 错误
    /// </summary>
    Error,

    /// <summary>
    /// 严重错误
    /// </summary>
    Critical
}

/// <summary>
/// Fallback操作
/// </summary>
public class FallbackAction
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作命令
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// 是否自动执行
    /// </summary>
    public bool CanAutoExecute { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public ActionPriority Priority { get; set; }

    /// <summary>
    /// 预期成功率
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// 操作类型枚举
/// </summary>
public enum ActionType
{
    /// <summary>
    /// 配置代理
    /// </summary>
    ConfigureProxy,

    /// <summary>
    /// 切换镜像源
    /// </summary>
    SwitchRegistry,

    /// <summary>
    /// 重启网络
    /// </summary>
    RestartNetwork,

    /// <summary>
    /// 检查防火墙
    /// </summary>
    CheckFirewall,

    /// <summary>
    /// 清除DNS缓存
    /// </summary>
    ClearDnsCache,

    /// <summary>
    /// 启用离线模式
    /// </summary>
    EnableOfflineMode,

    /// <summary>
    /// 联系管理员
    /// </summary>
    ContactAdmin
}

/// <summary>
/// 操作优先级枚举
/// </summary>
public enum ActionPriority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    Low,

    /// <summary>
    /// 中等优先级
    /// </summary>
    Medium,

    /// <summary>
    /// 高优先级
    /// </summary>
    High,

    /// <summary>
    /// 紧急
    /// </summary>
    Urgent
}

/// <summary>
/// 替代服务
/// </summary>
public class AlternativeService
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 服务URL
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// 服务类型
    /// </summary>
    public NetworkServiceType ServiceType { get; set; }

    /// <summary>
    /// 地理区域
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 可靠性评分（0-100）
    /// </summary>
    public int ReliabilityScore { get; set; }

    /// <summary>
    /// 速度评分（0-100）
    /// </summary>
    public int SpeedScore { get; set; }

    /// <summary>
    /// 是否推荐
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// 说明信息
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 代理配置结果
/// </summary>
public class ProxyConfigurationResult
{
    /// <summary>
    /// 是否成功配置
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// 检测到的代理信息
    /// </summary>
    public ProxyInfo? DetectedProxy { get; set; }

    /// <summary>
    /// 当前代理配置
    /// </summary>
    public ProxyInfo? CurrentProxy { get; set; }

    /// <summary>
    /// 代理测试结果
    /// </summary>
    public List<ProxyTestResult> ProxyTests { get; set; } = new();

    /// <summary>
    /// 建议的代理设置
    /// </summary>
    public List<ProxyRecommendation> ProxyRecommendations { get; set; } = new();

    /// <summary>
    /// 配置信息
    /// </summary>
    public List<string> ConfigurationMessages { get; set; } = new();
}

/// <summary>
/// 代理信息
/// </summary>
public class ProxyInfo
{
    /// <summary>
    /// 代理类型
    /// </summary>
    public ProxyType Type { get; set; }

    /// <summary>
    /// 主机地址
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 绕过代理的域名列表
    /// </summary>
    public List<string> BypassList { get; set; } = new();

    /// <summary>
    /// 是否自动检测
    /// </summary>
    public bool AutoDetect { get; set; }

    /// <summary>
    /// 代理配置脚本URL
    /// </summary>
    public string? ConfigurationScript { get; set; }
}

/// <summary>
/// 代理类型枚举
/// </summary>
public enum ProxyType
{
    /// <summary>
    /// 无代理
    /// </summary>
    None,

    /// <summary>
    /// HTTP代理
    /// </summary>
    Http,

    /// <summary>
    /// HTTPS代理
    /// </summary>
    Https,

    /// <summary>
    /// SOCKS4代理
    /// </summary>
    Socks4,

    /// <summary>
    /// SOCKS5代理
    /// </summary>
    Socks5,

    /// <summary>
    /// 自动检测
    /// </summary>
    Auto
}

/// <summary>
/// 代理测试结果
/// </summary>
public class ProxyTestResult
{
    /// <summary>
    /// 测试的代理信息
    /// </summary>
    public ProxyInfo ProxyInfo { get; set; } = new();

    /// <summary>
    /// 测试是否成功
    /// </summary>
    public bool IsWorking { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 测试URL
    /// </summary>
    public string TestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 测试时间
    /// </summary>
    public DateTime TestTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 代理推荐
/// </summary>
public class ProxyRecommendation
{
    /// <summary>
    /// 推荐的代理信息
    /// </summary>
    public ProxyInfo ProxyInfo { get; set; } = new();

    /// <summary>
    /// 推荐原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 置信度（0-100）
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// 配置说明
    /// </summary>
    public List<string> ConfigurationInstructions { get; set; } = new();
}

/// <summary>
/// 容器仓库连通性结果
/// </summary>
public class RegistryConnectivityResult
{
    /// <summary>
    /// 仓库类型
    /// </summary>
    public ContainerRegistryType RegistryType { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    public string RegistryName { get; set; } = string.Empty;

    /// <summary>
    /// 仓库URL
    /// </summary>
    public string RegistryUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 连通性状态
    /// </summary>
    public ConnectivityStatus Status { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 速度评级
    /// </summary>
    public SpeedRating SpeedRating { get; set; }

    /// <summary>
    /// 支持的功能
    /// </summary>
    public List<RegistryFeature> SupportedFeatures { get; set; } = new();

    /// <summary>
    /// API版本
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 地理区域
    /// </summary>
    public string? Region { get; set; }
}

/// <summary>
/// 速度评级枚举
/// </summary>
public enum SpeedRating
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown,

    /// <summary>
    /// 很慢
    /// </summary>
    VerySlow,

    /// <summary>
    /// 慢
    /// </summary>
    Slow,

    /// <summary>
    /// 一般
    /// </summary>
    Average,

    /// <summary>
    /// 快
    /// </summary>
    Fast,

    /// <summary>
    /// 很快
    /// </summary>
    VeryFast
}

/// <summary>
/// 仓库功能枚举
/// </summary>
public enum RegistryFeature
{
    /// <summary>
    /// Docker Registry API v2
    /// </summary>
    RegistryV2,

    /// <summary>
    /// 镜像签名验证
    /// </summary>
    ImageSigning,

    /// <summary>
    /// 漏洞扫描
    /// </summary>
    VulnerabilityScanning,

    /// <summary>
    /// 镜像缓存
    /// </summary>
    ImageCaching,

    /// <summary>
    /// 私有仓库
    /// </summary>
    PrivateRegistry,

    /// <summary>
    /// 公共镜像
    /// </summary>
    PublicImages
}

/// <summary>
/// 容器仓库推荐
/// </summary>
public class ContainerRegistryRecommendation
{
    /// <summary>
    /// 仓库类型
    /// </summary>
    public ContainerRegistryType RegistryType { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    public string RegistryName { get; set; } = string.Empty;

    /// <summary>
    /// 仓库URL
    /// </summary>
    public string RegistryUrl { get; set; } = string.Empty;

    /// <summary>
    /// 推荐评分（0-100）
    /// </summary>
    public int RecommendationScore { get; set; }

    /// <summary>
    /// 推荐原因
    /// </summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>
    /// 地理区域
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 优势
    /// </summary>
    public List<string> Advantages { get; set; } = new();

    /// <summary>
    /// 劣势
    /// </summary>
    public List<string> Disadvantages { get; set; } = new();

    /// <summary>
    /// 配置说明
    /// </summary>
    public List<string> ConfigurationInstructions { get; set; } = new();
}

/// <summary>
/// 网络验证结果
/// </summary>
public class NetworkValidationResult
{
    /// <summary>
    /// 验证是否通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 网络配置检查结果
    /// </summary>
    public List<NetworkConfigCheck> ConfigurationChecks { get; set; } = new();

    /// <summary>
    /// 性能检查结果
    /// </summary>
    public List<NetworkPerformanceCheck> PerformanceChecks { get; set; } = new();

    /// <summary>
    /// 安全检查结果
    /// </summary>
    public List<NetworkSecurityCheck> SecurityChecks { get; set; } = new();

    /// <summary>
    /// 总体评分（0-100）
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// 建议改进项
    /// </summary>
    public List<string> ImprovementSuggestions { get; set; } = new();

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidationTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 网络配置检查
/// </summary>
public class NetworkConfigCheck
{
    /// <summary>
    /// 检查项名称
    /// </summary>
    public string CheckName { get; set; } = string.Empty;

    /// <summary>
    /// 检查结果
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 检查描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? FixSuggestion { get; set; }
}

/// <summary>
/// 网络性能检查
/// </summary>
public class NetworkPerformanceCheck
{
    /// <summary>
    /// 检查项名称
    /// </summary>
    public string CheckName { get; set; } = string.Empty;

    /// <summary>
    /// 性能指标值
    /// </summary>
    public double MetricValue { get; set; }

    /// <summary>
    /// 指标单位
    /// </summary>
    public string MetricUnit { get; set; } = string.Empty;

    /// <summary>
    /// 基准值
    /// </summary>
    public double BenchmarkValue { get; set; }

    /// <summary>
    /// 是否达标
    /// </summary>
    public bool MeetsBenchmark { get; set; }

    /// <summary>
    /// 性能评级
    /// </summary>
    public PerformanceRating Rating { get; set; }
}

/// <summary>
/// 性能评级枚举
/// </summary>
public enum PerformanceRating
{
    /// <summary>
    /// 优秀
    /// </summary>
    Excellent,

    /// <summary>
    /// 良好
    /// </summary>
    Good,

    /// <summary>
    /// 一般
    /// </summary>
    Average,

    /// <summary>
    /// 较差
    /// </summary>
    Poor,

    /// <summary>
    /// 很差
    /// </summary>
    VeryPoor
}

/// <summary>
/// 网络安全检查
/// </summary>
public class NetworkSecurityCheck
{
    /// <summary>
    /// 检查项名称
    /// </summary>
    public string CheckName { get; set; } = string.Empty;

    /// <summary>
    /// 安全级别
    /// </summary>
    public SecurityLevel SecurityLevel { get; set; }

    /// <summary>
    /// 检查结果
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 风险描述
    /// </summary>
    public string? RiskDescription { get; set; }

    /// <summary>
    /// 缓解建议
    /// </summary>
    public string? MitigationSuggestion { get; set; }
}

/// <summary>
/// 安全级别枚举
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// 安全
    /// </summary>
    Secure,

    /// <summary>
    /// 低风险
    /// </summary>
    LowRisk,

    /// <summary>
    /// 中等风险
    /// </summary>
    MediumRisk,

    /// <summary>
    /// 高风险
    /// </summary>
    HighRisk,

    /// <summary>
    /// 严重风险
    /// </summary>
    CriticalRisk
}

/// <summary>
/// 离线模式结果
/// </summary>
public class OfflineModeResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 当前离线模式状态
    /// </summary>
    public bool IsOfflineModeEnabled { get; set; }

    /// <summary>
    /// 启用/禁用原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 离线模式功能限制
    /// </summary>
    public List<string> Limitations { get; set; } = new();

    /// <summary>
    /// 可用的离线功能
    /// </summary>
    public List<string> AvailableFeatures { get; set; } = new();

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 状态消息
    /// </summary>
    public string? StatusMessage { get; set; }
}

/// <summary>
/// 网络状态结果
/// </summary>
public class NetworkStatusResult
{
    /// <summary>
    /// 网络是否可用
    /// </summary>
    public bool IsNetworkAvailable { get; set; }

    /// <summary>
    /// 互联网是否可用
    /// </summary>
    public bool IsInternetAvailable { get; set; }

    /// <summary>
    /// 是否在离线模式
    /// </summary>
    public bool IsOfflineMode { get; set; }

    /// <summary>
    /// 当前网络类型
    /// </summary>
    public string NetworkType { get; set; } = string.Empty;

    /// <summary>
    /// 网络接口信息
    /// </summary>
    public List<NetworkInterface> NetworkInterfaces { get; set; } = new();

    /// <summary>
    /// 活跃连接统计
    /// </summary>
    public NetworkStatistics Statistics { get; set; } = new();

    /// <summary>
    /// DNS服务器列表
    /// </summary>
    public List<string> DnsServers { get; set; } = new();

    /// <summary>
    /// 代理状态
    /// </summary>
    public ProxyInfo? CurrentProxy { get; set; }

    /// <summary>
    /// 状态检测时间
    /// </summary>
    public DateTime StatusTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 网络接口信息
/// </summary>
public class NetworkInterface
{
    /// <summary>
    /// 接口名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 接口类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// IP地址列表
    /// </summary>
    public List<string> IPAddresses { get; set; } = new();

    /// <summary>
    /// MAC地址
    /// </summary>
    public string? MacAddress { get; set; }

    /// <summary>
    /// 接收字节数
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// 发送字节数
    /// </summary>
    public long BytesSent { get; set; }
}

/// <summary>
/// 网络统计信息
/// </summary>
public class NetworkStatistics
{
    /// <summary>
    /// 活跃TCP连接数
    /// </summary>
    public int ActiveTcpConnections { get; set; }

    /// <summary>
    /// 活跃UDP连接数
    /// </summary>
    public int ActiveUdpConnections { get; set; }

    /// <summary>
    /// 总接收字节数
    /// </summary>
    public long TotalBytesReceived { get; set; }

    /// <summary>
    /// 总发送字节数
    /// </summary>
    public long TotalBytesSent { get; set; }

    /// <summary>
    /// 网络错误数
    /// </summary>
    public int NetworkErrors { get; set; }

    /// <summary>
    /// 平均延迟（毫秒）
    /// </summary>
    public double AverageLatencyMs { get; set; }
}

/// <summary>
/// 网络速度测试结果
/// </summary>
public class NetworkSpeedResult
{
    /// <summary>
    /// 测试是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 下载速度（Mbps）
    /// </summary>
    public double DownloadSpeedMbps { get; set; }

    /// <summary>
    /// 上传速度（Mbps）
    /// </summary>
    public double UploadSpeedMbps { get; set; }

    /// <summary>
    /// 延迟（毫秒）
    /// </summary>
    public double LatencyMs { get; set; }

    /// <summary>
    /// 抖动（毫秒）
    /// </summary>
    public double JitterMs { get; set; }

    /// <summary>
    /// 丢包率（百分比）
    /// </summary>
    public double PacketLossPercent { get; set; }

    /// <summary>
    /// 测试URL
    /// </summary>
    public string TestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 测试数据大小（MB）
    /// </summary>
    public double TestDataSizeMB { get; set; }

    /// <summary>
    /// 测试持续时间（秒）
    /// </summary>
    public double TestDurationSeconds { get; set; }

    /// <summary>
    /// 网络质量评级
    /// </summary>
    public NetworkQualityRating QualityRating { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 测试时间
    /// </summary>
    public DateTime TestTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 网络质量评级枚举
/// </summary>
public enum NetworkQualityRating
{
    /// <summary>
    /// 优秀
    /// </summary>
    Excellent,

    /// <summary>
    /// 良好
    /// </summary>
    Good,

    /// <summary>
    /// 一般
    /// </summary>
    Fair,

    /// <summary>
    /// 较差
    /// </summary>
    Poor,

    /// <summary>
    /// 很差
    /// </summary>
    VeryPoor
}

/// <summary>
/// 网络服务配置映射
/// </summary>
public static class NetworkServiceEndpoints
{
    /// <summary>
    /// 获取服务对应的测试终端点
    /// </summary>
    public static readonly Dictionary<NetworkServiceType, string> ServiceUrls = new()
    {
        { NetworkServiceType.HttpConnectivity, "https://httpbin.org/get" }, // 通用HTTP测试端点
        { NetworkServiceType.DnsResolution, "8.8.8.8" }
    };

    /// <summary>
    /// 获取容器仓库对应的测试终端点
    /// </summary>
    public static readonly Dictionary<ContainerRegistryType, string> RegistryUrls = new()
    {
        { ContainerRegistryType.DockerHub, "https://registry-1.docker.io/v2/" },
        { ContainerRegistryType.QuayIo, "https://quay.io/api/v1/" },
        { ContainerRegistryType.AliyunRegistry, "https://registry.cn-hangzhou.aliyuncs.com/v2/" },
        { ContainerRegistryType.TencentRegistry, "https://ccr.ccs.tencentyun.com/v2/" },
        { ContainerRegistryType.NetEaseRegistry, "https://hub.c.163.com/v2/" },
        { ContainerRegistryType.UstcRegistry, "https://docker.mirrors.ustc.edu.cn/v2/" },
        { ContainerRegistryType.TsinghuaRegistry, "https://docker.mirrors.tuna.tsinghua.edu.cn/v2/" }
    };

    /// <summary>
    /// 获取服务的友好名称
    /// </summary>
    public static readonly Dictionary<NetworkServiceType, string> ServiceNames = new()
    {
        { NetworkServiceType.HttpConnectivity, "HTTP连通性" },
        { NetworkServiceType.DnsResolution, "DNS解析" }
    };

    /// <summary>
    /// 获取容器仓库的友好名称
    /// </summary>
    public static readonly Dictionary<ContainerRegistryType, string> RegistryNames = new()
    {
        { ContainerRegistryType.DockerHub, "Docker Hub" },
        { ContainerRegistryType.QuayIo, "Quay.io" },
        { ContainerRegistryType.AliyunRegistry, "阿里云容器仓库" },
        { ContainerRegistryType.TencentRegistry, "腾讯云容器仓库" },
        { ContainerRegistryType.NetEaseRegistry, "网易云容器仓库" },
        { ContainerRegistryType.UstcRegistry, "中科大镜像站" },
        { ContainerRegistryType.TsinghuaRegistry, "清华大学镜像站" },
        { ContainerRegistryType.Custom, "自定义仓库" }
    };
}