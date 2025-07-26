using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Deck.Console.Commands;

/// <summary>
/// 重启容器命令
/// </summary>
public class RestartCommand : ContainerCommandBase
{
    private readonly IGlobalExceptionHandler _globalExceptionHandler;
    private readonly IContainerService _containerService;

    public RestartCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement,
        IGlobalExceptionHandler globalExceptionHandler,
        IContainerService containerService)
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
        _globalExceptionHandler = globalExceptionHandler;
        _containerService = containerService;
    }

    /// <summary>
    /// 执行重启命令
    /// </summary>
    public override async Task<bool> ExecuteAsync(string? imageName, CancellationToken cancellationToken = default)
    {
        var logger = LoggingService.GetLogger("Deck.Console.Restart");
        
        try
        {
            // 获取镜像名称
            var selectedImageName = await GetOrSelectImageNameAsync(imageName, "Restart");
            if (string.IsNullOrEmpty(selectedImageName))
            {
                ConsoleDisplay.ShowError("没有选择镜像，重启操作取消。");
                return false;
            }

            // 验证镜像存在
            if (!await ValidateImageExistsAsync(selectedImageName))
            {
                ConsoleDisplay.ShowError($"镜像 '{selectedImageName}' 不存在。");
                return false;
            }

            logger.LogInformation("Starting restart command for image: {ImageName}", selectedImageName);

            // 显示重启信息
            ConsoleDisplay.ShowInfo($"🔄 正在重启环境: {selectedImageName}");

            // 获取实际的容器名称
            var containerName = await GetActualContainerNameAsync(selectedImageName);
            if (string.IsNullOrEmpty(containerName))
            {
                ConsoleDisplay.ShowError($"无法确定 '{selectedImageName}' 的容器名称");
                return false;
            }

            // 使用容器服务重启容器
            var result = await _containerService.RestartContainerAsync(containerName);

            if (result.Success)
            {
                ConsoleDisplay.ShowSuccess($"✅ 环境 '{selectedImageName}' 已成功重启");
                
                // 显示教育性的 Podman 命令提示
                ShowPodmanHint(selectedImageName, "restart");
                
                logger.LogInformation("Restart command completed successfully for image: {ImageName}", selectedImageName);
                return true;
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 重启环境 '{selectedImageName}' 失败: {result.Message}");
                logger.LogWarning("Restart command failed for image: {ImageName}. Error: {ErrorMessage}", selectedImageName, result.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Restart command execution failed");
            ConsoleDisplay.ShowError($"执行重启命令时出错: {ex.Message}");
            return false;
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
            LoggingService.GetLogger("Deck.Console.Restart")
                .LogWarning(ex, "Failed to get actual container name for image: {ImageName}", imageName);
            return imageName; // 回退到镜像名称
        }
    }
}