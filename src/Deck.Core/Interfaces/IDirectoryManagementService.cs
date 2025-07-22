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
    Task<ImageMetadata?> GetImageMetadataAsync(string imageName);

    /// <summary>
    /// 更新镜像元数据
    /// </summary>
    Task SaveImageMetadataAsync(ImageMetadata metadata);

    /// <summary>
    /// 获取所有已构建的镜像列表
    /// </summary>
    Task<List<ConfigurationOption>> GetImagesAsync();

    /// <summary>
    /// 根据名称获取镜像配置
    /// </summary>
    Task<ConfigurationOption?> GetImageByNameAsync(string imageName);
}