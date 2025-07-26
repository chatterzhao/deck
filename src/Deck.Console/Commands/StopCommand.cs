using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Deck.Console.Commands;

/// <summary>
/// 停止容器命令
/// </summary>
public class StopCommand : ContainerCommandBase
{
    private readonly IGlobalExceptionHandler _globalExceptionHandler; // 添加全局异常处理服务
    private readonly IContainerService _containerService; // 添加容器服务

    public StopCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement,
        IGlobalExceptionHandler globalExceptionHandler,
        IContainerService containerService) // 添加容器服务参数
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
        _globalExceptionHandler = globalExceptionHandler; // 初始化全局异常处理服务
        _containerService = containerService; // 初始化容器服务
    }

    /// <summary>
    /// 执行停止命令
    /// </summary>
    public override async Task<bool> ExecuteAsync(string? imageName, CancellationToken cancellationToken = default)
    {
        var logger = LoggingService.GetLogger("Deck.Console.Stop");
        
        try
        {
            // 获取镜像名称
            var selectedImageName = await GetOrSelectImageNameAsync(imageName, "Stop");
            if (string.IsNullOrEmpty(selectedImageName))
            {
                ConsoleDisplay.ShowError("没有选择镜像，停止操作取消。");
                return false;
            }

            // 验证镜像存在
            if (!await ValidateImageExistsAsync(selectedImageName))
            {
                ConsoleDisplay.ShowError($"镜像 '{selectedImageName}' 不存在。");
                return false;
            }

            logger.LogInformation("Starting stop command for image: {ImageName}", selectedImageName);

            // 显示停止信息
            ConsoleDisplay.ShowInfo($"⏹️  正在停止环境: {selectedImageName}");

            // 获取实际的容器名称
            var containerName = await GetActualContainerNameAsync(selectedImageName);
            if (string.IsNullOrEmpty(containerName))
            {
                ConsoleDisplay.ShowError($"无法确定 '{selectedImageName}' 的容器名称");
                return false;
            }

            // 使用容器服务停止容器
            var result = await _containerService.StopContainerAsync(containerName);

            if (result.Success)
            {
                ConsoleDisplay.ShowSuccess($"✅ 环境 '{selectedImageName}' 已成功停止");
                
                // 显示教育性的 Podman 命令提示
                ShowPodmanHint(selectedImageName, "stop");
                
                logger.LogInformation("Stop command completed successfully for image: {ImageName}", selectedImageName);
                return true;
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 停止环境 '{selectedImageName}' 失败: {result.Message}");
                logger.LogWarning("Stop command failed for image: {ImageName}. Error: {ErrorMessage}", selectedImageName, result.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            // 使用全局异常处理服务处理异常
            var context = new ExceptionContext
            {
                CommandName = "Stop",
                Operation = "执行Stop命令",
                ResourcePath = imageName
            };
            
            var result = await _globalExceptionHandler.HandleExceptionAsync(ex, context);
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
            LoggingService.GetLogger("Deck.Console.Stop")
                .LogWarning(ex, "Failed to get actual container name for image: {ImageName}", imageName);
            return imageName; // 回退到镜像名称
        }
    }

}