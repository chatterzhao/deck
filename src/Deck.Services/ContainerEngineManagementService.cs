using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 容器引擎管理服务，用于处理容器引擎的安装、检查和初始化
/// </summary>
public class ContainerEngineManagementService : IContainerEngineManagementService
{
    private readonly ILogger<ContainerEngineManagementService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConsoleUIService _consoleUIService;

    public ContainerEngineManagementService(
        ILogger<ContainerEngineManagementService> logger,
        ILoggerFactory loggerFactory,
        IConsoleUIService consoleUIService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _consoleUIService = consoleUIService;
    }

    /// <summary>
    /// 检查并处理容器引擎状态
    /// </summary>
    /// <returns>容器引擎信息</returns>
    public async Task<ContainerEngineInfo> CheckAndHandleContainerEngineAsync()
    {
        var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
        var containerEngineInfo = await systemService.DetectContainerEngineAsync();

        // 如果容器引擎不可用但错误信息提到 Podman machine，尝试自动初始化
        if (!containerEngineInfo.IsAvailable &&
            containerEngineInfo.Type == ContainerEngineType.Podman &&
            !string.IsNullOrEmpty(containerEngineInfo.ErrorMessage) &&
            (containerEngineInfo.ErrorMessage.Contains("machine") || 
             containerEngineInfo.ErrorMessage.Contains("Podman machine 未运行") ||
             containerEngineInfo.ErrorMessage.Contains("需要初始化") ||
             containerEngineInfo.ErrorMessage.Contains("需要启动")))
        {
            _consoleUIService.ShowInfo("🔧 检测到 Podman machine 未运行，尝试自动初始化...");
            _logger.LogInformation("[调试] 检测到 Podman machine 未运行，尝试自动初始化...");
            _logger.LogInformation("[调试] 错误信息: {ErrorMessage}", containerEngineInfo.ErrorMessage);
            var initResult = await systemService.TryInitializePodmanMachineAsync();
            if (initResult)
            {
                _consoleUIService.ShowSuccess("✅ Podman machine 初始化成功");
                _logger.LogInformation("[调试] Podman machine 初始化成功");
                // 重新检测容器引擎
                containerEngineInfo = await systemService.DetectContainerEngineAsync();
            }
            else
            {
                _consoleUIService.ShowWarning("⚠️ Podman machine 自动初始化失败，请手动运行: podman machine init && podman machine start");
                _logger.LogWarning("[调试] Podman machine 自动初始化失败");
            }
        }

        // 如果容器引擎是Podman但不可用，尝试启动Podman Machine
        if (!containerEngineInfo.IsAvailable &&
            containerEngineInfo.Type == ContainerEngineType.Podman &&
            string.IsNullOrEmpty(containerEngineInfo.ErrorMessage))
        {
            _consoleUIService.ShowInfo("🔧 尝试启动 Podman machine...");
            _logger.LogInformation("[调试] 尝试启动 Podman machine...");
            var startResult = await systemService.TryInitializePodmanMachineAsync();
            if (startResult)
            {
                _consoleUIService.ShowSuccess("✅ Podman machine 启动成功");
                _logger.LogInformation("[调试] Podman machine 启动成功");
                // 重新检测容器引擎
                containerEngineInfo = await systemService.DetectContainerEngineAsync();
            }
            else
            {
                _consoleUIService.ShowWarning("⚠️ Podman machine 启动失败，请手动运行: podman machine start");
                _logger.LogWarning("[调试] Podman machine 启动失败");
            }
        }

        // 如果容器引擎不可用且错误信息表明命令不存在，说明未安装容器引擎
        if (!containerEngineInfo.IsAvailable && 
            !string.IsNullOrEmpty(containerEngineInfo.ErrorMessage) &&
            (containerEngineInfo.ErrorMessage.Contains("No such file or directory") ||
             containerEngineInfo.ErrorMessage.Contains("command not found") ||
             containerEngineInfo.ErrorMessage.Contains("not found")))
        {
            _consoleUIService.ShowInfo("🔧 检测到容器引擎未安装");
            _logger.LogInformation("检测到容器引擎未安装: {ErrorMessage}", containerEngineInfo.ErrorMessage);
            // 返回容器引擎信息，让上层决定是否安装
        }

        return containerEngineInfo;
    }

    /// <summary>
    /// 检查是否需要重新安装Podman（例如从brew安装的情况）
    /// </summary>
    /// <returns>是否需要重新安装</returns>
    public async Task<bool> CheckAndHandlePodmanReinstallationAsync()
    {
        var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
        var containerEngineInfo = await systemService.DetectContainerEngineAsync();

        // 检查Podman是否通过brew安装
        if (containerEngineInfo.Type == ContainerEngineType.Podman &&
            !string.IsNullOrEmpty(containerEngineInfo.InstallPath) &&
            containerEngineInfo.InstallPath.Contains("brew"))
        {
            _consoleUIService.ShowWarning("⚠️ 检测到Podman通过Homebrew安装");
            _consoleUIService.ShowInfo("💡 Podman官方不推荐通过Homebrew安装，可能存在稳定性问题");
            _consoleUIService.ShowInfo("💡 建议卸载brew版本并安装官方版本以获得更好的体验");

            var reinstall = _consoleUIService.ShowConfirmation("是否卸载当前版本并重新安装官方版本？");
            if (reinstall)
            {
                // 尝试卸载brew版本
                _consoleUIService.ShowInfo("🔧 正在卸载brew版本的Podman...");
                var uninstallSuccess = await UninstallBrewPodmanAsync();
                if (uninstallSuccess)
                {
                    _consoleUIService.ShowSuccess("✅ 已卸载brew版本的Podman");
                    return await InstallContainerEngineAsync();
                }
                else
                {
                    _consoleUIService.ShowError("❌ 卸载brew版本的Podman失败");
                    return false;
                }
            }
        }

        return true; // 不需要重新安装
    }

    /// <summary>
    /// 尝试安装容器引擎
    /// </summary>
    /// <returns>是否安装成功</returns>
    public async Task<bool> InstallContainerEngineAsync()
    {
        _consoleUIService.ShowWarning("⚠️ 未检测到容器引擎");
        _consoleUIService.ShowInfo("💡 Deck需要Podman(或Docker)和podman-compose(或docker-compose)来运行容器");

        // 检查操作系统并提供适当建议
        var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
        var systemInfo = await systemService.GetSystemInfoAsync();

        if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
        {
            _consoleUIService.ShowInfo("💡 macOS用户建议：");
            _consoleUIService.ShowInfo("  1. 从 https://podman.io/downloads 下载官方安装包（推荐）");
            _consoleUIService.ShowInfo("  2. 或使用包管理器安装，如Homebrew: brew install podman podman-compose");
        }

        if (systemInfo.OperatingSystem == OperatingSystemType.Linux)
        {
            if (IsCommandAvailable("apt"))
            {
                _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  Ubuntu/Debian: sudo apt update && sudo apt install -y podman");
            }
            else if (IsCommandAvailable("dnf"))
            {
                _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  Fedora: sudo dnf install -y podman");
            }
            else if (IsCommandAvailable("yum"))
            {
                _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  CentOS/RHEL: sudo yum install -y podman");
            }
            else if (IsCommandAvailable("zypper"))
            {
                _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  openSUSE: sudo zypper install -y podman");
            }
            else if (IsCommandAvailable("pacman"))
            {
                _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  Arch Linux: sudo pacman -S --noconfirm podman");
            }
            else
            {
                _consoleUIService.ShowInfo("💡 请参考 https://podman.io/docs/installation 手动安装Podman");
            }
            _consoleUIService.ShowInfo("  更多信息请参考: https://podman.io/docs/installation");
        }

        if (systemInfo.OperatingSystem == OperatingSystemType.Windows)
        {
            _consoleUIService.ShowInfo("💡 Windows建议:");
            _consoleUIService.ShowInfo("  1. 安装Podman: ");
            _consoleUIService.ShowInfo("     - 使用 winget: install RedHat.Podman");
            _consoleUIService.ShowInfo("     - 使用Chocolatey: choco install podman");
            _consoleUIService.ShowInfo("     - 使用Scoop: scoop install podman");
            _consoleUIService.ShowInfo("     - 手动安装: 从 https://podman.io/downloads 下载并安装");
            _consoleUIService.ShowInfo("  2. 安装podman-compose:");
            _consoleUIService.ShowInfo("     - winget 暂时没有 podman-compose，如果有 pip3：pip3 install podman-compose");
            _consoleUIService.ShowInfo("     - 使用Chocolatey: choco install podman-compose");
            _consoleUIService.ShowInfo("     - 使用Scoop: scoop install podman-compose");
            _consoleUIService.ShowInfo("     - 手动安装: 从 https://github.com/containers/podman-compose 下载并安装");
        }

        var install = _consoleUIService.ShowConfirmation("是否尝试自动安装Podman和podman-compose？");
        if (!install)
        {
            _consoleUIService.ShowInfo("💡 您可以选择手动安装Podman(或Docker)和podman-compose(或docker-compose)");
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                _consoleUIService.ShowInfo("💡 macOS推荐:");
                _consoleUIService.ShowInfo("  1. 从 https://podman.io/downloads 下载官方安装包");
                _consoleUIService.ShowInfo("  2. 安装podman-compose: brew install podman-compose");
            }
            if (systemInfo.OperatingSystem == OperatingSystemType.Linux)
            {
                _consoleUIService.ShowInfo("💡 Linux推荐使用包管理器安装:");
                _consoleUIService.ShowInfo("  Ubuntu/Debian: sudo apt update && sudo apt install -y podman podman-compose");
                _consoleUIService.ShowInfo("  Fedora: sudo dnf install -y podman podman-compose");
                _consoleUIService.ShowInfo("  CentOS/RHEL: sudo yum install -y podman podman-compose");
            }
            if (systemInfo.OperatingSystem == OperatingSystemType.Windows)
            {
                _consoleUIService.ShowInfo("💡 Windows推荐:");
                _consoleUIService.ShowInfo("  1. 使用winget安装: winget install RedHat.Podman");
                _consoleUIService.ShowInfo("  2. 安装compose工具");
            }
            return false;
        }

        // 执行Podman安装
        _consoleUIService.ShowInfo("🔧 正在尝试安装Podman...");
        var installSuccess = await InstallPodmanEngineAsync();

        if (installSuccess)
        {
            _consoleUIService.ShowSuccess("✅ Podman安装成功");

            // 尝试安装podman-compose
            _consoleUIService.ShowInfo("🔧 正在尝试安装podman-compose...");
            var composeInstallSuccess = await InstallPodmanComposeAsync();

            if (composeInstallSuccess)
            {
                _consoleUIService.ShowSuccess("✅ podman-compose安装成功");
            }
            else
            {
                _consoleUIService.ShowWarning("⚠️ podman-compose安装失败，请手动安装");
                if (systemInfo.OperatingSystem == OperatingSystemType.Linux)
                {
                    if (IsCommandAvailable("apt"))
                    {
                        _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                        _consoleUIService.ShowInfo("  Ubuntu/Debian使用APT安装: sudo apt update && sudo apt install -y podman-compose");
                        _consoleUIService.ShowInfo("  Ubuntu/Debian使用pip安装: sudo pip3 install podman-compose");
                    }
                    else if (IsCommandAvailable("dnf"))
                    {
                        _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                        _consoleUIService.ShowInfo("  Fedora: sudo dnf install -y podman-compose");
                    }
                    else if (IsCommandAvailable("yum"))
                    {
                        _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                        _consoleUIService.ShowInfo("  CentOS/RHEL: sudo yum install -y podman-compose");
                    }
                    else if (IsCommandAvailable("zypper"))
                    {
                        _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                        _consoleUIService.ShowInfo("  openSUSE: sudo zypper install -y podman-compose");
                    }
                    else if (IsCommandAvailable("pacman"))
                    {
                        _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                        _consoleUIService.ShowInfo("  Arch Linux: sudo pacman -S --noconfirm podman-compose");
                    }
                    else
                    {
                        _consoleUIService.ShowInfo("💡 可以尝试使用pip安装:");
                        _consoleUIService.ShowInfo("  pip3 install podman-compose");
                        _consoleUIService.ShowInfo("  或参考 https://github.com/containers/podman-compose#installation 手动安装podman-compose");
                    }
                }
                if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
                {
                    _consoleUIService.ShowInfo("💡 macOS用户可以使用以下命令手动安装:");
                    _consoleUIService.ShowInfo("  Homebrew安装:");
                    _consoleUIService.ShowInfo("    brew install podman");
                    _consoleUIService.ShowInfo("    brew install podman-compose  # 如果需要");
                    _consoleUIService.ShowInfo("  或者从官网下载安装:");
                    _consoleUIService.ShowInfo("    https://podman.io/downloads");
                }
            }

            // 初始化Podman Machine（仅限macOS/Windows)
            if (systemInfo.OperatingSystem != OperatingSystemType.Linux)
            {
                _consoleUIService.ShowInfo("⚙️ 初始化 Podman Machine...");
                await InitializePodmanMachineAsync();
            }

            _consoleUIService.ShowSuccess("✅ Podman环境准备就绪");
            return true;
        }
        else
        {
            _consoleUIService.ShowError("❌ Podman安装失败");
            _consoleUIService.ShowInfo("💡 建议手动从 https://podman.io/downloads 下载并安装Podman");
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                _consoleUIService.ShowInfo("💡 macOS推荐从 https://podman.io/downloads 下载官方安装包");
            }
            if (systemInfo.OperatingSystem == OperatingSystemType.Linux)
            {
                _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  Ubuntu/Debian: sudo apt update && sudo apt install -y podman");
                _consoleUIService.ShowInfo("  Fedora: sudo dnf install -y podman");
                _consoleUIService.ShowInfo("  CentOS/RHEL: sudo yum install -y podman");
            }
            return false;
        }
    }

    /// <summary>
    /// 初始化 Podman Machine
    /// </summary>
    /// <returns>是否初始化成功</returns>
    public async Task<bool> InitializePodmanMachineAsync()
    {
        try
        {
            // 1. 初始化 machine
            _consoleUIService.ShowInfo("🔧 初始化 Podman Machine...");
            var initResult = await ExecuteCommandAsync("podman machine init");
            if (!initResult)
            {
                _consoleUIService.ShowError("❌ Podman Machine 初始化失败");
                return false;
            }

            // 2. 启动 machine
            _consoleUIService.ShowInfo("🚀 启动 Podman Machine...");
            var startResult = await ExecuteCommandAsync("podman machine start");
            if (!startResult)
            {
                _consoleUIService.ShowError("❌ Podman Machine 启动失败");
                return false;
            }

            _consoleUIService.ShowSuccess("✅ Podman Machine 初始化完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Podman machine initialization failed");
            _consoleUIService.ShowWarning("⚠️ Podman Machine 初始化失败，可能需要手动操作");
            _consoleUIService.ShowInfo("💡 请尝试手动运行: podman machine init && podman machine start");
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// 卸载通过brew安装的Podman
    /// </summary>
    private async Task<bool> UninstallBrewPodmanAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"brew uninstall podman\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载brew版本的Podman时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取Podman安装命令
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

        // 如果没有包管理器，则提供从官网下载pkg安装包的选项
        return new PodmanInstallCommand
        {
            PackageManager = "PKG Installer",
            Command = "download_and_install_podman_pkg",
            RequiresAdmin = true
        };
    }

    /// <summary>
    /// 获取Linux安装命令
    /// </summary>
    private PodmanInstallCommand? GetLinuxInstallCommand()
    {
        // 检查是否可以使用sudo
        bool hasSudo = IsCommandAvailable("sudo");
        
        // APT (Ubuntu/Debian)
        if (IsCommandAvailable("apt"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "APT",
                Command = hasSudo 
                    ? "sudo apt update && sudo apt install -y podman" 
                    : "apt update && apt install -y podman",
                RequiresAdmin = hasSudo
            };
        }

        // DNF (Fedora)
        if (IsCommandAvailable("dnf"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "DNF",
                Command = hasSudo 
                    ? "sudo dnf install -y podman" 
                    : "dnf install -y podman",
                RequiresAdmin = hasSudo
            };
        }

        // YUM (CentOS/RHEL)
        if (IsCommandAvailable("yum"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "YUM",
                Command = hasSudo 
                    ? "sudo yum install -y podman" 
                    : "yum install -y podman",
                RequiresAdmin = hasSudo
            };
        }

        // Zypper (openSUSE)
        if (IsCommandAvailable("zypper"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Zypper",
                Command = hasSudo 
                    ? "sudo zypper install -y podman" 
                    : "zypper install -y podman",
                RequiresAdmin = hasSudo
            };
        }

        // Pacman (Arch Linux)
        if (IsCommandAvailable("pacman"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Pacman",
                Command = hasSudo 
                    ? "sudo pacman -S --noconfirm podman" 
                    : "pacman -S --noconfirm podman",
                RequiresAdmin = hasSudo
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
                RequiresAdmin = true,
                WarningMessage = "注意：将通过Chocolatey安装Podman Desktop"
            };
        }

        // Scoop
        if (IsCommandAvailable("scoop"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Scoop",
                Command = "scoop install podman",
                RequiresAdmin = false,
                WarningMessage = "注意：将通过Scoop安装Podman"
            };
        }

        // WinGet
        if (IsCommandAvailable("winget"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "WinGet",
                Command = "winget install RedHat.Podman",
                RequiresAdmin = false,
                WarningMessage = "注意：将通过WinGet安装Podman"
            };
        }

        // 如果没有包管理器，则提供从GitHub下载MSI安装包的选项
        return new PodmanInstallCommand
        {
            PackageManager = "MSI Installer",
            Command = "download_and_install_podman_msi",
            RequiresAdmin = true,
            WarningMessage = "注意：将从GitHub(https://github.com/containers/podman-compose)下载Podman MSI安装包并安装"
        };
    }

    /// <summary>
    /// 获取Windows下podman-compose安装命令
    /// </summary>
    private PodmanInstallCommand? GetWindowsPodmanComposeInstallCommand()
    {
        // Chocolatey
        if (IsCommandAvailable("choco"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Chocolatey",
                Command = "choco install podman-compose -y",
                RequiresAdmin = true
            };
        }

        // Scoop
        if (IsCommandAvailable("scoop"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Scoop",
                Command = "scoop install podman-compose",
                RequiresAdmin = false
            };
        }

        // WinGet
        if (IsCommandAvailable("winget"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "WinGet",
                // winget 暂时没有 podman-compose
                Command = "pip3 install podman-compose",
                RequiresAdmin = false
            };
        }

        return new PodmanInstallCommand
        {
            PackageManager = "MSI Installer",
            Command = "download_and_install_podman_msi",
            RequiresAdmin = true,
            WarningMessage = "注意：将从GitHub(https://github.com/containers/podman-compose)下载Podman MSI安装包并安装"
        };
    }

    /// <summary>
    /// 安装Podman引擎
    /// </summary>
    private async Task<bool> InstallPodmanEngineAsync()
    {
        try
        {
            var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
            var systemInfo = await systemService.GetSystemInfoAsync();
            var installCommand = GetPodmanInstallCommand(systemInfo);

            if (installCommand == null)
            {
                _consoleUIService.ShowError("❌ 当前系统不支持自动安装 Podman");
                return false;
            }

            // 特殊处理直接下载安装包的方式（macOS和Windows）
            if (installCommand.Command.StartsWith("download_and_install_podman_"))
            {
                return await DownloadAndInstallPodmanPackageAsync(systemInfo);
            }

            // 执行包管理器安装命令
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

            _logger.LogInformation("执行安装命令: {Command}", installCommand.Command);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Podman安装成功");
                return true;
            }
            else
            {
                _logger.LogError("Podman安装失败，退出码: {ExitCode}", process.ExitCode);
                _consoleUIService.ShowError($"安装失败 (退出码: {process.ExitCode})");
                if (!string.IsNullOrEmpty(error))
                {
                    _consoleUIService.ShowError($"错误信息: {error}");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Podman安装命令时发生异常");
            _consoleUIService.ShowError($"安装过程中出现异常: {ex.Message}");
            return false;
        }
    }

    private bool IsPackageManagerAvailable(string packageManager)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "command",
                    Arguments = $"-v {packageManager}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return !string.IsNullOrEmpty(output);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> InstallPodmanComposeAsync()
    {
        try
        {
            var systemService = new SystemDetectionService(_loggerFactory.CreateLogger<SystemDetectionService>());
            var systemInfo = await systemService.GetSystemInfoAsync();
        
            // 检查是否可以通过包管理器安装 podman-compose
            bool isPackageManagerAvailable = IsPackageManagerAvailable("apt");
            if (isPackageManagerAvailable)
            {
                _consoleUIService.ShowInfo("🔧 正在通过APT查询 podman-compose 包...");
                bool isPackageAvailable = IsPackageAvailable("apt", "podman-compose");
                if (!isPackageAvailable)
                {
                    _consoleUIService.ShowWarning("⚠️ APT仓库中未找到 podman-compose 包");
                }
                else
                {
                    // 尝试通过APT安装 podman-compose
                    _consoleUIService.ShowInfo("🔧 正在尝试通过APT安装 podman-compose...");
                    var aptCommand = new PodmanInstallCommand
                    {
                        PackageManager = "apt",
                        Command = "sudo apt update && sudo apt install -y podman-compose",
                        RequiresAdmin = true
                    };
                    bool isAptInstallSuccessful = await ExecuteInstallCommand(aptCommand);
                    if (isAptInstallSuccessful)
                    {
                        _consoleUIService.ShowSuccess("✅ podman-compose 已通过APT安装成功");
                        return true;
                    }
                }
            }

            // 如果包管理器不可用或未安装 podman-compose，则检查是否有 pip3
            if (!IsCommandAvailable("pip3"))
            {
                var userConsent = _consoleUIService.ShowConfirmation("⚠️ 没有检测到pip3，是否要安装Python3和pip3？");
                if (userConsent)
                {
                    // 用户同意安装Python3和pip3
                    bool hasSudo = IsCommandAvailable("sudo");
                    var pythonInstallCommand = new PodmanInstallCommand
                    {
                        PackageManager = "apt",
                        Command = hasSudo ? "sudo apt update && sudo apt install -y python3 python3-pip" : "apt update && apt install -y python3 python3-pip",
                        RequiresAdmin = hasSudo
                    };

                    _consoleUIService.ShowInfo("🔧 正在安装Python3和pip3...");
                    bool isPythonInstallSuccessful = await ExecuteInstallCommand(pythonInstallCommand);
                    if (!isPythonInstallSuccessful)
                    {
                        _consoleUIService.ShowError("❌ Python3和pip3安装失败，请手动安装");
                        return false;
                    }
                }
                else
                {
                    _consoleUIService.ShowWarning("⚠️ 用户拒绝安装Python3和pip3，无法继续安装 podman-compose");
                    return false;
                }
            }

            // 尝试使用pip3安装 podman-compose
            _consoleUIService.ShowWarning("⚠️ 包管理器安装失败，尝试使用pip3安装...");
            bool hasSudoForPip = IsCommandAvailable("sudo");
            var pipCommand = new PodmanInstallCommand
            {
                PackageManager = "pip3",
                Command = hasSudoForPip ? "sudo pip3 install podman-compose" : "pip3 install podman-compose",
                RequiresAdmin = hasSudoForPip
            };
        
            _consoleUIService.ShowInfo("🔧 正在尝试通过pip3安装 podman-compose...");
            bool isPipInstallSuccessful = await ExecuteInstallCommand(pipCommand);
            if (isPipInstallSuccessful)
            {
                _consoleUIService.ShowSuccess("✅ 通过pip3成功安装 podman-compose");
                return true;
            }

            // 所有方法都失败了，显示错误信息
            _consoleUIService.ShowError("❌ podman-compose安装失败，请手动安装");
            if (systemInfo.OperatingSystem == OperatingSystemType.Linux)
            {
                if (IsCommandAvailable("apt"))
                {
                    _consoleUIService.ShowInfo("💡 Linux用户可以使用以下命令手动安装:");
                    _consoleUIService.ShowInfo("  Ubuntu/Debian使用APT安装: sudo apt update && sudo apt install -y podman-compose");
                    _consoleUIService.ShowInfo("  Ubuntu/Debian使用pip安装: sudo pip3 install podman-compose");
                }
            }
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                _consoleUIService.ShowInfo("💡 macOS用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  brew install podman-compose");
                _consoleUIService.ShowInfo("  或使用pip: pip3 install podman-compose");
            }
            if (systemInfo.OperatingSystem == OperatingSystemType.Windows)
            {
                _consoleUIService.ShowInfo("💡 Windows用户可以使用以下命令手动安装:");
                _consoleUIService.ShowInfo("  使用pip: pip3 install podman-compose");
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行 podman-compose 安装命令时发生异常");
            _consoleUIService.ShowError($"安装过程中出现异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取podman-compose安装命令
    /// </summary>
    private PodmanInstallCommand? GetPodmanComposeInstallCommand(SystemInfo systemInfo)
    {
        return systemInfo.OperatingSystem switch
        {
            OperatingSystemType.MacOS => GetMacOSPodmanComposeInstallCommand(),
            OperatingSystemType.Linux => GetLinuxPodmanComposeInstallCommand(),
            OperatingSystemType.Windows => GetWindowsPodmanComposeInstallCommand(),
            _ => null
        };
    }

    /// <summary>
    /// 获取macOS下podman-compose安装命令
    /// </summary>
    private PodmanInstallCommand? GetMacOSPodmanComposeInstallCommand()
    {
        // 优先检查 Homebrew
        if (IsCommandAvailable("brew"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Homebrew",
                Command = "brew install podman-compose",
                RequiresAdmin = false
            };
        }

        // 检查 MacPorts
        if (IsCommandAvailable("port"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "MacPorts",
                Command = "sudo port install podman-compose",
                RequiresAdmin = true
            };
        }

        return null;
    }

    /// <summary>
    /// 获取Linux下podman-compose安装命令
    /// </summary>
    private PodmanInstallCommand? GetLinuxPodmanComposeInstallCommand()
    {
        // 检查是否可以使用sudo
        bool hasSudo = IsCommandAvailable("sudo");
        
        // 优先检查pip3是否可用
        if (IsCommandAvailable("pip3"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "pip3",
                Command = hasSudo 
                    ? "sudo pip3 install podman-compose" 
                    : "pip3 install podman-compose",
                RequiresAdmin = hasSudo
            };
        }
        
        // 检查pip是否可用
        if (IsCommandAvailable("pip"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "pip",
                Command = hasSudo 
                    ? "sudo pip install podman-compose" 
                    : "pip install podman-compose",
                RequiresAdmin = hasSudo
            };
        }

        // APT (Ubuntu/Debian)
        if (IsCommandAvailable("apt"))
        {
            // 首先尝试检查podman-compose包是否可用
            if (IsPackageAvailable("podman-compose", "apt"))
            {
                return new PodmanInstallCommand
                {
                    PackageManager = "APT",
                    Command = hasSudo 
                        ? "sudo apt update && sudo apt install -y podman-compose" 
                        : "apt update && apt install -y podman-compose",
                    RequiresAdmin = hasSudo
                };
            }
        }

        // DNF (Fedora)
        if (IsCommandAvailable("dnf"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "DNF",
                Command = hasSudo 
                    ? "sudo dnf install -y podman-compose" 
                    : "dnf install -y podman-compose",
                RequiresAdmin = hasSudo
            };
        }

        // YUM (CentOS/RHEL)
        if (IsCommandAvailable("yum"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "YUM",
                Command = hasSudo 
                    ? "sudo yum install -y podman-compose" 
                    : "yum install -y podman-compose",
                RequiresAdmin = hasSudo
            };
        }

        // Zypper (openSUSE)
        if (IsCommandAvailable("zypper"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Zypper",
                Command = hasSudo 
                    ? "sudo zypper install -y podman-compose" 
                    : "zypper install -y podman-compose",
                RequiresAdmin = hasSudo
            };
        }

        // Pacman (Arch Linux)
        if (IsCommandAvailable("pacman"))
        {
            return new PodmanInstallCommand
            {
                PackageManager = "Pacman",
                Command = hasSudo 
                    ? "sudo pacman -S --noconfirm podman-compose" 
                    : "pacman -S --noconfirm podman-compose",
                RequiresAdmin = hasSudo
            };
        }

        return null;
    }

    /// <summary>
    /// 统一下载并安装Podman安装包（支持macOS PKG和Windows MSI）
    /// </summary>
    private async Task<bool> DownloadAndInstallPodmanPackageAsync(SystemInfo systemInfo)
    {
        try
        {
            _consoleUIService.ShowInfo("🔍 正在获取最新Podman版本信息...");

            // 获取最新版本信息（简化处理，实际应该通过API获取）
            var latestVersion = "5.5.1"; // 这里应该通过API动态获取
            var architecture = GetSystemArchitectureString(systemInfo.Architecture);

            // 构造下载URL和文件路径
            string downloadUrl, fileName, installerType, fallbackUrl;
            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                fileName = $"podman-{latestVersion}-macos-{architecture}.pkg";
                downloadUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-macos-{architecture}.pkg";
                fallbackUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-macos-{architecture}.pkg";
                installerType = "PKG";
            }
            else // Windows
            {
                fileName = $"podman-{latestVersion}-windows-{architecture}.msi";
                downloadUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-windows-{architecture}.msi";
                fallbackUrl = $"https://github.com/containers/podman/releases/download/v{latestVersion}/podman-installer-windows-{architecture}.msi";
                installerType = "MSI";
            }

            var tempPath = Path.GetTempPath();
            var packagePath = Path.Combine(tempPath, fileName);

            _consoleUIService.ShowInfo($"📦 将下载Podman v{latestVersion} ({architecture})");
            _consoleUIService.ShowInfo($"🔗 首先尝试从官网下载: {downloadUrl}");

            bool downloadSuccess = false;

            // 尝试从官网下载
            try
            {
                _consoleUIService.ShowInfo("📥 正在从官网下载Podman安装包...");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(downloadUrl);

                if (response.IsSuccessStatusCode)
                {
                    var fileContent = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(packagePath, fileContent);
                    downloadSuccess = true;
                    _consoleUIService.ShowSuccess("✅ Podman安装包下载完成");
                }
                else
                {
                    _consoleUIService.ShowWarning($"⚠️ 官网下载失败，HTTP状态码: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从官网下载Podman安装包时发生异常");
                _consoleUIService.ShowWarning("⚠️ 官网下载失败，尝试从GitHub下载...");
            }

            // 如果官网下载失败，尝试从GitHub下载
            if (!downloadSuccess)
            {
                try
                {
                    _consoleUIService.ShowInfo("📥 正在从GitHub下载Podman安装包...");
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(fallbackUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var fileContent = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(packagePath, fileContent);
                        downloadSuccess = true;
                        _consoleUIService.ShowSuccess("✅ Podman安装包下载完成");
                    }
                    else
                    {
                        _consoleUIService.ShowError($"❌ GitHub下载也失败，HTTP状态码: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从GitHub下载Podman安装包时发生异常");
                    _consoleUIService.ShowError($"❌ GitHub下载失败: {ex.Message}");
                }
            }

            // 如果下载都失败了，提示用户手动下载
            if (!downloadSuccess)
            {
                _consoleUIService.ShowError("❌ 无法自动下载Podman安装包");
                _consoleUIService.ShowInfo("💡 请手动从以下地址下载并安装Podman:");
                _consoleUIService.ShowInfo($"  官网地址: https://github.com/containers/podman/releases");
                _consoleUIService.ShowInfo($"  GitHub地址: https://github.com/containers/podman/releases");
                _consoleUIService.ShowInfo("💡 安装完成后请重新运行此命令");
                return false;
            }

            // 安装包
            _consoleUIService.ShowInfo($"🔧 正在安装Podman {installerType}包...");
            Process process;

            if (systemInfo.OperatingSystem == OperatingSystemType.MacOS)
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "installer",
                        Arguments = $"-pkg \"{packagePath}\" -target /",
                        UseShellExecute = true,
                        Verb = "runas" // 请求管理员权限
                    }
                };
            }
            else // Windows
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/i \"{packagePath}\" /quiet /norestart",
                        UseShellExecute = true,
                        Verb = "runas" // 请求管理员权限
                    }
                };
            }

            process.Start();
            await process.WaitForExitAsync();

            // 清理下载的文件
            try
            {
                File.Delete(packagePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理下载的安装包时发生异常: {Message}", ex.Message);
            }

            if (process.ExitCode == 0)
            {
                _consoleUIService.ShowSuccess("✅ Podman安装成功");
                if (systemInfo.OperatingSystem == OperatingSystemType.Windows)
                {
                    _consoleUIService.ShowInfo("💡 请重新启动终端以使环境变量生效");
                }
                return true;
            }
            else
            {
                _consoleUIService.ShowError($"❌ Podman安装失败，退出码: {process.ExitCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载并安装Podman安装包时发生异常");
            _consoleUIService.ShowError($"安装过程中出现异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 将系统架构转换为字符串表示形式
    /// </summary>
    private string GetSystemArchitectureString(SystemArchitecture architecture)
    {
        return architecture switch
        {
            SystemArchitecture.X64 => "amd64",
            SystemArchitecture.ARM64 => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "aarch64" : "arm64",
            SystemArchitecture.X86 => "386",
            _ => "amd64" // 默认使用amd64
        };
    }

    /// <summary>
    /// 检查包是否在包管理器中可用
    /// </summary>
    private bool IsPackageAvailable(string packageName, string packageManager)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            switch (packageManager)
            {
                case "apt":
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"apt list {packageName} 2>/dev/null | grep -q {packageName}\"";
                    break;
                case "dnf":
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"dnf list {packageName} 2>/dev/null | grep -q {packageName}\"";
                    break;
                case "yum":
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"yum list {packageName} 2>/dev/null | grep -q {packageName}\"";
                    break;
                case "zypper":
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"zypper search {packageName} 2>/dev/null | grep -q {packageName}\"";
                    break;
                case "pacman":
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"pacman -Ss {packageName} 2>/dev/null | grep -q {packageName}\"";
                    break;
                default:
                    return false;
            }

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
            _logger.LogError(ex, "执行命令失败: {Command}", command);
            return false;
        }
    }

    /// <summary>
    /// 执行安装命令
    /// </summary>
    private async Task<bool> ExecuteInstallCommand(PodmanInstallCommand installCommand)
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

            _logger.LogInformation("执行安装命令: {Command}", installCommand.Command);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("安装成功: {PackageManager}", installCommand.PackageManager);
                return true;
            }
            else
            {
                _logger.LogError("安装失败，退出码: {ExitCode}", process.ExitCode);
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("错误信息: {Error}", error);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行安装命令时发生异常");
            return false;
        }
    }
    /// <summary>
    /// Podman安装命令信息
    /// </summary>
    private class PodmanInstallCommand
    {
        public string PackageManager { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public bool RequiresAdmin { get; set; }
        public string? WarningMessage { get; set; }
    }
    
    #endregion

}