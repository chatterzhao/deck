using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Deck.Console.Commands;

/// <summary>
/// 查看容器日志命令
/// </summary>
public class LogsCommand : ContainerCommandBase
{
    public LogsCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement)
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
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

            // 执行日志命令
            var result = await ExecuteLogsCommandAsync(selectedImageName, follow, cancellationToken);

            if (result)
            {
                if (!follow)
                {
                    ConsoleDisplay.ShowSuccess($"✅ 日志查看完成");
                }
                
                // 显示教育性的 Podman 命令提示
                ShowPodmanHint(selectedImageName, "logs", follow ? "-f" : null);
                
                logger.LogInformation("Logs command completed successfully for image: {ImageName}", selectedImageName);
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 查看日志失败");
                logger.LogWarning("Logs command failed for image: {ImageName}", selectedImageName);
            }

            return result;
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
    /// 执行实际的日志查看命令
    /// </summary>
    private async Task<bool> ExecuteLogsCommandAsync(string imageName, bool follow, CancellationToken cancellationToken)
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

            // 构建命令参数
            var arguments = $"-f \"{composePath}\" logs";
            if (follow)
            {
                arguments += " -f";
            }

            // 使用 podman-compose 查看日志
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = arguments,
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
                    // 日志输出使用不同颜色显示
                    if (e.Data.Contains("ERROR") || e.Data.Contains("FATAL"))
                    {
                        ConsoleDisplay.ShowError(e.Data);
                    }
                    else if (e.Data.Contains("WARN"))
                    {
                        ConsoleDisplay.ShowWarning(e.Data);
                    }
                    else if (e.Data.Contains("INFO"))
                    {
                        ConsoleDisplay.ShowInfo(e.Data);
                    }
                    else
                    {
                        ConsoleDisplay.WriteLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorLines.Add(e.Data);
                    ConsoleDisplay.ShowError(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 用户取消时终止进程
                if (!process.HasExited)
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
                throw;
            }

            var success = process.ExitCode == 0;

            if (!success && errorLines.Any())
            {
                ConsoleDisplay.ShowError("查看日志过程中出现错误:");
                foreach (var error in errorLines)
                {
                    ConsoleDisplay.ShowError($"  {error}");
                }
                
                // 给用户一些建议
                ConsoleDisplay.ShowInfo("\n💡 提示:");
                ConsoleDisplay.WriteLine("  - 确保容器正在运行: deck start " + imageName);
                ConsoleDisplay.WriteLine("  - 检查容器状态: podman ps");
                ConsoleDisplay.WriteLine($"  - 手动查看日志: podman-compose -f ~/.deck/images/{imageName}/compose.yaml logs");
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            throw; // 重新抛出取消异常
        }
        catch (Exception ex)
        {
            ConsoleDisplay.ShowError($"执行日志命令失败: {ex.Message}");
            return false;
        }
    }
}