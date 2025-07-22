using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Console.Commands;

/// <summary>
/// Images主命令 - 镜像三层统一管理
/// </summary>
public class ImagesCommand
{
    private readonly IConsoleDisplay _consoleDisplay;
    private readonly IImagesUnifiedService _imagesUnifiedService;
    private readonly IInteractiveSelectionService _interactiveSelection;
    private readonly ILoggingService _loggingService;
    private readonly ILogger<ImagesCommand> _logger;

    public ImagesCommand(
        IConsoleDisplay consoleDisplay,
        IImagesUnifiedService imagesUnifiedService,
        IInteractiveSelectionService interactiveSelection,
        ILoggingService loggingService)
    {
        _consoleDisplay = consoleDisplay;
        _imagesUnifiedService = imagesUnifiedService;
        _interactiveSelection = interactiveSelection;
        _loggingService = loggingService;
        _logger = _loggingService.GetLogger<ImagesCommand>();
    }

    /// <summary>
    /// 执行Images列表显示 - 三层统一列表显示
    /// </summary>
    public async Task<bool> ExecuteListAsync()
    {
        try
        {
            _logger.LogInformation("执行Images列表显示命令");
            _consoleDisplay.ShowInfo("📋 正在加载三层统一镜像列表...");

            // 获取三层统一资源列表
            var resourceList = await _imagesUnifiedService.GetUnifiedResourceListAsync();
            
            if (IsResourceListEmpty(resourceList))
            {
                _consoleDisplay.ShowWarning("未找到任何镜像资源");
                _consoleDisplay.ShowInfo("使用 'deck start <env-type>' 创建第一个镜像");
                return true;
            }

            // 显示三层统一列表
            DisplayUnifiedResourceList(resourceList);
            
            _logger.LogInformation("Images列表显示完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Images列表显示失败");
            _consoleDisplay.ShowError($"列表显示失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行Images智能清理 - 智能清理选择
    /// </summary>
    public async Task<bool> ExecuteCleanAsync(int keepCount = 5)
    {
        try
        {
            _logger.LogInformation("执行Images智能清理命令, 保留数量: {KeepCount}", keepCount);
            _consoleDisplay.ShowInfo($"🧹 正在分析镜像清理策略 (保留: {keepCount} 个)...");

            // 获取清理选项
            var cleaningOptions = await _imagesUnifiedService.GetCleaningOptionsAsync();
            
            if (cleaningOptions.Count == 0)
            {
                _consoleDisplay.ShowInfo("没有需要清理的资源");
                return true;
            }

            // 显示清理选项供用户选择
            var selectedOption = await SelectCleaningOptionAsync(cleaningOptions, keepCount);
            if (selectedOption == null)
            {
                _consoleDisplay.ShowInfo("已取消清理操作");
                return true;
            }

            // 执行清理
            var result = await _imagesUnifiedService.ExecuteCleaningAsync(
                selectedOption,
                confirmationCallback: ConfirmCleaningAsync);

            if (result.IsSuccess)
            {
                _consoleDisplay.ShowSuccess($"清理完成: 删除了 {result.CleanedCount} 个资源");
                if (result.CleanedResources.Any())
                {
                    _consoleDisplay.ShowInfo("清理详情:");
                    foreach (var resource in result.CleanedResources)
                    {
                        _consoleDisplay.ShowInfo($"  • {resource}");
                    }
                }
            }
            else
            {
                _consoleDisplay.ShowError($"清理失败: {result.ErrorMessage}");
                return false;
            }

            _logger.LogInformation("Images智能清理完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Images智能清理失败");
            _consoleDisplay.ShowError($"清理失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行Images详细信息显示
    /// </summary>
    public async Task<bool> ExecuteInfoAsync(string? imageName = null)
    {
        try
        {
            _logger.LogInformation("执行Images详细信息显示命令, 镜像名称: {ImageName}", imageName ?? "interactive-select");

            // 如果没有指定镜像名称，进行交互式选择
            if (string.IsNullOrEmpty(imageName))
            {
                imageName = await SelectImageInteractivelyAsync();
                if (string.IsNullOrEmpty(imageName))
                {
                    _consoleDisplay.ShowInfo("已取消操作");
                    return true;
                }
            }

            _consoleDisplay.ShowInfo($"ℹ️  正在获取镜像详细信息: {imageName}...");

            // 获取资源详细信息
            var detail = await _imagesUnifiedService.GetResourceDetailAsync(UnifiedResourceType.Images, imageName);
            
            if (detail == null)
            {
                _consoleDisplay.ShowError($"未找到镜像: {imageName}");
                return false;
            }

            // 显示详细信息
            DisplayResourceDetail(detail);
            
            _logger.LogInformation("Images详细信息显示完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Images详细信息显示失败");
            _consoleDisplay.ShowError($"信息获取失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行Images权限帮助显示 - 三层管理逻辑说明
    /// </summary>
    public Task<bool> ExecuteHelpAsync()
    {
        try
        {
            _logger.LogInformation("执行Images权限帮助显示命令");
            
            ShowImagesPermissionHelp();
            
            _logger.LogInformation("Images权限帮助显示完成");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Images权限帮助显示失败");
            _consoleDisplay.ShowError($"帮助显示失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    #region Private Helper Methods

    private bool IsResourceListEmpty(UnifiedResourceList resourceList)
    {
        return resourceList.Images.Count == 0 && 
               resourceList.Custom.Count == 0 && 
               resourceList.Templates.Count == 0;
    }

    private void DisplayUnifiedResourceList(UnifiedResourceList resourceList)
    {
        _consoleDisplay.ShowTitle("🏗️  Deck 三层统一镜像管理");
        _consoleDisplay.ShowSeparator();

        // 显示Images层
        if (resourceList.Images.Any())
        {
            _consoleDisplay.ShowSubtitle($"📦 Images层 ({resourceList.Images.Count} 个已构建镜像)");
            foreach (var image in resourceList.Images)
            {
                var statusIcon = image.IsAvailable ? "✅" : "❌";
                var unavailableInfo = !image.IsAvailable ? $" - {image.UnavailableReason}" : "";
                _consoleDisplay.ShowInfo($"  {statusIcon} {image.Name} ({image.RelativeTime}){unavailableInfo}");
            }
            _consoleDisplay.ShowSeparator();
        }

        // 显示Custom层
        if (resourceList.Custom.Any())
        {
            _consoleDisplay.ShowSubtitle($"🛠️  Custom层 ({resourceList.Custom.Count} 个自定义配置)");
            foreach (var custom in resourceList.Custom)
            {
                var statusIcon = custom.IsAvailable ? "✅" : "❌";
                var unavailableInfo = !custom.IsAvailable ? $" - {custom.UnavailableReason}" : "";
                _consoleDisplay.ShowInfo($"  {statusIcon} {custom.Name}{unavailableInfo}");
            }
            _consoleDisplay.ShowSeparator();
        }

        // 显示Templates层
        if (resourceList.Templates.Any())
        {
            _consoleDisplay.ShowSubtitle($"📋 Templates层 ({resourceList.Templates.Count} 个模板)");
            foreach (var template in resourceList.Templates)
            {
                var statusIcon = template.Status == ResourceStatus.Builtin ? "🔧" : "📁";
                _consoleDisplay.ShowInfo($"  {statusIcon} {template.Name} {(template.Status == ResourceStatus.Builtin ? "(内置模板)" : "")}");
            }
        }
    }

    private void DisplayResourceDetail(UnifiedResourceDetail detail)
    {
        _consoleDisplay.ShowTitle($"📦 镜像详细信息: {detail.Resource.Name}");
        _consoleDisplay.ShowSeparator();

        // 基本信息
        _consoleDisplay.ShowSubtitle("基本信息:");
        _consoleDisplay.ShowInfo($"  名称: {detail.Resource.Name}");
        _consoleDisplay.ShowInfo($"  类型: {GetResourceTypeDisplayName(detail.Resource.Type)}");
        _consoleDisplay.ShowInfo($"  状态: {GetResourceStatusDisplayName(detail.Resource.Status)}");
        _consoleDisplay.ShowInfo($"  可用性: {(detail.Resource.IsAvailable ? "可用" : $"不可用 ({detail.Resource.UnavailableReason})")}");

        if (!string.IsNullOrEmpty(detail.Resource.RelativeTime))
        {
            _consoleDisplay.ShowInfo($"  创建时间: {detail.Resource.RelativeTime}");
        }

        _consoleDisplay.ShowSeparator();

        // 配置文件状态
        _consoleDisplay.ShowSubtitle("配置文件状态:");
        _consoleDisplay.ShowInfo($"  Dockerfile: {(detail.ConfigurationStatus.HasDockerfile ? "✅" : "❌")}");
        _consoleDisplay.ShowInfo($"  compose.yaml: {(detail.ConfigurationStatus.HasComposeYaml ? "✅" : "❌")}");
        _consoleDisplay.ShowInfo($"  .env: {(detail.ConfigurationStatus.HasEnvFile ? "✅" : "❌")}");

        if (detail.ConfigurationStatus.MissingFiles.Any())
        {
            _consoleDisplay.ShowWarning($"  缺少文件: {string.Join(", ", detail.ConfigurationStatus.MissingFiles)}");
        }

        _consoleDisplay.ShowSeparator();

        // 文件系统信息
        _consoleDisplay.ShowSubtitle("文件系统信息:");
        _consoleDisplay.ShowInfo($"  目录路径: {detail.FileSystemInfo.DirectoryPath}");
        _consoleDisplay.ShowInfo($"  目录大小: {detail.FileSystemInfo.DirectorySize}");
        _consoleDisplay.ShowInfo($"  创建时间: {detail.FileSystemInfo.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        _consoleDisplay.ShowInfo($"  修改时间: {detail.FileSystemInfo.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
    }

    private async Task<CleaningOption?> SelectCleaningOptionAsync(List<CleaningOption> options, int keepCount)
    {
        _consoleDisplay.ShowSubtitle("请选择清理策略:");
        _consoleDisplay.ShowSeparator();

        var selectableOptions = options.Select(option => new SelectableOption
        {
            Value = option.Id,
            DisplayName = option.DisplayName,
            Description = option.Description,
            ExtraInfo = $"{option.EstimatedCount} 个项目",
            IsAvailable = true
        }).ToList();

        var selector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "请选择清理策略",
            Items = selectableOptions,
            AllowMultiple = false,
            Required = true,
            EnableSearch = false
        };

        var result = await _interactiveSelection.ShowSingleSelectionAsync(selector);
        
        if (result.IsCancelled)
            return null;
            
        var selectedId = result.SelectedItems.FirstOrDefault()?.Value;
        return options.FirstOrDefault(o => o.Id == selectedId);
    }

    private async Task<bool> ConfirmCleaningAsync(string confirmationMessage)
    {
        _consoleDisplay.ShowWarning(confirmationMessage);
        return await _interactiveSelection.ShowConfirmationAsync("确认执行清理操作?", false);
    }

    private async Task<string?> SelectImageInteractivelyAsync()
    {
        _consoleDisplay.ShowInfo("正在加载镜像列表...");

        var resourceList = await _imagesUnifiedService.GetUnifiedResourceListAsync();
        
        if (!resourceList.Images.Any())
        {
            _consoleDisplay.ShowWarning("未找到任何镜像");
            return null;
        }

        var selectableItems = resourceList.Images
            .Where(image => image.IsAvailable)
            .Select(image => new SelectableOption
            {
                Value = image.Name,
                DisplayName = image.Name,
                Description = $"{image.RelativeTime} - {(image.IsAvailable ? "可用" : "不可用")}",
                IsAvailable = image.IsAvailable
            }).ToList();

        if (!selectableItems.Any())
        {
            _consoleDisplay.ShowWarning("没有可用的镜像");
            return null;
        }

        var selector = new InteractiveSelector<SelectableOption>
        {
            Prompt = "请选择镜像",
            Items = selectableItems,
            AllowMultiple = false,
            Required = false,
            EnableSearch = true,
            SearchPlaceholder = "输入镜像名称进行搜索..."
        };

        var result = await _interactiveSelection.ShowSingleSelectionAsync(selector);

        return result.IsCancelled ? null : result.SelectedItems.FirstOrDefault()?.Value;
    }

    private void ShowImagesPermissionHelp()
    {
        _consoleDisplay.ShowTitle("🛡️  Deck 三层统一管理 - Images目录权限说明");
        _consoleDisplay.ShowSeparator();

        _consoleDisplay.ShowSubtitle("📋 三层架构说明:");
        _consoleDisplay.ShowInfo("  🔸 Templates层 (.deck/templates/) - 基础模板，系统管理，只读");
        _consoleDisplay.ShowInfo("  🔸 Custom层 (.deck/custom/) - 用户配置，完全可编辑");
        _consoleDisplay.ShowInfo("  🔸 Images层 (.deck/images/) - 构建快照，受限编辑");
        
        _consoleDisplay.ShowSeparator();
        
        _consoleDisplay.ShowSubtitle("🔐 Images目录权限规则:");
        _consoleDisplay.ShowInfo("  ✅ 允许操作:");
        _consoleDisplay.ShowInfo("    • 修改 .env 文件中的运行时变量");
        _consoleDisplay.ShowInfo("    • 调整端口设置 (DEV_PORT, DEBUG_PORT 等)");
        _consoleDisplay.ShowInfo("    • 更新 PROJECT_NAME 避免容器名冲突");
        _consoleDisplay.ShowInfo("    • 查看和管理镜像生命周期");
        
        _consoleDisplay.ShowSeparator();
        
        _consoleDisplay.ShowInfo("  ❌ 禁止操作:");
        _consoleDisplay.ShowInfo("    • 修改 Dockerfile 或 compose.yaml (它们是构建时快照)");
        _consoleDisplay.ShowInfo("    • 重命名镜像目录 (会破坏镜像-名称映射)");
        _consoleDisplay.ShowInfo("    • 删除关键配置文件");
        
        _consoleDisplay.ShowSeparator();
        
        _consoleDisplay.ShowSubtitle("🔄 推荐工作流程:");
        _consoleDisplay.ShowInfo("  1️⃣  开发阶段: 在 Custom层 创建和编辑配置");
        _consoleDisplay.ShowInfo("  2️⃣  构建阶段: 使用 'deck start' 从 Custom 构建到 Images");
        _consoleDisplay.ShowInfo("  3️⃣  运行阶段: 在 Images层 调整运行时参数(.env)");
        _consoleDisplay.ShowInfo("  4️⃣  清理阶段: 使用 'deck images clean' 智能清理旧镜像");
        
        _consoleDisplay.ShowSeparator();
        
        _consoleDisplay.ShowSubtitle("💡 相关命令:");
        _consoleDisplay.ShowInfo("  • deck images list     - 查看三层统一列表");
        _consoleDisplay.ShowInfo("  • deck images info     - 查看镜像详细信息");
        _consoleDisplay.ShowInfo("  • deck images clean    - 智能清理旧镜像");
        _consoleDisplay.ShowInfo("  • deck config create   - 在 Custom层 创建配置");
    }

    private static string GetResourceTypeDisplayName(UnifiedResourceType type)
    {
        return type switch
        {
            UnifiedResourceType.Images => "构建镜像",
            UnifiedResourceType.Custom => "自定义配置",
            UnifiedResourceType.Templates => "基础模板",
            _ => "未知类型"
        };
    }

    private static string GetResourceStatusDisplayName(ResourceStatus status)
    {
        return status switch
        {
            ResourceStatus.Ready => "就绪",
            ResourceStatus.Building => "构建中",
            ResourceStatus.Running => "运行中",
            ResourceStatus.Stopped => "已停止",
            ResourceStatus.Unavailable => "失败",
            ResourceStatus.Builtin => "内置",
            _ => "未知状态"
        };
    }

    #endregion
}