#pragma warning disable CS1998 // 异步方法缺少await运算符
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 三层统一管理服务 - 简化版本实现（用于快速验证）
/// </summary>
public class ImagesUnifiedServiceSimple : IImagesUnifiedService
{
    private readonly ILogger<ImagesUnifiedServiceSimple> _logger;
    private readonly IFileSystemService _fileSystemService;
    
    // 三层目录路径
    private readonly string _imagesDirectory = ".deck/images";
    private readonly string _customDirectory = ".deck/custom";
    private readonly string _templatesDirectory = ".deck/templates";

    public ImagesUnifiedServiceSimple(
        ILogger<ImagesUnifiedServiceSimple> logger,
        IFileSystemService fileSystemService)
    {
        _logger = logger;
        _fileSystemService = fileSystemService;
    }

    public async Task<UnifiedResourceList> GetUnifiedResourceListAsync(string? environmentType = null)
    {
        _logger.LogDebug("获取三层统一资源列表，环境类型过滤: {EnvironmentType}", environmentType ?? "无");

        var result = new UnifiedResourceList();

        // 获取Images层资源
        result.Images = GetImagesResources();
        
        // 获取Custom层资源  
        result.Custom = GetCustomResources();
        
        // 获取Templates层资源（包括内置模板）
        result.Templates = GetTemplatesResources();

        // 应用环境类型过滤
        if (!string.IsNullOrEmpty(environmentType) && environmentType != "unknown")
        {
            result = result.FilterByEnvironmentType(environmentType);
        }

        _logger.LogInformation("获取到资源统计: Images={ImagesCount}, Custom={CustomCount}, Templates={TemplatesCount}",
            result.Images.Count, result.Custom.Count, result.Templates.Count);

        return result;
    }

    public Task<Dictionary<string, ResourceRelationship>> GetResourceRelationshipsAsync()
    {
        _logger.LogDebug("获取资源关联关系映射");
        var relationships = new Dictionary<string, ResourceRelationship>();
        
        // 简化实现：暂时返回空关系
        return Task.FromResult(relationships);
    }

    public async Task<UnifiedResourceDetail?> GetResourceDetailAsync(UnifiedResourceType resourceType, string resourceName)
    {
        _logger.LogDebug("获取资源详细信息: {ResourceType}/{ResourceName}", resourceType, resourceName);

        var resourcePath = GetResourcePath(resourceType, resourceName);
        if (string.IsNullOrEmpty(resourcePath) || !_fileSystemService.DirectoryExists(resourcePath))
        {
            _logger.LogWarning("资源目录不存在: {ResourcePath}", resourcePath);
            return null;
        }

        var resource = CreateUnifiedResource(resourceType, resourceName, resourcePath);
        var configStatus = GetConfigurationStatus(resourcePath);
        var fileSystemInfo = await GetFileSystemInfoAsync(resourcePath);

        return new UnifiedResourceDetail
        {
            Resource = resource,
            ConfigurationStatus = configStatus,
            FileSystemInfo = fileSystemInfo
        };
    }

    public async Task<List<CleaningOption>> GetCleaningOptionsAsync()
    {
        _logger.LogDebug("获取三层清理选项");

        var options = new List<CleaningOption>();

        // Images清理选项
        var imagesCount = _fileSystemService.DirectoryExists(_imagesDirectory) 
            ? Directory.GetDirectories(_imagesDirectory).Length 
            : 0;

        if (imagesCount > 0)
        {
            options.Add(new CleaningOption
            {
                Id = "images_keep_latest",
                DisplayName = "清理旧镜像（保留最新3个）",
                Description = "删除每个前缀下的旧镜像，每个前缀保留最新的3个镜像",
                ResourceType = UnifiedResourceType.Images,
                Strategy = CleaningStrategy.KeepLatestN,
                WarningLevel = ConfirmationLevel.Medium,
                EstimatedCount = Math.Max(0, imagesCount - 3),
                Parameters = new Dictionary<string, object> { ["keepCount"] = 3 }
            });
        }

        // Templates智能提示
        options.Add(new CleaningOption
        {
            Id = "templates_smart_suggestion",
            DisplayName = "Templates清理建议",
            Description = "提供Templates管理的智能建议和替代方案",
            ResourceType = UnifiedResourceType.Templates,
            Strategy = CleaningStrategy.SmartSuggestion,
            WarningLevel = ConfirmationLevel.Low,
            EstimatedCount = 0
        });

        return options;
    }

    public async Task<CleaningResult> ExecuteCleaningAsync(CleaningOption cleaningOption, Func<string, Task<bool>>? confirmationCallback = null)
    {
        _logger.LogInformation("执行清理操作: {CleaningOptionId}", cleaningOption.Id);

        // 简化实现：仅返回成功结果
        return new CleaningResult
        {
            IsSuccess = true,
            CleanedCount = 0,
            CleanedResources = new List<string> { "清理功能暂未完整实现" }
        };
    }

    public async Task<ResourceValidationResult> ValidateResourceAsync(UnifiedResourceType resourceType, string resourceName)
    {
        _logger.LogDebug("验证资源: {ResourceType}/{ResourceName}", resourceType, resourceName);

        var result = new ResourceValidationResult { IsValid = true };
        var resourcePath = GetResourcePath(resourceType, resourceName);

        if (string.IsNullOrEmpty(resourcePath) || !_fileSystemService.DirectoryExists(resourcePath))
        {
            result.ValidationErrors.Add($"资源目录不存在: {resourcePath}");
            result.IsValid = false;
            return result;
        }

        // 检查配置文件完整性
        var configStatus = GetConfigurationStatus(resourcePath);
        result.ConfigurationStatus = configStatus;

        if (!configStatus.IsComplete)
        {
            result.ValidationErrors.AddRange(configStatus.MissingFiles.Select(f => $"缺少必需文件: {f}"));
            result.IsValid = false;
        }

        return result;
    }

    #region Private Helper Methods

    private List<UnifiedResource> GetImagesResources()
    {
        var resources = new List<UnifiedResource>();

        if (!_fileSystemService.DirectoryExists(_imagesDirectory))
        {
            return resources;
        }

        foreach (var imageDir in Directory.GetDirectories(_imagesDirectory))
        {
            var imageName = Path.GetFileName(imageDir);
            var resource = CreateUnifiedResource(UnifiedResourceType.Images, imageName, imageDir);
            resources.Add(resource);
        }

        // 按名称排序
        resources.Sort((a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));

        return resources;
    }

    private List<UnifiedResource> GetCustomResources()
    {
        var resources = new List<UnifiedResource>();

        if (!_fileSystemService.DirectoryExists(_customDirectory))
        {
            return resources;
        }

        foreach (var customDir in Directory.GetDirectories(_customDirectory))
        {
            var customName = Path.GetFileName(customDir);
            var resource = CreateUnifiedResource(UnifiedResourceType.Custom, customName, customDir);
            resources.Add(resource);
        }

        return resources;
    }

    private List<UnifiedResource> GetTemplatesResources()
    {
        var resources = new List<UnifiedResource>();

        // 检查实际模板目录
        if (_fileSystemService.DirectoryExists(_templatesDirectory))
        {
            foreach (var templateDir in Directory.GetDirectories(_templatesDirectory))
            {
                var templateName = Path.GetFileName(templateDir);
                var resource = CreateUnifiedResource(UnifiedResourceType.Templates, templateName, templateDir);
                resources.Add(resource);
            }
        }
        else
        {
            // 使用内置模板
            var defaultTemplates = new[] { "tauri-default", "flutter-default", "avalonia-default" };
            foreach (var template in defaultTemplates)
            {
                resources.Add(new UnifiedResource
                {
                    Name = template,
                    Type = UnifiedResourceType.Templates,
                    Status = ResourceStatus.Builtin,
                    DisplayLabel = $"{template}（内置）",
                    IsAvailable = true
                });
            }
        }

        return resources;
    }

    private UnifiedResource CreateUnifiedResource(UnifiedResourceType type, string name, string path)
    {
        var configStatus = GetConfigurationStatus(path);
        var relativeTime = GetRelativeTime(path);

        return new UnifiedResource
        {
            Name = name,
            Type = type,
            Status = ResourceStatus.Ready,
            DisplayLabel = CreateDisplayLabel(type, name, relativeTime),
            RelativeTime = relativeTime,
            IsAvailable = configStatus.IsComplete,
            UnavailableReason = configStatus.IsComplete ? null : $"缺少: {string.Join(", ", configStatus.MissingFiles)}"
        };
    }

    private ConfigurationStatus GetConfigurationStatus(string path)
    {
        var status = new ConfigurationStatus();
        var requiredFiles = new[] { "compose.yaml", "Dockerfile" };

        foreach (var file in requiredFiles)
        {
            var filePath = Path.Combine(path, file);
            var exists = _fileSystemService.FileExists(filePath);

            switch (file)
            {
                case "compose.yaml":
                    status.HasComposeYaml = exists;
                    break;
                case "Dockerfile":
                    status.HasDockerfile = exists;
                    break;
            }

            if (!exists)
            {
                status.MissingFiles.Add(file);
            }
        }

        // 检查.env文件
        var envPath = Path.Combine(path, ".env");
        status.HasEnvFile = _fileSystemService.FileExists(envPath);

        return status;
    }

    private async Task<ResourceFileSystemInfo> GetFileSystemInfoAsync(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var size = await _fileSystemService.GetDirectorySizeAsync(path);
        
        return new ResourceFileSystemInfo
        {
            DirectoryPath = path,
            DirectorySize = size >= 0 ? FormatFileSize(size) : "未知",
            CreatedAt = dirInfo.CreationTime,
            ModifiedAt = dirInfo.LastWriteTime
        };
    }

    private string CreateDisplayLabel(UnifiedResourceType type, string name, string? relativeTime)
    {
        var typeLabel = type switch
        {
            UnifiedResourceType.Images => "启动镜像",
            UnifiedResourceType.Custom => "从配置构建",
            UnifiedResourceType.Templates => "从模板创建",
            _ => "未知"
        };

        return string.IsNullOrEmpty(relativeTime) 
            ? $"{typeLabel}: {name}" 
            : $"{typeLabel}: {name} ({relativeTime})";
    }

    private string? GetRelativeTime(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            var timeSpan = DateTime.Now - dirInfo.CreationTime;

            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}天前";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}小时前";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes}分钟前";

            return "刚刚";
        }
        catch
        {
            return null;
        }
    }

    private string FormatFileSize(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "B", "KB", "MB", "GB", "TB" };
        
        var order = 0;
        var size = (decimal)bytes;
        
        while (size >= scale && order < orders.Length - 1)
        {
            order++;
            size /= scale;
        }
        
        return $"{size:0.##} {orders[order]}";
    }

    private string GetResourcePath(UnifiedResourceType resourceType, string resourceName)
    {
        return resourceType switch
        {
            UnifiedResourceType.Images => Path.Combine(_imagesDirectory, resourceName),
            UnifiedResourceType.Custom => Path.Combine(_customDirectory, resourceName),
            UnifiedResourceType.Templates => Path.Combine(_templatesDirectory, resourceName),
            _ => string.Empty
        };
    }

    #endregion
}