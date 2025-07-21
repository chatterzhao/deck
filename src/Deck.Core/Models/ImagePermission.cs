namespace Deck.Core.Models;

/// <summary>
/// 文件操作类型枚举
/// </summary>
public enum FileOperation
{
    /// <summary>
    /// 读取文件
    /// </summary>
    Read,

    /// <summary>
    /// 写入/修改文件
    /// </summary>
    Write,

    /// <summary>
    /// 删除文件
    /// </summary>
    Delete,

    /// <summary>
    /// 创建文件
    /// </summary>
    Create,

    /// <summary>
    /// 移动/重命名文件
    /// </summary>
    Move
}

/// <summary>
/// 目录操作类型枚举
/// </summary>
public enum DirectoryOperation
{
    /// <summary>
    /// 读取目录内容
    /// </summary>
    Read,

    /// <summary>
    /// 在目录中创建文件/子目录
    /// </summary>
    Create,

    /// <summary>
    /// 删除目录
    /// </summary>
    Delete,

    /// <summary>
    /// 重命名/移动目录
    /// </summary>
    Rename,

    /// <summary>
    /// 修改目录权限
    /// </summary>
    ModifyPermissions
}

/// <summary>
/// 权限级别枚举
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// 允许
    /// </summary>
    Allowed,

    /// <summary>
    /// 警告但允许
    /// </summary>
    Warning,

    /// <summary>
    /// 禁止
    /// </summary>
    Denied
}

/// <summary>
/// 文件权限验证结果
/// </summary>
public class FilePermissionResult
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    public FileOperation Operation { get; set; }

    /// <summary>
    /// 权限级别
    /// </summary>
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// 是否允许操作
    /// </summary>
    public bool IsAllowed => Permission == PermissionLevel.Allowed || Permission == PermissionLevel.Warning;

    /// <summary>
    /// 权限说明
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 修复建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// 替代方案
    /// </summary>
    public List<string> Alternatives { get; set; } = new();

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 目录权限验证结果
/// </summary>
public class DirectoryPermissionResult
{
    /// <summary>
    /// 目录路径
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    public DirectoryOperation Operation { get; set; }

    /// <summary>
    /// 权限级别
    /// </summary>
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// 是否允许操作
    /// </summary>
    public bool IsAllowed => Permission == PermissionLevel.Allowed || Permission == PermissionLevel.Warning;

    /// <summary>
    /// 权限说明
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 修复建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// 替代方案
    /// </summary>
    public List<string> Alternatives { get; set; } = new();

    /// <summary>
    /// 影响评估
    /// </summary>
    public DirectoryOperationImpact? Impact { get; set; }

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 环境变量权限验证结果
/// </summary>
public class EnvPermissionResult
{
    /// <summary>
    /// 验证结果（基于是否有被拒绝的变更）
    /// </summary>
    public bool IsValid => DeniedChanges.Count == 0;

    /// <summary>
    /// 允许修改的变量
    /// </summary>
    public Dictionary<string, string> AllowedChanges { get; set; } = new();

    /// <summary>
    /// 被拒绝的变量
    /// </summary>
    public Dictionary<string, string> DeniedChanges { get; set; } = new();

    /// <summary>
    /// 警告的变量（允许但需要注意）
    /// </summary>
    public Dictionary<string, string> WarningChanges { get; set; } = new();

    /// <summary>
    /// 验证详情
    /// </summary>
    public List<EnvVariableValidation> ValidationDetails { get; set; } = new();

    /// <summary>
    /// 总体说明
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 环境变量验证详情
/// </summary>
public class EnvVariableValidation
{
    /// <summary>
    /// 变量名
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// 原值
    /// </summary>
    public string? OriginalValue { get; set; }

    /// <summary>
    /// 新值
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// 权限级别
    /// </summary>
    public PermissionLevel Permission { get; set; }

    /// <summary>
    /// 变量类型
    /// </summary>
    public EnvVariableType VariableType { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 建议
    /// </summary>
    public string? Suggestion { get; set; }
}

/// <summary>
/// 环境变量类型枚举
/// </summary>
public enum EnvVariableType
{
    /// <summary>
    /// 运行时变量（可在镜像中修改）
    /// </summary>
    Runtime,

    /// <summary>
    /// 构建时变量（只能在 Custom/ 中修改）
    /// </summary>
    BuildTime,

    /// <summary>
    /// 系统变量（受保护）
    /// </summary>
    System,

    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown
}

/// <summary>
/// 镜像权限概况
/// </summary>
public class ImagePermissionSummary
{
    /// <summary>
    /// 镜像目录路径
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// 镜像名称
    /// </summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>
    /// 是否为有效的镜像目录
    /// </summary>
    public bool IsValidImageDirectory { get; set; }

    /// <summary>
    /// 受保护的文件列表
    /// </summary>
    public List<ProtectedFile> ProtectedFiles { get; set; } = new();

    /// <summary>
    /// 可修改的文件列表
    /// </summary>
    public List<ModifiableFile> ModifiableFiles { get; set; } = new();

    /// <summary>
    /// 运行时环境变量
    /// </summary>
    public List<string> RuntimeVariables { get; set; } = new();

    /// <summary>
    /// 构建时环境变量
    /// </summary>
    public List<string> BuildTimeVariables { get; set; } = new();

    /// <summary>
    /// 权限策略说明
    /// </summary>
    public string PolicyDescription { get; set; } = string.Empty;

    /// <summary>
    /// 最佳实践建议
    /// </summary>
    public List<string> BestPractices { get; set; } = new();

    /// <summary>
    /// 扫描时间
    /// </summary>
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 受保护的文件信息
/// </summary>
public class ProtectedFile
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public ProtectedFileType FileType { get; set; }

    /// <summary>
    /// 保护原因
    /// </summary>
    public string ProtectionReason { get; set; } = string.Empty;

    /// <summary>
    /// 修改此文件的替代方案
    /// </summary>
    public List<string> Alternatives { get; set; } = new();
}

/// <summary>
/// 可修改的文件信息
/// </summary>
public class ModifiableFile
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 允许的操作类型
    /// </summary>
    public List<FileOperation> AllowedOperations { get; set; } = new();

    /// <summary>
    /// 修改建议
    /// </summary>
    public List<string> ModificationGuidelines { get; set; } = new();
}

/// <summary>
/// 受保护文件类型枚举
/// </summary>
public enum ProtectedFileType
{
    /// <summary>
    /// Docker Compose 配置
    /// </summary>
    ComposeConfig,

    /// <summary>
    /// Dockerfile
    /// </summary>
    Dockerfile,

    /// <summary>
    /// 构建脚本
    /// </summary>
    BuildScript,

    /// <summary>
    /// 元数据文件
    /// </summary>
    Metadata,

    /// <summary>
    /// 系统配置
    /// </summary>
    SystemConfig
}

/// <summary>
/// 目录名称验证结果
/// </summary>
public class DirectoryNameValidationResult
{
    /// <summary>
    /// 目录名称
    /// </summary>
    public string DirectoryName { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// 建议的修正名称
    /// </summary>
    public string? SuggestedName { get; set; }

    /// <summary>
    /// 名称格式说明
    /// </summary>
    public string FormatDescription { get; set; } = string.Empty;

    /// <summary>
    /// 解析出的信息（如果有效）
    /// </summary>
    public DirectoryNameInfo? ParsedInfo { get; set; }
}

/// <summary>
/// 目录名称信息
/// </summary>
public class DirectoryNameInfo
{
    /// <summary>
    /// 前缀
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 时间戳字符串
    /// </summary>
    public string TimeStamp { get; set; } = string.Empty;

    /// <summary>
    /// 是否符合标准格式
    /// </summary>
    public bool IsStandardFormat { get; set; }
}

/// <summary>
/// 目录操作影响评估
/// </summary>
public class DirectoryOperationImpact
{
    /// <summary>
    /// 影响级别
    /// </summary>
    public ImpactLevel Level { get; set; }

    /// <summary>
    /// 影响描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 受影响的组件
    /// </summary>
    public List<string> AffectedComponents { get; set; } = new();

    /// <summary>
    /// 风险评估
    /// </summary>
    public List<string> Risks { get; set; } = new();

    /// <summary>
    /// 回滚建议
    /// </summary>
    public List<string> RollbackSuggestions { get; set; } = new();
}

/// <summary>
/// 影响级别枚举
/// </summary>
public enum ImpactLevel
{
    /// <summary>
    /// 无影响
    /// </summary>
    None,

    /// <summary>
    /// 低影响
    /// </summary>
    Low,

    /// <summary>
    /// 中等影响
    /// </summary>
    Medium,

    /// <summary>
    /// 高影响
    /// </summary>
    High,

    /// <summary>
    /// 严重影响
    /// </summary>
    Critical
}

/// <summary>
/// 权限违规信息
/// </summary>
public class PermissionViolation
{
    /// <summary>
    /// 违规类型
    /// </summary>
    public ViolationType Type { get; set; }

    /// <summary>
    /// 涉及的路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 尝试的操作
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// 违规描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    public ViolationSeverity Severity { get; set; }

    /// <summary>
    /// 违规时间
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 违规类型枚举
/// </summary>
public enum ViolationType
{
    /// <summary>
    /// 修改受保护文件
    /// </summary>
    ProtectedFileModification,

    /// <summary>
    /// 目录重命名
    /// </summary>
    DirectoryRename,

    /// <summary>
    /// 构建时变量修改
    /// </summary>
    BuildTimeVariableModification,

    /// <summary>
    /// 删除核心文件
    /// </summary>
    CoreFileDeletion,

    /// <summary>
    /// 无效目录名称
    /// </summary>
    InvalidDirectoryName
}

/// <summary>
/// 违规严重程度枚举
/// </summary>
public enum ViolationSeverity
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
/// 权限指导信息
/// </summary>
public class PermissionGuidance
{
    /// <summary>
    /// 违规信息
    /// </summary>
    public PermissionViolation Violation { get; set; } = new();

    /// <summary>
    /// 详细说明
    /// </summary>
    public string DetailedExplanation { get; set; } = string.Empty;

    /// <summary>
    /// 为什么这样设计
    /// </summary>
    public string DesignRationale { get; set; } = string.Empty;

    /// <summary>
    /// 修复步骤
    /// </summary>
    public List<string> FixSteps { get; set; } = new();

    /// <summary>
    /// 替代方案
    /// </summary>
    public List<string> Alternatives { get; set; } = new();

    /// <summary>
    /// 相关文档链接
    /// </summary>
    public List<string> DocumentationLinks { get; set; } = new();

    /// <summary>
    /// 示例代码或命令
    /// </summary>
    public List<string> Examples { get; set; } = new();
}