using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

public class ContainerService : IContainerService
{
    private readonly ILogger<ContainerService> _logger;
    private readonly IPortConflictService _portConflictService;
    private readonly ISystemDetectionService _systemDetectionService;
    private readonly INetworkService _networkService;
    private readonly IContainerEngineFactory _containerEngineFactory;

    public ContainerService(
        ILogger<ContainerService> logger,
        IPortConflictService portConflictService,
        ISystemDetectionService systemDetectionService,
        INetworkService networkService,
        IContainerEngineFactory containerEngineFactory)
    {
        _logger = logger;
        _portConflictService = portConflictService;
        _systemDetectionService = systemDetectionService;
        _networkService = networkService;
        _containerEngineFactory = containerEngineFactory;
    }

    public async Task<ContainerInfo?> GetContainerInfoAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Getting container info for: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                _logger.LogError("No container engine available");
                return null;
            }

            return await engine.GetContainerInfoAsync(containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container info for {ContainerName}", containerName);
            return null;
        }
    }

    public async Task<List<ContainerInfo>> GetProjectRelatedContainersAsync(string? projectPath = null, ProjectType? projectType = null)
    {
        try
        {
            _logger.LogDebug("Getting project-related containers for path: {ProjectPath}, type: {ProjectType}", 
                projectPath, projectType);

            var allContainers = await GetAllContainersAsync();
            
            if (string.IsNullOrEmpty(projectPath))
                projectPath = Directory.GetCurrentDirectory();

            if (!projectType.HasValue)
                projectType = DetectProjectType(projectPath);

            return await FilterContainersByProjectAsync(allContainers, projectPath, projectType.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project-related containers");
            return new List<ContainerInfo>();
        }
    }

    public async Task<ContainerStatusResult> DetectContainerStatusAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Detecting container status for: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new ContainerStatusResult
                {
                    Status = ContainerStatus.Error,
                    Message = "No container engine available"
                };
            }

            var isRunning = await engine.IsContainerRunningAsync(containerName);
            var containerInfo = await engine.GetContainerInfoAsync(containerName);
            
            var status = containerInfo?.Status ?? (isRunning ? ContainerStatus.Running : ContainerStatus.Unknown);
            var isHealthy = await CheckContainerIsHealthy(containerName, status);

            return new ContainerStatusResult
            {
                Status = status,
                Message = $"Container {containerName} is {status}",
                IsHealthy = isHealthy,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect container status for {ContainerName}", containerName);
            return new ContainerStatusResult
            {
                Status = ContainerStatus.Error,
                Message = $"Error detecting status: {ex.Message}"
            };
        }
    }

    public async Task<StartContainerResult> StartContainerAsync(string containerName, StartOptions? options = null)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Starting container: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new StartContainerResult
                {
                    Success = false,
                    Message = "No container engine available"
                };
            }

            // 检查容器状态并决定启动模式
            var statusResult = await DetectContainerStatusAsync(containerName);
            var startMode = DetermineStartMode(statusResult.Status);

            // 如果容器已经在运行，直接返回成功
            if (statusResult.Status == ContainerStatus.Running)
            {
                var existingContainer = await GetContainerInfoAsync(containerName);
                return new StartContainerResult
                {
                    Success = true,
                    Mode = StartMode.AttachedToRunning,
                    Message = "Container is already running",
                    Container = existingContainer,
                    StartupTime = DateTime.UtcNow - startTime
                };
            }

            // 检查端口冲突
            var container = await GetContainerInfoAsync(containerName);
            if (container != null)
            {
                _logger.LogDebug("检查容器 {ContainerName} 的端口冲突", containerName);
                var portConflictResult = await CheckAndResolvePortConflictsAsync(container);
                if (portConflictResult.HasConflict)
                {
                    _logger.LogWarning("容器 {ContainerName} 存在端口冲突: 端口 {Port}", containerName, portConflictResult.Port);
                    
                    // 获取详细的解决建议
                    var detailedConflictInfo = await _portConflictService.DetectPortConflictAsync(portConflictResult.Port);
                    var suggestions = await _portConflictService.GetResolutionSuggestionsAsync(detailedConflictInfo);
                    
                    var message = new StringBuilder();
                    message.AppendLine($"端口冲突检测到在端口 {portConflictResult.Port}");
                    message.AppendLine($"冲突服务: {string.Join(", ", portConflictResult.ConflictingServices)}");
                    
                    if (suggestions.Any())
                    {
                        message.AppendLine("建议解决方案:");
                        foreach (var suggestion in suggestions.Take(3)) // 只显示前3个建议
                        {
                            message.AppendLine($"  • {suggestion.Description}");
                            if (!string.IsNullOrEmpty(suggestion.Command))
                            {
                                message.AppendLine($"    命令: {suggestion.Command}");
                            }
                        }
                    }
                    
                    return new StartContainerResult
                    {
                        Success = false,
                        Mode = startMode,
                        Message = message.ToString().Trim()
                    };
                }
                else
                {
                    _logger.LogDebug("容器 {ContainerName} 端口检查通过，无冲突", containerName);
                }
            }

            // 执行启动命令
            return await engine.StartContainerAsync(containerName, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerName}", containerName);
            return new StartContainerResult
            {
                Success = false,
                Mode = StartMode.New,
                Message = $"Exception occurred while starting container: {ex.Message}"
            };
        }
    }

    public async Task<StopContainerResult> StopContainerAsync(string containerName, bool force = false)
    {
        try
        {
            _logger.LogInformation("Stopping container: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new StopContainerResult
                {
                    Success = false,
                    Message = "No container engine available"
                };
            }

            return await engine.StopContainerAsync(containerName, force);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerName}", containerName);
            return new StopContainerResult
            {
                Success = false,
                Message = $"Exception occurred while stopping container: {ex.Message}"
            };
        }
    }

    public async Task<RestartContainerResult> RestartContainerAsync(string containerName)
    {
        try
        {
            _logger.LogInformation("Restarting container: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new RestartContainerResult
                {
                    Success = false,
                    Message = "No container engine available"
                };
            }

            return await engine.RestartContainerAsync(containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart container {ContainerName}", containerName);
            return new RestartContainerResult
            {
                Success = false,
                Message = $"Exception occurred while restarting container: {ex.Message}"
            };
        }
    }

    public async Task<List<ContainerInfo>> GetRunningContainersAsync()
    {
        try
        {
            _logger.LogDebug("Getting running containers");
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                _logger.LogWarning("No container engine available");
                return new List<ContainerInfo>();
            }

            return await engine.GetRunningContainersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get running containers");
            return new List<ContainerInfo>();
        }
    }

    public async Task<List<ContainerInfo>> GetAllContainersAsync()
    {
        try
        {
            _logger.LogDebug("Getting all containers");
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                _logger.LogWarning("No container engine available");
                return new List<ContainerInfo>();
            }

            return await engine.GetAllContainersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all containers");
            return new List<ContainerInfo>();
        }
    }

    public async Task<bool> IsContainerRunningAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Checking if container is running: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                _logger.LogWarning("No container engine available");
                return false;
            }

            return await engine.IsContainerRunningAsync(containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if container is running: {ContainerName}", containerName);
            return false;
        }
    }

    public async Task<PortConflictResult> CheckAndResolvePortConflictsAsync(ContainerInfo container)
    {
        return await _portConflictService.CheckAndResolvePortConflictsAsync(container);
    }

    public async Task<ContainerLifecycleResult> ManageContainerLifecycleAsync(string containerName, ContainerOperation operation)
    {
        try
        {
            _logger.LogInformation("Managing container lifecycle: {ContainerName}, Operation: {Operation}", 
                containerName, operation);

            return operation switch
            {
                ContainerOperation.Start => new ContainerLifecycleResult
                {
                    Success = (await StartContainerAsync(containerName)).Success,
                    Operation = operation,
                    Message = "Start operation completed"
                },
                ContainerOperation.Stop => new ContainerLifecycleResult
                {
                    Success = (await StopContainerAsync(containerName)).Success,
                    Operation = operation,
                    Message = "Stop operation completed"
                },
                ContainerOperation.Restart => new ContainerLifecycleResult
                {
                    Success = (await RestartContainerAsync(containerName)).Success,
                    Operation = operation,
                    Message = "Restart operation completed"
                },
                ContainerOperation.Remove => await RemoveContainerAsync(containerName),
                _ => new ContainerLifecycleResult
                {
                    Success = false,
                    Operation = operation,
                    Message = $"Unsupported operation: {operation}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to manage container lifecycle: {ContainerName}, Operation: {Operation}", 
                containerName, operation);
            return new ContainerLifecycleResult
            {
                Success = false,
                Operation = operation,
                Message = $"Exception occurred: {ex.Message}"
            };
        }
    }

    public Task<List<ContainerInfo>> FilterContainersByProjectAsync(List<ContainerInfo> containers, string projectPath, ProjectType projectType)
    {
        var projectName = new DirectoryInfo(projectPath).Name.ToLowerInvariant();
        var filteredContainers = new List<ContainerInfo>();

        foreach (var container in containers)
        {
            // 检查容器名称是否包含项目名称
            if (container.Name.Contains(projectName, StringComparison.OrdinalIgnoreCase))
            {
                filteredContainers.Add(container);
                continue;
            }

            // 检查标签
            if (container.Labels.ContainsKey("deck.project") && 
                container.Labels["deck.project"].Equals(projectName, StringComparison.OrdinalIgnoreCase))
            {
                filteredContainers.Add(container);
                continue;
            }

            // 检查挂载点
            // 这里简化处理，实际可能需要更复杂的逻辑
        }

        return Task.FromResult(filteredContainers);
    }

    public async Task<ContainerHealthResult> CheckContainerHealthAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Checking container health: {ContainerName}", containerName);

            var statusResult = await DetectContainerStatusAsync(containerName);
            
            return new ContainerHealthResult
            {
                IsHealthy = statusResult.IsHealthy,
                Status = statusResult.Status.ToString(),
                ContainerStatus = statusResult.Status,
                Message = statusResult.Message,
                LastChecked = statusResult.LastChecked
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check container health: {ContainerName}", containerName);
            return new ContainerHealthResult
            {
                IsHealthy = false,
                Status = ContainerStatus.Error.ToString(),
                ContainerStatus = ContainerStatus.Error,
                Message = $"Health check failed: {ex.Message}",
                LastChecked = DateTime.UtcNow
            };
        }
    }

    public async Task<ContainerLogsResult> GetContainerLogsAsync(string containerName, int tailLines = 100)
    {
        try
        {
            _logger.LogDebug("Getting logs for container: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new ContainerLogsResult
                {
                    Success = false,
                    Error = "No container engine available"
                };
            }

            return await engine.GetContainerLogsAsync(containerName, tailLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerName}", containerName);
            return new ContainerLogsResult
            {
                Success = false,
                Error = $"Exception occurred while getting logs: {ex.Message}"
            };
        }
    }

    public async Task<ShellExecutionResult> ExecuteInContainerAsync(string containerName, string command, ShellOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Executing command in container: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new ShellExecutionResult
                {
                    Success = false,
                    Error = "No container engine available"
                };
            }

            return await engine.ExecuteInContainerAsync(containerName, command, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in container {ContainerName}", containerName);
            return new ShellExecutionResult
            {
                Success = false,
                Error = $"Exception occurred while executing command: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    #region Private Methods

    private async Task<ContainerLifecycleResult> RemoveContainerAsync(string containerName)
    {
        try
        {
            // 先停止容器
            var stopResult = await StopContainerAsync(containerName, true);
            if (!stopResult.Success)
            {
                return new ContainerLifecycleResult
                {
                    Success = false,
                    Operation = ContainerOperation.Remove,
                    Message = $"Failed to stop container before removal: {stopResult.Message}"
                };
            }

            // 然后删除容器
            _logger.LogInformation("Removing container: {ContainerName}", containerName);
            
            var engine = await _containerEngineFactory.GetPreferredEngineAsync();
            if (engine == null)
            {
                return new ContainerLifecycleResult
                {
                    Success = false,
                    Operation = ContainerOperation.Remove,
                    Message = "No container engine available"
                };
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = engine.Type == ContainerEngineType.Podman ? "podman" : "docker",
                    Arguments = $"rm {containerName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new ContainerLifecycleResult
            {
                Success = process.ExitCode == 0,
                Operation = ContainerOperation.Remove,
                Message = process.ExitCode == 0 
                    ? "Container removed successfully" 
                    : $"Failed to remove container: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerName}", containerName);
            return new ContainerLifecycleResult
            {
                Success = false,
                Operation = ContainerOperation.Remove,
                Message = $"Exception occurred while removing container: {ex.Message}"
            };
        }
    }

    private ProjectType DetectProjectType(string projectPath)
    {
        // 简化的项目类型检测逻辑
        if (File.Exists(Path.Combine(projectPath, "package.json")))
            return ProjectType.Node;
        
        if (File.Exists(Path.Combine(projectPath, "Cargo.toml")))
            return ProjectType.Rust;
        
        if (File.Exists(Path.Combine(projectPath, "pubspec.yaml")))
            return ProjectType.Flutter;
        
        if (Directory.GetFiles(projectPath, "*.csproj").Length > 0)
            return ProjectType.DotNet;
        
        if (Directory.GetFiles(projectPath, "*.py").Length > 0)
            return ProjectType.Python;

        return ProjectType.Unknown;
    }

    private StartMode DetermineStartMode(ContainerStatus status)
    {
        return status switch
        {
            ContainerStatus.Created => StartMode.New,
            ContainerStatus.Exited => StartMode.Resume,
            ContainerStatus.Dead => StartMode.New,
            _ => StartMode.New
        };
    }

    private Task<bool> CheckContainerIsHealthy(string containerName, ContainerStatus status)
    {
        // 简化的健康检查逻辑
        return Task.FromResult(status == ContainerStatus.Running);
    }

    private string BuildStartCommand(string containerName, StartOptions? options)
    {
        var command = $"start {containerName}";
        
        if (options?.Attach == true)
            command += " -a";
            
        return command;
    }

    private ContainerInfo ParseContainerInfo(string jsonOutput, string containerName)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonOutput);
            var root = doc.RootElement;

            if (root.GetArrayLength() == 0)
                throw new InvalidOperationException("No container data found");

            var containerElement = root[0]; // 取第一个容器

            return new ContainerInfo
            {
                Id = containerElement.GetProperty("Id").GetString() ?? string.Empty,
                Name = containerElement.GetProperty("Name").GetString() ?? containerName,
                Image = containerElement.GetProperty("Image").GetString() ?? string.Empty,
                Status = ParseContainerStatus(containerElement.GetProperty("State").GetProperty("Status").GetString() ?? "unknown"),
                Created = containerElement.GetProperty("Created").GetDateTime(),
                Ports = ParsePorts(containerElement),
                Labels = ParseLabels(containerElement)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse container info from JSON: {JsonOutput}", jsonOutput);
            throw;
        }
    }

    private ContainerStatus ParseContainerStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "running" => ContainerStatus.Running,
            "exited" => ContainerStatus.Exited,
            "created" => ContainerStatus.Created,
            "paused" => ContainerStatus.Paused,
            "restarting" => ContainerStatus.Restarting,
            "removing" => ContainerStatus.Removing,
            "dead" => ContainerStatus.Dead,
            _ => ContainerStatus.Unknown
        };
    }

    private Dictionary<string, string> ParseLabels(JsonElement containerElement)
    {
        var labels = new Dictionary<string, string>();

        try
        {
            if (containerElement.TryGetProperty("Config", out var configElement) &&
                configElement.TryGetProperty("Labels", out var labelsElement))
            {
                foreach (var property in labelsElement.EnumerateObject())
                {
                    labels[property.Name] = property.Value.GetString() ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse container labels");
        }

        return labels;
    }

    private List<PortMapping> ParsePorts(JsonElement containerElement)
    {
        var ports = new List<PortMapping>();

        try
        {
            if (containerElement.TryGetProperty("NetworkSettings", out var networkSettingsElement) &&
                networkSettingsElement.TryGetProperty("Ports", out var portsElement))
            {
                foreach (var property in portsElement.EnumerateObject())
                {
                    var portParts = property.Name.Split('/');
                    if (portParts.Length >= 2)
                    {
                        var portMapping = new PortMapping
                        {
                            ContainerPort = int.Parse(portParts[0]),
                            Protocol = ParseProtocolType(portParts[1]),
                            HostPorts = new List<int>()
                        };

                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var hostPortElement in property.Value.EnumerateArray())
                            {
                                if (hostPortElement.TryGetProperty("HostPort", out var hostPortProperty) &&
                                    int.TryParse(hostPortProperty.GetString(), out var hostPort))
                                {
                                    portMapping.HostPorts.Add(hostPort);
                                }
                            }
                        }

                        ports.Add(portMapping);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse container ports");
        }

        return ports;
    }

    private DeckProtocolType ParseProtocolType(string protocolString)
    {
        return protocolString.ToLowerInvariant() switch
        {
            "tcp" => DeckProtocolType.TCP,
            "udp" => DeckProtocolType.UDP,
            "sctp" => DeckProtocolType.SCTP,
            _ => DeckProtocolType.TCP
        };
    }

    #endregion
}