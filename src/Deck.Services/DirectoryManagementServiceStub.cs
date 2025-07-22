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
}