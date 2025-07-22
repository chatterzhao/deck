using System.Diagnostics.CodeAnalysis;
using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 目录管理服务接口 - 对应 deck-shell 的 directory-mgmt.sh
/// 管理 .deck 目录的三层结构：templates、custom、images
/// </summary>
public interface IDirectoryManagementService
{
    /// <summary>
    /// 初始化 .deck 目录结构
    /// </summary>
    Task InitializeDeckDirectoryAsync(string projectPath);

    /// <summary>
    /// 获取三层配置列表
    /// </summary>
    Task<ThreeLayerOptions> GetThreeLayerOptionsAsync();

    /// <summary>
    /// 验证目录结构完整性
    /// </summary>
    Task<DirectoryStructureResult> ValidateDirectoryStructureAsync();

    /// <summary>
    /// 复制模板到自定义配置目录
    /// </summary>
    Task<string> CopyTemplateToCustomAsync(string templateName, string? customName = null);

    /// <summary>
    /// 获取镜像元数据
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed")]
    Task<ImageMetadata?> GetImageMetadataAsync(string imageName);

    /// <summary>
    /// 更新镜像元数据
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed")]
    Task SaveImageMetadataAsync(ImageMetadata metadata);

    /// <summary>
    /// 获取所有已构建的镜像列表
    /// </summary>
    Task<List<ConfigurationOption>> GetImagesAsync();

    /// <summary>
    /// 根据名称获取镜像配置
    /// </summary>
    Task<ConfigurationOption?> GetImageByNameAsync(string imageName);

    /// <summary>
    /// 生成唯一的Custom配置名称
    /// </summary>
    string GenerateUniqueCustomName(string baseName);

    /// <summary>
    /// 生成带时间戳的名称
    /// </summary>
    string GenerateTimestampedName(string baseName);

    /// <summary>
    /// 生成带时间戳的镜像名称
    /// </summary>
    string GenerateTimestampedImageName(string baseName);

    /// <summary>
    /// 生成镜像名称
    /// </summary>
    string GenerateImageName(string baseName);

    /// <summary>
    /// 获取模板目录路径
    /// </summary>
    string GetTemplateDirectory(string templateName);

    /// <summary>
    /// 获取Custom配置路径
    /// </summary>
    string GetCustomConfigPath(string configName);

    /// <summary>
    /// 获取镜像目录路径
    /// </summary>
    string GetImageDirectory(string imageName);

    /// <summary>
    /// 从模板创建Custom配置
    /// </summary>
    Task<bool> CreateCustomFromTemplateAsync(string templateName, string customName, string envType);

    /// <summary>
    /// 从Custom配置创建镜像目录
    /// </summary>
    Task<string> CreateImageFromCustomAsync(string imageName, string customDir);
}