namespace Deck.Core.Models;

/// <summary>
/// 三层配置选项 - 对应 deck-shell 的三层目录结构
/// </summary>
public class ThreeLayerOptions
{
    /// <summary>
    /// Images 层配置 - 已构建的镜像配置（可直接启动）
    /// </summary>
    public List<ConfigurationOption> Images { get; set; } = new();

    /// <summary>
    /// Custom 层配置 - 用户自定义配置（需要构建）
    /// </summary>
    public List<ConfigurationOption> Custom { get; set; } = new();

    /// <summary>
    /// Templates 层配置 - 官方模板（需要复制或直接构建）
    /// </summary>
    public List<ConfigurationOption> Templates { get; set; } = new();
}

/// <summary>
/// 配置选项
/// </summary>
public class ConfigurationOption
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 配置类型
    /// </summary>
    public ConfigurationType Type { get; set; }

    /// <summary>
    /// 配置路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 描述信息
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// 镜像元数据（仅 Images 类型）
    /// </summary>
    public ImageMetadata? Metadata { get; set; }
}

/// <summary>
/// 配置类型枚举
/// </summary>
public enum ConfigurationType
{
    /// <summary>
    /// 已构建镜像配置
    /// </summary>
    Images,

    /// <summary>
    /// 用户自定义配置
    /// </summary>
    Custom,

    /// <summary>
    /// 官方模板配置
    /// </summary>
    Templates
}

/// <summary>
/// 镜像元数据 - 对应 deck-shell 的 .deck-metadata
/// </summary>
public class ImageMetadata
{
    /// <summary>
    /// 镜像名称
    /// </summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 源配置路径
    /// </summary>
    public string SourceConfig { get; set; } = string.Empty;

    /// <summary>
    /// 构建状态
    /// </summary>
    public BuildStatus BuildStatus { get; set; }

    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastStarted { get; set; }

    /// <summary>
    /// 容器名称
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// 运行时变量（来自 .env）
    /// </summary>
    public Dictionary<string, string> RuntimeVariables { get; set; } = new();

    /// <summary>
    /// 构建时变量
    /// </summary>
    public Dictionary<string, string> BuildTimeVariables { get; set; } = new();
}

/// <summary>
/// 构建状态枚举
/// </summary>
public enum BuildStatus
{
    /// <summary>
    /// 构建中
    /// </summary>
    Building,

    /// <summary>
    /// 构建完成
    /// </summary>
    Built,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// 构建失败
    /// </summary>
    Failed
}

/// <summary>
/// 目录结构验证结果
/// </summary>
public class DirectoryStructureResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告信息
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 修复建议
    /// </summary>
    public List<string> RepairSuggestions { get; set; } = new();
}