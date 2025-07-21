using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Deck.Services;

/// <summary>
/// 配置验证服务实现 - 提供深度配置验证和网络检查
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private static readonly Regex GitUrlRegex = new(@"^https?://[\w\.-]+/[\w\.-]+/[\w\.-]+\.git$", RegexOptions.Compiled);
    private static readonly Regex CacheTtlRegex = new(@"^\d+[hmd]$", RegexOptions.Compiled);

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }

    public async Task<ConfigValidationResult> ValidateAsync(DeckConfig config)
    {
        _logger.LogDebug("开始验证完整配置");
        
        var result = new ConfigValidationResult { IsValid = true };
        
        // 验证远程模板配置
        var remoteTemplatesResult = await ValidateRemoteTemplatesAsync(config.RemoteTemplates);
        result.Errors.AddRange(remoteTemplatesResult.Errors);
        result.Warnings.AddRange(remoteTemplatesResult.Warnings);
        
        if (!remoteTemplatesResult.IsValid)
        {
            result.IsValid = false;
        }

        // 验证网络连接
        if (!string.IsNullOrWhiteSpace(config.RemoteTemplates.Repository))
        {
            var networkResult = await ValidateNetworkConnectivityAsync(config.RemoteTemplates.Repository);
            if (!networkResult.IsReachable)
            {
                result.Warnings.Add($"无法连接到模板仓库 {config.RemoteTemplates.Repository}，可能影响模板同步功能");
                result.Warnings.AddRange(networkResult.SuggestedAlternatives.Select(alt => $"建议的替代方案: {alt}"));
            }
            else
            {
                _logger.LogInformation("网络连接验证通过，响应时间: {ResponseTime}ms", networkResult.ResponseTime.TotalMilliseconds);
            }
        }

        _logger.LogDebug("配置验证完成: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}", 
            result.IsValid, result.Errors.Count, result.Warnings.Count);
        
        return result;
    }

    public async Task<ValidationResult> ValidateRemoteTemplatesAsync(RemoteTemplatesConfig config)
    {
        var result = new ValidationResult { IsValid = true };
        
        // 验证仓库 URL
        if (string.IsNullOrWhiteSpace(config.Repository))
        {
            result.Errors.Add("模板仓库URL不能为空");
            result.IsValid = false;
        }
        else if (!Uri.TryCreate(config.Repository, UriKind.Absolute, out var uri))
        {
            result.Errors.Add($"无效的模板仓库URL格式: {config.Repository}");
            result.IsValid = false;
        }
        else if (uri.Scheme != "https" && uri.Scheme != "http")
        {
            result.Errors.Add($"模板仓库URL必须使用HTTP或HTTPS协议: {config.Repository}");
            result.IsValid = false;
        }
        else if (!GitUrlRegex.IsMatch(config.Repository))
        {
            result.Warnings.Add($"模板仓库URL格式可能不是标准的Git仓库格式: {config.Repository}");
        }

        // 验证分支名称
        if (string.IsNullOrWhiteSpace(config.Branch))
        {
            result.Warnings.Add("模板仓库分支为空，将使用默认值'main'");
        }
        else if (config.Branch.Contains(' ') || config.Branch.Contains('\t'))
        {
            result.Errors.Add($"分支名称不能包含空格或制表符: '{config.Branch}'");
            result.IsValid = false;
        }

        // 验证缓存TTL
        if (!string.IsNullOrWhiteSpace(config.CacheTtl))
        {
            if (!CacheTtlRegex.IsMatch(config.CacheTtl))
            {
                result.Warnings.Add($"缓存TTL格式可能无效: {config.CacheTtl}，期望格式如 '24h', '1d', '30m'");
            }
            else
            {
                // 验证TTL值的合理性
                if (TryParseTtl(config.CacheTtl, out var timeSpan))
                {
                    if (timeSpan.TotalMinutes < 1)
                    {
                        result.Warnings.Add($"缓存TTL太短 ({config.CacheTtl})，可能导致频繁的网络请求");
                    }
                    else if (timeSpan.TotalDays > 7)
                    {
                        result.Warnings.Add($"缓存TTL太长 ({config.CacheTtl})，可能导致模板更新不及时");
                    }
                }
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<NetworkValidationResult> ValidateNetworkConnectivityAsync(string repositoryUrl)
    {
        var result = new NetworkValidationResult();
        
        try
        {
            if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri))
            {
                result.Errors.Add("无效的URL格式");
                return result;
            }

            _logger.LogDebug("检查网络连接: {Host}", uri.Host);
            
            var ping = new Ping();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var reply = await ping.SendPingAsync(uri.Host, 5000);
            stopwatch.Stop();
            
            result.ResponseTime = stopwatch.Elapsed;
            result.IsReachable = reply.Status == IPStatus.Success;
            result.ResolvedIpAddress = reply.Address?.ToString();
            
            if (!result.IsReachable)
            {
                result.Errors.Add($"无法连接到主机 {uri.Host}: {reply.Status}");
                
                // 提供替代建议
                if (uri.Host.Contains("github.com"))
                {
                    result.SuggestedAlternatives.Add("尝试使用Gitee等国内Git服务");
                    result.SuggestedAlternatives.Add("检查网络代理设置");
                    result.SuggestedAlternatives.Add("尝试使用不同的DNS服务器");
                }
            }
            else
            {
                result.InfoMessages.Add($"成功连接到 {uri.Host}，响应时间: {result.ResponseTime.TotalMilliseconds:F1}ms");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "网络连接检查失败: {Url}", repositoryUrl);
            result.Errors.Add($"网络连接检查异常: {ex.Message}");
        }

        result.IsValid = result.IsReachable;
        return result;
    }

    public async Task<List<ConfigurationFix>> GetRepairSuggestionsAsync(DeckConfig config)
    {
        var fixes = new List<ConfigurationFix>();
        
        // 检查模板仓库URL
        if (string.IsNullOrWhiteSpace(config.RemoteTemplates.Repository))
        {
            fixes.Add(new ConfigurationFix
            {
                Issue = "模板仓库URL为空",
                Suggestion = "设置默认的模板仓库URL",
                Priority = FixPriority.High,
                CanAutoFix = true,
                AutoFixAction = (cfg) =>
                {
                    cfg.RemoteTemplates.Repository = "https://github.com/chatterzhao/deck-templates.git";
                    return Task.FromResult(cfg);
                }
            });
        }

        // 检查分支名称
        if (string.IsNullOrWhiteSpace(config.RemoteTemplates.Branch))
        {
            fixes.Add(new ConfigurationFix
            {
                Issue = "模板仓库分支为空",
                Suggestion = "设置默认分支为 'main'",
                Priority = FixPriority.Medium,
                CanAutoFix = true,
                AutoFixAction = (cfg) =>
                {
                    cfg.RemoteTemplates.Branch = "main";
                    return Task.FromResult(cfg);
                }
            });
        }

        // 检查缓存TTL
        if (string.IsNullOrWhiteSpace(config.RemoteTemplates.CacheTtl))
        {
            fixes.Add(new ConfigurationFix
            {
                Issue = "缓存TTL未设置",
                Suggestion = "设置合理的缓存时间，如 '24h'",
                Priority = FixPriority.Low,
                CanAutoFix = true,
                AutoFixAction = (cfg) =>
                {
                    cfg.RemoteTemplates.CacheTtl = "24h";
                    return Task.FromResult(cfg);
                }
            });
        }

        _logger.LogDebug("生成了 {FixCount} 个修复建议", fixes.Count);
        
        return await Task.FromResult(fixes);
    }

    private static bool TryParseTtl(string ttl, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        
        if (string.IsNullOrWhiteSpace(ttl) || ttl.Length < 2)
            return false;

        var unit = ttl[^1];
        var valueStr = ttl[..^1];
        
        if (!int.TryParse(valueStr, out var value))
            return false;

        timeSpan = unit switch
        {
            'h' => TimeSpan.FromHours(value),
            'm' => TimeSpan.FromMinutes(value),
            'd' => TimeSpan.FromDays(value),
            _ => TimeSpan.Zero
        };

        return timeSpan > TimeSpan.Zero;
    }
}