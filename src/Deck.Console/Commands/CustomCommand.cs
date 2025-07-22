using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Console.Commands;

/// <summary>
/// Custom配置管理命令 - 替代原config命令
/// 管理.deck/custom/目录中的用户自定义配置
/// </summary>
public class CustomCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly IDirectoryManagementService _directoryManagement;
    private readonly IFileSystemService _fileSystem;
    private readonly ILoggingService _loggingService;
    private readonly ILogger _logger;

    public CustomCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        IDirectoryManagementService directoryManagement,
        IFileSystemService fileSystem,
        ILoggingService loggingService)
    {
        _consoleDisplay = consoleDisplay;
        _interactiveSelection = interactiveSelection;
        _directoryManagement = directoryManagement;
        _fileSystem = fileSystem;
        _loggingService = loggingService;
        _logger = _loggingService.GetLogger("Deck.Console.CustomCommand");
    }

    public async Task<bool> ExecuteListAsync()
    {
        try
        {
            _logger.LogInformation("Starting custom list command execution");

            _consoleDisplay.ShowInfo("📋 用户自定义配置列表");
            _consoleDisplay.WriteLine();

            // Get custom configurations using directory management service
            var customConfigs = await GetCustomConfigurationsAsync();

            if (!customConfigs.Any())
            {
                _consoleDisplay.ShowInfo("暂无用户自定义配置");
                _consoleDisplay.ShowInfo("💡 使用 'deck custom create' 创建新配置");
                return true;
            }

            // Display configurations
            for (int i = 0; i < customConfigs.Count; i++)
            {
                var config = customConfigs[i];
                _consoleDisplay.ShowInfo($"  {i + 1,2}. {config.Name}");
            }

            _consoleDisplay.WriteLine();
            _consoleDisplay.ShowInfo("💡 使用以下命令管理配置:");
            _consoleDisplay.ShowInfo("   deck custom create [name] [type]  # 创建新配置");
            _consoleDisplay.ShowInfo("   deck custom edit [name]           # 编辑配置");
            _consoleDisplay.ShowInfo("   deck custom clean                 # 清理配置");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom list command execution failed");
            _consoleDisplay.ShowError($"❌ 执行失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteCreateAsync(string? configName, string? envType)
    {
        try
        {
            _logger.LogInformation("Starting custom create command execution");

            _consoleDisplay.ShowInfo("🆕 创建新的自定义配置");

            // For now, show a placeholder message
            // TODO: Implement full custom creation logic
            _consoleDisplay.ShowWarning("⚠️  Custom create 功能正在开发中");
            _consoleDisplay.ShowInfo($"配置名称: {configName ?? "交互式输入"}");
            _consoleDisplay.ShowInfo($"环境类型: {envType ?? "交互式选择"}");

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom create command execution failed");
            _consoleDisplay.ShowError($"❌ 创建配置失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteEditAsync(string? configName)
    {
        try
        {
            _logger.LogInformation("Starting custom edit command execution");

            _consoleDisplay.ShowInfo("✏️ 编辑自定义配置");

            // For now, show a placeholder message  
            // TODO: Implement full custom edit logic
            _consoleDisplay.ShowWarning("⚠️  Custom edit 功能正在开发中");
            _consoleDisplay.ShowInfo($"配置名称: {configName ?? "交互式选择"}");

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom edit command execution failed");
            _consoleDisplay.ShowError($"❌ 编辑配置失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteCleanAsync()
    {
        try
        {
            _logger.LogInformation("Starting custom clean command execution");

            _consoleDisplay.ShowInfo("🧹 自定义配置清理");

            // Get custom configurations
            var customConfigs = await GetCustomConfigurationsAsync();

            if (!customConfigs.Any())
            {
                _consoleDisplay.ShowInfo("📋 暂无用户自定义配置需要清理");
                return true;
            }

            // For now, show available configs and placeholder message
            // TODO: Implement full custom clean logic
            _consoleDisplay.ShowInfo($"找到 {customConfigs.Count} 个自定义配置:");
            for (int i = 0; i < customConfigs.Count; i++)
            {
                _consoleDisplay.ShowInfo($"  {i + 1}. {customConfigs[i].Name}");
            }

            _consoleDisplay.ShowWarning("⚠️  Custom clean 功能正在开发中");
            _consoleDisplay.ShowInfo("💡 当前仅显示配置列表");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom clean command execution failed");
            _consoleDisplay.ShowError($"❌ 清理配置失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取自定义配置列表（简化实现）
    /// </summary>
    private async Task<List<ConfigurationOption>> GetCustomConfigurationsAsync()
    {
        try
        {
            // Try to get configurations from directory management service
            // This is a simplified approach for now
            var threeLayerOptions = await _directoryManagement.GetThreeLayerOptionsAsync();
            return threeLayerOptions?.Custom ?? new List<ConfigurationOption>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get custom configurations, returning empty list");
            return new List<ConfigurationOption>();
        }
    }
}