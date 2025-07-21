using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Diagnostics.CodeAnalysis;

namespace Deck.Services;

/// <summary>
/// 配置管理服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    [RequiresDynamicCode("YAML serialization requires reflection for now. Will be replaced with static generation later.")]
    [RequiresUnreferencedCode("YAML serialization uses reflection.")]
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        // 配置YAML序列化器，使用snake_case命名约定
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties() // 忽略未匹配的属性，保持向后兼容
            .Build();
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
            var yamlContent = await File.ReadAllTextAsync(configPath);
            var config = _yamlDeserializer.Deserialize<DeckConfig>(yamlContent);
            
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

            var yamlContent = _yamlSerializer.Serialize(config);
            
            // 添加配置文件头部注释
            var configWithComments = GenerateConfigWithComments(yamlContent);
            
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
        if (string.IsNullOrWhiteSpace(config.Templates.Repository.Url))
        {
            result.Errors.Add("模板仓库URL不能为空");
            result.IsValid = false;
        }
        else if (!Uri.TryCreate(config.Templates.Repository.Url, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            result.Errors.Add($"无效的模板仓库URL: {config.Templates.Repository.Url}");
            result.IsValid = false;
        }

        // 验证容器引擎
        var validEngines = new[] { "podman", "docker" };
        if (!validEngines.Contains(config.Container.Engine.ToLowerInvariant()))
        {
            result.Errors.Add($"不支持的容器引擎: {config.Container.Engine}，支持的引擎: {string.Join(", ", validEngines)}");
            result.IsValid = false;
        }

        // 验证缓存过期时间格式
        if (!IsValidTimeSpanFormat(config.Templates.CacheExpire))
        {
            result.Warnings.Add($"无效的缓存过期时间格式: {config.Templates.CacheExpire}，将使用默认值24h");
        }

        // 验证日志级别
        var validLogLevels = new[] { "trace", "debug", "info", "warn", "error" };
        if (!validLogLevels.Contains(config.Logging.Level.ToLowerInvariant()))
        {
            result.Warnings.Add($"无效的日志级别: {config.Logging.Level}，将使用默认值info");
        }

        // 验证语言设置
        var validLanguages = new[] { "zh-CN", "en-US" };
        if (!validLanguages.Contains(config.UI.Language))
        {
            result.Warnings.Add($"不支持的语言: {config.UI.Language}，将使用默认值zh-CN");
        }

        _logger.LogDebug("配置验证完成: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}", 
            result.IsValid, result.Errors.Count, result.Warnings.Count);

        return await Task.FromResult(result);
    }

    public async Task<DeckConfig> CreateDefaultConfigAsync()
    {
        _logger.LogInformation("创建默认配置");
        
        // 获取当前目录名作为默认项目名
        var currentDir = Directory.GetCurrentDirectory();
        var projectName = Path.GetFileName(currentDir);

        var config = new DeckConfig
        {
            Templates = new TemplateConfig
            {
                Repository = new RepositoryConfig
                {
                    Url = "https://github.com/chatterzhao/deck-templates.git",
                    Branch = "main",
                    FallbackUrl = "https://gitee.com/zhaoquan/deck-templates.git"
                },
                AutoUpdate = true,
                CacheExpire = "24h",
                UpdateOnStart = true
            },
            Container = new ContainerEngineConfig
            {
                Engine = "podman",
                AutoInstall = true,
                CheckOnStart = true,
                FallbackEngine = "docker"
            },
            Network = new NetworkConfig
            {
                Proxy = new ProxyConfig
                {
                    Http = string.Empty,
                    Https = string.Empty,
                    NoProxy = "localhost,127.0.0.1"
                },
                DNS = new List<string> { "8.8.8.8", "1.1.1.1" },
                Mirrors = new MirrorsConfig
                {
                    DockerRegistry = "docker.m.daocloud.io",
                    AptMirror = "mirrors.ustc.edu.cn"
                }
            },
            Project = new ProjectConfig
            {
                Name = projectName,
                WorkspacePath = "/workspace",
                AutoCreateGitignore = true
            },
            Cache = new CacheConfig
            {
                EnableBuildCache = true,
                CacheDirectory = ".deck/cache",
                MaxCacheSize = "5GB",
                AutoCleanup = true,
                CleanupDays = 30
            },
            UI = new UIConfig
            {
                Language = "zh-CN",
                ShowTips = true,
                InteractiveMode = true,
                ShowPodmanCommands = true
            },
            Development = new DevelopmentConfig
            {
                AutoPortForward = true,
                DefaultMemoryLimit = "4g",
                DefaultCpuLimit = "2",
                EnableHotReload = true,
                ShowBuildOutput = true
            },
            Security = new SecurityConfig
            {
                EnableNoNewPrivileges = true,
                RestrictedCapabilities = true,
                ScanTemplates = false
            },
            Logging = new LoggingConfig
            {
                Level = "info",
                File = ".deck/logs/deck.log",
                MaxFileSize = "10MB",
                KeepFiles = 5
            }
        };

        return await Task.FromResult(config);
    }

    public string GetConfigFilePath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        return Path.Combine(currentDir, ".deck", "config.yaml");
    }

    public bool ConfigExists()
    {
        return File.Exists(GetConfigFilePath());
    }

    /// <summary>
    /// 验证时间跨度格式
    /// </summary>
    private static bool IsValidTimeSpanFormat(string timeSpan)
    {
        // 简单验证时间格式 (例如: 24h, 30m, 7d)
        if (string.IsNullOrWhiteSpace(timeSpan))
            return false;

        var pattern = @"^\d+[hdm]$";
        return System.Text.RegularExpressions.Regex.IsMatch(timeSpan.ToLowerInvariant(), pattern);
    }

    /// <summary>
    /// 生成带注释的配置文件内容
    /// </summary>
    private static string GenerateConfigWithComments(string yamlContent)
    {
        var header = @"# =============================================================================
# Deck .NET版本配置文件
# =============================================================================
# 此文件控制Deck工具的所有配置选项
# 更多信息请参考: https://github.com/chatterzhao/deck
# =============================================================================

";
        return header + yamlContent;
    }
}