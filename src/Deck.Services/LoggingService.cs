using Deck.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.DependencyInjection;

namespace Deck.Services;

/// <summary>
/// 日志服务实现 - 基于 .NET 9 ILogger 的统一日志管理
/// 支持控制台彩色输出、结构化日志和性能优化
/// </summary>
public class LoggingService : ILoggingService, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private LoggingConfiguration _configuration;

    public LoggingService() : this(new LoggingConfiguration())
    {
    }

    public LoggingService(LoggingConfiguration configuration)
    {
        _configuration = configuration;
        
        var services = new ServiceCollection();
        ConfigureLogging(services, configuration);
        
        _serviceProvider = services.BuildServiceProvider();
        _loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    public ILogger<T> GetLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    public ILogger GetLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }

    public void SetLogLevel(string categoryName, LogLevel logLevel)
    {
        _configuration.CategoryLevels[categoryName] = logLevel;
        // 注意：动态修改日志级别需要重新配置 LoggerFactory
        // 这里提供接口，实际应用中可能需要重新创建 LoggerFactory
    }

    public void EnableConsoleColors(bool enabled)
    {
        _configuration.EnableConsoleColors = enabled;
    }

    public LoggingConfiguration GetCurrentConfiguration()
    {
        return _configuration;
    }

    /// <summary>
    /// 创建默认的日志服务实例
    /// </summary>
    public static ILoggingService CreateDefault()
    {
        var config = new LoggingConfiguration
        {
            DefaultLevel = LogLevel.Information,
            EnableConsoleColors = true,
            EnableTimestamps = true
        };

        // 为 Deck 组件设置合适的日志级别
        config.CategoryLevels["Deck.Services.ConfigurationService"] = LogLevel.Information;
        config.CategoryLevels["Deck.Services.SystemDetectionService"] = LogLevel.Information;
        config.CategoryLevels["Microsoft"] = LogLevel.Warning;
        config.CategoryLevels["System"] = LogLevel.Warning;

        return new LoggingService(config);
    }

    /// <summary>
    /// 创建用于调试的详细日志服务实例
    /// </summary>
    public static ILoggingService CreateVerbose()
    {
        var config = new LoggingConfiguration
        {
            DefaultLevel = LogLevel.Debug,
            EnableConsoleColors = true,
            EnableTimestamps = true
        };

        config.CategoryLevels["Deck"] = LogLevel.Debug;
        config.CategoryLevels["Microsoft"] = LogLevel.Information;
        config.CategoryLevels["System"] = LogLevel.Information;

        return new LoggingService(config);
    }

    private static void ConfigureLogging(IServiceCollection services, LoggingConfiguration config)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            
            // 配置控制台日志
            builder.AddSimpleConsole(options =>
            {
                options.ColorBehavior = config.EnableConsoleColors ? 
                    LoggerColorBehavior.Enabled : 
                    LoggerColorBehavior.Disabled;
                options.IncludeScopes = false;
                options.TimestampFormat = config.EnableTimestamps ? "HH:mm:ss " : null;
            });

            // 设置默认日志级别
            builder.SetMinimumLevel(config.DefaultLevel);

            // 配置分类级别
            foreach (var categoryLevel in config.CategoryLevels)
            {
                builder.AddFilter(categoryLevel.Key, categoryLevel.Value);
            }
        });
    }

    public void Dispose()
    {
        _loggerFactory?.Dispose();
        
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}