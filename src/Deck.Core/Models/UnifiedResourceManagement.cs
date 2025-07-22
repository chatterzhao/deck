using System.ComponentModel;

namespace Deck.Core.Models;

/// <summary>
/// 统一资源类型枚举
/// </summary>
public enum UnifiedResourceType
{
    [Description("已构建镜像")]
    Images,
    
    [Description("用户自定义配置")]
    Custom,
    
    [Description("模板库")]
    Templates
}

/// <summary>
/// 三层统一资源列表
/// </summary>
public class UnifiedResourceList
{
    /// <summary>
    /// Images层资源列表
    /// </summary>
    public List<UnifiedResource> Images { get; set; } = new();
    
    /// <summary>
    /// Custom层资源列表
    /// </summary>
    public List<UnifiedResource> Custom { get; set; } = new();
    
    /// <summary>
    /// Templates层资源列表
    /// </summary>
    public List<UnifiedResource> Templates { get; set; } = new();
    
    /// <summary>
    /// 获取所有资源的扁平化列表（用于交互式选择）
    /// </summary>
    public List<UnifiedResource> GetFlattenedResources()
    {
        var result = new List<UnifiedResource>();
        result.AddRange(Images);
        result.AddRange(Custom);
        result.AddRange(Templates);
        return result;
    }
    
    /// <summary>
    /// 获取指定环境类型的过滤资源
    /// </summary>
    public UnifiedResourceList FilterByEnvironmentType(string environmentType)
    {
        if (string.IsNullOrEmpty(environmentType) || environmentType == "unknown")
        {
            return this;
        }
        
        return new UnifiedResourceList
        {
            Images = Images.Where(r => r.Name.StartsWith($"{environmentType}-", StringComparison.OrdinalIgnoreCase)).ToList(),
            Custom = Custom.Where(r => r.Name.StartsWith($"{environmentType}-", StringComparison.OrdinalIgnoreCase)).ToList(),
            Templates = Templates.Where(r => r.Name.StartsWith($"{environmentType}-", StringComparison.OrdinalIgnoreCase)).ToList()
        };
    }
}

/// <summary>
/// 统一资源项
/// </summary>
public class UnifiedResource
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// 资源类型
    /// </summary>
    public required UnifiedResourceType Type { get; set; }
    
    /// <summary>
    /// 资源状态
    /// </summary>
    public required ResourceStatus Status { get; set; }
    
    /// <summary>
    /// 显示标签（用于UI显示）
    /// </summary>
    public required string DisplayLabel { get; set; }
    
    /// <summary>
    /// 相对时间信息（如："2小时前"）
    /// </summary>
    public string? RelativeTime { get; set; }
    
    /// <summary>
    /// 是否可用（配置文件完整）
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    
    /// <summary>
    /// 不可用原因（当IsAvailable=false时）
    /// </summary>
    public string? UnavailableReason { get; set; }
    
    /// <summary>
    /// 元数据信息
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// 资源状态枚举
/// </summary>
public enum ResourceStatus
{
    [Description("就绪")]
    Ready,
    
    [Description("构建中")]
    Building,
    
    [Description("运行中")]
    Running,
    
    [Description("已停止")]
    Stopped,
    
    [Description("不可用")]
    Unavailable,
    
    [Description("内置")]
    Builtin
}

/// <summary>
/// 资源关联关系
/// </summary>
public class ResourceRelationship
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public required string ResourceName { get; set; }
    
    /// <summary>
    /// 关联的Podman镜像名称
    /// </summary>
    public string? PodmanImageName { get; set; }
    
    /// <summary>
    /// 关联的容器名称列表
    /// </summary>
    public List<string> ContainerNames { get; set; } = new();
    
    /// <summary>
    /// 依赖关系（从哪个资源创建的）
    /// </summary>
    public string? SourceResource { get; set; }
    
    /// <summary>
    /// 依赖关系类型
    /// </summary>
    public string? SourceType { get; set; }
}

/// <summary>
/// 资源详细信息
/// </summary>
public class UnifiedResourceDetail
{
    /// <summary>
    /// 基本资源信息
    /// </summary>
    public required UnifiedResource Resource { get; set; }
    
    /// <summary>
    /// 配置文件完整性状态
    /// </summary>
    public required ConfigurationStatus ConfigurationStatus { get; set; }
    
    /// <summary>
    /// 文件系统信息
    /// </summary>
    public required ResourceFileSystemInfo FileSystemInfo { get; set; }
    
    /// <summary>
    /// 构建/创建信息
    /// </summary>
    public BuildInfo? BuildInfo { get; set; }
    
    /// <summary>
    /// 关联的资源关系
    /// </summary>
    public ResourceRelationship? Relationship { get; set; }
}

/// <summary>
/// 配置状态
/// </summary>
public class ConfigurationStatus
{
    /// <summary>
    /// 是否有compose.yaml
    /// </summary>
    public bool HasComposeYaml { get; set; }
    
    /// <summary>
    /// 是否有Dockerfile
    /// </summary>
    public bool HasDockerfile { get; set; }
    
    /// <summary>
    /// 是否有.env文件
    /// </summary>
    public bool HasEnvFile { get; set; }
    
    /// <summary>
    /// 缺失的配置文件列表
    /// </summary>
    public List<string> MissingFiles { get; set; } = new();
    
    /// <summary>
    /// 是否完整（所有必需文件都存在）
    /// </summary>
    public bool IsComplete => MissingFiles.Count == 0;
}

/// <summary>
/// 资源文件系统信息
/// </summary>
public class ResourceFileSystemInfo
{
    /// <summary>
    /// 目录路径
    /// </summary>
    public required string DirectoryPath { get; set; }
    
    /// <summary>
    /// 目录大小
    /// </summary>
    public string? DirectorySize { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// 构建信息
/// </summary>
public class BuildInfo
{
    /// <summary>
    /// 构建状态
    /// </summary>
    public string? BuildStatus { get; set; }
    
    /// <summary>
    /// 构建时间
    /// </summary>
    public DateTime? BuildTime { get; set; }
    
    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastStartTime { get; set; }
    
    /// <summary>
    /// 源配置路径
    /// </summary>
    public string? SourceConfigPath { get; set; }
    
    /// <summary>
    /// 用户前缀
    /// </summary>
    public string? UserPrefix { get; set; }
}

/// <summary>
/// 清理选项
/// </summary>
public class CleaningOption
{
    /// <summary>
    /// 选项ID
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public required string DisplayName { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// 影响的资源类型
    /// </summary>
    public required UnifiedResourceType ResourceType { get; set; }
    
    /// <summary>
    /// 清理策略
    /// </summary>
    public required CleaningStrategy Strategy { get; set; }
    
    /// <summary>
    /// 警告级别
    /// </summary>
    public required ConfirmationLevel WarningLevel { get; set; }
    
    /// <summary>
    /// 预估清理的资源数量
    /// </summary>
    public int EstimatedCount { get; set; }
    
    /// <summary>
    /// 额外参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 清理策略枚举
/// </summary>
public enum CleaningStrategy
{
    [Description("保留最新N个")]
    KeepLatestN,
    
    [Description("删除指定资源")]
    DeleteSpecific,
    
    [Description("删除所有")]
    DeleteAll,
    
    [Description("智能提示替代")]
    SmartSuggestion
}

/// <summary>
/// 清理执行结果
/// </summary>
public class CleaningResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 实际清理的资源数量
    /// </summary>
    public int CleanedCount { get; set; }
    
    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 清理的资源详情
    /// </summary>
    public List<string> CleanedResources { get; set; } = new();
    
    /// <summary>
    /// 跳过的资源及原因
    /// </summary>
    public Dictionary<string, string> SkippedResources { get; set; } = new();
}

/// <summary>
/// 资源验证结果
/// </summary>
public class ResourceValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// 验证错误信息
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();
    
    /// <summary>
    /// 验证警告信息
    /// </summary>
    public List<string> ValidationWarnings { get; set; } = new();
    
    /// <summary>
    /// 配置状态
    /// </summary>
    public ConfigurationStatus? ConfigurationStatus { get; set; }
}