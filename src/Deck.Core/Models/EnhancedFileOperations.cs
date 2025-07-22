namespace Deck.Core.Models;

/// <summary>
/// 标准端口处理结果
/// </summary>
public class StandardPortsResult
{
    public bool IsSuccess { get; set; }
    public Dictionary<string, int> ModifiedPorts { get; set; } = new();
    public Dictionary<string, int> AllPorts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// PROJECT_NAME更新结果
/// </summary>
public class ProjectNameUpdateResult
{
    public bool IsSuccess { get; set; }
    public string UpdatedProjectName { get; set; } = string.Empty;
    public string? PreviousProjectName { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 文件复制结果
/// </summary>
public class FileCopyResult
{
    public bool IsSuccess { get; set; }
    public List<string> CopiedFiles { get; set; } = new();
    public List<string> SkippedFiles { get; set; } = new();
    public List<string> FailedFiles { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 配置备份结果
/// </summary>
public class ConfigBackupResult
{
    public bool IsSuccess { get; set; }
    public string? BackupFilePath { get; set; }
    public DateTime BackupTimestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 配置恢复结果
/// </summary>
public class ConfigRestoreResult
{
    public bool IsSuccess { get; set; }
    public string? RestoredFilePath { get; set; }
    public DateTime RestoreTimestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 环境文件验证结果
/// </summary>
public class EnvFileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, string> ParsedVariables { get; set; } = new();
}

/// <summary>
/// 标准端口配置
/// </summary>
public static class StandardPorts
{
    public static readonly Dictionary<string, int> DefaultPorts = new()
    {
        { "DEV_PORT", 5000 },
        { "DEBUG_PORT", 9229 },
        { "WEB_PORT", 8080 },
        { "HTTPS_PORT", 8443 },
        { "ANDROID_DEBUG_PORT", 5037 }
    };

    public static readonly HashSet<string> RequiredPortVariables = new()
    {
        "DEV_PORT",
        "DEBUG_PORT", 
        "WEB_PORT",
        "HTTPS_PORT",
        "ANDROID_DEBUG_PORT"
    };
}

/// <summary>
/// 文件操作类型
/// </summary>
public enum FileOperationType
{
    Create,
    Update,
    Delete,
    Copy,
    Move,
    Backup,
    Restore
}

/// <summary>
/// 文件操作记录
/// </summary>
public class FileOperationRecord
{
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public FileOperationType Type { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string? TargetPath { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BackupPath { get; set; }
}

/// <summary>
/// 增强文件操作选项
/// </summary>
public class EnhancedFileOperationOptions
{
    /// <summary>
    /// 是否在操作前自动创建备份
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// 备份文件保留天数
    /// </summary>
    public int BackupRetentionDays { get; set; } = 30;

    /// <summary>
    /// 是否验证文件格式
    /// </summary>
    public bool ValidateFormat { get; set; } = true;

    /// <summary>
    /// 端口分配范围起始
    /// </summary>
    public int PortRangeStart { get; set; } = 5000;

    /// <summary>
    /// 端口分配范围结束
    /// </summary>
    public int PortRangeEnd { get; set; } = 9999;

    /// <summary>
    /// 是否包含隐藏文件复制
    /// </summary>
    public bool IncludeHiddenFiles { get; set; } = true;

    /// <summary>
    /// 覆盖现有文件
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;
}