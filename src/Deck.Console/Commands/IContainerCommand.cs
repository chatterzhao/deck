using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Console.Commands;

/// <summary>
/// 容器命令接口
/// </summary>
public interface IContainerCommand
{
    /// <summary>
    /// 执行容器命令
    /// </summary>
    /// <param name="imageName">镜像名称，如果为null则进行交互式选择</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<bool> ExecuteAsync(string? imageName, CancellationToken cancellationToken = default);
}

/// <summary>
/// 容器命令基类
/// </summary>
public abstract class ContainerCommandBase : IContainerCommand
{
    protected readonly IConsoleDisplay ConsoleDisplay;
    protected readonly IInteractiveSelectionService InteractiveSelection;
    protected readonly ILoggingService LoggingService;
    protected readonly IDirectoryManagementService DirectoryManagement;

    protected ContainerCommandBase(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement)
    {
        ConsoleDisplay = consoleDisplay;
        InteractiveSelection = interactiveSelection;
        LoggingService = loggingService;
        DirectoryManagement = directoryManagement;
    }

    /// <summary>
    /// 执行容器命令
    /// </summary>
    public abstract Task<bool> ExecuteAsync(string? imageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取或选择镜像名称
    /// </summary>
    protected async Task<string?> GetOrSelectImageNameAsync(string? imageName, string commandName)
    {
        if (!string.IsNullOrEmpty(imageName))
        {
            return imageName;
        }

        var logger = LoggingService.GetLogger($"Deck.Console.{commandName}");
        logger.LogInformation("No image name provided, starting interactive selection");

        try
        {
            // 获取可用镜像列表
            var images = await DirectoryManagement.GetImagesAsync();
            
            if (!images.Any())
            {
                ConsoleDisplay.ShowWarning("没有找到任何镜像配置。请先使用 'deck start' 命令创建镜像。");
                return null;
            }

            // 交互式选择
            ConsoleDisplay.ShowInfo($"选择要{GetChineseCommandName(commandName)}的镜像：");
            
            var options = images.Select(img => new SelectableOption
            {
                DisplayName = img.Name,
                Value = img.Name,
                Description = $"{img.Metadata?.BuildStatus.ToString() ?? "Unknown"} - {img.Metadata?.ContainerName ?? img.Name}",
                ExtraInfo = img.LastModified?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown",
                IsAvailable = true
            }).ToList();

            var selector = new InteractiveSelector<SelectableOption>
            {
                Prompt = $"请选择要{GetChineseCommandName(commandName)}的镜像",
                Items = options,
                AllowMultiple = false,
                Required = true,
                EnableSearch = true,
                SearchPlaceholder = "输入镜像名称进行搜索..."
            };

            var result = await InteractiveSelection.ShowSingleSelectionAsync(selector);
            return !result.IsCancelled ? result.SelectedItems.FirstOrDefault()?.Value : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interactive image selection failed");
            ConsoleDisplay.ShowError($"交互式选择失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取命令的中文名称
    /// </summary>
    private static string GetChineseCommandName(string commandName) => commandName.ToLower() switch
    {
        "stop" => "停止",
        "restart" => "重启",
        "logs" => "查看日志",
        "shell" => "进入",
        _ => commandName
    };

    /// <summary>
    /// 验证镜像是否存在
    /// </summary>
    protected async Task<bool> ValidateImageExistsAsync(string imageName)
    {
        try
        {
            var image = await DirectoryManagement.GetImageByNameAsync(imageName);
            return image != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 显示Podman命令提示
    /// </summary>
    protected void ShowPodmanHint(string imageName, string operation, string? additionalCommand = null)
    {
        ConsoleDisplay.ShowInfo("💡 等效的 Podman 命令：");
        
        var commands = new List<string>();
        
        switch (operation.ToLower())
        {
            case "stop":
                commands.Add($"podman-compose -f ~/.deck/images/{imageName}/compose.yaml down");
                break;
            case "restart":
                commands.Add($"podman-compose -f ~/.deck/images/{imageName}/compose.yaml restart");
                break;
            case "logs":
                commands.Add($"podman-compose -f ~/.deck/images/{imageName}/compose.yaml logs");
                if (!string.IsNullOrEmpty(additionalCommand))
                {
                    commands.Add($"podman-compose -f ~/.deck/images/{imageName}/compose.yaml logs -f");
                }
                break;
            case "shell":
                commands.Add($"podman-compose -f ~/.deck/images/{imageName}/compose.yaml exec <service-name> bash");
                commands.Add($"# 或直接使用容器名:");
                commands.Add($"podman exec -it {imageName}-dev bash");
                break;
        }

        foreach (var cmd in commands)
        {
            ConsoleDisplay.WriteLine($"  {cmd}");
        }
        
        ConsoleDisplay.WriteLine();
    }
}