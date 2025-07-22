using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Deck.Console.Commands;

/// <summary>
/// Install命令 - 自动安装系统组件（如Podman）
/// 基于deck-shell的install_podman实现，支持多平台自动安装
/// </summary>
public class InstallCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly ISystemDetectionService _systemDetectionService;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly ILoggingService _loggingService;
    private readonly ILogger _logger;

    public InstallCommand(
        IConsoleDisplay consoleDisplay,
        ISystemDetectionService systemDetectionService,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService)
    {
        _consoleDisplay = consoleDisplay;
        _systemDetectionService = systemDetectionService;
        _interactiveSelection = interactiveSelection;
        _loggingService = loggingService;
        _logger = _loggingService.GetLogger("Deck.Console.InstallCommand");
    }

    /// <summary>
    /// 执行组件安装
    /// </summary>
    public async Task<bool> ExecuteAsync(string component)
    {
        try
        {
            _logger.LogInformation("Starting install command for component: {Component}", component);

            if (string.IsNullOrWhiteSpace(component))
            {
                _consoleDisplay.ShowError("❌ 请指定要安装的组件");
                ShowUsage();
                return false;
            }

            return component.ToLowerInvariant() switch
            {
                "podman" => await InstallPodmanAsync(),
                _ => HandleUnsupportedComponent(component)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install command execution failed for component: {Component}", component);
            _consoleDisplay.ShowError($"❌ 安装组件失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 安装Podman - 基于deck-shell的install_podman实现
    /// </summary>
    private async Task<bool> InstallPodmanAsync()
    {
        try
        {
            _consoleDisplay.ShowInfo("📦 Podman 自动安装工具");
            _consoleDisplay.WriteLine();

            // 1. 检查是否已安装
            var containerEngine = await _systemDetectionService.DetectContainerEngineAsync();
            if (containerEngine.IsAvailable && containerEngine.Type == ContainerEngineType.Podman)
            {
                _consoleDisplay.ShowSuccess($"✅ Podman 已安装 (版本: {containerEngine.Version})");
                _consoleDisplay.ShowInfo("💡 无需重复安装");
                return true;
            }

            // 2. 检查系统支持
            var systemInfo = await _systemDetectionService.GetSystemInfoAsync();
            var installCommand = GetPodmanInstallCommand(systemInfo);

            if (installCommand == null)
            {
                _consoleDisplay.ShowError("❌ 当前系统不支持自动安装 Podman");
                ShowManualInstallInstructions(systemInfo);
                return false;
            }

            // 3. 显示安装信息
            _consoleDisplay.ShowInfo($"📍 检测到系统: {systemInfo.OperatingSystem} {systemInfo.Architecture}");
            _consoleDisplay.ShowInfo($"🔧 将使用包管理器: {installCommand.PackageManager}");
            _consoleDisplay.WriteLine();

            // 4. 用户确认
            if (!await ConfirmInstallationAsync(installCommand))
            {
                _consoleDisplay.ShowInfo("❌ 用户取消安装");
                return false;
            }

            // 5. 执行安装
            _consoleDisplay.ShowInfo("🚀 开始安装 Podman...");
            var installSuccess = await ExecuteInstallCommandAsync(installCommand);

            if (!installSuccess)
            {
                _consoleDisplay.ShowError("❌ Podman 安装失败");
                ShowTroubleshootingInfo();
                return false;
            }

            // 6. 验证安装
            _consoleDisplay.ShowInfo("🔍 验证 Podman 安装...");
            var postInstallCheck = await _systemDetectionService.DetectContainerEngineAsync();

            if (!postInstallCheck.IsAvailable || postInstallCheck.Type != ContainerEngineType.Podman)
            {
                _consoleDisplay.ShowWarning("⚠️ Podman 安装完成但未能正确检测到");
                _consoleDisplay.ShowInfo("💡 请重新打开终端后再试");
                return true; // 安装可能成功，只是需要重新加载环境
            }

            // 7. 初始化 Podman Machine (仅限 macOS/Windows)
            if (systemInfo.OperatingSystem != OperatingSystemType.Linux)
            {
                _consoleDisplay.ShowInfo("⚙️ 初始化 Podman Machine...");
                await InitializePodmanMachineAsync();
            }

            _consoleDisplay.ShowSuccess($"🎉 Podman 安装成功！(版本: {postInstallCheck.Version})");
            _consoleDisplay.ShowInfo("💡 现在可以运行 'deck doctor' 检查环境状态");
            _consoleDisplay.ShowInfo("💡 或直接运行 'deck start' 开始使用");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Podman installation failed");
            _consoleDisplay.ShowError($"❌ Podman 安装过程中出现异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取Podman安装命令 - 基于deck-shell的get_podman_install_command
    /// </summary>
    private PodmanInstallCommand? GetPodmanInstallCommand(SystemInfo systemInfo)
    {
        return systemInfo.OperatingSystem switch
        {
            OperatingSystemType.MacOS => GetMacOSInstallCommand(),
            OperatingSystemType.Linux => GetLinuxInstallCommand(),
            OperatingSystemType.Windows => GetWindowsInstallCommand(),
            _ => null
        };
    }

    /// <summary>
    /// 获取macOS安装命令
    /// </summary>
    private PodmanInstallCommand? GetMacOSInstallCommand()
    {
        // 优先检查 Homebrew
        if (IsCommandAvailable("brew"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Homebrew",
                Command = "brew install podman",
                RequiresAdmin = false
            };
        }

        // 检查 MacPorts
        if (IsCommandAvailable("port"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "MacPorts",
                Command = "sudo port install podman",
                RequiresAdmin = true
            };
        }

        return null;
    }

    /// <summary>
    /// 获取Linux安装命令
    /// </summary>
    private PodmanInstallCommand? GetLinuxInstallCommand()
    {
        // APT (Ubuntu/Debian)
        if (IsCommandAvailable("apt"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "APT",
                Command = "sudo apt update && sudo apt install -y podman",
                RequiresAdmin = true
            };
        }

        // DNF (Fedora)
        if (IsCommandAvailable("dnf"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "DNF",
                Command = "sudo dnf install -y podman",
                RequiresAdmin = true
            };
        }

        // YUM (CentOS/RHEL)
        if (IsCommandAvailable("yum"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "YUM",
                Command = "sudo yum install -y podman",
                RequiresAdmin = true
            };
        }

        // Zypper (openSUSE)
        if (IsCommandAvailable("zypper"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Zypper",
                Command = "sudo zypper install -y podman",
                RequiresAdmin = true
            };
        }

        // Pacman (Arch Linux)
        if (IsCommandAvailable("pacman"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Pacman",
                Command = "sudo pacman -S --noconfirm podman",
                RequiresAdmin = true
            };
        }

        return null;
    }

    /// <summary>
    /// 获取Windows安装命令
    /// </summary>
    private PodmanInstallCommand? GetWindowsInstallCommand()
    {
        // Chocolatey
        if (IsCommandAvailable("choco"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Chocolatey",
                Command = "choco install podman-desktop -y",
                RequiresAdmin = true
            };
        }

        // Scoop
        if (IsCommandAvailable("scoop"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Scoop",
                Command = "scoop install podman",
                RequiresAdmin = false
            };
        }

        // WinGet
        if (IsCommandAvailable("winget"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "WinGet",
                Command = "winget install RedHat.Podman",
                RequiresAdmin = false
            };
        }

        return null;
    }

    /// <summary>
    /// 用户确认安装
    /// </summary>
    private async Task<bool> ConfirmInstallationAsync(PodmanInstallCommand installCommand)
    {
        _consoleDisplay.ShowInfo("即将执行的安装命令:");
        _consoleDisplay.ShowInfo($"  {installCommand.Command}");
        
        if (installCommand.RequiresAdmin)
        {
            _consoleDisplay.ShowWarning("⚠️ 此操作需要管理员权限");
        }

        _consoleDisplay.WriteLine();
        
        var options = new List<SelectableOption>
        {
            new SelectableOption { Value = "yes", DisplayName = "是 - 开始安装" },
            new SelectableOption { Value = "no", DisplayName = "否 - 取消安装" }
        };

        var selector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "是否继续安装 Podman？",
            Items = options
        };
        
        var result = await _interactiveSelection.ShowSingleSelectionAsync(selector);
        
        return !result.IsCancelled && result.SelectedItem?.Value == "yes";
    }

    /// <summary>
    /// 执行安装命令
    /// </summary>
    private async Task<bool> ExecuteInstallCommandAsync(PodmanInstallCommand installCommand)
    {
        try
        {
            using var process = new Process();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {installCommand.Command}";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{installCommand.Command}\"";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            _logger.LogInformation("Executing install command: {Command}", installCommand.Command);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Install command completed successfully");
                if (!string.IsNullOrEmpty(output))
                {
                    _consoleDisplay.ShowInfo("安装输出:");
                    _consoleDisplay.ShowInfo(output);
                }
                return true;
            }
            else
            {
                _logger.LogError("Install command failed with exit code: {ExitCode}", process.ExitCode);
                _consoleDisplay.ShowError($"安装失败 (退出码: {process.ExitCode})");
                if (!string.IsNullOrEmpty(error))
                {
                    _consoleDisplay.ShowError($"错误信息: {error}");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute install command");
            _consoleDisplay.ShowError($"执行安装命令失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 初始化 Podman Machine
    /// </summary>
    private async Task InitializePodmanMachineAsync()
    {
        try
        {
            // 1. 初始化 machine
            _consoleDisplay.ShowInfo("🔧 初始化 Podman Machine...");
            await ExecuteCommandAsync("podman machine init");

            // 2. 启动 machine
            _consoleDisplay.ShowInfo("🚀 启动 Podman Machine...");
            await ExecuteCommandAsync("podman machine start");

            _consoleDisplay.ShowSuccess("✅ Podman Machine 初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Podman machine initialization failed");
            _consoleDisplay.ShowWarning("⚠️ Podman Machine 初始化失败，可能需要手动操作");
            _consoleDisplay.ShowInfo("💡 请尝试手动运行: podman machine init && podman machine start");
        }
    }

    /// <summary>
    /// 执行命令并等待完成
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
            _logger.LogError(ex, "Failed to execute command: {Command}", command);
            return false;
        }
    }

    /// <summary>
    /// 检查命令是否可用
    /// </summary>
    private bool IsCommandAvailable(string command)
    {
        try
        {
            using var process = new Process();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "where";
                process.StartInfo.Arguments = command;
            }
            else
            {
                process.StartInfo.FileName = "which";
                process.StartInfo.Arguments = command;
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 处理不支持的组件
    /// </summary>
    private bool HandleUnsupportedComponent(string component)
    {
        _consoleDisplay.ShowError($"❌ 不支持的组件: {component}");
        ShowUsage();
        return false;
    }

    /// <summary>
    /// 显示使用说明
    /// </summary>
    private void ShowUsage()
    {
        _consoleDisplay.ShowInfo("💡 用法: deck install <component>");
        _consoleDisplay.ShowInfo("支持的组件:");
        _consoleDisplay.ShowInfo("  podman  - 安装 Podman 容器引擎");
    }

    /// <summary>
    /// 显示手动安装说明
    /// </summary>
    private void ShowManualInstallInstructions(SystemInfo systemInfo)
    {
        _consoleDisplay.ShowInfo("💡 手动安装说明:");
        _consoleDisplay.WriteLine();

        switch (systemInfo.OperatingSystem)
        {
            case OperatingSystemType.MacOS:
                _consoleDisplay.ShowInfo("macOS 用户可以:");
                _consoleDisplay.ShowInfo("1. 安装 Homebrew: /bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"");
                _consoleDisplay.ShowInfo("2. 然后运行: brew install podman");
                break;
            
            case OperatingSystemType.Linux:
                _consoleDisplay.ShowInfo("Linux 用户请参考官方文档:");
                _consoleDisplay.ShowInfo("https://podman.io/getting-started/installation");
                break;
            
            case OperatingSystemType.Windows:
                _consoleDisplay.ShowInfo("Windows 用户可以:");
                _consoleDisplay.ShowInfo("1. 从官网下载 Podman Desktop: https://podman.io/desktop");
                _consoleDisplay.ShowInfo("2. 或安装 Chocolatey 后运行: choco install podman-desktop");
                break;
        }
    }

    /// <summary>
    /// 显示故障排除信息
    /// </summary>
    private void ShowTroubleshootingInfo()
    {
        _consoleDisplay.ShowInfo("💡 故障排除:");
        _consoleDisplay.ShowInfo("1. 检查网络连接");
        _consoleDisplay.ShowInfo("2. 确保包管理器正常工作");
        _consoleDisplay.ShowInfo("3. 检查是否需要管理员权限");
        _consoleDisplay.ShowInfo("4. 参考官方安装文档: https://podman.io/getting-started/installation");
    }
}

/// <summary>
/// Podman安装命令信息
/// </summary>
public class PodmanInstallCommand
{
    public string PackageManager { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; }
}