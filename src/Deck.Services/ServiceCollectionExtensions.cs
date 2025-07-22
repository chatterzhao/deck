using Deck.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Deck.Services;

/// <summary>
/// 服务集合扩展方法 - 统一注册 Deck 服务
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 Deck 核心服务
    /// </summary>
    [RequiresUnreferencedCode("YAML serialization uses reflection")]
    public static IServiceCollection AddDeckServices(this IServiceCollection services)
    {
        // 注册核心服务
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<ISystemDetectionService, SystemDetectionService>();
        services.AddSingleton<IPortConflictService, PortConflictService>();
        services.AddSingleton<IImagePermissionService, ImagePermissionService>();
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<IEnhancedFileOperationsService, EnhancedFileOperationsService>();
        services.AddSingleton<IStartCommandService, StartCommandServiceSimple>();
        services.AddSingleton<IConsoleUIService, ConsoleUIService>();
        services.AddSingleton<IInteractiveSelectionService, InteractiveSelectionService>();
        
        // 注册目录管理服务的临时实现
        services.AddSingleton<IDirectoryManagementService, DirectoryManagementServiceStub>();
        
        // 注册文件系统服务
        services.AddSingleton<IFileSystemService, FileSystemService>();
        
        // 注册三层统一管理服务（简化版本）
        services.AddSingleton<IImagesUnifiedService, ImagesUnifiedServiceSimple>();
        
        // 注册清理服务
        services.AddSingleton<ICleaningService, CleaningService>();
        
        // 注册容器管理服务
        services.AddSingleton<IContainerService, ContainerService>();
        
        // 注册 HttpClient 用于网络服务
        services.AddHttpClient<INetworkService, NetworkService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Deck-Network-Service/1.0");
        });
        
        // 注册日志服务
        services.AddSingleton<ILoggingService>(provider => 
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                // 如果已有ILoggerFactory，使用现有的
                var logger = loggerFactory.CreateLogger<LoggingService>();
                return new LoggingService();
            }
            
            // 否则创建默认的
            return LoggingService.CreateDefault();
        });

        return services;
    }

    /// <summary>
    /// 添加 Deck 服务并配置日志
    /// </summary>
    [RequiresUnreferencedCode("YAML serialization uses reflection")]
    public static IServiceCollection AddDeckServicesWithLogging(
        this IServiceCollection services, 
        LogLevel defaultLogLevel = LogLevel.Information)
    {
        // 配置日志
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(defaultLogLevel);
            
            // 为 Deck 服务设置适当的日志级别
            builder.AddFilter("Deck.Services", LogLevel.Information);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
        });

        // 添加 Deck 服务
        return services.AddDeckServices();
    }

    /// <summary>
    /// 添加 Deck 服务用于调试（详细日志）
    /// </summary>
    [RequiresUnreferencedCode("YAML serialization uses reflection")]
    public static IServiceCollection AddDeckServicesForDebug(this IServiceCollection services)
    {
        return services.AddDeckServicesWithLogging(LogLevel.Debug);
    }
}