using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Deck.Services;

/// <summary>
/// 端口冲突检测服务 - 提供跨平台端口管理和冲突解决
/// </summary>
public class PortConflictService : IPortConflictService
{
    private readonly ILogger<PortConflictService> _logger;
    private static readonly Regex NetstatRegex = new(@"^\s*(TCP|UDP)\s+([^\s]+):(\d+)\s+([^\s]+):(\d+)\s+(\w+)?\s*(\d+)?", RegexOptions.Compiled);
    
    // 项目类型默认端口映射
    private static readonly Dictionary<ProjectType, Dictionary<ProjectPortType, int>> DefaultPortMappings = new()
    {
        [ProjectType.Tauri] = new()
        {
            [ProjectPortType.DevServer] = 1420,
            [ProjectPortType.Debug] = 9229,
            [ProjectPortType.HotReload] = 1421
        },
        [ProjectType.Flutter] = new()
        {
            [ProjectPortType.DevServer] = 5000,
            [ProjectPortType.Debug] = 9100,
            [ProjectPortType.HotReload] = 5001
        },
        [ProjectType.Avalonia] = new()
        {
            [ProjectPortType.DevServer] = 5000,
            [ProjectPortType.Debug] = 5001,
            [ProjectPortType.Api] = 5002
        },
        [ProjectType.DotNet] = new()
        {
            [ProjectPortType.Api] = 5000,
            [ProjectPortType.Debug] = 5001,
            [ProjectPortType.HotReload] = 5002
        },
        [ProjectType.Python] = new()
        {
            [ProjectPortType.DevServer] = 8000,
            [ProjectPortType.Debug] = 5678,
            [ProjectPortType.Api] = 8001
        },
        [ProjectType.Node] = new()
        {
            [ProjectPortType.DevServer] = 3000,
            [ProjectPortType.Debug] = 9229,
            [ProjectPortType.HotReload] = 3001
        }
    };

    public PortConflictService(ILogger<PortConflictService> logger)
    {
        _logger = logger;
    }

    public async Task<PortCheckResult> CheckPortAsync(int port, DeckProtocolType protocol = DeckProtocolType.TCP)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("检查端口 {Port}/{Protocol} 可用性", port, protocol);
            
            bool isAvailable = protocol switch
            {
                DeckProtocolType.TCP => await CheckTcpPortAsync(port),
                DeckProtocolType.UDP => await CheckUdpPortAsync(port),
                _ => throw new ArgumentException($"不支持的协议类型: {protocol}")
            };

            stopwatch.Stop();
            
            var result = new PortCheckResult
            {
                Port = port,
                Protocol = protocol,
                IsAvailable = isAvailable,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };

            _logger.LogDebug("端口 {Port}/{Protocol} 检查完成: {Available}, 耗时 {ElapsedMs}ms", 
                port, protocol, isAvailable ? "可用" : "占用", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "端口 {Port}/{Protocol} 检查失败", port, protocol);
            
            return new PortCheckResult
            {
                Port = port,
                Protocol = protocol,
                IsAvailable = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<PortCheckResult>> CheckPortsAsync(IEnumerable<int> ports, DeckProtocolType protocol = DeckProtocolType.TCP)
    {
        _logger.LogDebug("批量检查 {Count} 个端口", ports.Count());
        
        var tasks = ports.Select(port => CheckPortAsync(port, protocol));
        var results = await Task.WhenAll(tasks);
        
        return results.ToList();
    }

    public async Task<PortConflictInfo> DetectPortConflictAsync(int port, DeckProtocolType protocol = DeckProtocolType.TCP)
    {
        _logger.LogDebug("检测端口 {Port}/{Protocol} 冲突详情", port, protocol);
        
        var checkResult = await CheckPortAsync(port, protocol);
        
        var conflictInfo = new PortConflictInfo
        {
            Port = port,
            Protocol = protocol,
            HasConflict = !checkResult.IsAvailable
        };

        if (conflictInfo.HasConflict)
        {
            try
            {
                conflictInfo.OccupyingProcess = await GetProcessUsingPortAsync(port, protocol);
                conflictInfo.Severity = DetermineSeverity(conflictInfo.OccupyingProcess);
                conflictInfo.ServiceType = IdentifyServiceType(port, conflictInfo.OccupyingProcess);
                
                // 获取监听地址和状态
                var usageInfo = await GetPortUsageDetailsAsync(port, protocol);
                if (usageInfo != null)
                {
                    conflictInfo.ListenAddress = usageInfo.LocalAddress;
                    conflictInfo.State = usageInfo.State;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取端口 {Port} 冲突详情时出错", port);
            }
        }

        return conflictInfo;
    }

    public async Task<int?> FindAvailablePortAsync(int? preferredPort = null, int startRange = 8000, int endRange = 9000, DeckProtocolType protocol = DeckProtocolType.TCP)
    {
        _logger.LogDebug("查找可用端口 - 优选: {PreferredPort}, 范围: {Start}-{End}", 
            preferredPort, startRange, endRange);

        // 首先检查首选端口
        if (preferredPort.HasValue && preferredPort.Value >= startRange && preferredPort.Value <= endRange)
        {
            var checkResult = await CheckPortAsync(preferredPort.Value, protocol);
            if (checkResult.IsAvailable)
            {
                _logger.LogDebug("首选端口 {Port} 可用", preferredPort.Value);
                return preferredPort.Value;
            }
        }

        // 搜索可用端口
        for (int port = startRange; port <= endRange; port++)
        {
            if (port == preferredPort) continue; // 已经检查过了
            
            var checkResult = await CheckPortAsync(port, protocol);
            if (checkResult.IsAvailable)
            {
                _logger.LogDebug("找到可用端口 {Port}", port);
                return port;
            }
        }

        _logger.LogWarning("在范围 {Start}-{End} 内未找到可用端口", startRange, endRange);
        return null;
    }

    public async Task<ProjectPortAllocation> AllocateProjectPortsAsync(ProjectType projectType, params ProjectPortType[] portTypes)
    {
        _logger.LogDebug("为项目 {ProjectType} 分配端口: {PortTypes}", 
            projectType, string.Join(", ", portTypes));

        var allocation = new ProjectPortAllocation
        {
            ProjectType = projectType
        };

        if (!DefaultPortMappings.TryGetValue(projectType, out var defaultPorts))
        {
            _logger.LogWarning("项目类型 {ProjectType} 没有默认端口映射", projectType);
            allocation.FailedAllocations.AddRange(portTypes);
            allocation.AllocationSummary = $"项目类型 {projectType} 不支持自动端口分配";
            return allocation;
        }

        foreach (var portType in portTypes)
        {
            if (defaultPorts.TryGetValue(portType, out var preferredPort))
            {
                var allocatedPort = await FindAvailablePortAsync(preferredPort, preferredPort, preferredPort + 100);
                if (allocatedPort.HasValue)
                {
                    allocation.AllocatedPorts[portType] = allocatedPort.Value;
                    
                    if (allocatedPort.Value != preferredPort)
                    {
                        allocation.Recommendations.Add($"{portType} 端口从 {preferredPort} 调整为 {allocatedPort}");
                    }
                }
                else
                {
                    allocation.FailedAllocations.Add(portType);
                    _logger.LogWarning("无法为 {ProjectType}.{PortType} 分配端口", projectType, portType);
                }
            }
            else
            {
                allocation.FailedAllocations.Add(portType);
                _logger.LogWarning("项目类型 {ProjectType} 不支持端口类型 {PortType}", projectType, portType);
            }
        }

        allocation.AllocationSummary = $"成功分配 {allocation.AllocatedPorts.Count}/{portTypes.Length} 个端口";
        
        if (allocation.FailedAllocations.Count > 0)
        {
            allocation.Recommendations.Add($"建议手动配置失败的端口类型: {string.Join(", ", allocation.FailedAllocations)}");
        }

        return allocation;
    }

    public async Task<List<PortResolutionSuggestion>> GetResolutionSuggestionsAsync(PortConflictInfo conflictInfo)
    {
        var suggestions = new List<PortResolutionSuggestion>();
        
        if (!conflictInfo.HasConflict)
        {
            return suggestions;
        }

        _logger.LogDebug("生成端口 {Port} 冲突解决建议", conflictInfo.Port);

        // 建议1: 使用替代端口
        var alternativePort = await FindAvailablePortAsync(null, conflictInfo.Port + 1, conflictInfo.Port + 100, conflictInfo.Protocol);
        if (alternativePort.HasValue)
        {
            suggestions.Add(new PortResolutionSuggestion
            {
                Type = ResolutionType.UseAlternativePort,
                Description = $"使用替代端口 {alternativePort}",
                AlternativePort = alternativePort,
                Priority = SuggestionPriority.High,
                Risk = RiskLevel.None,
                CanAutoExecute = true
            });
        }

        // 建议2: 停止占用进程
        if (conflictInfo.OccupyingProcess != null && conflictInfo.OccupyingProcess.CanBeStopped)
        {
            var risk = conflictInfo.OccupyingProcess.IsSystemProcess ? RiskLevel.High : RiskLevel.Low;
            var priority = conflictInfo.Severity == ConflictSeverity.Critical ? 
                SuggestionPriority.Low : SuggestionPriority.Medium;

            suggestions.Add(new PortResolutionSuggestion
            {
                Type = ResolutionType.StopProcess,
                Description = $"停止进程 {conflictInfo.OccupyingProcess.ProcessName} (PID: {conflictInfo.OccupyingProcess.ProcessId})",
                Command = GetStopProcessCommand(conflictInfo.OccupyingProcess),
                Priority = priority,
                Risk = risk,
                CanAutoExecute = !conflictInfo.OccupyingProcess.IsSystemProcess
            });
        }

        // 建议3: 等待释放（针对短暂占用）
        if (conflictInfo.State == ConnectionState.TimeWait || conflictInfo.State == ConnectionState.CloseWait)
        {
            suggestions.Add(new PortResolutionSuggestion
            {
                Type = ResolutionType.WaitForRelease,
                Description = "等待连接自然关闭（通常1-2分钟）",
                Priority = SuggestionPriority.Medium,
                Risk = RiskLevel.None,
                CanAutoExecute = false
            });
        }

        // 建议4: 修改配置
        suggestions.Add(new PortResolutionSuggestion
        {
            Type = ResolutionType.ModifyConfiguration,
            Description = "修改应用程序配置文件中的端口设置",
            Priority = SuggestionPriority.Low,
            Risk = RiskLevel.None,
            CanAutoExecute = false
        });

        // 根据优先级和风险排序
        return suggestions.OrderBy(s => s.Risk).ThenByDescending(s => s.Priority).ToList();
    }

    public async Task<ProcessStopResult> StopProcessUsingPortAsync(int port, DeckProtocolType protocol = DeckProtocolType.TCP, bool force = false)
    {
        _logger.LogInformation("尝试停止占用端口 {Port}/{Protocol} 的进程", port, protocol);
        
        var processInfo = await GetProcessUsingPortAsync(port, protocol);
        if (processInfo == null)
        {
            return new ProcessStopResult
            {
                Success = false,
                ErrorMessage = "未找到占用端口的进程"
            };
        }

        return await StopProcessAsync(processInfo, force);
    }

    public async Task<SystemPortUsage> GetSystemPortUsageAsync()
    {
        _logger.LogDebug("获取系统端口使用情况");
        
        var usage = new SystemPortUsage();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await GetWindowsPortUsageAsync(usage);
            }
            else
            {
                await GetUnixPortUsageAsync(usage);
            }

            CalculateStatistics(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统端口使用情况失败");
        }

        return usage;
    }

    public Task<PortValidationResult> ValidatePortAsync(int port, bool checkPrivileged = true)
    {
        var result = new PortValidationResult
        {
            Port = port,
            IsValid = true
        };

        // 基本范围检查
        if (port < 1 || port > 65535)
        {
            result.IsValid = false;
            result.Errors.Add($"端口号 {port} 超出有效范围 (1-65535)");
            return Task.FromResult(result);
        }

        // 特权端口检查
        if (checkPrivileged && port < 1024)
        {
            result.RequiresPrivilege = true;
            result.Warnings.Add($"端口 {port} 是特权端口，需要管理员权限");
            
            // 建议替代端口
            if (port == 80) result.SuggestedAlternatives.AddRange([8080, 3000, 5000]);
            else if (port == 443) result.SuggestedAlternatives.AddRange([8443, 3443, 5443]);
            else if (port == 22) result.SuggestedAlternatives.AddRange([2222, 2200]);
            else result.SuggestedAlternatives.AddRange([port + 8000, port + 3000]);
        }

        // 知名服务端口警告
        var wellKnownPorts = new Dictionary<int, string>
        {
            [21] = "FTP", [22] = "SSH", [23] = "Telnet", [25] = "SMTP",
            [53] = "DNS", [80] = "HTTP", [110] = "POP3", [143] = "IMAP",
            [443] = "HTTPS", [993] = "IMAPS", [995] = "POP3S"
        };

        if (wellKnownPorts.TryGetValue(port, out var service))
        {
            result.Warnings.Add($"端口 {port} 通常用于 {service} 服务");
        }

        return Task.FromResult(result);
    }

    #region Private Methods

    private Task<bool> CheckTcpPortAsync(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return Task.FromResult(true);
        }
        catch (SocketException)
        {
            return Task.FromResult(false);
        }
    }

    private Task<bool> CheckUdpPortAsync(int port)
    {
        try
        {
            using var client = new UdpClient(port);
            return Task.FromResult(true);
        }
        catch (SocketException)
        {
            return Task.FromResult(false);
        }
    }

    private async Task<ProcessInfo?> GetProcessUsingPortAsync(int port, DeckProtocolType protocol)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await GetWindowsProcessUsingPortAsync(port, protocol);
            }
            else
            {
                return await GetUnixProcessUsingPortAsync(port, protocol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取占用端口 {Port} 的进程信息失败", port);
            return null;
        }
    }

    private async Task<ProcessInfo?> GetWindowsProcessUsingPortAsync(int port, DeckProtocolType protocol)
    {
        var protocolStr = protocol.ToString().ToUpper();
        var startInfo = new ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = "-ano",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return null;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        foreach (var line in output.Split('\n'))
        {
            var match = NetstatRegex.Match(line);
            if (match.Success && 
                match.Groups[1].Value == protocolStr && 
                int.Parse(match.Groups[3].Value) == port)
            {
                if (int.TryParse(match.Groups[7].Value, out var pid))
                {
                    return await GetProcessInfoAsync(pid);
                }
            }
        }

        return null;
    }

    private async Task<ProcessInfo?> GetUnixProcessUsingPortAsync(int port, DeckProtocolType protocol)
    {
        var protocolStr = protocol.ToString().ToLower();
        var startInfo = new ProcessStartInfo
        {
            FileName = "lsof",
            Arguments = $"-i {protocolStr}:{port}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n');
            if (lines.Length > 1) // 跳过头部
            {
                var parts = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && int.TryParse(parts[1], out var pid))
                {
                    return await GetProcessInfoAsync(pid);
                }
            }
        }
        catch (Exception)
        {
            // lsof 可能不可用，尝试 netstat
            return await GetUnixProcessUsingNetstatAsync(port, protocol);
        }

        return null;
    }

    private async Task<ProcessInfo?> GetUnixProcessUsingNetstatAsync(int port, DeckProtocolType protocol)
    {
        var protocolStr = protocol.ToString().ToLower();
        var startInfo = new ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = $"-tulnp",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return null;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        foreach (var line in output.Split('\n'))
        {
            if (line.Contains($":{port} "))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lastPart = parts.LastOrDefault();
                if (lastPart?.Contains('/') == true)
                {
                    var pidPart = lastPart.Split('/')[0];
                    if (int.TryParse(pidPart, out var pid))
                    {
                        return await GetProcessInfoAsync(pid);
                    }
                }
            }
        }

        return null;
    }

    private Task<ProcessInfo?> GetProcessInfoAsync(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return Task.FromResult<ProcessInfo?>(new ProcessInfo
            {
                ProcessId = pid,
                ProcessName = process.ProcessName,
                StartTime = process.StartTime,
                IsSystemProcess = IsSystemProcess(process),
                CanBeStopped = CanProcessBeStopped(process)
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取进程 {PID} 信息失败", pid);
            return Task.FromResult<ProcessInfo?>(null);
        }
    }

    private ConflictSeverity DetermineSeverity(ProcessInfo? processInfo)
    {
        if (processInfo == null) return ConflictSeverity.Medium;
        
        if (processInfo.IsSystemProcess) return ConflictSeverity.Critical;
        if (!processInfo.CanBeStopped) return ConflictSeverity.High;
        
        return ConflictSeverity.Medium;
    }

    private string? IdentifyServiceType(int port, ProcessInfo? processInfo)
    {
        // 常见端口服务识别
        var commonServices = new Dictionary<int, string>
        {
            [80] = "HTTP Web Server",
            [443] = "HTTPS Web Server", 
            [3000] = "Node.js Dev Server",
            [5000] = ".NET Web API",
            [8080] = "HTTP Proxy/Alt Web",
            [3306] = "MySQL Database",
            [5432] = "PostgreSQL Database",
            [6379] = "Redis Cache",
            [27017] = "MongoDB Database"
        };

        if (commonServices.TryGetValue(port, out var service))
        {
            return service;
        }

        // 基于进程名称识别
        if (processInfo != null)
        {
            var processName = processInfo.ProcessName.ToLower();
            if (processName.Contains("node")) return "Node.js Application";
            if (processName.Contains("dotnet")) return ".NET Application";  
            if (processName.Contains("python")) return "Python Application";
            if (processName.Contains("nginx")) return "Nginx Web Server";
            if (processName.Contains("apache")) return "Apache Web Server";
        }

        return null;
    }

    private string GetStopProcessCommand(ProcessInfo processInfo)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"taskkill /PID {processInfo.ProcessId} /F";
        }
        else
        {
            return $"kill {processInfo.ProcessId}";
        }
    }

    private Task<ProcessStopResult> StopProcessAsync(ProcessInfo processInfo, bool force)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ProcessStopResult
        {
            ProcessId = processInfo.ProcessId,
            ProcessName = processInfo.ProcessName
        };

        try
        {
            var process = Process.GetProcessById(processInfo.ProcessId);
            
            if (force || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.Kill();
                result.Method = StopMethod.Kill;
            }
            else
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(5000))
                {
                    process.Kill();
                    result.Method = StopMethod.Kill;
                }
                else
                {
                    result.Method = StopMethod.Graceful;
                }
            }

            result.Success = true;
            result.ExecutedCommand = GetStopProcessCommand(processInfo);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "停止进程 {ProcessId} 失败", processInfo.ProcessId);
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;
        return Task.FromResult(result);
    }

    private Task<PortUsageInfo?> GetPortUsageDetailsAsync(int port, DeckProtocolType protocol)
    {
        // 简化实现，返回基本信息
        var info = new PortUsageInfo
        {
            Port = port,
            LocalAddress = $"0.0.0.0:{port}",
            State = ConnectionState.Listen
        };
        return Task.FromResult<PortUsageInfo?>(info);
    }

    private async Task GetWindowsPortUsageAsync(SystemPortUsage usage)
    {
        // Windows netstat 实现
        var startInfo = new ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = "-ano",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        ParseNetstatOutput(output, usage);
    }

    private async Task GetUnixPortUsageAsync(SystemPortUsage usage)
    {
        // Unix netstat 实现
        var startInfo = new ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = "-tulnp",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        ParseNetstatOutput(output, usage);
    }

    private void ParseNetstatOutput(string output, SystemPortUsage usage)
    {
        foreach (var line in output.Split('\n'))
        {
            var match = NetstatRegex.Match(line);
            if (match.Success)
            {
                var protocol = match.Groups[1].Value;
                var localAddress = match.Groups[2].Value;
                var localPort = int.Parse(match.Groups[3].Value);
                var state = match.Groups[6].Value;

                var portInfo = new PortUsageInfo
                {
                    Port = localPort,
                    LocalAddress = $"{localAddress}:{localPort}",
                    State = ParseConnectionState(state)
                };

                if (protocol == "TCP")
                {
                    usage.TcpPorts.Add(portInfo);
                }
                else if (protocol == "UDP")
                {
                    usage.UdpPorts.Add(portInfo);
                }
            }
        }
    }

    private ConnectionState ParseConnectionState(string state)
    {
        return state.ToUpper() switch
        {
            "LISTENING" or "LISTEN" => ConnectionState.Listen,
            "ESTABLISHED" => ConnectionState.Established,
            "TIME_WAIT" => ConnectionState.TimeWait,
            "CLOSE_WAIT" => ConnectionState.CloseWait,
            "SYN_SENT" => ConnectionState.SynSent,
            "SYN_RECEIVED" => ConnectionState.SynReceived,
            "CLOSING" => ConnectionState.Closing,
            "CLOSED" => ConnectionState.Closed,
            _ => ConnectionState.Unknown
        };
    }

    private void CalculateStatistics(SystemPortUsage usage)
    {
        usage.Statistics.TcpListeningPorts = usage.TcpPorts.Count(p => p.State == ConnectionState.Listen);
        usage.Statistics.UdpListeningPorts = usage.UdpPorts.Count;
        usage.Statistics.ActiveConnections = usage.TcpPorts.Count(p => p.State == ConnectionState.Established);

        // 计算常用端口范围
        var allPorts = usage.TcpPorts.Concat(usage.UdpPorts).Select(p => p.Port);
        foreach (var port in allPorts)
        {
            var range = port switch
            {
                < 1024 => "System (1-1023)",
                < 5000 => "User (1024-4999)", 
                < 10000 => "Dynamic (5000-9999)",
                _ => "High (10000+)"
            };

            usage.Statistics.PopularPortRanges.TryGetValue(range, out var count);
            usage.Statistics.PopularPortRanges[range] = count + 1;
        }
    }

    private bool IsSystemProcess(Process process)
    {
        try
        {
            // 简化的系统进程判断
            var systemProcesses = new[] { "System", "svchost", "winlogon", "csrss", "lsass", "services" };
            return systemProcesses.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase) ||
                   process.SessionId == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool CanProcessBeStopped(Process process)
    {
        try
        {
            // 不能停止的进程类型
            if (IsSystemProcess(process)) return false;
            if (process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)) return false;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}