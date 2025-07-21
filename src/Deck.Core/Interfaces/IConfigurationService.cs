using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 配置管理服务接口
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 获取配置，如果不存在则创建默认配置
    /// </summary>
    Task<DeckConfig> GetConfigAsync();

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    Task SaveConfigAsync(DeckConfig config);

    /// <summary>
    /// 验证配置文件的完整性和有效性
    /// </summary>
    Task<ConfigValidationResult> ValidateConfigAsync(DeckConfig config);

    /// <summary>
    /// 创建默认配置文件
    /// </summary>
    Task<DeckConfig> CreateDefaultConfigAsync();

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    string GetConfigFilePath();

    /// <summary>
    /// 检查配置文件是否存在
    /// </summary>
    bool ConfigExists();
}

/// <summary>
/// 配置验证结果
/// </summary>
public class ConfigValidationResult
{
    /// <summary>
    /// 验证是否通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 验证警告信息
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 配置文件版本
    /// </summary>
    public string? Version { get; set; }
}