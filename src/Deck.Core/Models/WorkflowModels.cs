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