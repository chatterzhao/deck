using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Deck.Console.Commands;

/// <summary>
/// 重启容器命令
/// </summary>
public class RestartCommand : ContainerCommandBase
{
    private readonly StopCommand _stopCommand;

    public RestartCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement,
        IGlobalExceptionHandler globalExceptionHandler)
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
        _stopCommand = new StopCommand(consoleDisplay, interactiveSelection, loggingService, directoryManagement, globalExceptionHandler);
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

            // 方法1: 直接使用 podman-compose restart 命令
            var result = await ExecuteRestartCommandAsync(selectedImageName, cancellationToken);

            if (result)
            {
                ConsoleDisplay.ShowSuccess($"✅ 环境 '{selectedImageName}' 已成功重启");
                
                // 显示教育性的 Podman 命令提示
                ShowPodmanHint(selectedImageName, "restart");
                
                logger.LogInformation("Restart command completed successfully for image: {ImageName}", selectedImageName);
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 重启环境 '{selectedImageName}' 失败");
                logger.LogWarning("Restart command failed for image: {ImageName}", selectedImageName);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Restart command execution failed");
            ConsoleDisplay.ShowError($"执行重启命令时出错: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行实际的重启命令
    /// </summary>
    private async Task<bool> ExecuteRestartCommandAsync(string imageName, CancellationToken cancellationToken)
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

            // 使用 podman-compose 重启容器
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = $"-f \"{composePath}\" restart",
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
                ConsoleDisplay.ShowError("重启过程中出现错误:");
                foreach (var error in errorLines)
                {
                    ConsoleDisplay.ShowError($"  {error}");
                }
                
                // 如果直接重启失败，尝试先停止再启动的方法
                ConsoleDisplay.ShowInfo("尝试使用停止然后启动的方式重启...");
                
                return await FallbackRestartAsync(imageName, cancellationToken);
            }

            return success;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.ShowError($"执行重启命令失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 备用重启方法：先停止后启动
    /// </summary>
    private async Task<bool> FallbackRestartAsync(string imageName, CancellationToken cancellationToken)
    {
        try
        {
            var imagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deck", "images");
            var imagePath = Path.Combine(imagesDir, imageName);
            var composePath = Path.Combine(imagePath, "compose.yaml");

            // 停止
            ConsoleDisplay.ShowInfo("正在停止容器...");
            var stopInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = $"-f \"{composePath}\" down",
                WorkingDirectory = imagePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var stopProcess = new Process { StartInfo = stopInfo })
            {
                stopProcess.Start();
                await stopProcess.WaitForExitAsync(cancellationToken);
                
                if (stopProcess.ExitCode != 0)
                {
                    ConsoleDisplay.ShowWarning("停止过程可能有问题，继续尝试启动...");
                }
            }

            // 等待一下确保完全停止
            await Task.Delay(2000, cancellationToken);

            // 启动
            ConsoleDisplay.ShowInfo("正在启动容器...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = $"-f \"{composePath}\" up -d",
                WorkingDirectory = imagePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var startProcess = new Process { StartInfo = startInfo };
            
            startProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ConsoleDisplay.WriteLine($"    {e.Data}");
                }
            };

            startProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ConsoleDisplay.ShowWarning($"    {e.Data}");
                }
            };

            startProcess.Start();
            startProcess.BeginOutputReadLine();
            startProcess.BeginErrorReadLine();
            
            await startProcess.WaitForExitAsync(cancellationToken);

            return startProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.ShowError($"备用重启方法失败: {ex.Message}");
            return false;
        }
    }
}