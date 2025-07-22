using System.Diagnostics;
using System.Runtime.InteropServices;
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

    public ContainerService(
        ILogger<ContainerService> logger,
        IPortConflictService portConflictService,
        ISystemDetectionService systemDetectionService,
        INetworkService networkService)
    {
        _logger = logger;
        _portConflictService = portConflictService;
        _systemDetectionService = systemDetectionService;
        _networkService = networkService;
    }

    public async Task<ContainerInfo?> GetContainerInfoAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Getting container info for: {ContainerName}", containerName);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"inspect {containerName} --format json",
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

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Container {ContainerName} not found: {Error}", containerName, error);
                return null;
            }

            return ParseContainerInfo(output, containerName);
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
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"ps -a --filter name={containerName} --format \"{{{{.Status}}}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var status = ParseContainerStatus(output.Trim());
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
                var portConflictResult = await CheckAndResolvePortConflictsAsync(container);
                if (!portConflictResult.CanProceed)
                {
                    return new StartContainerResult
                    {
                        Success = false,
                        Mode = startMode,
                        Message = $"Port conflicts detected: {string.Join(", ", portConflictResult.Conflicts.Select(c => c.Port))}"
                    };
                }
            }

            // 执行启动命令
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = BuildStartCommand(containerName, options),
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

            var success = process.ExitCode == 0;
            var updatedContainer = success ? await GetContainerInfoAsync(containerName) : null;

            return new StartContainerResult
            {
                Success = success,
                Mode = startMode,
                Message = success ? "Container started successfully" : error,
                Container = updatedContainer,
                AllocatedPorts = ExtractAllocatedPorts(updatedContainer),
                StartupTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerName}", containerName);
            return new StartContainerResult
            {
                Success = false,
                Message = $"Error starting container: {ex.Message}",
                StartupTime = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<StopContainerResult> StopContainerAsync(string containerName, bool force = false)
    {
        var stopTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Stopping container: {ContainerName}, Force: {Force}", containerName, force);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = force ? $"kill {containerName}" : $"stop {containerName}",
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

            var success = process.ExitCode == 0;

            return new StopContainerResult
            {
                Success = success,
                Message = success ? "Container stopped successfully" : error,
                StopTime = DateTime.UtcNow - stopTime,
                WasForced = force
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerName}", containerName);
            return new StopContainerResult
            {
                Success = false,
                Message = $"Error stopping container: {ex.Message}",
                StopTime = DateTime.UtcNow - stopTime
            };
        }
    }

    public async Task<RestartContainerResult> RestartContainerAsync(string containerName)
    {
        var restartTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Restarting container: {ContainerName}", containerName);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"restart {containerName}",
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

            var success = process.ExitCode == 0;

            return new RestartContainerResult
            {
                Success = success,
                Message = success ? "Container restarted successfully" : error,
                RestartMode = success ? StartMode.StartedExisting : StartMode.StartedExisting,
                RestartTime = DateTime.UtcNow - restartTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart container {ContainerName}", containerName);
            return new RestartContainerResult
            {
                Success = false,
                Message = $"Error restarting container: {ex.Message}",
                RestartTime = DateTime.UtcNow - restartTime
            };
        }
    }

    public async Task<List<ContainerInfo>> GetRunningContainersAsync()
    {
        return await GetContainersByStatusAsync(ContainerStatus.Running);
    }

    public async Task<List<ContainerInfo>> GetAllContainersAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = "ps -a --format \"{{.Names}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return new List<ContainerInfo>();

            var containerNames = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var containers = new List<ContainerInfo>();

            foreach (var name in containerNames)
            {
                var containerInfo = await GetContainerInfoAsync(name.Trim());
                if (containerInfo != null)
                    containers.Add(containerInfo);
            }

            return containers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all containers");
            return new List<ContainerInfo>();
        }
    }

    public async Task<bool> IsContainerRunningAsync(string containerName)
    {
        var statusResult = await DetectContainerStatusAsync(containerName);
        return statusResult.Status == ContainerStatus.Running;
    }

    public async Task<PortConflictResult> CheckAndResolvePortConflictsAsync(ContainerInfo container)
    {
        try
        {
            var conflicts = new List<PortConflict>();
            
            foreach (var portMapping in container.PortMappings)
            {
                var conflict = await _portConflictService.CheckPortConflictAsync(portMapping.HostPort);
                if (conflict != null)
                {
                    conflicts.Add(conflict);
                }
            }

            return new PortConflictResult
            {
                HasConflicts = conflicts.Any(),
                Conflicts = conflicts,
                CanProceed = !conflicts.Any() || conflicts.All(c => c.CanResolve),
                Suggestions = GeneratePortResolutionSuggestions(conflicts)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check port conflicts for container {ContainerName}", container.Name);
            return new PortConflictResult
            {
                HasConflicts = false,
                CanProceed = true
            };
        }
    }

    public async Task<ContainerLifecycleResult> ManageContainerLifecycleAsync(string containerName, ContainerOperation operation)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var result = operation switch
            {
                ContainerOperation.Start => await ConvertStartResult(await StartContainerAsync(containerName)),
                ContainerOperation.Stop => await ConvertStopResult(await StopContainerAsync(containerName)),
                ContainerOperation.Restart => await ConvertRestartResult(await RestartContainerAsync(containerName)),
                ContainerOperation.Remove => await RemoveContainerAsync(containerName),
                ContainerOperation.Pause => await PauseContainerAsync(containerName),
                ContainerOperation.Unpause => await UnpauseContainerAsync(containerName),
                ContainerOperation.Kill => await StopContainerAsync(containerName, force: true).ContinueWith(t => ConvertStopResult(t.Result)),
                _ => throw new ArgumentException($"Unsupported operation: {operation}")
            };

            result.OperationTime = DateTime.UtcNow - startTime;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute {Operation} on container {ContainerName}", operation, containerName);
            return new ContainerLifecycleResult
            {
                Success = false,
                Operation = operation,
                Message = $"Error executing {operation}: {ex.Message}",
                OperationTime = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<List<ContainerInfo>> FilterContainersByProjectAsync(List<ContainerInfo> containers, string projectPath, ProjectType projectType)
    {
        try
        {
            var filtered = new List<ContainerInfo>();
            var projectName = Path.GetFileName(projectPath);
            var expectedPrefix = GetProjectPrefix(projectType);

            foreach (var container in containers)
            {
                // 基于命名规范过滤
                if (IsProjectRelatedContainer(container, projectName, expectedPrefix, projectType))
                {
                    filtered.Add(container);
                }
            }

            // 按创建时间排序，最新的在前面
            return filtered.OrderByDescending(c => c.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to filter containers by project");
            return containers;
        }
    }

    public async Task<ContainerHealthResult> CheckContainerHealthAsync(string containerName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"healthcheck run {containerName}",
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

            var isHealthy = process.ExitCode == 0;
            var issues = new List<string>();

            if (!isHealthy && !string.IsNullOrEmpty(error))
            {
                issues.Add(error);
            }

            return new ContainerHealthResult
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "healthy" : "unhealthy",
                Issues = issues,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check container health for {ContainerName}", containerName);
            return new ContainerHealthResult
            {
                IsHealthy = false,
                Status = "unknown",
                Issues = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ContainerLogsResult> GetContainerLogsAsync(string containerName, int tailLines = 100)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"logs --tail {tailLines} {containerName}",
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

            var success = process.ExitCode == 0;
            var logLines = success ? output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();

            return new ContainerLogsResult
            {
                Success = success,
                LogLines = logLines,
                Error = success ? string.Empty : error,
                TotalLines = logLines.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container logs for {ContainerName}", containerName);
            return new ContainerLogsResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ShellExecutionResult> ExecuteInContainerAsync(string containerName, string command, ShellOptions? options = null)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var shellType = options?.ShellType ?? "/bin/bash";
            var workingDir = options?.WorkingDirectory ?? "/";
            var user = options?.User ?? "root";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"exec -it -w {workingDir} -u {user} {containerName} {shellType} -c \"{command}\"",
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

            return new ShellExecutionResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error,
                ExitCode = process.ExitCode,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in container {ContainerName}", containerName);
            return new ShellExecutionResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    #region Private Methods

    private ContainerInfo ParseContainerInfo(string json, string containerName)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(json);
            var containerArray = jsonDoc.RootElement;
            
            if (containerArray.GetArrayLength() == 0)
                return CreateEmptyContainerInfo(containerName);

            var container = containerArray[0];
            
            return new ContainerInfo
            {
                Id = container.GetProperty("Id").GetString() ?? string.Empty,
                Name = container.GetProperty("Name").GetString()?.TrimStart('/') ?? containerName,
                Status = ParseContainerStatusFromInspect(container),
                Image = container.GetProperty("Config").GetProperty("Image").GetString() ?? string.Empty,
                CreatedAt = DateTime.Parse(container.GetProperty("Created").GetString() ?? DateTime.UtcNow.ToString()),
                PortMappings = ParsePortMappings(container),
                EnvironmentVariables = ParseEnvironmentVariables(container),
                ProjectType = DetectProjectTypeFromContainer(container),
                ProjectRootDirectory = ExtractProjectRootDirectory(container)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse container info for {ContainerName}", containerName);
            return CreateEmptyContainerInfo(containerName);
        }
    }

    private ContainerInfo CreateEmptyContainerInfo(string containerName)
    {
        return new ContainerInfo
        {
            Name = containerName,
            Status = ContainerStatus.NotExists
        };
    }

    private ContainerStatus ParseContainerStatus(string statusOutput)
    {
        if (string.IsNullOrEmpty(statusOutput))
            return ContainerStatus.NotExists;

        var status = statusOutput.ToLowerInvariant();
        
        if (status.Contains("up") || status.Contains("running"))
            return ContainerStatus.Running;
        if (status.Contains("exited") || status.Contains("stopped"))
            return ContainerStatus.Stopped;
        if (status.Contains("paused"))
            return ContainerStatus.Paused;
        
        return ContainerStatus.Error;
    }

    private ContainerStatus ParseContainerStatusFromInspect(JsonElement container)
    {
        try
        {
            var state = container.GetProperty("State");
            var status = state.GetProperty("Status").GetString()?.ToLowerInvariant();
            
            return status switch
            {
                "running" => ContainerStatus.Running,
                "exited" => ContainerStatus.Stopped,
                "paused" => ContainerStatus.Paused,
                _ => ContainerStatus.Error
            };
        }
        catch
        {
            return ContainerStatus.Error;
        }
    }

    private async Task<bool> CheckContainerIsHealthy(string containerName, ContainerStatus status)
    {
        if (status != ContainerStatus.Running)
            return false;

        try
        {
            var healthResult = await CheckContainerHealthAsync(containerName);
            return healthResult.IsHealthy;
        }
        catch
        {
            return status == ContainerStatus.Running; // 如果无法检查健康状态，运行中就算健康
        }
    }

    private StartMode DetermineStartMode(ContainerStatus status)
    {
        return status switch
        {
            ContainerStatus.Running => StartMode.AttachedToRunning,
            ContainerStatus.Stopped => StartMode.StartedExisting,
            ContainerStatus.NotExists => StartMode.CreatedNew,
            _ => StartMode.StartedExisting
        };
    }

    private string BuildStartCommand(string containerName, StartOptions? options)
    {
        var args = new List<string> { "start" };
        
        if (options?.DetachedMode == true)
            args.Add("-d");

        args.Add(containerName);
        
        return string.Join(" ", args);
    }

    private List<int> ExtractAllocatedPorts(ContainerInfo? container)
    {
        if (container == null)
            return new List<int>();

        return container.PortMappings.Select(pm => pm.HostPort).ToList();
    }

    private async Task<List<ContainerInfo>> GetContainersByStatusAsync(ContainerStatus status)
    {
        var allContainers = await GetAllContainersAsync();
        return allContainers.Where(c => c.Status == status).ToList();
    }

    private List<PortMapping> ParsePortMappings(JsonElement container)
    {
        try
        {
            var portMappings = new List<PortMapping>();
            
            if (container.TryGetProperty("NetworkSettings", out var networkSettings) &&
                networkSettings.TryGetProperty("Ports", out var ports))
            {
                foreach (var port in ports.EnumerateObject())
                {
                    var containerPort = int.Parse(port.Name.Split('/')[0]);
                    var protocol = port.Name.Split('/').Length > 1 ? port.Name.Split('/')[1] : "tcp";
                    
                    if (port.Value.ValueKind == JsonValueKind.Array && port.Value.GetArrayLength() > 0)
                    {
                        var hostPortStr = port.Value[0].GetProperty("HostPort").GetString();
                        if (int.TryParse(hostPortStr, out var hostPort))
                        {
                            portMappings.Add(new PortMapping
                            {
                                ContainerPort = containerPort,
                                HostPort = hostPort,
                                Protocol = protocol
                            });
                        }
                    }
                }
            }
            
            return portMappings;
        }
        catch
        {
            return new List<PortMapping>();
        }
    }

    private Dictionary<string, string> ParseEnvironmentVariables(JsonElement container)
    {
        try
        {
            var envVars = new Dictionary<string, string>();
            
            if (container.TryGetProperty("Config", out var config) &&
                config.TryGetProperty("Env", out var env))
            {
                foreach (var envVar in env.EnumerateArray())
                {
                    var envString = envVar.GetString();
                    if (!string.IsNullOrEmpty(envString))
                    {
                        var parts = envString.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            envVars[parts[0]] = parts[1];
                        }
                    }
                }
            }
            
            return envVars;
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private ProjectType DetectProjectTypeFromContainer(JsonElement container)
    {
        try
        {
            var envVars = ParseEnvironmentVariables(container);
            
            if (envVars.ContainsKey("PROJECT_TYPE"))
            {
                if (Enum.TryParse<ProjectType>(envVars["PROJECT_TYPE"], true, out var projectType))
                    return projectType;
            }

            // 基于镜像名推断项目类型
            var image = container.GetProperty("Config").GetProperty("Image").GetString();
            if (!string.IsNullOrEmpty(image))
            {
                if (image.Contains("tauri", StringComparison.OrdinalIgnoreCase))
                    return ProjectType.Tauri;
                if (image.Contains("flutter", StringComparison.OrdinalIgnoreCase))
                    return ProjectType.Flutter;
                if (image.Contains("avalonia", StringComparison.OrdinalIgnoreCase))
                    return ProjectType.Avalonia;
            }

            return ProjectType.Unknown;
        }
        catch
        {
            return ProjectType.Unknown;
        }
    }

    private string ExtractProjectRootDirectory(JsonElement container)
    {
        try
        {
            var envVars = ParseEnvironmentVariables(container);
            return envVars.GetValueOrDefault("PROJECT_ROOT", string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

    private ProjectType DetectProjectType(string projectPath)
    {
        if (File.Exists(Path.Combine(projectPath, "Cargo.toml")))
            return ProjectType.Tauri;
        if (File.Exists(Path.Combine(projectPath, "pubspec.yaml")))
            return ProjectType.Flutter;
        if (Directory.GetFiles(projectPath, "*.csproj").Any())
            return ProjectType.Avalonia;
        if (File.Exists(Path.Combine(projectPath, "package.json")))
            return ProjectType.React;
        
        return ProjectType.Unknown;
    }

    private string GetProjectPrefix(ProjectType projectType)
    {
        return projectType.ToString().ToLowerInvariant();
    }

    private bool IsProjectRelatedContainer(ContainerInfo container, string projectName, string expectedPrefix, ProjectType projectType)
    {
        // 检查命名规范：{prefix}-{timestamp} 或直接包含项目名
        var containerName = container.Name.ToLowerInvariant();
        var projectNameLower = projectName.ToLowerInvariant();
        
        // 方式1：基于前缀匹配
        if (containerName.StartsWith(expectedPrefix + "-"))
            return true;
            
        // 方式2：包含项目名
        if (containerName.Contains(projectNameLower))
            return true;
            
        // 方式3：基于容器中的环境变量
        if (container.ProjectType == projectType)
            return true;

        // 方式4：基于项目根目录
        if (!string.IsNullOrEmpty(container.ProjectRootDirectory) && 
            container.ProjectRootDirectory.Contains(projectName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private List<string> GeneratePortResolutionSuggestions(List<PortConflict> conflicts)
    {
        var suggestions = new List<string>();
        
        foreach (var conflict in conflicts)
        {
            if (conflict.CanResolve)
            {
                suggestions.Add($"Port {conflict.Port}: Stop process {conflict.ProcessInfo?.ProcessName} (PID: {conflict.ProcessInfo?.ProcessId})");
            }
            else
            {
                suggestions.Add($"Port {conflict.Port}: Use alternative port or resolve manually");
            }
        }
        
        return suggestions;
    }

    private async Task<ContainerLifecycleResult> ConvertStartResult(StartContainerResult startResult)
    {
        return new ContainerLifecycleResult
        {
            Success = startResult.Success,
            Operation = ContainerOperation.Start,
            Message = startResult.Message,
            Container = startResult.Container,
            OperationTime = startResult.StartupTime
        };
    }

    private async Task<ContainerLifecycleResult> ConvertStopResult(StopContainerResult stopResult)
    {
        return new ContainerLifecycleResult
        {
            Success = stopResult.Success,
            Operation = ContainerOperation.Stop,
            Message = stopResult.Message,
            OperationTime = stopResult.StopTime
        };
    }

    private async Task<ContainerLifecycleResult> ConvertRestartResult(RestartContainerResult restartResult)
    {
        return new ContainerLifecycleResult
        {
            Success = restartResult.Success,
            Operation = ContainerOperation.Restart,
            Message = restartResult.Message,
            OperationTime = restartResult.RestartTime
        };
    }

    private async Task<ContainerLifecycleResult> RemoveContainerAsync(string containerName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"rm -f {containerName}",
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

            var success = process.ExitCode == 0;

            return new ContainerLifecycleResult
            {
                Success = success,
                Operation = ContainerOperation.Remove,
                Message = success ? "Container removed successfully" : error
            };
        }
        catch (Exception ex)
        {
            return new ContainerLifecycleResult
            {
                Success = false,
                Operation = ContainerOperation.Remove,
                Message = ex.Message
            };
        }
    }

    private async Task<ContainerLifecycleResult> PauseContainerAsync(string containerName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"pause {containerName}",
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

            var success = process.ExitCode == 0;

            return new ContainerLifecycleResult
            {
                Success = success,
                Operation = ContainerOperation.Pause,
                Message = success ? "Container paused successfully" : error
            };
        }
        catch (Exception ex)
        {
            return new ContainerLifecycleResult
            {
                Success = false,
                Operation = ContainerOperation.Pause,
                Message = ex.Message
            };
        }
    }

    private async Task<ContainerLifecycleResult> UnpauseContainerAsync(string containerName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"unpause {containerName}",
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

            var success = process.ExitCode == 0;

            return new ContainerLifecycleResult
            {
                Success = success,
                Operation = ContainerOperation.Unpause,
                Message = success ? "Container unpaused successfully" : error
            };
        }
        catch (Exception ex)
        {
            return new ContainerLifecycleResult
            {
                Success = false,
                Operation = ContainerOperation.Unpause,
                Message = ex.Message
            };
        }
    }

    #endregion
}