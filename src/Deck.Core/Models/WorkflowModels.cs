namespace Deck.Core.Models;

/// <summary>
/// 工作流程执行结果
/// </summary>
public class WorkflowExecutionResult
{
    public required WorkflowType WorkflowType { get; set; }
    public bool IsComplete { get; set; }
    public bool Success { get; set; }
    public string? CustomConfigName { get; set; }
    public string? ImageName { get; set; }
    public string? ContainerName { get; set; }
    public ContainerAction? Action { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// 工作流程类型
/// </summary>
public enum WorkflowType
{
    CreateEditableConfig,
    DirectBuildAndStart
}

/// <summary>
/// 容器操作类型
/// </summary>
public enum ContainerAction
{
    Enter,
    Restart, 
    CreateAndStart,
    BuildAndStart
}

/// <summary>
/// 配置状态验证结果
/// </summary>
public class ConfigurationStateResult
{
    public bool IsValid { get; set; }
    public ConfigValidationStatus Status { get; set; }
    public List<string> MissingFiles { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 配置验证状态类型
/// </summary>
public enum ConfigValidationStatus
{
    Complete,
    Incomplete,
    Invalid,
    NotFound
}

/// <summary>
/// Custom配置工作流程结果
/// </summary>
public class CustomWorkflowResult
{
    public bool Success { get; set; }
    public string? ImageName { get; set; }
    public string? ContainerName { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Images工作流程结果
/// </summary>
public class ImagesWorkflowResult
{
    public bool Success { get; set; }
    public ContainerAction Action { get; set; }
    public string? ContainerName { get; set; }
    public string? ImageName { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// 全局异常处理结果
/// </summary>
public class GlobalExceptionResult
{
    /// <summary>
    /// 是否处理成功
    /// </summary>
    public bool IsHandled { get; set; }
    
    /// <summary>
    /// 异常类型
    /// </summary>
    public ExceptionType ExceptionType { get; set; }
    
    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public string UserMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 详细的技术错误信息
    /// </summary>
    public string TechnicalDetails { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否可以自动恢复
    /// </summary>
    public bool CanAutoRecover { get; set; }
    
    /// <summary>
    /// 自动恢复结果
    /// </summary>
    public RecoveryResult? RecoveryResult { get; set; }
    
    /// <summary>
    /// 建议的解决方案
    /// </summary>
    public List<string> SuggestedSolutions { get; set; } = new();
}

/// <summary>
/// 异常类型枚举
/// </summary>
public enum ExceptionType
{
    /// <summary>
    /// 未知异常
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 文件系统异常
    /// </summary>
    FileSystem,
    
    /// <summary>
    /// 网络异常
    /// </summary>
    Network,
    
    /// <summary>
    /// 权限异常
    /// </summary>
    Permission,
    
    /// <summary>
    /// 容器引擎异常
    /// </summary>
    ContainerEngine,
    
    /// <summary>
    /// 配置异常
    /// </summary>
    Configuration,
    
    /// <summary>
    /// 端口冲突异常
    /// </summary>
    PortConflict
}

/// <summary>
/// 自动恢复结果
/// </summary>
public class RecoveryResult
{
    /// <summary>
    /// 恢复是否成功
    /// </summary>
    public bool IsRecovered { get; set; }
    
    /// <summary>
    /// 恢复操作描述
    /// </summary>
    public string ActionDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 恢复过程中产生的警告
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// 恢复过程中产生的错误
    /// </summary>
    public List<string> Errors { get; set; } = new();
}