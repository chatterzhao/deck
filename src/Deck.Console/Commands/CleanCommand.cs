using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;

namespace Deck.Console.Commands;

/// <summary>
/// Clean命令 - 三层配置清理选择
/// 提供Images/Custom/Templates三层清理选项
/// </summary>
public class CleanCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly ICleaningService _cleaningService;
    private readonly IDirectoryManagementService _directoryManagement;
    private readonly ILoggingService _loggingService;
    private readonly ILogger _logger;

    public CleanCommand(
        IConsoleDisplay consoleDisplay,
        IInteractiveSelectionService interactiveSelection,
        ICleaningService cleaningService,
        IDirectoryManagementService directoryManagement,
        ILoggingService loggingService)
    {
        _consoleDisplay = consoleDisplay;
        _interactiveSelection = interactiveSelection;
        _cleaningService = cleaningService;
        _directoryManagement = directoryManagement;
        _loggingService = loggingService;
        _logger = _loggingService.GetLogger("Deck.Console.CleanCommand");
    }

    public async Task<bool> ExecuteAsync(int keepCount = 5)
    {
        try
        {
            _logger.LogInformation("Starting clean command execution with keep-count: {KeepCount}", keepCount);

            _consoleDisplay.ShowInfo("🧹 资源清理 - 三层配置选择");
            _consoleDisplay.WriteLine();

            // Get three-layer options to show what's available for cleaning
            await DisplayThreeLayerCleaningOptionsAsync(keepCount);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clean command execution failed");
            _consoleDisplay.ShowError($"❌ 清理操作失败: {ex.Message}");
            return false;
        }
    }

    private async Task DisplayThreeLayerCleaningOptionsAsync(int keepCount)
    {
        try
        {
            // Get three-layer options from directory management service
            var threeLayerOptions = await _directoryManagement.GetThreeLayerOptionsAsync();

            var layerOptions = new List<(string Name, int Count, string Description)>();

            // Images layer
            if (threeLayerOptions?.Images != null && threeLayerOptions.Images.Any())
            {
                layerOptions.Add(("images", threeLayerOptions.Images.Count, 
                    "已构建镜像配置 - 包含 .deck/images/ 目录内容"));
            }

            // Custom layer
            if (threeLayerOptions?.Custom != null && threeLayerOptions.Custom.Any())
            {
                layerOptions.Add(("custom", threeLayerOptions.Custom.Count, 
                    "用户自定义配置 - 包含 .deck/custom/ 目录内容"));
            }

            // Templates layer
            if (threeLayerOptions?.Templates != null && threeLayerOptions.Templates.Any())
            {
                layerOptions.Add(("templates", threeLayerOptions.Templates.Count, 
                    "远程模板 - 包含 .deck/templates/ 目录内容"));
            }

            if (!layerOptions.Any())
            {
                _consoleDisplay.ShowInfo("✨ 暂无资源需要清理");
                _consoleDisplay.ShowInfo("💡 您的环境已经很干净了！");
                return;
            }

            // Display available cleaning options
            _consoleDisplay.ShowInfo("请选择要清理的配置层:");
            _consoleDisplay.WriteLine();

            for (int i = 0; i < layerOptions.Count; i++)
            {
                var option = layerOptions[i];
                _consoleDisplay.ShowInfo($"  {i + 1}. {GetLayerIcon(option.Name)} {GetLayerDisplayName(option.Name)} ({option.Count} 个配置)");
                _consoleDisplay.ShowInfo($"     {option.Description}");
                if (i < layerOptions.Count - 1)
                {
                    _consoleDisplay.WriteLine();
                }
            }

            _consoleDisplay.WriteLine();

            // Show cleaning recommendations
            _consoleDisplay.ShowInfo("💡 清理建议:");
            _consoleDisplay.ShowInfo($"   Images: 保留最新 {keepCount} 个镜像配置");
            _consoleDisplay.ShowInfo("   Custom: 交互式选择要删除的配置");
            _consoleDisplay.ShowInfo("   Templates: 不推荐清理（会自动更新）");

            _consoleDisplay.WriteLine();

            // For now, just show the available options
            // TODO: Implement interactive selection and actual cleaning logic
            _consoleDisplay.ShowWarning("⚠️  Clean 命令功能正在开发中");
            _consoleDisplay.ShowInfo("💡 当前仅显示可清理的配置统计");
            _consoleDisplay.ShowInfo("💡 您可以使用以下命令进行具体清理:");
            _consoleDisplay.ShowInfo("   deck images clean    # 清理镜像配置");
            _consoleDisplay.ShowInfo("   deck custom clean    # 清理自定义配置");
            _consoleDisplay.ShowInfo("   deck templates clean # 清理模板（不推荐）");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display cleaning options");
            _consoleDisplay.ShowError($"❌ 获取清理选项失败: {ex.Message}");
        }
    }

    private static string GetLayerIcon(string layerName) => layerName switch
    {
        "images" => "🖼️",
        "custom" => "⚙️",
        "templates" => "📦",
        _ => "📁"
    };

    private static string GetLayerDisplayName(string layerName) => layerName switch
    {
        "images" => "Images",
        "custom" => "Custom", 
        "templates" => "Templates",
        _ => layerName
    };
}