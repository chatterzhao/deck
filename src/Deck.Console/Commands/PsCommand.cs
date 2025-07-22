using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Deck.Console.Commands;

/// <summary>
/// Ps命令 - 智能容器列表过滤
/// 基于deck-shell的list_images功能，提供详细的容器状态和管理信息
/// </summary>
public class PsCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IDirectoryManagementService _directoryManagement;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly ILoggingService _loggingService;
    private readonly ILogger _logger;

    public PsCommand(
        IConsoleDisplay consoleDisplay,
        IDirectoryManagementService directoryManagement,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService)
    {
        _consoleDisplay = consoleDisplay;
        _directoryManagement = directoryManagement;
        _interactiveSelection = interactiveSelection;
        _loggingService = loggingService;
        _logger = _loggingService.GetLogger("Deck.Console.PsCommand");
    }

    /// <summary>
    /// 执行容器列表显示
    /// </summary>
    public async Task<bool> ExecuteAsync(bool showAll = false, string? environmentFilter = null)
    {
        try
        {
            _logger.LogInformation("Starting ps command execution with showAll: {ShowAll}, filter: {Filter}", 
                showAll, environmentFilter ?? "none");

            _consoleDisplay.ShowInfo("🐳 容器状态列表");
            _consoleDisplay.WriteLine();

            // 获取三层配置选项
            var threeLayerOptions = await _directoryManagement.GetThreeLayerOptionsAsync();
            if (threeLayerOptions == null)
            {
                _consoleDisplay.ShowInfo("📋 当前目录未初始化 .deck 配置");
                _consoleDisplay.ShowInfo("💡 运行 'deck start' 初始化项目配置");
                return true;
            }

            // 获取容器信息
            var containerInfos = await GetContainerInfosAsync(threeLayerOptions, environmentFilter);
            
            if (!containerInfos.Any())
            {
                if (!string.IsNullOrEmpty(environmentFilter))
                {
                    _consoleDisplay.ShowInfo($"📋 未找到环境类型为 '{environmentFilter}' 的容器");
                }
                else
                {
                    _consoleDisplay.ShowInfo("📋 暂无容器信息");
                }
                
                _consoleDisplay.ShowInfo("💡 运行 'deck start' 创建开发环境");
                return true;
            }

            // 过滤显示的容器
            var containersToShow = showAll ? 
                containerInfos : 
                containerInfos.Where(c => c.Status != ContainerStatus.NotFound);

            if (!containersToShow.Any())
            {
                _consoleDisplay.ShowInfo("📋 当前无运行或已停止的容器");
                _consoleDisplay.ShowInfo("💡 使用 --all 参数查看所有配置状态");
                return true;
            }

            // 分组显示
            await DisplayContainersByStatusAsync(containersToShow);

            // 显示管理建议
            ShowManagementSuggestions(containerInfos);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ps command execution failed");
            _consoleDisplay.ShowError($"❌ 获取容器列表失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取容器信息列表
    /// </summary>
    private async Task<List<ContainerInfo>> GetContainerInfosAsync(ThreeLayerOptions threeLayerOptions, string? environmentFilter)
    {
        var containerInfos = new List<ContainerInfo>();

        // 处理Images层配置
        if (threeLayerOptions.Images != null)
        {
            foreach (var imageConfig in threeLayerOptions.Images)
            {
                if (ShouldIncludeContainer(imageConfig, environmentFilter))
                {
                    var containerInfo = await GetContainerInfoFromConfigAsync(imageConfig, "Images");
                    containerInfos.Add(containerInfo);
                }
            }
        }

        // 处理Custom层配置
        if (threeLayerOptions.Custom != null)
        {
            foreach (var customConfig in threeLayerOptions.Custom)
            {
                if (ShouldIncludeContainer(customConfig, environmentFilter))
                {
                    var containerInfo = await GetContainerInfoFromConfigAsync(customConfig, "Custom");
                    containerInfos.Add(containerInfo);
                }
            }
        }

        // 处理Templates层配置
        if (threeLayerOptions.Templates != null)
        {
            foreach (var templateConfig in threeLayerOptions.Templates)
            {
                if (ShouldIncludeContainer(templateConfig, environmentFilter))
                {
                    var containerInfo = await GetContainerInfoFromConfigAsync(templateConfig, "Templates");
                    containerInfos.Add(containerInfo);
                }
            }
        }

        return containerInfos.OrderBy(c => c.ConfigLayer)
                           .ThenBy(c => c.Name)
                           .ToList();
    }

    /// <summary>
    /// 从配置获取容器信息
    /// </summary>
    private async Task<ContainerInfo> GetContainerInfoFromConfigAsync(ConfigurationOption config, string layer)
    {
        var containerInfo = new ContainerInfo
        {
            Name = config.Name,
            ConfigLayer = layer,
            ImageName = GenerateImageName(config.Name),
            ContainerName = GenerateContainerName(config.Name),
            EnvironmentType = GetEnvironmentType(config),
            CreatedTime = config.LastModified ?? DateTime.MinValue
        };

        // 检测容器状态
        containerInfo.Status = await DetectContainerStatusAsync(containerInfo.ContainerName);
        
        // 获取端口信息
        if (containerInfo.Status == ContainerStatus.Running)
        {
            containerInfo.Ports = await GetContainerPortsAsync(containerInfo.ContainerName);
        }

        return containerInfo;
    }

    /// <summary>
    /// 检测容器状态 - 基于deck-shell的容器检测逻辑
    /// </summary>
    private async Task<ContainerStatus> DetectContainerStatusAsync(string containerName)
    {
        try
        {
            var statusOutput = await ExecuteCommandAsync($"podman ps -a --filter \"name=^{containerName}$\" --format \"{{{{.Status}}}}\"");
            
            if (string.IsNullOrWhiteSpace(statusOutput))
            {
                return ContainerStatus.NotFound;
            }

            var status = statusOutput.Trim().ToLowerInvariant();
            
            if (status.Contains("up") || status.Contains("running"))
            {
                return ContainerStatus.Running;
            }
            else if (status.Contains("exited") || status.Contains("stopped"))
            {
                return ContainerStatus.Stopped;
            }
            else if (status.Contains("created"))
            {
                return ContainerStatus.Created;
            }
            
            return ContainerStatus.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect container status for: {ContainerName}", containerName);
            return ContainerStatus.Unknown;
        }
    }

    /// <summary>
    /// 获取容器端口信息
    /// </summary>
    private async Task<List<string>> GetContainerPortsAsync(string containerName)
    {
        try
        {
            var portsOutput = await ExecuteCommandAsync($"podman port {containerName}");
            
            if (string.IsNullOrWhiteSpace(portsOutput))
            {
                return new List<string>();
            }

            return portsOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                             .Select(line => line.Trim())
                             .Where(line => !string.IsNullOrEmpty(line))
                             .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get container ports for: {ContainerName}", containerName);
            return new List<string>();
        }
    }

    /// <summary>
    /// 分组显示容器
    /// </summary>
    private Task DisplayContainersByStatusAsync(IEnumerable<ContainerInfo> containers)
    {
        var groupedContainers = containers.GroupBy(c => c.Status);

        foreach (var group in groupedContainers.OrderBy(g => GetStatusOrder(g.Key)))
        {
            var statusIcon = GetStatusIcon(group.Key);
            var statusName = GetStatusName(group.Key);
            var statusColor = GetStatusColor(group.Key);

            _consoleDisplay.ShowInfo($"{statusIcon} {statusName} ({group.Count()})");
            _consoleDisplay.WriteLine();

            // 表头
            _consoleDisplay.ShowInfo($"{"名称",-25} {"层级",-10} {"环境类型",-12} {"创建时间",-20} {"端口映射",-20}");
            _consoleDisplay.ShowInfo(new string('-', 87));

            foreach (var container in group.OrderBy(c => c.ConfigLayer).ThenBy(c => c.Name))
            {
                var name = TruncateString(container.Name, 24);
                var layer = TruncateString(container.ConfigLayer, 9);
                var envType = TruncateString(container.EnvironmentType ?? "Unknown", 11);
                var createdTime = container.CreatedTime == DateTime.MinValue ? 
                    "Unknown" : 
                    container.CreatedTime.ToString("MM-dd HH:mm");
                var ports = container.Ports.Any() ? 
                    TruncateString(string.Join(",", container.Ports.Take(2)), 19) : 
                    "-";

                _consoleDisplay.ShowInfo($"{name,-25} {layer,-10} {envType,-12} {createdTime,-20} {ports,-20}");
            }

            _consoleDisplay.WriteLine();
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 显示管理建议
    /// </summary>
    private void ShowManagementSuggestions(List<ContainerInfo> containerInfos)
    {
        _consoleDisplay.ShowInfo("💡 容器管理命令:");

        var runningContainers = containerInfos.Where(c => c.Status == ContainerStatus.Running).ToList();
        var stoppedContainers = containerInfos.Where(c => c.Status == ContainerStatus.Stopped).ToList();

        if (runningContainers.Any())
        {
            _consoleDisplay.ShowInfo("   deck stop [name]     # 停止运行中的容器");
            _consoleDisplay.ShowInfo("   deck logs [name]     # 查看容器日志");
            _consoleDisplay.ShowInfo("   deck shell [name]    # 进入容器Shell");
        }

        if (stoppedContainers.Any())
        {
            _consoleDisplay.ShowInfo("   deck start [name]    # 启动已停止的容器");
            _consoleDisplay.ShowInfo("   deck rm [name]       # 删除已停止的容器");
        }

        _consoleDisplay.ShowInfo("   deck ps --all        # 显示所有容器状态（包括未创建）");
        
        if (!string.IsNullOrEmpty(GetAvailableEnvironmentTypes(containerInfos)))
        {
            _consoleDisplay.ShowInfo($"   deck ps --env [type]  # 过滤环境类型: {GetAvailableEnvironmentTypes(containerInfos)}");
        }
    }

    /// <summary>
    /// 获取可用的环境类型列表
    /// </summary>
    private string GetAvailableEnvironmentTypes(List<ContainerInfo> containerInfos)
    {
        var envTypes = containerInfos
            .Where(c => !string.IsNullOrEmpty(c.EnvironmentType))
            .Select(c => c.EnvironmentType!)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return string.Join(", ", envTypes);
    }

    /// <summary>
    /// 判断是否应该包含容器
    /// </summary>
    private bool ShouldIncludeContainer(ConfigurationOption config, string? environmentFilter)
    {
        if (string.IsNullOrEmpty(environmentFilter))
        {
            return true;
        }

        var envType = GetEnvironmentType(config);
        return string.Equals(envType, environmentFilter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行命令并获取输出
    /// </summary>
    private async Task<string> ExecuteCommandAsync(string command)
    {
        try
        {
            using var process = new Process();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to execute command: {Command}", command);
            return string.Empty;
        }
    }

    #region 辅助方法

    /// <summary>
    /// 生成镜像名称
    /// </summary>
    private string GenerateImageName(string configName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        return $"{configName}-{timestamp}";
    }

    /// <summary>
    /// 生成容器名称
    /// </summary>
    private string GenerateContainerName(string configName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        return $"{configName}-{timestamp}";
    }

    /// <summary>
    /// 获取环境类型
    /// </summary>
    private string? GetEnvironmentType(ConfigurationOption config)
    {
        // 这里可以根据配置文件内容或文件名推断环境类型
        // 暂时返回配置名称或从其他地方获取
        return config.Name?.Split('-').FirstOrDefault()?.ToLowerInvariant();
    }

    /// <summary>
    /// 获取状态排序优先级
    /// </summary>
    private int GetStatusOrder(ContainerStatus status) => status switch
    {
        ContainerStatus.Running => 1,
        ContainerStatus.Stopped => 2,
        ContainerStatus.Created => 3,
        ContainerStatus.NotFound => 4,
        ContainerStatus.Unknown => 5,
        _ => 6
    };

    /// <summary>
    /// 获取状态图标
    /// </summary>
    private string GetStatusIcon(ContainerStatus status) => status switch
    {
        ContainerStatus.Running => "🟢",
        ContainerStatus.Stopped => "🟡",
        ContainerStatus.Created => "🔵",
        ContainerStatus.NotFound => "⚪",
        ContainerStatus.Unknown => "🔴",
        _ => "❓"
    };

    /// <summary>
    /// 获取状态名称
    /// </summary>
    private string GetStatusName(ContainerStatus status) => status switch
    {
        ContainerStatus.Running => "运行中",
        ContainerStatus.Stopped => "已停止",
        ContainerStatus.Created => "已创建",
        ContainerStatus.NotFound => "未创建",
        ContainerStatus.Unknown => "状态未知",
        _ => "未定义"
    };

    /// <summary>
    /// 获取状态颜色
    /// </summary>
    private ConsoleColor GetStatusColor(ContainerStatus status) => status switch
    {
        ContainerStatus.Running => ConsoleColor.Green,
        ContainerStatus.Stopped => ConsoleColor.Yellow,
        ContainerStatus.Created => ConsoleColor.Blue,
        ContainerStatus.NotFound => ConsoleColor.Gray,
        ContainerStatus.Unknown => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    /// <summary>
    /// 截断字符串
    /// </summary>
    private string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
            
        if (text.Length <= maxLength)
            return text;
            
        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}

/// <summary>
/// 容器信息
/// </summary>
public class ContainerInfo
{
    public string Name { get; set; } = string.Empty;
    public string ConfigLayer { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string? EnvironmentType { get; set; }
    public ContainerStatus Status { get; set; }
    public DateTime CreatedTime { get; set; }
    public List<string> Ports { get; set; } = new List<string>();
}

/// <summary>
/// 容器状态枚举
/// </summary>
public enum ContainerStatus
{
    Running,
    Stopped,
    Created,
    NotFound,
    Unknown
}