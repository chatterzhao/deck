using Deck.Core.Interfaces;
using Deck.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

public static class ServiceCollectionExtensions
{
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
        services.AddSingleton<IRemoteTemplatesService, RemoteTemplatesService>();
        services.AddSingleton<IStartCommandService, StartCommandServiceSimple>();
        services.AddSingleton<IConsoleUIService, ConsoleUIService>();
        services.AddSingleton<IConsoleDisplay, ConsoleDisplayService>();
        services.AddSingleton<IInteractiveSelectionService, InteractiveSelectionService>();
        services.AddSingleton<IAdvancedInteractiveSelectionService, AdvancedInteractiveSelectionService>();
        
        // 注册配置合并服务
        services.AddSingleton<IConfigurationMerger, ConfigurationMerger>();
        
        // 注册目录管理服务（完整实现）
        services.AddSingleton<IDirectoryManagementService, DirectoryManagementService>();
        
        // 注册文件系统服务
        services.AddSingleton<IFileSystemService, FileSystemService>();
        
        // 注册三层统一管理服务（简化版本）
        services.AddSingleton<IImagesUnifiedService, ImagesUnifiedServiceSimple>();
        
        // 注册清理服务
        services.AddSingleton<ICleaningService, CleaningService>();
        
        // 注册三层工作流程服务（完整实现）
        services.AddSingleton<IThreeLayerWorkflowService, ThreeLayerWorkflowService>();
        
        // 注册模板变量引擎服务
        services.AddSingleton<ITemplateVariableEngine, TemplateVariableEngine>();
        
        // 注册容器管理服务
        services.AddSingleton<IContainerService, ContainerService>();
        
        // 注册容器引擎服务
        services.AddSingleton<IContainerEngine, PodmanEngine>();
        services.AddSingleton<IContainerEngine, DockerEngine>();
        
        // 注册容器引擎工厂
        services.AddSingleton<IContainerEngineFactory, ContainerEngineFactory>();
        
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
    public static IServiceCollection AddDeckServicesWithLogging(
        this IServiceCollection services, 
        Action<LoggingConfiguration>? configureLogging = null)
    {
        services.AddDeckServices();
        
        // 配置日志
        if (configureLogging != null)
        {
            services.AddSingleton(provider =>
            {
                var config = new LoggingConfiguration();
                configureLogging(config);
                return config;
            });
        }

        return services;
    }
}