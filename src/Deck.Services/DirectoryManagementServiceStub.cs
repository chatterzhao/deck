using Deck.Core.Interfaces;
using Deck.Core.Models;

namespace Deck.Services;

/// <summary>
/// 目录管理服务的临时存根实现
/// 用于支持 StartCommandService 的基本功能
/// </summary>
public class DirectoryManagementServiceStub : IDirectoryManagementService
{
    public Task InitializeDeckDirectoryAsync(string projectPath)
    {
        // 创建基本的目录结构
        var deckDir = Path.Combine(projectPath, ".deck");
        var templatesDir = Path.Combine(deckDir, "templates");
        var customDir = Path.Combine(deckDir, "custom");
        var imagesDir = Path.Combine(deckDir, "images");

        Directory.CreateDirectory(deckDir);
        Directory.CreateDirectory(templatesDir);
        Directory.CreateDirectory(customDir);
        Directory.CreateDirectory(imagesDir);

        return Task.CompletedTask;
    }

    public Task<ThreeLayerOptions> GetThreeLayerOptionsAsync()
    {
        // 返回空的选项，实际实现在 StartCommandService 中
        return Task.FromResult(new ThreeLayerOptions());
    }

    public Task<DirectoryStructureResult> ValidateDirectoryStructureAsync()
    {
        // 简单的验证实现
        var result = new DirectoryStructureResult
        {
            IsValid = true,
            Errors = new List<string>(),
            Warnings = new List<string>()
        };

        var deckDir = ".deck";
        if (!Directory.Exists(deckDir))
        {
            result.IsValid = false;
            result.Errors.Add("缺少 .deck 目录");
        }

        return Task.FromResult(result);
    }

    public Task<string> CopyTemplateToCustomAsync(string templateName, string? customName = null)
    {
        // 临时实现：简单返回配置名称
        var configName = customName ?? templateName;
        return Task.FromResult(configName);
    }

    public Task<ImageMetadata?> GetImageMetadataAsync(string imageName)
    {
        // 临时实现：返回空元数据
        return Task.FromResult<ImageMetadata?>(null);
    }

    public Task SaveImageMetadataAsync(ImageMetadata metadata)
    {
        // 临时实现：什么都不做
        return Task.CompletedTask;
    }

    public Task<List<ConfigurationOption>> GetImagesAsync()
    {
        var images = new List<ConfigurationOption>();
        
        try
        {
            var imagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deck", "images");
            
            if (Directory.Exists(imagesDir))
            {
                var directories = Directory.GetDirectories(imagesDir);
                
                foreach (var dir in directories)
                {
                    var imageName = Path.GetFileName(dir);
                    var metadata = GetSimpleImageMetadata(dir);
                    
                    images.Add(new ConfigurationOption
                    {
                        Name = imageName,
                        Type = ConfigurationType.Images,
                        Path = dir,
                        ProjectType = ProjectType.Unknown,
                        IsAvailable = true,
                        Description = metadata?.BuildStatus.ToString() ?? "Unknown",
                        LastModified = metadata?.LastStarted ?? Directory.GetLastWriteTime(dir),
                        Metadata = metadata
                    });
                }
            }
        }
        catch
        {
            // 出错时返回空列表
        }
        
        return Task.FromResult(images.OrderByDescending(i => i.LastModified).ToList());
    }

    public Task<ConfigurationOption?> GetImageByNameAsync(string imageName)
    {
        try
        {
            var imagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".deck", "images");
            var imagePath = Path.Combine(imagesDir, imageName);
            
            if (Directory.Exists(imagePath))
            {
                var metadata = GetSimpleImageMetadata(imagePath);
                
                return Task.FromResult<ConfigurationOption?>(new ConfigurationOption
                {
                    Name = imageName,
                    Type = ConfigurationType.Images,
                    Path = imagePath,
                    ProjectType = ProjectType.Unknown,
                    IsAvailable = true,
                    Description = metadata?.BuildStatus.ToString() ?? "Unknown",
                    LastModified = metadata?.LastStarted ?? Directory.GetLastWriteTime(imagePath),
                    Metadata = metadata
                });
            }
        }
        catch
        {
            // 出错时返回空
        }
        
        return Task.FromResult<ConfigurationOption?>(null);
    }

    private static ImageMetadata? GetSimpleImageMetadata(string imagePath)
    {
        try
        {
            var metadataFile = Path.Combine(imagePath, ".deck-metadata");
            if (File.Exists(metadataFile))
            {
                // 简单的元数据读取，这里可以后续实现JSON反序列化
                return new ImageMetadata
                {
                    ImageName = Path.GetFileName(imagePath),
                    CreatedAt = Directory.GetCreationTime(imagePath),
                    BuildStatus = Directory.GetFiles(imagePath, "*.log").Any() ? BuildStatus.Built : BuildStatus.Stopped,
                    ContainerName = $"{Path.GetFileName(imagePath)}-dev"
                };
            }
            
            // 如果没有元数据文件，创建基本元数据
            return new ImageMetadata
            {
                ImageName = Path.GetFileName(imagePath),
                CreatedAt = Directory.GetCreationTime(imagePath),
                BuildStatus = BuildStatus.Built,
                ContainerName = $"{Path.GetFileName(imagePath)}-dev"
            };
        }
        catch
        {
            return null;
        }
    }
}