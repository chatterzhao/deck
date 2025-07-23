using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Core.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Deck.Services;

/// <summary>
/// 配置管理服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        // 配置JSON序列化选项，使用camelCase命名约定（与AOT兼容）
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public async Task<DeckConfig> GetConfigAsync()
    {
        var configPath = GetConfigFilePath();
        
        if (!File.Exists(configPath))
        {
            _logger.LogInformation("配置文件不存在，创建默认配置: {ConfigPath}", configPath);
            var defaultConfig = await CreateDefaultConfigAsync();
            await SaveConfigAsync(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<DeckConfig>(jsonContent, DeckJsonSerializerContext.Default.DeckConfig);
            
            if (config == null)
            {
                _logger.LogError("配置文件反序列化失败，返回null: {ConfigPath}", configPath);
                throw new InvalidOperationException($"配置文件反序列化失败: {configPath}");
            }
            
            _logger.LogDebug("成功加载配置文件: {ConfigPath}", configPath);
            
            // 验证配置
            var validation = await ValidateConfigAsync(config);
            if (!validation.IsValid)
            {
                _logger.LogWarning("配置文件验证失败: {Errors}", string.Join(", ", validation.Errors));
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置文件失败: {ConfigPath}", configPath);
            
            // 如果配置文件损坏，备份原文件并创建新的默认配置
            var backupPath = $"{configPath}.backup.{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(configPath, backupPath);
            _logger.LogWarning("已备份损坏的配置文件到: {BackupPath}", backupPath);
            
            var defaultConfig = await CreateDefaultConfigAsync();
            await SaveConfigAsync(defaultConfig);
            return defaultConfig;
        }
    }

    public async Task SaveConfigAsync(DeckConfig config)
    {
        var configPath = GetConfigFilePath();
        var configDir = Path.GetDirectoryName(configPath);
        
        // 确保.deck目录存在
        if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            _logger.LogDebug("创建配置目录: {ConfigDir}", configDir);
        }

        try
        {
            // 验证配置
            var validation = await ValidateConfigAsync(config);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"配置验证失败: {string.Join(", ", validation.Errors)}");
            }

            var jsonContent = JsonSerializer.Serialize(config, DeckJsonSerializerContext.Default.DeckConfig);
            
            // 添加配置文件头部注释
            var configWithComments = GenerateConfigWithComments(jsonContent);
            
            await File.WriteAllTextAsync(configPath, configWithComments);
            _logger.LogDebug("成功保存配置文件: {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置文件失败: {ConfigPath}", configPath);
            throw;
        }
    }

    public async Task<ConfigValidationResult> ValidateConfigAsync(DeckConfig config)
    {
        var result = new ConfigValidationResult { IsValid = true };

        // 验证仓库URL
        if (string.IsNullOrWhiteSpace(config.RemoteTemplates.Repository))
        {
            result.Errors.Add("模板仓库URL不能为空");
            result.IsValid = false;
        }
        else if (!Uri.TryCreate(config.RemoteTemplates.Repository, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            result.Errors.Add($"无效的模板仓库URL: {config.RemoteTemplates.Repository}");
            result.IsValid = false;
        }

        // 验证分支名称
        if (string.IsNullOrWhiteSpace(config.RemoteTemplates.Branch))
        {
            result.Warnings.Add("模板仓库分支为空，将使用默认值main");
        }

        // 验证缓存TTL格式 (如: "24h", "1d", "30m")
        if (!string.IsNullOrWhiteSpace(config.RemoteTemplates.CacheTtl) && 
            !System.Text.RegularExpressions.Regex.IsMatch(config.RemoteTemplates.CacheTtl, @"^\d+[hmd]$"))
        {
            result.Warnings.Add($"缓存TTL格式可能无效: {config.RemoteTemplates.CacheTtl}，建议使用格式如 '24h', '1d', '30m'");
        }

        _logger.LogDebug("配置验证完成: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}", 
            result.IsValid, result.Errors.Count, result.Warnings.Count);

        return await Task.FromResult(result);
    }

    public async Task<DeckConfig> CreateDefaultConfigAsync()
    {
        _logger.LogInformation("创建默认配置");
        
        var config = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/chatterzhao/deck-templates.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        return await Task.FromResult(config);
    }

    public string GetConfigFilePath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        return Path.Combine(currentDir, ".deck", "config.json");
    }

    public bool ConfigExists()
    {
        return File.Exists(GetConfigFilePath());
    }


    /// <summary>
    /// 生成带注释的配置文件内容
    /// </summary>
    private static string GenerateConfigWithComments(string jsonContent)
    {
        var header = @"// =============================================================================
// Deck .NET版本配置文件
// =============================================================================
// 此文件用于配置远程模板仓库，参考deck-shell的配置设计
// 更多信息请参考: https://github.com/chatterzhao/deck
// =============================================================================

";
        return header + jsonContent;
    }
}