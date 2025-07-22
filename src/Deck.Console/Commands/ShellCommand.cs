using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Deck.Console.Commands;

/// <summary>
/// 进入容器Shell命令
/// </summary>
public class ShellCommand : ContainerCommandBase
{
    public ShellCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagement)
        : base(consoleDisplay, interactiveSelection, loggingService, directoryManagement)
    {
    }

    /// <summary>
    /// 执行进入容器Shell命令
    /// </summary>
    public override async Task<bool> ExecuteAsync(string? imageName, CancellationToken cancellationToken = default)
    {
        var logger = LoggingService.GetLogger("Deck.Console.Shell");
        
        try
        {
            // 获取镜像名称
            var selectedImageName = await GetOrSelectImageNameAsync(imageName, "Shell");
            if (string.IsNullOrEmpty(selectedImageName))
            {
                ConsoleDisplay.ShowError("没有选择镜像，进入Shell操作取消。");
                return false;
            }

            // 验证镜像存在
            if (!await ValidateImageExistsAsync(selectedImageName))
            {
                ConsoleDisplay.ShowError($"镜像 '{selectedImageName}' 不存在。");
                return false;
            }

            logger.LogInformation("Starting shell command for image: {ImageName}", selectedImageName);

            // 显示进入Shell信息
            ConsoleDisplay.ShowInfo($"💻 正在进入 '{selectedImageName}' 容器环境...");

            // 首先检查容器是否运行
            var containerName = await GetContainerNameAsync(selectedImageName);
            if (string.IsNullOrEmpty(containerName))
            {
                ConsoleDisplay.ShowError($"无法确定容器名称或容器未运行");
                return false;
            }

            // 检查容器是否在运行
            var isRunning = await CheckContainerRunningAsync(containerName);
            if (!isRunning)
            {
                ConsoleDisplay.ShowWarning($"容器 '{containerName}' 没有运行");
                ConsoleDisplay.ShowInfo("💡 提示: 请先使用 'deck start " + selectedImageName + "' 启动容器");
                return false;
            }

            // 显示教育性的 Podman 命令提示
            ShowPodmanHint(selectedImageName, "shell");

            // 执行Shell命令
            var result = await ExecuteShellCommandAsync(selectedImageName, containerName, cancellationToken);

            if (result)
            {
                ConsoleDisplay.ShowSuccess($"✅ 已退出 '{selectedImageName}' 容器环境");
                logger.LogInformation("Shell command completed successfully for image: {ImageName}", selectedImageName);
            }
            else
            {
                ConsoleDisplay.ShowError($"❌ 进入容器环境失败");
                logger.LogWarning("Shell command failed for image: {ImageName}", selectedImageName);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Shell command execution failed");
            ConsoleDisplay.ShowError($"执行Shell命令时出错: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取容器名称
    /// </summary>
    private async Task<string?> GetContainerNameAsync(string imageName)
    {
        try
        {
            var imagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deck", "images");
            var imagePath = Path.Combine(imagesDir, imageName);
            var composePath = Path.Combine(imagePath, "compose.yaml");

            if (!File.Exists(composePath))
            {
                return null;
            }

            // 方法1: 尝试使用 podman-compose config --services 获取服务名
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = $"-f \"{composePath}\" config --services",
                WorkingDirectory = imagePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var services = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (services.Length > 0)
                {
                    return services[0].Trim(); // 使用第一个服务
                }
            }

            // 方法2: 使用约定的容器名称
            return $"{imageName}-dev";
        }
        catch
        {
            // 如果出错，使用默认约定
            return $"{imageName}-dev";
        }
    }

    /// <summary>
    /// 检查容器是否在运行
    /// </summary>
    private async Task<bool> CheckContainerRunningAsync(string containerName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman",
                Arguments = $"ps -q -f name={containerName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                return !string.IsNullOrWhiteSpace(output);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 执行实际的Shell命令
    /// </summary>
    private async Task<bool> ExecuteShellCommandAsync(string imageName, string containerName, CancellationToken cancellationToken)
    {
        try
        {
            var imagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deck", "images");
            var imagePath = Path.Combine(imagesDir, imageName);
            var composePath = Path.Combine(imagePath, "compose.yaml");

            // 显示进入提示
            ConsoleDisplay.ShowInfo($"🚀 进入容器开发环境: {containerName}");
            ConsoleDisplay.WriteLine($"   工作目录: /workspace");
            ConsoleDisplay.WriteLine($"   退出方式: 输入 'exit' 或按 Ctrl+D");
            ConsoleDisplay.WriteLine();

            // 方法1: 优先使用 podman-compose exec
            var startInfo = new ProcessStartInfo
            {
                FileName = "podman-compose",
                Arguments = $"-f \"{composePath}\" exec {containerName} bash",
                WorkingDirectory = imagePath,
                UseShellExecute = false,
                CreateNoWindow = false // 允许交互式终端
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            // 如果 podman-compose exec 失败，尝试直接使用 podman exec
            if (process.ExitCode != 0)
            {
                ConsoleDisplay.ShowWarning("使用 podman-compose exec 失败，尝试直接使用 podman exec...");
                
                var fallbackStartInfo = new ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = $"exec -it {containerName} bash",
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                using var fallbackProcess = new Process { StartInfo = fallbackStartInfo };
                fallbackProcess.Start();
                await fallbackProcess.WaitForExitAsync(cancellationToken);
                
                return fallbackProcess.ExitCode == 0;
            }

            return process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            ConsoleDisplay.ShowInfo("\nShell会话已取消");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.ShowError($"执行Shell命令失败: {ex.Message}");
            
            // 给用户一些建议
            ConsoleDisplay.ShowInfo("\n💡 故障排除建议:");
            ConsoleDisplay.WriteLine("  - 确保容器正在运行: deck start " + imageName);
            ConsoleDisplay.WriteLine("  - 检查容器状态: podman ps");
            ConsoleDisplay.WriteLine($"  - 手动进入容器: podman exec -it {containerName} bash");
            ConsoleDisplay.WriteLine($"  - 或使用: podman-compose -f ~/.deck/images/{imageName}/compose.yaml exec {containerName} bash");
            
            return false;
        }
    }
}