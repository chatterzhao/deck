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

    public StopCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement,
        IGlobalExceptionHandler globalExceptionHandler) // 添加全局异常处理服务参数
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
        _globalExceptionHandler = globalExceptionHandler; // 初始化全局异常处理服务
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

            // 构建停止命令
            var result = await ExecuteStopCommandAsync(selectedImageName, cancellationToken);

            if (result)
            {
                ConsoleDisplay.ShowSuccess($"✅ 环境 '{selectedImageName}' 已成功停止");
                
                // 显示教育性的 Podman 命令提示
                ShowPodmanHint(selectedImageName, "stop");
                
                logger.LogInformation("Stop command completed successfully for image: {ImageName}", selectedImageName);
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 停止环境 '{selectedImageName}' 失败");
                logger.LogWarning("Stop command failed for image: {ImageName}", selectedImageName);
            }

            return result;
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
    /// 执行实际的停止命令
    /// </summary>
    private async Task<bool> ExecuteStopCommandAsync(string imageName, CancellationToken cancellationToken)
    {
        try
        {
            var imagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deck", "images");
            var imagePath = Path.Combine(imagesDir, imageName);
            var composePath = Path.Combine(imagePath, "compose.yaml");

            if (!File.Exists(composePath))
            {
                ConsoleDisplay.ShowWarning($"未找到 compose.yaml 文件: {composePath}");
                return false;
            }

            // 使用 podman-compose 停止容器
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = $"-f \"{composePath}\" down",
                WorkingDirectory = imagePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            var outputLines = new List<string>();
            var errorLines = new List<string>();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputLines.Add(e.Data);
                    ConsoleDisplay.WriteLine($"    {e.Data}");
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorLines.Add(e.Data);
                    ConsoleDisplay.ShowWarning($"    {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var success = process.ExitCode == 0;

            if (!success && errorLines.Any())
            {
                ConsoleDisplay.ShowError("停止过程中出现错误:");
                foreach (var error in errorLines)
                {
                    ConsoleDisplay.ShowError($"  {error}");
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.ShowError($"执行停止命令失败: {ex.Message}");
            return false;
        }
    }
}