using System.Diagnostics;
using System.Text.Json;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// Docker容器引擎实现
/// </summary>
public class DockerEngine : IContainerEngine
{
    private readonly ILogger<DockerEngine> _logger;
    private readonly IPortConflictService _portConflictService;
    private readonly INetworkService _networkService;

    public DockerEngine(
        ILogger<DockerEngine> logger,
        IPortConflictService portConflictService,
        INetworkService networkService)
    {
        _logger = logger;
        _portConflictService = portConflictService;
        _networkService = networkService;
        Type = ContainerEngineType.Docker;
    }

    /// <summary>
    /// 容器引擎类型
    /// </summary>
    public ContainerEngineType Type { get; }

    public async Task<bool> ContainerExistsAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Checking if container exists: {ContainerName}", containerName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"inspect {containerName} --format json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if container exists: {ContainerName}", containerName);
            return false;
        }
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
                    FileName = "docker",
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

    public async Task<List<ContainerInfo>> GetAllContainersAsync()
    {
        try
        {
            _logger.LogDebug("Getting all containers");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps -a --format json",
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
                _logger.LogError("Failed to get all containers: {Error}", error);
                return new List<ContainerInfo>();
            }

            return ParseContainerList(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all containers");
            return new List<ContainerInfo>();
        }
    }

    public async Task<List<ContainerInfo>> GetRunningContainersAsync()
    {
        try
        {
            _logger.LogDebug("Getting running containers");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --filter status=running --format json",
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
                _logger.LogError("Failed to get running containers: {Error}", error);
                return new List<ContainerInfo>();
            }

            return ParseContainerList(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get running containers");
            return new List<ContainerInfo>();
        }
    }

    public async Task<StartContainerResult> StartContainerAsync(string containerName, StartOptions? options = null)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Starting container: {ContainerName}", containerName);

            // 检查容器是否已经在运行
            var isRunning = await IsContainerRunningAsync(containerName);
            if (isRunning)
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
                var portConflictResult = await _portConflictService.CheckAndResolvePortConflictsAsync(container);
                if (portConflictResult.HasConflict)
                {
                    return new StartContainerResult
                    {
                        Success = false,
                        Mode = StartMode.New,
                        Message = $"Port conflicts detected on port {portConflictResult.Port}: {string.Join(", ", portConflictResult.ConflictingServices)}"
                    };
                }
            }

            // 执行启动命令
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"start {containerName}",
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
                Mode = StartMode.New,
                Message = success ? "Container started successfully" : $"Failed to start container: {error}",
                Container = updatedContainer,
                StartupTime = DateTime.UtcNow - startTime
            };
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

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"stop {(force ? "--time 0 " : "")}{containerName}",
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
                Message = success ? "Container stopped successfully" : $"Failed to stop container: {error}"
            };
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

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
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
            var updatedContainer = success ? await GetContainerInfoAsync(containerName) : null;

            return new RestartContainerResult
            {
                Success = success,
                Message = success ? "Container restarted successfully" : $"Failed to restart container: {error}",
                Container = updatedContainer
            };
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

    public async Task<bool> IsContainerRunningAsync(string containerName)
    {
        try
        {
            _logger.LogDebug("Checking if container is running: {ContainerName}", containerName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"ps --filter name={containerName} --filter status=running --format \"{{{{.ID}}}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return !string.IsNullOrWhiteSpace(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if container is running: {ContainerName}", containerName);
            return false;
        }
    }

    public async Task<ShellExecutionResult> ExecuteInContainerAsync(string containerName, string command, ShellOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Executing command in container: {ContainerName}", containerName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec {containerName} {command}",
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
                ExitCode = process.ExitCode
            };
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

    public async Task<ContainerLogsResult> GetContainerLogsAsync(string containerName, int tailLines = 100)
    {
        try
        {
            _logger.LogDebug("Getting logs for container: {ContainerName}", containerName);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
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

            return new ContainerLogsResult
            {
                Success = process.ExitCode == 0,
                Logs = output,
                Error = process.ExitCode != 0 ? error : null
            };
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

    #region Private Methods

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

    private List<ContainerInfo> ParseContainerList(string jsonOutput)
    {
        var containers = new List<ContainerInfo>();

        try
        {
            using var doc = JsonDocument.Parse(jsonOutput);
            var root = doc.RootElement;

            foreach (var containerElement in root.EnumerateArray())
            {
                var container = new ContainerInfo
                {
                    Id = containerElement.GetProperty("Id").GetString() ?? string.Empty,
                    Name = containerElement.GetProperty("Names")[0].GetString() ?? string.Empty,
                    Image = containerElement.GetProperty("Image").GetString() ?? string.Empty,
                    Status = ParseContainerStatus(containerElement.GetProperty("Status").GetString() ?? "unknown"),
                    Created = DateTimeOffset.FromUnixTimeSeconds(containerElement.GetProperty("CreatedAt").GetInt64()).DateTime,
                    Ports = ParsePortsFromList(containerElement)
                };

                containers.Add(container);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse container list from JSON: {JsonOutput}", jsonOutput);
        }

        return containers;
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

    private List<PortMapping> ParsePortsFromList(JsonElement containerElement)
    {
        var ports = new List<PortMapping>();

        try
        {
            if (containerElement.TryGetProperty("Ports", out var portsElement))
            {
                foreach (var portElement in portsElement.EnumerateArray())
                {
                    var portMapping = new PortMapping
                    {
                        ContainerPort = portElement.GetProperty("container_port").GetInt32(),
                        Protocol = ParseProtocolType(portElement.GetProperty("protocol").GetString() ?? "tcp"),
                        HostIp = portElement.GetProperty("host_ip").GetString(),
                        HostPort = portElement.GetProperty("host_port").GetInt32()
                    };

                    ports.Add(portMapping);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse container ports from list");
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