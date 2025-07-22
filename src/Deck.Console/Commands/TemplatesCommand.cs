using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Console.Commands;

/// <summary>
/// Templates模板管理命令
/// 管理.deck/templates/目录中的远程模板
/// </summary>
public class TemplatesCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly IDirectoryManagementService _directoryManagement;
    private readonly IConfigurationService _configurationService;
    private readonly INetworkService _networkService;
    private readonly ILoggingService _loggingService;
    private readonly IRemoteTemplatesService _remoteTemplatesService;
    private readonly ILogger _logger;

    public TemplatesCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        IDirectoryManagementService directoryManagement,
        IConfigurationService configurationService,
        INetworkService networkService,
        ILoggingService loggingService,
        IRemoteTemplatesService remoteTemplatesService)
    {
        _consoleDisplay = consoleDisplay;
        _interactiveSelection = interactiveSelection;
        _directoryManagement = directoryManagement;
        _configurationService = configurationService;
        _networkService = networkService;
        _loggingService = loggingService;
        _remoteTemplatesService = remoteTemplatesService;
        _logger = _loggingService.GetLogger("Deck.Console.TemplatesCommand");
    }

    public async Task<bool> ExecuteListAsync()
    {
        try
        {
            _logger.LogInformation("Starting templates list command execution");

            _consoleDisplay.ShowInfo("📋 可用模板列表");
            _consoleDisplay.WriteLine();

            // Get template configurations using directory management service
            var templateConfigs = await GetTemplateConfigurationsAsync();

            if (!templateConfigs.Any())
            {
                _consoleDisplay.ShowInfo("暂无可用模板");
                _consoleDisplay.ShowInfo("💡 使用 'deck templates update' 获取远程模板");
                return true;
            }

            // Display templates
            _consoleDisplay.ShowInfo($"🌐 远程模板 ({templateConfigs.Count} 个):");
            for (int i = 0; i < templateConfigs.Count; i++)
            {
                var template = templateConfigs[i];
                _consoleDisplay.ShowInfo($"  {i + 1,2}. {template.Name}");
            }

            _consoleDisplay.WriteLine();
            _consoleDisplay.ShowInfo("💡 模板管理命令:");
            _consoleDisplay.ShowInfo("   deck templates update     # 更新远程模板");
            _consoleDisplay.ShowInfo("   deck templates config     # 显示模板配置");
            _consoleDisplay.ShowInfo("   deck templates sync       # 同步模板到项目");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templates list command execution failed");
            _consoleDisplay.ShowError($"❌ 执行失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteUpdateAsync()
    {
        try
        {
            _logger.LogInformation("Starting templates update command execution");
            _consoleDisplay.ShowInfo("🔄 更新远程模板...");
            _consoleDisplay.WriteLine();
            
            // 执行模板同步
            var syncResult = await _remoteTemplatesService.SyncTemplatesAsync(forceUpdate: true);
            
            if (syncResult.Success)
            {
                _consoleDisplay.ShowSuccess($"✅ 模板同步成功！同步了 {syncResult.SyncedTemplateCount} 个模板");
                
                if (syncResult.NewTemplates.Any())
                {
                    _consoleDisplay.ShowInfo("📋 新同步的模板:");
                    foreach (var template in syncResult.NewTemplates)
                    {
                        _consoleDisplay.ShowInfo($"  • {template}");
                    }
                }
                
                // 显示同步日志
                foreach (var log in syncResult.SyncLogs)
                {
                    _consoleDisplay.ShowInfo($"💡 {log}");
                }
                
                _consoleDisplay.WriteLine();
                _consoleDisplay.ShowInfo("💡 现在可以使用 'deck templates list' 查看可用模板");
                _consoleDisplay.ShowInfo("💡 或者使用 'deck start' 选择模板创建开发环境");
                
                return true;
            }
            else
            {
                _consoleDisplay.ShowError("❌ 模板同步失败");
                
                // 显示错误日志
                foreach (var log in syncResult.SyncLogs)
                {
                    _consoleDisplay.ShowError($"   {log}");
                }
                
                _consoleDisplay.WriteLine();
                _consoleDisplay.ShowInfo("💡 请检查网络连接和Git配置，然后重试");
                _consoleDisplay.ShowInfo("💡 可以使用 'deck doctor' 检查系统环境");
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templates update command execution failed");
            _consoleDisplay.ShowError($"❌ 更新模板失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteConfigAsync()
    {
        try
        {
            _logger.LogInformation("Starting templates config command execution");

            _consoleDisplay.ShowInfo("⚙️ 模板配置信息");
            _consoleDisplay.WriteLine();

            // Try to load configuration
            try
            {
                var config = await _configurationService.GetConfigAsync();
                if (config?.RemoteTemplates != null)
                {
                    _consoleDisplay.ShowSuccess($"📦 模板仓库: {config.RemoteTemplates.Repository}");
                    _consoleDisplay.ShowInfo($"🌲 分支: {config.RemoteTemplates.Branch}");
                    _consoleDisplay.ShowInfo($"🔄 自动更新: {(config.RemoteTemplates.AutoUpdate ? "开启" : "关闭")}");
                    _consoleDisplay.ShowInfo($"💾 缓存TTL: {config.RemoteTemplates.CacheTtl}");
                }
                else
                {
                    _consoleDisplay.ShowWarning("⚠️ 未找到模板配置");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load configuration");
                _consoleDisplay.ShowWarning("⚠️ 配置加载失败，使用默认设置");
            }

            _consoleDisplay.WriteLine();
            _consoleDisplay.ShowInfo("💡 模板管理命令:");
            _consoleDisplay.ShowInfo("   deck templates update     # 更新远程模板");
            _consoleDisplay.ShowInfo("   deck templates sync       # 同步模板到项目");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templates config command execution failed");
            _consoleDisplay.ShowError($"❌ 显示模板配置失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteSyncAsync()
    {
        try
        {
            _logger.LogInformation("Starting templates sync command execution");

            _consoleDisplay.ShowInfo("🔄 手动同步模板到项目...");

            // For now, show a placeholder message
            // TODO: Implement full template sync logic
            _consoleDisplay.ShowWarning("⚠️  Templates sync 功能正在开发中");
            _consoleDisplay.ShowInfo("💡 将同步远程模板到项目目录");

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templates sync command execution failed");
            _consoleDisplay.ShowError($"❌ 同步模板失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteCleanAsync()
    {
        try
        {
            _logger.LogInformation("Starting templates clean command execution");

            _consoleDisplay.ShowWarning("💡 Templates 清理特别提示");
            _consoleDisplay.WriteLine();
            _consoleDisplay.ShowInfo("Templates 目录每次执行 'deck start' 时都会从远程仓库自动覆盖更新");
            _consoleDisplay.ShowInfo("");
            _consoleDisplay.ShowInfo("建议使用以下命令替代:");
            _consoleDisplay.ShowSuccess("  deck templates update  # 立即从仓库更新模板");
            _consoleDisplay.ShowSuccess("  直接执行 deck start   # 会自动更新并使用最新模板");
            _consoleDisplay.ShowInfo("");
            _consoleDisplay.ShowWarning("清理 Templates 目录意义不大，因为会被自动覆盖。");

            // For now, just show the recommendation
            // TODO: Implement full template clean logic if user confirms
            _consoleDisplay.ShowWarning("⚠️  Templates clean 功能正在开发中");
            _consoleDisplay.ShowInfo("💡 推荐使用 'deck templates update' 更新模板");

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templates clean command execution failed");
            _consoleDisplay.ShowError($"❌ 清理模板失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取模板配置列表（简化实现）
    /// </summary>
    private async Task<List<ConfigurationOption>> GetTemplateConfigurationsAsync()
    {
        try
        {
            // Try to get templates from directory management service
            var threeLayerOptions = await _directoryManagement.GetThreeLayerOptionsAsync();
            return threeLayerOptions?.Templates ?? new List<ConfigurationOption>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get template configurations, returning empty list");
            return new List<ConfigurationOption>();
        }
    }
}