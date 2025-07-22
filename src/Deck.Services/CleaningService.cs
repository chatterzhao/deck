using Deck.Core.Models;
using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

public class CleaningService : ICleaningService
{
    private readonly ILogger<CleaningService> _logger;
    private readonly IFileSystemService _fileSystemService;
    private readonly IImagesUnifiedService _imagesUnifiedService;
    private readonly IInteractiveSelectionService _interactiveSelectionService;
    private readonly IDirectoryManagementService _directoryService;

    public CleaningService(
        ILogger<CleaningService> logger,
        IFileSystemService fileSystemService,
        IImagesUnifiedService imagesUnifiedService,
        IInteractiveSelectionService interactiveSelectionService,
        IDirectoryManagementService directoryService)
    {
        _logger = logger;
        _fileSystemService = fileSystemService;
        _imagesUnifiedService = imagesUnifiedService;
        _interactiveSelectionService = interactiveSelectionService;
        _directoryService = directoryService;
    }

    public async Task<ThreeLayerCleaningResult> GetCleaningOptionsAsync()
    {
        _logger.LogInformation("获取三层清理选项");

        var result = new ThreeLayerCleaningResult();

        try
        {
            // 获取统一资源列表
            var resources = await _imagesUnifiedService.GetUnifiedResourceListAsync();

            // 生成 Templates 清理选项
            result.TemplateOptions = GenerateTemplateCleaningOptions(resources.Templates);

            // 生成 Custom 清理选项  
            result.CustomOptions = GenerateCustomCleaningOptions(resources.Custom);

            // 生成 Images 清理选项
            result.ImageOptions = GenerateImageCleaningOptions(resources.Images);

            // 生成智能推荐
            result.Recommendation = GenerateCleaningRecommendation(result);

            _logger.LogInformation("三层清理选项生成完成 - Templates:{TemplateCount}, Custom:{CustomCount}, Images:{ImageCount}", 
                result.TemplateOptions.Count, result.CustomOptions.Count, result.ImageOptions.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取清理选项时发生错误");
            throw;
        }
    }

    public async Task<ImagesCleaningResult> GetImagesCleaningStrategyAsync(int keepCount = 3, bool dryRun = false)
    {
        _logger.LogInformation("获取镜像清理策略 - 保留数量:{KeepCount}, 试运行:{DryRun}", keepCount, dryRun);

        try
        {
            var result = new ImagesCleaningResult
            {
                IsDryRun = dryRun
            };

            // 获取所有镜像资源
            var resources = await _imagesUnifiedService.GetUnifiedResourceListAsync();
            var imageResources = resources.Images;

            // 按前缀分组镜像 (基于 deck-shell 的 clean_old_images 逻辑)
            result.ImagesByPrefix = GroupImagesByPrefix(imageResources);

            // 计算每个前缀组的清理策略
            foreach (var (prefix, images) in result.ImagesByPrefix)
            {
                var sortedImages = SortImagesByTimestamp(images);
                
                if (sortedImages.Count <= keepCount)
                {
                    result.ImagesToKeep.AddRange(sortedImages.Select(ConvertToImageInfo));
                    continue;
                }

                // 保留最新的 N 个
                var toKeep = sortedImages.Take(keepCount).ToList();
                var toRemove = sortedImages.Skip(keepCount).ToList();

                result.ImagesToKeep.AddRange(toKeep.Select(ConvertToImageInfo));
                result.ImagesToRemove.AddRange(toRemove.Select(ConvertToImageInfo));
            }

            result.TotalToRemove = result.ImagesToRemove.Count;
            result.SpaceToFree = CalculateSpaceToFree(result.ImagesToRemove);

            _logger.LogInformation("镜像清理策略生成完成 - 保留:{KeepCount}, 删除:{RemoveCount}, 释放空间:{SpaceToFree}MB", 
                result.ImagesToKeep.Count, result.TotalToRemove, result.SpaceToFree / (1024 * 1024));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取镜像清理策略时发生错误");
            throw;
        }
    }

    public async Task<bool> ExecuteCleaningAsync(CleaningOperation operation)
    {
        _logger.LogInformation("执行清理操作 - 类型:{Type}, 项目数:{ItemCount}, 试运行:{DryRun}", 
            operation.Type, operation.ItemsToClean.Count, operation.DryRun);

        try
        {
            // 验证清理操作
            if (!await ValidateCleaningOperationAsync(operation))
            {
                return false;
            }

            // 根据类型执行不同的清理逻辑
            return operation.Type switch
            {
                CleaningType.Images => await ExecuteImagesCleaningAsync(operation),
                CleaningType.Custom => await ExecuteCustomCleaningAsync(operation),
                CleaningType.Templates => await ExecuteTemplatesCleaningAsync(operation),
                CleaningType.All => await ExecuteAllCleaningAsync(operation),
                CleaningType.Selective => await ExecuteSelectiveCleaningAsync(operation),
                _ => throw new ArgumentException($"不支持的清理类型: {operation.Type}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行清理操作时发生错误");
            return false;
        }
    }

    public async Task<TemplatesCleaningResult> GetTemplatesCleaningAlternativesAsync()
    {
        _logger.LogInformation("获取模板清理替代方案");

        var result = new TemplatesCleaningResult();

        try
        {
            // 获取模板资源
            var resources = await _imagesUnifiedService.GetUnifiedResourceListAsync();
            var templates = resources.Templates;

            // 分析过时和未使用的模板
            result.OutdatedTemplates = await FindOutdatedTemplatesAsync(templates);
            result.UnusedTemplates = await FindUnusedTemplatesAsync(templates);

            // 生成智能替代方案
            result.Alternatives = GenerateTemplateAlternatives();

            _logger.LogInformation("模板清理替代方案生成完成 - 过时:{OutdatedCount}, 未使用:{UnusedCount}, 替代方案:{AlternativeCount}",
                result.OutdatedTemplates.Count, result.UnusedTemplates.Count, result.Alternatives.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模板清理替代方案时发生错误");
            throw;
        }
    }

    public async Task<List<CleaningWarning>> GetCleaningWarningsAsync(CleaningType cleaningType, List<string> itemsToClean)
    {
        _logger.LogInformation("获取清理警告 - 类型:{Type}, 项目数:{ItemCount}", cleaningType, itemsToClean.Count);

        var warnings = new List<CleaningWarning>();

        try
        {
            switch (cleaningType)
            {
                case CleaningType.Images:
                    warnings.AddRange(await GetImagesCleaningWarningsAsync(itemsToClean));
                    break;
                case CleaningType.Custom:
                    warnings.AddRange(await GetCustomCleaningWarningsAsync(itemsToClean));
                    break;
                case CleaningType.Templates:
                    warnings.AddRange(await GetTemplatesCleaningWarningsAsync(itemsToClean));
                    break;
                case CleaningType.All:
                    warnings.AddRange(await GetAllCleaningWarningsAsync());
                    break;
            }

            return warnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取清理警告时发生错误");
            return warnings;
        }
    }

    public async Task<bool> ValidateCleaningOperationAsync(CleaningOperation operation)
    {
        try
        {
            // 基本验证
            if (operation.ItemsToClean.Count == 0 && operation.Type != CleaningType.All)
            {
                _logger.LogWarning("清理操作验证失败: 没有指定要清理的项目");
                return false;
            }

            // 验证项目是否存在
            var resources = await _imagesUnifiedService.GetUnifiedResourceListAsync();
            var allResources = resources.GetFlattenedResources();

            foreach (var item in operation.ItemsToClean)
            {
                if (!allResources.Any(r => r.Name == item))
                {
                    _logger.LogWarning("清理操作验证失败: 项目不存在 - {Item}", item);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证清理操作时发生错误");
            return false;
        }
    }

    #region 私有辅助方法

    private List<CleaningOption> GenerateTemplateCleaningOptions(List<UnifiedResource> templates)
    {
        return new List<CleaningOption>
        {
            new CleaningOption
            {
                Id = "templates-update",
                DisplayName = "更新模板库", 
                Description = "从远程仓库更新模板库到最新版本",
                ResourceType = UnifiedResourceType.Templates,
                Strategy = CleaningStrategy.SmartSuggestion,
                WarningLevel = ConfirmationLevel.Low,
                EstimatedCount = templates.Count
            },
            new CleaningOption
            {
                Id = "templates-smart-suggest",
                DisplayName = "智能提示替代",
                Description = "不直接删除模板，而是提供管理建议",
                ResourceType = UnifiedResourceType.Templates,
                Strategy = CleaningStrategy.SmartSuggestion,
                WarningLevel = ConfirmationLevel.Low,
                EstimatedCount = 0
            }
        };
    }

    private List<CleaningOption> GenerateCustomCleaningOptions(List<UnifiedResource> customs)
    {
        return new List<CleaningOption>
        {
            new CleaningOption
            {
                Id = "custom-selective",
                DisplayName = "选择性清理",
                Description = "选择特定的自定义配置进行清理",
                ResourceType = UnifiedResourceType.Custom,
                Strategy = CleaningStrategy.DeleteSpecific,
                WarningLevel = ConfirmationLevel.Medium,
                EstimatedCount = customs.Count
            },
            new CleaningOption
            {
                Id = "custom-unused",
                DisplayName = "清理未使用配置",
                Description = "清理长时间未使用的自定义配置",
                ResourceType = UnifiedResourceType.Custom,
                Strategy = CleaningStrategy.DeleteSpecific,
                WarningLevel = ConfirmationLevel.Medium,
                EstimatedCount = customs.Count(c => !c.IsAvailable)
            }
        };
    }

    private List<CleaningOption> GenerateImageCleaningOptions(List<UnifiedResource> images)
    {
        var groupsByPrefix = GroupImagesByPrefix(images);

        return new List<CleaningOption>
        {
            new CleaningOption
            {
                Id = "images-keep3",
                DisplayName = "保留最新3个",
                Description = "每个项目保留最新3个镜像版本",
                ResourceType = UnifiedResourceType.Images,
                Strategy = CleaningStrategy.KeepLatestN,
                WarningLevel = ConfirmationLevel.Low,
                EstimatedCount = CalculateCleanupCount(groupsByPrefix, 3),
                Parameters = new Dictionary<string, object> { ["keepCount"] = 3 }
            },
            new CleaningOption
            {
                Id = "images-keep5",
                DisplayName = "保留最新5个",
                Description = "每个项目保留最新5个镜像版本",
                ResourceType = UnifiedResourceType.Images,
                Strategy = CleaningStrategy.KeepLatestN,
                WarningLevel = ConfirmationLevel.Low,
                EstimatedCount = CalculateCleanupCount(groupsByPrefix, 5),
                Parameters = new Dictionary<string, object> { ["keepCount"] = 5 }
            },
            new CleaningOption
            {
                Id = "images-selective",
                DisplayName = "选择性清理",
                Description = "手动选择要清理的镜像",
                ResourceType = UnifiedResourceType.Images,
                Strategy = CleaningStrategy.DeleteSpecific,
                WarningLevel = ConfirmationLevel.Medium,
                EstimatedCount = images.Count
            }
        };
    }

    private CleaningRecommendation GenerateCleaningRecommendation(ThreeLayerCleaningResult result)
    {
        var recommendation = new CleaningRecommendation
        {
            Strategy = CleaningStrategy.Balanced
        };

        var recommendedActions = new List<string>();
        var warnings = new List<string>();

        // 分析推荐的清理策略
        if (result.ImageOptions.Any(o => o.Strategy == CleaningStrategy.KeepLatestN))
        {
            recommendedActions.Add("建议优先清理镜像层，保留每个项目最新3个版本");
        }

        if (result.TemplateOptions.Any(o => o.Strategy == CleaningStrategy.SmartSuggestion))
        {
            recommendedActions.Add("建议更新模板库而不是删除，确保使用最新模板");
        }

        if (result.CustomOptions.Any(o => o.Strategy == CleaningStrategy.DeleteSpecific))
        {
            recommendedActions.Add("建议清理长时间未使用的自定义配置");
        }

        warnings.Add("清理前建议备份重要配置文件");
        warnings.Add("首次清理建议使用试运行模式预览结果");

        recommendation.RecommendedActions = recommendedActions;
        recommendation.Warnings = warnings;
        recommendation.Summary = $"建议采用平衡策略，优先清理{result.ImageOptions.Where(o => o.Strategy == CleaningStrategy.KeepLatestN).Sum(o => o.EstimatedCount)}个镜像";

        return recommendation;
    }

    private Dictionary<string, List<ImageInfo>> GroupImagesByPrefix(List<UnifiedResource> imageResources)
    {
        var groups = new Dictionary<string, List<ImageInfo>>();

        foreach (var resource in imageResources)
        {
            var prefix = ExtractImagePrefix(resource.Name);
            if (!groups.ContainsKey(prefix))
            {
                groups[prefix] = new List<ImageInfo>();
            }

            groups[prefix].Add(ConvertToImageInfo(resource));
        }

        return groups;
    }

    private string ExtractImagePrefix(string imageName)
    {
        // 基于 deck-shell 的命名规则：前缀-YYYYMMDD-HHMM
        var match = System.Text.RegularExpressions.Regex.Match(imageName, @"^(.+)-\d{8}-\d{4}$");
        return match.Success ? match.Groups[1].Value : imageName;
    }

    private List<UnifiedResource> SortImagesByTimestamp(List<ImageInfo> images)
    {
        return images.OrderByDescending(i => i.Created)
                    .Select(ConvertToUnifiedResource)
                    .ToList();
    }

    private ImageInfo ConvertToImageInfo(UnifiedResource resource)
    {
        return new ImageInfo
        {
            Id = resource.Metadata.GetValueOrDefault("Id", resource.Name),
            Name = resource.Name,
            Tag = resource.Metadata.GetValueOrDefault("Tag", "latest"),
            Created = DateTime.TryParse(resource.Metadata.GetValueOrDefault("Created"), out var created) ? created : DateTime.MinValue,
            Size = long.TryParse(resource.Metadata.GetValueOrDefault("Size"), out var size) ? size : 0,
            Exists = resource.IsAvailable
        };
    }

    private UnifiedResource ConvertToUnifiedResource(ImageInfo image)
    {
        return new UnifiedResource
        {
            Name = image.Name,
            Type = UnifiedResourceType.Images,
            Status = image.Exists ? ResourceStatus.Ready : ResourceStatus.Unavailable,
            DisplayLabel = $"{image.Name}:{image.Tag}",
            RelativeTime = GetRelativeTime(image.Created),
            IsAvailable = image.Exists,
            Metadata = new Dictionary<string, string>
            {
                ["Id"] = image.Id,
                ["Tag"] = image.Tag,
                ["Created"] = image.Created.ToString("O"),
                ["Size"] = image.Size.ToString()
            }
        };
    }

    private long CalculateSpaceToFree(List<ImageInfo> images)
    {
        return images.Sum(i => i.Size);
    }

    private int CalculateCleanupCount(Dictionary<string, List<ImageInfo>> groupsByPrefix, int keepCount)
    {
        return groupsByPrefix.Values.Sum(images => Math.Max(0, images.Count - keepCount));
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;
        return timeSpan.TotalDays switch
        {
            < 1 => $"{(int)timeSpan.TotalHours}小时前",
            < 7 => $"{(int)timeSpan.TotalDays}天前",
            < 30 => $"{(int)(timeSpan.TotalDays / 7)}周前",
            _ => $"{(int)(timeSpan.TotalDays / 30)}个月前"
        };
    }

    private async Task<List<string>> FindOutdatedTemplatesAsync(List<UnifiedResource> templates)
    {
        // TODO: 实现模板版本检查逻辑
        await Task.Delay(100);
        return templates.Where(t => !t.IsAvailable).Select(t => t.Name).ToList();
    }

    private async Task<List<string>> FindUnusedTemplatesAsync(List<UnifiedResource> templates)
    {
        // TODO: 实现使用频率分析逻辑
        await Task.Delay(100);
        return templates.Where(t => !t.Metadata.ContainsKey("LastUsed")).Select(t => t.Name).ToList();
    }

    private List<CleaningAlternative> GenerateTemplateAlternatives()
    {
        return new List<CleaningAlternative>
        {
            new CleaningAlternative
            {
                Action = "update",
                Description = "更新模板库到最新版本",
                Command = "deck templates update",
                IsPreferred = true
            },
            new CleaningAlternative
            {
                Action = "sync",
                Description = "同步特定模板",
                Command = "deck templates sync <template-name>",
                IsPreferred = false
            },
            new CleaningAlternative
            {
                Action = "reset",
                Description = "重置模板库到干净状态",
                Command = "deck templates reset",
                IsPreferred = false
            }
        };
    }

    private Task<List<CleaningWarning>> GetImagesCleaningWarningsAsync(List<string> itemsToClean)
    {
        var warnings = new List<CleaningWarning>();
        
        // 检查是否有正在运行的容器
        foreach (var item in itemsToClean)
        {
            // TODO: 检查容器状态
            warnings.Add(new CleaningWarning
            {
                Message = $"镜像 {item} 可能有关联的运行容器",
                Level = CleaningWarningLevel.Warning,
                AffectedItems = new List<string> { item },
                Suggestion = "建议先停止相关容器再清理镜像"
            });
        }

        return Task.FromResult(warnings);
    }

    private Task<List<CleaningWarning>> GetCustomCleaningWarningsAsync(List<string> itemsToClean)
    {
        return Task.FromResult(new List<CleaningWarning>
        {
            new CleaningWarning
            {
                Message = "清理自定义配置将删除您的个人修改",
                Level = CleaningWarningLevel.Warning,
                AffectedItems = itemsToClean,
                Suggestion = "建议先备份重要的配置文件"
            }
        });
    }

    private Task<List<CleaningWarning>> GetTemplatesCleaningWarningsAsync(List<string> itemsToClean)
    {
        return Task.FromResult(new List<CleaningWarning>
        {
            new CleaningWarning
            {
                Message = "不建议直接删除模板，这可能影响新项目创建",
                Level = CleaningWarningLevel.Error,
                AffectedItems = itemsToClean,
                Suggestion = "建议使用更新命令而不是删除模板"
            }
        });
    }

    private Task<List<CleaningWarning>> GetAllCleaningWarningsAsync()
    {
        return Task.FromResult(new List<CleaningWarning>
        {
            new CleaningWarning
            {
                Message = "全量清理是高风险操作，将影响所有三个层级",
                Level = CleaningWarningLevel.Critical,
                AffectedItems = new List<string> { "Templates", "Custom", "Images" },
                Suggestion = "建议分层级逐步清理，避免一次性清理全部"
            }
        });
    }

    private Task<bool> ExecuteImagesCleaningAsync(CleaningOperation operation)
    {
        _logger.LogInformation("执行镜像清理");
        
        foreach (var item in operation.ItemsToClean)
        {
            if (operation.DryRun)
            {
                _logger.LogInformation("[试运行] 将删除镜像: {Item}", item);
                continue;
            }

            try
            {
                var imagePath = Path.Combine(".deck", "images", item);
                if (Directory.Exists(imagePath))
                {
                    Directory.Delete(imagePath, true);
                    _logger.LogInformation("已删除镜像目录: {Item}", item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除镜像时发生错误: {Item}", item);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    private Task<bool> ExecuteCustomCleaningAsync(CleaningOperation operation)
    {
        _logger.LogInformation("执行自定义配置清理");
        
        foreach (var item in operation.ItemsToClean)
        {
            if (operation.DryRun)
            {
                _logger.LogInformation("[试运行] 将删除自定义配置: {Item}", item);
                continue;
            }

            try
            {
                var customPath = Path.Combine(".deck", "custom", item);
                if (Directory.Exists(customPath))
                {
                    Directory.Delete(customPath, true);
                    _logger.LogInformation("已删除自定义配置目录: {Item}", item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除自定义配置时发生错误: {Item}", item);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    private Task<bool> ExecuteTemplatesCleaningAsync(CleaningOperation operation)
    {
        _logger.LogWarning("模板清理操作被拒绝，建议使用更新命令");
        return Task.FromResult(false);
    }

    private async Task<bool> ExecuteAllCleaningAsync(CleaningOperation operation)
    {
        _logger.LogInformation("执行全量清理");
        
        // 分别执行各层清理
        var imagesOp = new CleaningOperation { Type = CleaningType.Images, DryRun = operation.DryRun };
        var customOp = new CleaningOperation { Type = CleaningType.Custom, DryRun = operation.DryRun };

        var imagesResult = await ExecuteImagesCleaningAsync(imagesOp);
        var customResult = await ExecuteCustomCleaningAsync(customOp);

        return imagesResult && customResult;
    }

    private async Task<bool> ExecuteSelectiveCleaningAsync(CleaningOperation operation)
    {
        _logger.LogInformation("执行选择性清理");
        
        // 根据项目类型分发到具体的清理方法
        foreach (var item in operation.ItemsToClean)
        {
            var itemOp = new CleaningOperation 
            { 
                Type = operation.Type, 
                ItemsToClean = new List<string> { item },
                DryRun = operation.DryRun
            };

            // 根据项目所在位置确定清理类型
            // TODO: 实现更智能的类型检测
            await ExecuteImagesCleaningAsync(itemOp);
        }

        return true;
    }

    #endregion
}