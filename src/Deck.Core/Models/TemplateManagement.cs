namespace Deck.Core.Models;

/// <summary>
/// 模板信息
/// </summary>
public class TemplateInfo
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 是否为官方模板
    /// </summary>
    public bool IsOfficial { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 必需文件
    /// </summary>
    public List<string> RequiredFiles { get; set; } = new();

    /// <summary>
    /// 模板变量
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}

/// <summary>
/// 同步结果
/// </summary>
public class SyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 同步的模板数量
    /// </summary>
    public int SyncedTemplateCount { get; set; }

    /// <summary>
    /// 更新的模板列表
    /// </summary>
    public List<string> UpdatedTemplates { get; set; } = new();

    /// <summary>
    /// 新增的模板列表
    /// </summary>
    public List<string> NewTemplates { get; set; } = new();

    /// <summary>
    /// 删除的模板列表
    /// </summary>
    public List<string> RemovedTemplates { get; set; } = new();

    /// <summary>
    /// 同步日志
    /// </summary>
    public List<string> SyncLogs { get; set; } = new();

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 同步时间（毫秒）
    /// </summary>
    public long SyncTimeMs { get; set; }
}

/// <summary>
/// 更新检查结果
/// </summary>
public class UpdateCheckResult
{
    /// <summary>
    /// 是否有更新
    /// </summary>
    public bool HasUpdates { get; set; }

    /// <summary>
    /// 可更新的模板列表
    /// </summary>
    public List<TemplateUpdateInfo> AvailableUpdates { get; set; } = new();

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 网络状态
    /// </summary>
    public NetworkStatus NetworkStatus { get; set; }
}

/// <summary>
/// 模板更新信息
/// </summary>
public class TemplateUpdateInfo
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 当前版本
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// 最新版本
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// 更新说明
    /// </summary>
    public string? UpdateDescription { get; set; }

    /// <summary>
    /// 更新类型
    /// </summary>
    public UpdateType UpdateType { get; set; }
}

/// <summary>
/// 更新类型枚举
/// </summary>
public enum UpdateType
{
    Major,
    Minor,
    Patch,
    Hotfix
}

/// <summary>
/// 网络状态枚举
/// </summary>
public enum NetworkStatus
{
    Online,
    Offline,
    Limited,
    Unknown
}

/// <summary>
/// 模板验证结果
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证错误
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 验证警告
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 缺失文件
    /// </summary>
    public List<string> MissingFiles { get; set; } = new();

    /// <summary>
    /// 验证详情
    /// </summary>
    public List<ValidationDetail> Details { get; set; } = new();
}

/// <summary>
/// 验证详情
/// </summary>
public class ValidationDetail
{
    /// <summary>
    /// 检查项
    /// </summary>
    public string Item { get; set; } = string.Empty;

    /// <summary>
    /// 是否通过
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 详细说明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? Suggestion { get; set; }
}