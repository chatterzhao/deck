using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Deck.Console.Commands;

/// <summary>
/// Rm命令 - 容器删除和交互式选择
/// 基于deck-shell的remove_image和clean_old_images实现，提供智能清理和交互式确认
/// </summary>
public class RmCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IDirectoryManagementService _directoryManagement;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly ILoggingService _loggingService;
    private readonly ILogger _logger;

    public RmCommand(
        IConsoleDisplay consoleDisplay,
        IDirectoryManagementService directoryManagement,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService)
    {
        _consoleDisplay = consoleDisplay;
        _directoryManagement = directoryManagement;
        _interactiveSelection = interactiveSelection;
        _loggingService = loggingService;
        _logger = _loggingService.GetLogger("Deck.Console.RmCommand");
    }

    /// <summary>
    /// 执行容器删除
    /// </summary>
    public async Task<bool> ExecuteAsync(string? containerName = null, bool force = false, bool all = false)
    {
        try
        {
            _logger.LogInformation("Starting rm command execution with container: {Container}, force: {Force}, all: {All}", 
                containerName ?? "interactive", force, all);

            if (all)
            {
                return await RemoveAllContainersAsync(force);
            }
            
            if (!string.IsNullOrEmpty(containerName))
            {
                return await RemoveSpecificContainerAsync(containerName, force);
            }

            return await RemoveContainerInteractiveAsync(force);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rm command execution failed");
            _consoleDisplay.ShowError($"❌ 删除容器失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 交互式删除容器
    /// </summary>
    private async Task<bool> RemoveContainerInteractiveAsync(bool force)
    {
        _consoleDisplay.ShowInfo("🗑️ 容器删除 - 交互式选择");
        _consoleDisplay.WriteLine();

        // 获取可删除的容器列表
        var containers = await GetRemovableContainersAsync();
        
        if (!containers.Any())
        {
            _consoleDisplay.ShowInfo("📋 当前无可删除的容器");
            _consoleDisplay.ShowInfo("💡 只能删除已停止或已创建状态的容器");
            return true;
        }

        // 显示容器列表
        _consoleDisplay.ShowInfo($"找到 {containers.Count} 个可删除的容器:");
        _consoleDisplay.WriteLine();

        var selectionOptions = containers.Select(c => new SelectableOption
        {
            Value = c.ContainerName,
            DisplayName = FormatContainerForSelection(c),
            Description = GetContainerDescription(c)
        }).ToList();

        // 添加批量选项
        if (containers.Count > 1)
        {
            selectionOptions.Add(new SelectableOption
            {
                Value = "cleanup-old",
                DisplayName = "🧹 智能清理 - 保留最新容器，删除旧版本",
                Description = "按项目分组，每个项目保留最新的容器"
            });

            selectionOptions.Add(new SelectableOption
            {
                Value = "remove-all-stopped",
                DisplayName = "🗑️ 删除所有已停止的容器",
                Description = "删除所有状态为已停止的容器"
            });
        }

        selectionOptions.Add(new SelectableOption
        {
            Value = "cancel",
            DisplayName = "❌ 取消删除",
            Description = "退出删除操作"
        });

        var selector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "请选择要删除的容器:",
            Items = selectionOptions,
            AllowMultiple = true
        };
        
        var result = await _interactiveSelection.ShowMultipleSelectionAsync(selector);
        
        if (result.IsCancelled)
        {
            _consoleDisplay.ShowInfo("❌ 用户取消删除操作");
            return true;
        }
        
        var selectedOptions = result.SelectedItems;

        if (!selectedOptions.Any())
        {
            return true;
        }

        // 处理选择结果
        return await ProcessRemovalSelectionsAsync(selectedOptions, containers, force);
    }

    /// <summary>
    /// 处理删除选择
    /// </summary>
    private async Task<bool> ProcessRemovalSelectionsAsync(
        List<SelectableOption> selections, 
        List<RemovableContainer> containers, 
        bool force)
    {
        var allSuccess = true;

        foreach (var selection in selections)
        {
            switch (selection.Value)
            {
                case "cleanup-old":
                    allSuccess &= await SmartCleanupContainersAsync(containers, force);
                    break;
                
                case "remove-all-stopped":
                    var stoppedContainers = containers.Where(c => c.Status == ContainerStatus.Stopped).ToList();
                    allSuccess &= await RemoveMultipleContainersAsync(stoppedContainers, force);
                    break;
                
                default:
                    // 删除特定容器
                    var container = containers.FirstOrDefault(c => c.ContainerName == selection.Value);
                    if (container != null)
                    {
                        allSuccess &= await RemoveContainerAsync(container, force);
                    }
                    break;
            }
        }

        return allSuccess;
    }

    /// <summary>
    /// 智能清理容器 - 基于deck-shell的clean_old_images逻辑
    /// </summary>
    private async Task<bool> SmartCleanupContainersAsync(List<RemovableContainer> containers, bool force)
    {
        try
        {
            _consoleDisplay.ShowInfo("🧹 开始智能清理...");

            // 按项目前缀分组
            var groupedContainers = containers
                .GroupBy(c => GetProjectPrefix(c.Name))
                .ToList();

            var allSuccess = true;
            var totalRemoved = 0;

            foreach (var group in groupedContainers)
            {
                var projectContainers = group.OrderByDescending(c => c.CreatedTime).ToList();
                
                if (projectContainers.Count <= 1)
                {
                    // 只有一个容器，跳过
                    continue;
                }

                // 保留最新的，删除其余的
                var containersToRemove = projectContainers.Skip(1).ToList();
                
                _consoleDisplay.ShowInfo($"📦 项目 '{group.Key}': 保留最新容器，删除 {containersToRemove.Count} 个旧容器");

                foreach (var container in containersToRemove)
                {
                    if (await RemoveContainerAsync(container, force, false))
                    {
                        totalRemoved++;
                    }
                    else
                    {
                        allSuccess = false;
                    }
                }
            }

            if (totalRemoved > 0)
            {
                _consoleDisplay.ShowSuccess($"🎉 智能清理完成，已删除 {totalRemoved} 个容器");
            }
            else
            {
                _consoleDisplay.ShowInfo("✨ 无需清理，所有项目都只有最新容器");
            }

            return allSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart cleanup failed");
            _consoleDisplay.ShowError($"❌ 智能清理失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除指定名称的容器
    /// </summary>
    private async Task<bool> RemoveSpecificContainerAsync(string containerName, bool force)
    {
        var containers = await GetRemovableContainersAsync();
        var container = containers.FirstOrDefault(c => 
            c.ContainerName.Equals(containerName, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));

        if (container == null)
        {
            _consoleDisplay.ShowError($"❌ 容器 '{containerName}' 未找到或不可删除");
            _consoleDisplay.ShowInfo("💡 使用 'deck ps' 查看可用容器");
            _consoleDisplay.ShowInfo("💡 只能删除已停止或已创建状态的容器");
            return false;
        }

        return await RemoveContainerAsync(container, force);
    }

    /// <summary>
    /// 删除所有容器
    /// </summary>
    private async Task<bool> RemoveAllContainersAsync(bool force)
    {
        _consoleDisplay.ShowWarning("⚠️ 即将删除所有容器！");
        
        if (!force)
        {
            var options = new List<SelectableOption>
            {
                new SelectableOption { Value = "yes", DisplayName = "是 - 删除所有容器" },
                new SelectableOption { Value = "no", DisplayName = "否 - 取消删除" }
            };

            var selector = new InteractiveSelector<SelectableOption>
            {
                Prompt = "确定要删除所有容器吗？",
                Items = options
            };
            
            var confirmation = await _interactiveSelection.ShowSingleSelectionAsync(selector);
            
            if (confirmation.IsCancelled || confirmation.SelectedItem?.Value != "yes")
            {
                _consoleDisplay.ShowInfo("❌ 用户取消删除操作");
                return true;
            }
        }

        var containers = await GetRemovableContainersAsync();
        return await RemoveMultipleContainersAsync(containers, force);
    }

    /// <summary>
    /// 删除多个容器
    /// </summary>
    private async Task<bool> RemoveMultipleContainersAsync(List<RemovableContainer> containers, bool force)
    {
        if (!containers.Any())
        {
            _consoleDisplay.ShowInfo("📋 无容器需要删除");
            return true;
        }

        var allSuccess = true;
        var successCount = 0;

        foreach (var container in containers)
        {
            if (await RemoveContainerAsync(container, force, false))
            {
                successCount++;
            }
            else
            {
                allSuccess = false;
            }
        }

        _consoleDisplay.ShowInfo($"📊 删除结果: {successCount}/{containers.Count} 个容器已删除");
        
        if (allSuccess)
        {
            _consoleDisplay.ShowSuccess("🎉 所有容器删除成功");
        }
        else
        {
            _consoleDisplay.ShowWarning("⚠️ 部分容器删除失败");
        }

        return allSuccess;
    }

    /// <summary>
    /// 删除单个容器
    /// </summary>
    private async Task<bool> RemoveContainerAsync(RemovableContainer container, bool force, bool showIndividualResult = true)
    {
        try
        {
            if (showIndividualResult)
            {
                _consoleDisplay.ShowInfo($"🗑️ 删除容器: {container.Name}");
            }

            // 1. 停止容器（如果正在运行）
            if (container.Status == ContainerStatus.Running)
            {
                _consoleDisplay.ShowInfo("  ⏹️  容器正在运行，先停止...");
                var stopSuccess = await ExecuteCommandAsync($"podman stop {container.ContainerName}");
                
                if (!stopSuccess && !force)
                {
                    _consoleDisplay.ShowError($"  ❌ 无法停止容器 {container.ContainerName}");
                    return false;
                }
            }

            // 2. 删除容器
            var removeCommand = force ? 
                $"podman rm -f {container.ContainerName}" : 
                $"podman rm {container.ContainerName}";

            var removeSuccess = await ExecuteCommandAsync(removeCommand);

            if (!removeSuccess)
            {
                _consoleDisplay.ShowError($"  ❌ 无法删除容器 {container.ContainerName}");
                return false;
            }

            // 3. 删除关联的镜像（如果存在且没有其他容器使用）
            if (await ShouldRemoveImageAsync(container.ImageName))
            {
                _consoleDisplay.ShowInfo("  🖼️  删除关联镜像...");
                await ExecuteCommandAsync($"podman rmi {container.ImageName}");
            }

            if (showIndividualResult)
            {
                _consoleDisplay.ShowSuccess($"  ✅ 容器 {container.Name} 删除成功");
            }

            _logger.LogInformation("Container removed successfully: {Container}", container.ContainerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container: {Container}", container.ContainerName);
            _consoleDisplay.ShowError($"  ❌ 删除容器失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取可删除的容器列表
    /// </summary>
    private async Task<List<RemovableContainer>> GetRemovableContainersAsync()
    {
        try
        {
            var containers = new List<RemovableContainer>();

            // 获取所有容器状态
            var allContainersOutput = await ExecuteCommandWithOutputAsync("podman ps -a --format \"{{.Names}},{{.Image}},{{.Status}},{{.CreatedAt}}\"");
            
            if (string.IsNullOrWhiteSpace(allContainersOutput))
            {
                return containers;
            }

            var lines = allContainersOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    var containerName = parts[0].Trim();
                    var imageName = parts[1].Trim();
                    var status = parts[2].Trim().ToLowerInvariant();
                    var createdAt = parts.Length > 3 ? parts[3].Trim() : "";

                    // 只包含可删除的容器（非运行状态）
                    var containerStatus = ParseContainerStatus(status);
                    if (containerStatus != ContainerStatus.Running)
                    {
                        containers.Add(new RemovableContainer
                        {
                            Name = ExtractProjectName(containerName),
                            ContainerName = containerName,
                            ImageName = imageName,
                            Status = containerStatus,
                            CreatedTime = ParseCreatedTime(createdAt)
                        });
                    }
                }
            }

            return containers.OrderBy(c => c.Name).ThenByDescending(c => c.CreatedTime).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get removable containers");
            return new List<RemovableContainer>();
        }
    }

    /// <summary>
    /// 判断是否应该删除镜像
    /// </summary>
    private async Task<bool> ShouldRemoveImageAsync(string imageName)
    {
        try
        {
            // 检查是否有其他容器使用此镜像
            var containersUsingImage = await ExecuteCommandWithOutputAsync($"podman ps -a --filter \"ancestor={imageName}\" --format \"{{.Names}}\"");
            
            return string.IsNullOrWhiteSpace(containersUsingImage);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 执行命令并返回是否成功
    /// </summary>
    private async Task<bool> ExecuteCommandAsync(string command)
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
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to execute command: {Command}", command);
            return false;
        }
    }

    /// <summary>
    /// 执行命令并返回输出
    /// </summary>
    private async Task<string> ExecuteCommandWithOutputAsync(string command)
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
    /// 解析容器状态
    /// </summary>
    private ContainerStatus ParseContainerStatus(string status)
    {
        if (status.Contains("up") || status.Contains("running"))
            return ContainerStatus.Running;
        if (status.Contains("exited") || status.Contains("stopped"))
            return ContainerStatus.Stopped;
        if (status.Contains("created"))
            return ContainerStatus.Created;
        
        return ContainerStatus.Unknown;
    }

    /// <summary>
    /// 解析创建时间
    /// </summary>
    private DateTime ParseCreatedTime(string createdAt)
    {
        if (string.IsNullOrWhiteSpace(createdAt))
            return DateTime.MinValue;

        if (DateTime.TryParse(createdAt, out var date))
            return date;

        return DateTime.MinValue;
    }

    /// <summary>
    /// 从容器名称提取项目名称
    /// </summary>
    private string ExtractProjectName(string containerName)
    {
        // 移除时间戳后缀 (如: project-name-20241215)
        var parts = containerName.Split('-');
        if (parts.Length > 1 && parts.Last().All(char.IsDigit) && parts.Last().Length == 8)
        {
            return string.Join("-", parts.Take(parts.Length - 1));
        }
        
        return containerName;
    }

    /// <summary>
    /// 获取项目前缀
    /// </summary>
    private string GetProjectPrefix(string name)
    {
        // 提取项目类型前缀
        return name.Split('-').FirstOrDefault() ?? name;
    }

    /// <summary>
    /// 格式化容器选择显示
    /// </summary>
    private string FormatContainerForSelection(RemovableContainer container)
    {
        var statusIcon = container.Status switch
        {
            ContainerStatus.Stopped => "🟡",
            ContainerStatus.Created => "🔵",
            _ => "⚪"
        };

        var timeInfo = container.CreatedTime == DateTime.MinValue ? 
            "Unknown" : 
            container.CreatedTime.ToString("MM-dd HH:mm");

        return $"{statusIcon} {container.Name} ({container.Status}, {timeInfo})";
    }

    /// <summary>
    /// 获取容器描述
    /// </summary>
    private string GetContainerDescription(RemovableContainer container)
    {
        return $"容器名: {container.ContainerName}, 镜像: {container.ImageName}";
    }

    #endregion
}

/// <summary>
/// 可删除的容器信息
/// </summary>
public class RemovableContainer
{
    public string Name { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; }
    public DateTime CreatedTime { get; set; }
}