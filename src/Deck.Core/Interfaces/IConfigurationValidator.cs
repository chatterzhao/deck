using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 配置验证服务接口 - 提供深度配置验证和修复建议
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// 验证完整的Deck配置
    /// </summary>
    Task<ConfigValidationResult> ValidateAsync(DeckConfig config);

    /// <summary>
    /// 验证远程模板配置
    /// </summary>
    Task<ValidationResult> ValidateRemoteTemplatesAsync(RemoteTemplatesConfig config);

    /// <summary>
    /// 验证网络连接性
    /// </summary>
    Task<NetworkConfigValidationResult> ValidateNetworkConnectivityAsync(string repositoryUrl);

    /// <summary>
    /// 获取配置修复建议
    /// </summary>
    Task<List<ConfigurationFix>> GetRepairSuggestionsAsync(DeckConfig config);
}

/// <summary>
/// 验证结果基类
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> InfoMessages { get; set; } = new();
}

/// <summary>
/// 网络配置验证结果
/// </summary>
public class NetworkConfigValidationResult : ValidationResult
{
    public TimeSpan ResponseTime { get; set; }
    public bool IsReachable { get; set; }
    public string? ResolvedIpAddress { get; set; }
    public List<string> SuggestedAlternatives { get; set; } = new();
}

/// <summary>
/// 配置修复建议
/// </summary>
public class ConfigurationFix
{
    public string Issue { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public FixPriority Priority { get; set; }
    public bool CanAutoFix { get; set; }
    public Func<DeckConfig, Task<DeckConfig>>? AutoFixAction { get; set; }
}

/// <summary>
/// 修复优先级
/// </summary>
public enum FixPriority
{
    Low,
    Medium,
    High,
    Critical
}