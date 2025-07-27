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
    private readonly IConfigurationMerger _configMerger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService(ILogger<ConfigurationService> logger, IConfigurationMerger configMerger)
    {
        _logger = logger;
        _configMerger = configMerger;
        
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
            var fileContent = await File.ReadAllTextAsync(configPath);
            
            // 查找JSON内容的开始位置（跳过注释头）
            var jsonStartIndex = fileContent.IndexOf('{');
            var jsonContent = jsonStartIndex >= 0 ? fileContent.Substring(jsonStartIndex) : fileContent;
            
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

            // 合并默认配置和加载的配置，确保所有字段都有默认值
            var defaultConfig = await CreateDefaultConfigAsync();
            var mergedConfig = _configMerger.Merge(defaultConfig, config);
            
            return mergedConfig;
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
        
        // 确保.deck目录存在 - 使用重试机制处理并发竞争
        if (!string.IsNullOrEmpty(configDir))
        {
            await EnsureDirectoryExistsAsync(configDir);
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
            
            // 使用重试机制写入文件，处理并发竞争条件
            await WriteFileWithRetryAsync(configPath, configWithComments);
            _logger.LogDebug("成功保存配置文件: {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置文件失败: {ConfigPath}", configPath);
            throw;
        }
    }

    /// <summary>
    /// 确保目录存在的健壮方法，处理并发竞争条件
    /// </summary>
    private async Task EnsureDirectoryExistsAsync(string directoryPath)
    {
        const int maxRetries = 3;
        const int delayMs = 50;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogDebug("创建配置目录: {ConfigDir}", directoryPath);
                }
                
                // 验证目录创建成功
                if (Directory.Exists(directoryPath))
                {
                    return;
                }
            }
            catch (DirectoryNotFoundException) when (i < maxRetries - 1)
            {
                _logger.LogWarning("目录创建失败，重试中... 尝试 {Attempt}/{MaxRetries}", i + 1, maxRetries);
                await Task.Delay(delayMs);
                continue;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                _logger.LogWarning("目录创建遇到IO异常，重试中... 尝试 {Attempt}/{MaxRetries}", i + 1, maxRetries);
                await Task.Delay(delayMs);
                continue;
            }
        }

        throw new InvalidOperationException($"无法创建配置目录: {directoryPath}");
    }

    /// <summary>
    /// 带重试机制的文件写入方法
    /// </summary>
    private async Task WriteFileWithRetryAsync(string filePath, string content)
    {
        const int maxRetries = 3;
        const int delayMs = 50;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content);
                
                // 验证文件写入成功
                if (File.Exists(filePath))
                {
                    return;
                }
            }
            catch (DirectoryNotFoundException) when (i < maxRetries - 1)
            {
                _logger.LogWarning("文件写入失败(目录不存在)，重试中... 尝试 {Attempt}/{MaxRetries}: {FilePath}", i + 1, maxRetries, filePath);
                
                // 重新确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    await EnsureDirectoryExistsAsync(directory);
                }
                
                await Task.Delay(delayMs);
                continue;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                _logger.LogWarning("文件写入遇到IO异常，重试中... 尝试 {Attempt}/{MaxRetries}: {FilePath}", i + 1, maxRetries, filePath);
                await Task.Delay(delayMs);
                continue;
            }
        }

        throw new InvalidOperationException($"无法写入配置文件: {filePath}");
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
                Repository = "https://gitee.com/zhaoquan/deck.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        return await Task.FromResult(config);
    }

    public string GetConfigFilePath()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            return Path.Combine(currentDir, ".deck", "config.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "无法获取当前工作目录，使用临时目录作为备用");
            // 使用临时目录作为备用方案
            var tempDir = Path.GetTempPath();
            var fallbackDir = Path.Combine(tempDir, $"deck-fallback-{Environment.ProcessId}");
            
            if (!Directory.Exists(fallbackDir))
            {
                Directory.CreateDirectory(fallbackDir);
            }
            
            return Path.Combine(fallbackDir, ".deck", "config.json");
        }
    }

    public bool ConfigExists()
    {
        try
        {
            return File.Exists(GetConfigFilePath());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查配置文件存在性时发生错误");
            return false;
        }
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
// 更多信息请参考: https://gitee.com/zhaoquan/deck
// =============================================================================

";
        return header + jsonContent;
    }
}