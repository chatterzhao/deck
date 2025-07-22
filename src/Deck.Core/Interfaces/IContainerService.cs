using Deck.Core.Models;

namespace Deck.Core.Interfaces;

public interface IContainerService
{
    Task<ContainerInfo?> GetContainerInfoAsync(string containerName);
    
    Task<List<ContainerInfo>> GetProjectRelatedContainersAsync(string? projectPath = null, ProjectType? projectType = null);
    
    Task<ContainerStatusResult> DetectContainerStatusAsync(string containerName);
    
    Task<StartContainerResult> StartContainerAsync(string containerName, StartOptions? options = null);
    
    Task<StopContainerResult> StopContainerAsync(string containerName, bool force = false);
    
    Task<RestartContainerResult> RestartContainerAsync(string containerName);
    
    Task<List<ContainerInfo>> GetRunningContainersAsync();
    
    Task<List<ContainerInfo>> GetAllContainersAsync();
    
    Task<bool> IsContainerRunningAsync(string containerName);
    
    Task<PortConflictResult> CheckAndResolvePortConflictsAsync(ContainerInfo container);
    
    Task<ContainerLifecycleResult> ManageContainerLifecycleAsync(string containerName, ContainerOperation operation);
    
    Task<List<ContainerInfo>> FilterContainersByProjectAsync(List<ContainerInfo> containers, string projectPath, ProjectType projectType);
    
    Task<ContainerHealthResult> CheckContainerHealthAsync(string containerName);
    
    Task<ContainerLogsResult> GetContainerLogsAsync(string containerName, int tailLines = 100);
    
    Task<ShellExecutionResult> ExecuteInContainerAsync(string containerName, string command, ShellOptions? options = null);
}

public class ContainerStatusResult
{
    public ContainerStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

public class StartContainerResult
{
    public bool Success { get; set; }
    public StartMode Mode { get; set; }
    public string Message { get; set; } = string.Empty;
    public ContainerInfo? Container { get; set; }
    public List<int> AllocatedPorts { get; set; } = new();
    public TimeSpan StartupTime { get; set; }
}

public class StopContainerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan StopTime { get; set; }
    public bool WasForced { get; set; }
}

public class RestartContainerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StartMode RestartMode { get; set; }
    public TimeSpan RestartTime { get; set; }
}

public class ContainerLifecycleResult
{
    public bool Success { get; set; }
    public ContainerOperation Operation { get; set; }
    public string Message { get; set; } = string.Empty;
    public ContainerInfo? Container { get; set; }
    public TimeSpan OperationTime { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class ContainerHealthResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> HealthDetails { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

public class ContainerLogsResult
{
    public bool Success { get; set; }
    public List<string> LogLines { get; set; } = new();
    public string Error { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class ShellExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

public enum ContainerOperation
{
    Start,
    Stop,
    Restart,
    Remove,
    Pause,
    Unpause,
    Kill
}