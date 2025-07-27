using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Deck.Console.Commands;

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

            // 4. 模板仓库连接检查
            allChecksPassed &= await CheckTemplateRepositoryAsync(logger, cancellationToken);

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

    private async Task<bool> CheckTemplateRepositoryAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            _consoleDisplay.ShowTitle("📡 模板仓库连接检查");

            // 从配置获取模板仓库地址
            var templateUrl = "https://gitee.com/zhaoquan/deck.git"; // 从配置服务获取
            var fallbackUrl = "https://github.com/zhaoqing/deck.git"; // 从配置服务获取

            // 测试主要仓库
            var primarySuccess = await _networkService.TestTemplateRepositoryAsync(templateUrl);
            DisplayNetworkCheckResult("主要模板仓库", primarySuccess, 
                primarySuccess ? $"可连接 ({templateUrl})" : $"连接失败 ({templateUrl})");

            if (!primarySuccess)
            {
                // 测试备用仓库
                var fallbackSuccess = await _networkService.TestTemplateRepositoryAsync(fallbackUrl);
                DisplayNetworkCheckResult("备用模板仓库", fallbackSuccess,
                    fallbackSuccess ? $"可连接 ({fallbackUrl})" : $"连接失败 ({fallbackUrl})");

                if (!fallbackSuccess)
                {
                    _consoleDisplay.ShowWarning("  ⚠️  所有模板仓库均无法连接");
                    _consoleDisplay.ShowInfo("  💡 解决方案:");
                    _consoleDisplay.ShowInfo("     1. 检查网络连接");
                    _consoleDisplay.ShowInfo("     2. 手动修改 .deck/config.json 更换仓库地址");  
                    _consoleDisplay.ShowInfo("     3. 使用本地模板（如果已下载）");
                    _consoleDisplay.ShowInfo("     4. 在 .deck/templates/ 目录下手动创建模板");
                    return false;
                }
                
                return true; // 备用仓库可用
            }

            return true; // 主要仓库可用
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "模板仓库连接检查失败");
            _consoleDisplay.ShowError("模板仓库连接检查过程中发生错误");
            return false;
        }
    }

    // ... 其他方法保持不变
    private void DisplayNetworkCheckResult(string checkName, bool success, string message)
    {
        var status = success ? 
            ColorizeText("✅ 正常", ConsoleColor.Green) :
            ColorizeText("❌ 异常", ConsoleColor.Red);
        ShowKeyValue($"  {checkName}", $"{status} - {message}");
    }

    private void DisplayFinalResult(bool allPassed)
    {
        if (allPassed)
        {
            _consoleDisplay.ShowSuccess("✅ 系统诊断完成，所有检查项目均通过！");
            _consoleDisplay.ShowInfo("🚀 您的 Deck 开发环境已就绪，可以开始使用了");
        }
        else
        {
            _consoleDisplay.ShowError("❌ 系统诊断发现问题，请根据上述建议进行修复");
            _consoleDisplay.ShowInfo("💡 修复问题后，请重新运行 'deck doctor' 进行检查");
        }
    }

    private void ShowKeyValue(string key, string value)
    {
        _consoleDisplay.ShowInfo($"{key}: {value}");
    }

    private string ColorizeText(string text, ConsoleColor color)
    {
        return text; // 简化实现，实际可以添加颜色
    }

    // 其他必要的方法...
    private async Task<bool> DisplaySystemInfoAsync(ILogger logger, CancellationToken cancellationToken) 
    {
        try
        {
            _consoleDisplay.ShowTitle("💻 系统信息");
            
            var systemInfo = await _systemDetectionService.GetSystemInfoAsync();
            _consoleDisplay.ShowInfo($"  操作系统: {systemInfo.OperatingSystem} {systemInfo.Version}");
            _consoleDisplay.ShowInfo($"  架构: {systemInfo.Architecture}");
            _consoleDisplay.ShowInfo($"  可用内存: {systemInfo.AvailableMemoryMb}MB");
            _consoleDisplay.ShowInfo($"  可用磁盘空间: {systemInfo.AvailableDiskSpaceGb}GB");
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取系统信息失败");
            _consoleDisplay.ShowError("❌ 系统信息获取失败");
            return false;
        }
    }
    
    private async Task<bool> DisplayProjectInfoAsync(ILogger logger, CancellationToken cancellationToken) 
    {
        try
        {
            _consoleDisplay.ShowTitle("📁 项目环境");
            
            var containerEngine = await _systemDetectionService.DetectContainerEngineAsync();
            var status = containerEngine.IsAvailable ? "✅ 可用" : "❌ 不可用";
            _consoleDisplay.ShowInfo($"  容器引擎: {containerEngine.Type} - {status}");
            
            if (!string.IsNullOrEmpty(containerEngine.Version))
            {
                _consoleDisplay.ShowInfo($"  版本: {containerEngine.Version}");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取项目信息失败");
            _consoleDisplay.ShowError("❌ 项目信息获取失败");
            return false;
        }
    }
    
    private async Task<bool> CheckSystemRequirementsAsync(ILogger logger, CancellationToken cancellationToken) 
    {
        try
        {
            _consoleDisplay.ShowTitle("🔍 系统要求检查");
            
            var requirements = await _systemDetectionService.CheckSystemRequirementsAsync();
            
            if (requirements.MeetsRequirements)
            {
                _consoleDisplay.ShowInfo("  ✅ 系统要求满足");
                return true;
            }
            else
            {
                _consoleDisplay.ShowWarning("  ⚠️ 系统要求不满足");
                foreach (var check in requirements.Checks.Where(c => !c.Passed))
                {
                    _consoleDisplay.ShowWarning($"    • {check.Name}: {check.Description}");
                    if (!string.IsNullOrEmpty(check.Suggestion))
                    {
                        _consoleDisplay.ShowInfo($"      建议: {check.Suggestion}");
                    }
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "系统要求检查失败");
            _consoleDisplay.ShowError("❌ 系统要求检查失败");
            return false;
        }
    }
    
    private async Task<bool> CheckDeckDirectoryStructureAsync(ILogger logger, CancellationToken cancellationToken) 
    {
        try
        {
            _consoleDisplay.ShowTitle("📂 .deck 目录结构");
            
            var result = await _directoryManagementService.ValidateDirectoryStructureAsync();
            
            if (result.IsValid)
            {
                _consoleDisplay.ShowInfo("  ✅ 目录结构正常");
                return true;
            }
            else
            {
                _consoleDisplay.ShowWarning("  ⚠️ 目录结构异常");
                foreach (var error in result.Errors)
                {
                    _consoleDisplay.ShowWarning($"    • {error}");
                }
                foreach (var suggestion in result.RepairSuggestions)
                {
                    _consoleDisplay.ShowInfo($"      建议: {suggestion}");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "目录结构检查失败");
            _consoleDisplay.ShowError("❌ 目录结构检查失败");
            return false;
        }
    }
}