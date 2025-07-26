using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Deck.Console.Commands;

/// <summary>
/// 查看容器日志命令
/// </summary>
public class LogsCommand : ContainerCommandBase
{
    private readonly IContainerService _containerService;

    public LogsCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement,
        IContainerService containerService)
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
        _containerService = containerService;
    }

    /// <summary>
    /// 执行查看日志命令
    /// </summary>
    public async Task<bool> ExecuteAsync(string? imageName, bool follow = false, CancellationToken cancellationToken = default)
    {
        var logger = LoggingService.GetLogger("Deck.Console.Logs");
        
        try
        {
            // 获取镜像名称
            var selectedImageName = await GetOrSelectImageNameAsync(imageName, "Logs");
            if (string.IsNullOrEmpty(selectedImageName))
            {
                ConsoleDisplay.ShowError("没有选择镜像，查看日志操作取消。");
                return false;
            }

            // 验证镜像存在
            if (!await ValidateImageExistsAsync(selectedImageName))
            {
                ConsoleDisplay.ShowError($"镜像 '{selectedImageName}' 不存在。");
                return false;
            }

            logger.LogInformation("Starting logs command for image: {ImageName}, follow: {Follow}", selectedImageName, follow);

            // 显示日志信息
            if (follow)
            {
                ConsoleDisplay.ShowInfo($"📋 正在实时查看 '{selectedImageName}' 的日志 (按 Ctrl+C 停止):");
            }
            else
            {
                ConsoleDisplay.ShowInfo($"📋 正在查看 '{selectedImageName}' 的日志:");
            }

            // 获取实际的容器名称
            var containerName = await GetActualContainerNameAsync(selectedImageName);
            if (string.IsNullOrEmpty(containerName))
            {
                ConsoleDisplay.ShowError($"无法确定 '{selectedImageName}' 的容器名称");
                return false;
            }

            // 使用容器服务获取日志
            var result = await _containerService.GetContainerLogsAsync(containerName, follow ? 0 : 100); // 如果follow则获取所有日志，否则获取最后100行

            if (result.Success)
            {
                // 使用颜色分类显示日志
                DisplayColoredLogs(result.Logs);
                
                if (!follow)
                {
                    ConsoleDisplay.ShowSuccess($"✅ 日志查看完成");
                }
                
                // 显示教育性的 Podman 命令提示
                ShowPodmanHint(selectedImageName, "logs", follow ? "-f" : null);
                
                logger.LogInformation("Logs command completed successfully for image: {ImageName}", selectedImageName);
                return true;
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 查看日志失败: {result.Error}");
                logger.LogWarning("Logs command failed for image: {ImageName}. Error: {ErrorMessage}", selectedImageName, result.Error);
                
                // 给用户一些建议
                ConsoleDisplay.ShowInfo("\n💡 提示:");
                ConsoleDisplay.WriteLine("  - 确保容器正在运行: deck start " + selectedImageName);
                ConsoleDisplay.WriteLine("  - 检查容器状态: deck ps");
                ConsoleDisplay.WriteLine($"  - 手动查看日志: podman logs {containerName}");
                
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            ConsoleDisplay.ShowInfo("\n日志查看已取消");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logs command execution failed");
            ConsoleDisplay.ShowError($"执行查看日志命令时出错: {ex.Message}");
            return false;
        }
    }

    public override async Task<bool> ExecuteAsync(string? imageName, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(imageName, false, cancellationToken);
    }

    /// <summary>
    /// 使用颜色分类显示日志
    /// </summary>
    private void DisplayColoredLogs(string logs)
    {
        if (string.IsNullOrEmpty(logs))
            return;

        var lines = logs.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleDisplay.ShowError(line);
            }
            else if (line.Contains("WARN", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleDisplay.ShowWarning(line);
            }
            else if (line.Contains("INFO", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleDisplay.ShowInfo(line);
            }
            else
            {
                ConsoleDisplay.WriteLine(line);
            }
        }
    }

    /// <summary>
    /// 获取实际的容器名称
    /// </summary>
    private async Task<string?> GetActualContainerNameAsync(string imageName)
    {
        try
        {
            // 首先尝试使用镜像名称作为容器名称
            var directNameContainer = await _containerService.GetContainerInfoAsync(imageName);
            if (directNameContainer != null)
            {
                return imageName;
            }

            // 然后尝试使用镜像名-dev格式
            var devName = $"{imageName}-dev";
            var devContainer = await _containerService.GetContainerInfoAsync(devName);
            if (devContainer != null)
            {
                return devName;
            }

            // 如果找不到确切的容器，返回镜像名称作为默认值
            return imageName;
        }
        catch (Exception ex)
        {
            LoggingService.GetLogger("Deck.Console.Logs")
                .LogWarning(ex, "Failed to get actual container name for image: {ImageName}", imageName);
            return imageName; // 回退到镜像名称
        }
    }
}