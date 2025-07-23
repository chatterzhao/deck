using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Deck.Console.Commands;

/// <summary>
/// Doctor系统诊断命令 - 基于deck-shell的doctor命令实现
/// 全面检查系统环境、依赖、网络连接等
/// </summary>
public class DoctorCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly ISystemDetectionService _systemDetectionService;
    private readonly INetworkService _networkService;
    private readonly ILoggingService _loggingService;
    private readonly IDirectoryManagementService _directoryManagementService;

    public DoctorCommand(
        IConsoleDisplay consoleDisplay,
        ISystemDetectionService systemDetectionService,
        INetworkService networkService,
        ILoggingService loggingService,
        IDirectoryManagementService directoryManagementService)
    {
        _consoleDisplay = consoleDisplay;
        _systemDetectionService = systemDetectionService;
        _networkService = networkService;
        _loggingService = loggingService;
        _directoryManagementService = directoryManagementService;
    }

    /// <summary>
    /// 执行系统诊断
    /// </summary>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var logger = _loggingService.GetLogger("Deck.Console.Doctor");
        logger.LogInformation("开始执行 Doctor 系统诊断命令");

        try
        {
            _consoleDisplay.ShowInfo("🩺 Deck 系统诊断开始...");
            _consoleDisplay.WriteLine();

            var allChecksPassed = true;

            // 1. 系统信息检测和显示
            allChecksPassed &= await DisplaySystemInfoAsync(logger, cancellationToken);

            // 2. 项目环境检测和显示  
            allChecksPassed &= await DisplayProjectInfoAsync(logger, cancellationToken);

            // 3. 系统要求检查
            allChecksPassed &= await CheckSystemRequirementsAsync(logger, cancellationToken);

            // 4. 网络连接检查
            allChecksPassed &= await CheckNetworkConnectivityAsync(logger, cancellationToken);

            // 5. .deck目录结构检查
            allChecksPassed &= await CheckDeckDirectoryStructureAsync(logger, cancellationToken);

            // 显示最终诊断结果
            _consoleDisplay.WriteLine();
            DisplayFinalResult(allChecksPassed);

            logger.LogInformation("Doctor 诊断完成，整体状态: {AllChecksPassed}", allChecksPassed);
            return allChecksPassed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Doctor 诊断过程中发生异常");
            _consoleDisplay.ShowError($"系统诊断过程中发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 显示系统信息 (基于deck-shell的display_system_info_enhanced)
    /// </summary>
    private async Task<bool> DisplaySystemInfoAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            _consoleDisplay.ShowTitle("🔍 系统信息");

            var systemInfo = await _systemDetectionService.GetSystemInfoAsync();
            var containerEngine = await _systemDetectionService.DetectContainerEngineAsync();

            // 显示系统基础信息
            ShowKeyValue("  操作系统", GetColoredSystemInfo(systemInfo.OperatingSystem.ToString(), systemInfo.Version));
            ShowKeyValue("  系统架构", GetColoredArchInfo(systemInfo.Architecture.ToString()));
            ShowKeyValue("  系统内存", GetColoredMemoryInfo(systemInfo.AvailableMemoryMb));
            ShowKeyValue("  可用磁盘", GetColoredDiskInfo(systemInfo.AvailableDiskSpaceGb));

            // 容器引擎信息
            if (containerEngine.IsAvailable)
            {
                ShowKeyValue("  容器引擎", GetColoredEngineInfo(containerEngine.Type.ToString(), containerEngine.Version));
            }
            else
            {
                ShowKeyValue("  容器引擎", ColorizeText("❌ 未安装", ConsoleColor.Red));
            }

            // WSL检测
            if (systemInfo.IsWsl)
            {
                ShowKeyValue("  运行环境", ColorizeText("WSL", ConsoleColor.Yellow));
            }

            logger.LogDebug("系统信息显示完成");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "显示系统信息失败");
            _consoleDisplay.ShowWarning("无法获取完整系统信息");
            return false;
        }
    }

    /// <summary>
    /// 显示项目信息 (基于deck-shell的项目检测逻辑)
    /// </summary>
    private async Task<bool> DisplayProjectInfoAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            _consoleDisplay.ShowTitle("🎯 项目信息");

            var currentDirectory = Directory.GetCurrentDirectory();
            var projectInfo = await _systemDetectionService.DetectProjectTypeAsync(currentDirectory);

            if (projectInfo.DetectedTypes.Any())
            {
                // 推荐项目类型
                if (projectInfo.RecommendedType.HasValue)
                {
                    ShowKeyValue("  推荐类型", 
                        ColorizeText(projectInfo.RecommendedType.Value.ToString(), ConsoleColor.Green));
                }

                // 所有检测到的类型
                if (projectInfo.DetectedTypes.Count > 1)
                {
                    ShowKeyValue("  检测类型", 
                        string.Join(", ", projectInfo.DetectedTypes.Select(t => 
                            ColorizeText(t.ToString(), ConsoleColor.Cyan))));
                }

                // 项目文件
                if (projectInfo.ProjectFiles.Any())
                {
                    ShowKeyValue("  项目文件", 
                        ColorizeText(string.Join(", ", projectInfo.ProjectFiles), ConsoleColor.Gray));
                }
            }
            else
            {
                ShowKeyValue("  项目类型", 
                    ColorizeText("未识别项目类型", ConsoleColor.Yellow));
            }

            ShowKeyValue("  项目路径", 
                ColorizeText(currentDirectory, ConsoleColor.Gray));

            logger.LogDebug("项目信息显示完成，检测到类型数量: {TypeCount}", projectInfo.DetectedTypes.Count);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "显示项目信息失败");
            _consoleDisplay.ShowWarning("无法获取项目信息");
            return false;
        }
    }

    /// <summary>
    /// 检查系统要求 (基于deck-shell的check_system_requirements)
    /// </summary>
    private async Task<bool> CheckSystemRequirementsAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            _consoleDisplay.ShowTitle("⚡ 系统要求检查");

            var requirements = await _systemDetectionService.CheckSystemRequirementsAsync();
            var allPassed = true;

            foreach (var check in requirements.Checks)
            {
                var status = check.Passed ? 
                    ColorizeText("✅ 通过", ConsoleColor.Green) :
                    ColorizeText("❌ 失败", ConsoleColor.Red);

                ShowKeyValue($"  {check.Name}", $"{status} - {check.Description}");

                if (!check.Passed && !string.IsNullOrEmpty(check.Suggestion))
                {
                    _consoleDisplay.ShowWarning($"    💡 建议: {check.Suggestion}");
                }

                allPassed &= check.Passed;
            }

            // 显示警告信息
            foreach (var warning in requirements.Warnings)
            {
                _consoleDisplay.ShowWarning($"  ⚠️  {warning}");
            }

            logger.LogInformation("系统要求检查完成，整体结果: {MeetsRequirements}", requirements.MeetsRequirements);
            return requirements.MeetsRequirements;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "系统要求检查失败");
            _consoleDisplay.ShowError("系统要求检查过程中发生错误");
            return false;
        }
    }

    /// <summary>
    /// 检查网络连接 (基于deck-shell的网络检测逻辑)
    /// </summary>
    private async Task<bool> CheckNetworkConnectivityAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            _consoleDisplay.ShowTitle("🌐 网络连接检查");

            // 基础网络连接检查
            var basicConnectivity = await _networkService.CheckConnectivityAsync();
            DisplayNetworkCheckResult("基础网络连接", basicConnectivity.IsConnected, 
                basicConnectivity.IsConnected ? "网络连接正常" : "网络连接异常");

            // 容器镜像仓库检查
            var dockerHubResult = await _networkService.CheckRegistryConnectivityAsync(ContainerRegistryType.DockerHub);
            var aliyunResult = await _networkService.CheckRegistryConnectivityAsync(ContainerRegistryType.AliyunRegistry);
            var tencentResult = await _networkService.CheckRegistryConnectivityAsync(ContainerRegistryType.TencentRegistry);
            
            var registryConnected = dockerHubResult.IsAvailable || aliyunResult.IsAvailable || tencentResult.IsAvailable;
            DisplayNetworkCheckResult("容器镜像仓库", registryConnected,
                registryConnected ? "至少一个镜像仓库可访问" : "所有镜像仓库均不可访问");

            // 包管理器镜像源检查  
            var serviceTypes = new[] { NetworkServiceType.GitHub, NetworkServiceType.AliyunRegistry };
            var packageResults = await _networkService.CheckMultipleServicesAsync(serviceTypes);
            var packageConnected = packageResults.Any(r => r.IsAvailable);
            DisplayNetworkCheckResult("包管理器镜像", packageConnected,
                packageConnected ? "至少一个包管理器镜像可访问" : "所有包管理器镜像均不可访问");

            var overallNetworkStatus = basicConnectivity.IsConnected || registryConnected || packageConnected;

            // 网络故障时的建议
            if (!overallNetworkStatus)
            {
                _consoleDisplay.ShowWarning("  💡 网络连接建议:");
                _consoleDisplay.ShowWarning("    - 检查网络连接状态");
                _consoleDisplay.ShowWarning("    - 检查防火墙设置");
                _consoleDisplay.ShowWarning("    - 考虑配置代理服务器");
                _consoleDisplay.ShowWarning("    - 可以使用离线模式进行开发");
            }

            logger.LogInformation("网络连接检查完成，整体状态: {NetworkStatus}", overallNetworkStatus);
            return overallNetworkStatus;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "网络连接检查失败");
            _consoleDisplay.ShowWarning("网络连接检查过程中发生错误，可能影响远程功能");
            return false;
        }
    }

    /// <summary>
    /// 检查.deck目录结构
    /// </summary>
    private Task<bool> CheckDeckDirectoryStructureAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            _consoleDisplay.ShowTitle("📁 .deck目录结构检查");

            var currentDir = Directory.GetCurrentDirectory();
            var deckDir = Path.Combine(currentDir, ".deck");

            // 检查.deck目录是否存在
            if (!Directory.Exists(deckDir))
            {
                ShowKeyValue("  .deck目录", 
                    ColorizeText("❌ 不存在", ConsoleColor.Red));
                _consoleDisplay.ShowInfo("  💡 建议: 运行 'deck start' 来初始化目录结构");
                return Task.FromResult(false);
            }

            ShowKeyValue("  .deck目录", 
                ColorizeText("✅ 存在", ConsoleColor.Green));

            // 检查子目录结构
            var subDirectories = new[] { "templates", "custom", "images" };
            var allSubDirsExist = true;

            foreach (var subDir in subDirectories)
            {
                var subDirPath = Path.Combine(deckDir, subDir);
                var exists = Directory.Exists(subDirPath);
                
                ShowKeyValue($"  .deck/{subDir}", 
                    exists ? ColorizeText("✅ 存在", ConsoleColor.Green) :
                            ColorizeText("❌ 缺失", ConsoleColor.Yellow));

                if (!exists)
                {
                    allSubDirsExist = false;
                }
            }

            // 检查配置文件
            var configFile = Path.Combine(deckDir, "config.json");
            var configExists = File.Exists(configFile);
            
            ShowKeyValue("  config.json", 
                configExists ? ColorizeText("✅ 存在", ConsoleColor.Green) :
                              ColorizeText("❌ 缺失", ConsoleColor.Yellow));

            var overallStructureOk = allSubDirsExist && configExists;

            if (!overallStructureOk)
            {
                _consoleDisplay.ShowInfo("  💡 建议: 运行 'deck start' 来修复目录结构");
            }

            logger.LogInformation(".deck目录结构检查完成，结果: {StructureOk}", overallStructureOk);
            return Task.FromResult(overallStructureOk);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, ".deck目录结构检查失败");
            _consoleDisplay.ShowWarning("目录结构检查过程中发生错误");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 显示最终诊断结果
    /// </summary>
    private void DisplayFinalResult(bool allChecksPassed)
    {
        if (allChecksPassed)
        {
            _consoleDisplay.ShowSuccess("🎉 系统诊断完成！所有检查均已通过，您的环境已准备就绪。");
            _consoleDisplay.ShowInfo("💡 现在可以运行 'deck start' 开始开发环境配置。");
        }
        else
        {
            _consoleDisplay.ShowWarning("⚠️  系统诊断发现了一些问题。");
            _consoleDisplay.ShowInfo("💡 请根据上述建议修复问题后重新运行 'deck doctor'。");
            _consoleDisplay.ShowInfo("💡 即使存在警告，您仍可以尝试运行 'deck start'，但可能会遇到问题。");
        }
    }

    #region 辅助方法

    /// <summary>
    /// 显示键值对信息
    /// </summary>
    private void ShowKeyValue(string key, string value)
    {
        _consoleDisplay.Write(key, ConsoleColor.Gray);
        _consoleDisplay.Write(": ");
        _consoleDisplay.WriteLine(value);
    }

    /// <summary>
    /// 显示网络检查结果
    /// </summary>
    private void DisplayNetworkCheckResult(string name, bool passed, string description)
    {
        var status = passed ? 
            ColorizeText("✅ 正常", ConsoleColor.Green) :
            ColorizeText("❌ 异常", ConsoleColor.Red);

        ShowKeyValue($"  {name}", $"{status} - {description}");
    }

    /// <summary>
    /// 彩色文本辅助方法
    /// </summary>
    private string ColorizeText(string text, ConsoleColor color)
    {
        // 简化实现，直接返回文本（实际显示时会使用对应颜色）
        return text;
    }

    /// <summary>
    /// 获取彩色系统信息
    /// </summary>
    private string GetColoredSystemInfo(string os, string version)
    {
        return $"{os} {version}";
    }

    /// <summary>
    /// 获取彩色架构信息
    /// </summary>
    private string GetColoredArchInfo(string arch)
    {
        return arch;
    }

    /// <summary>
    /// 获取彩色内存信息
    /// </summary>
    private string GetColoredMemoryInfo(long memoryMb)
    {
        var memoryGb = memoryMb / 1024.0;
        return $"{memoryGb:F1}GB";
    }

    /// <summary>
    /// 获取彩色磁盘信息
    /// </summary>
    private string GetColoredDiskInfo(long diskGb)
    {
        return $"{diskGb}GB";
    }

    /// <summary>
    /// 获取彩色容器引擎信息
    /// </summary>
    private string GetColoredEngineInfo(string engine, string? version)
    {
        return string.IsNullOrEmpty(version) ? engine : $"{engine} {version}";
    }

    #endregion
}