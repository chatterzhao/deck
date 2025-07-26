using Deck.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

public static class ServiceCollectionExtensions
{
    public static void AddDeckServicesWithLogging(this IServiceCollection services)
    {
        // 添加日志服务
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // 注册服务
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<ISystemDetectionService, SystemDetectionService>();
        services.AddSingleton<IRemoteTemplatesService, RemoteTemplatesService>();
        services.AddSingleton<IImagePermissionService, ImagePermissionService>();
        services.AddSingleton<IPortConflictService, PortConflictService>();
        services.AddTransient<IConfigurationService, ConfigurationService>();
        services.AddTransient<IEnhancedFileOperationsService, EnhancedFileOperationsService>();
        services.AddTransient<IContainerEngineFactory, ContainerEngineFactory>();
        services.AddTransient<IContainerService, ContainerService>();
        services.AddTransient<IAdvancedInteractiveSelectionService, AdvancedInteractiveSelectionService>();
        services.AddTransient<IConsoleDisplay, ConsoleDisplayService>();
        services.AddTransient<IDirectoryManagementService, DirectoryManagementService>();
        services.AddTransient<IStartCommandService, StartCommandServiceSimple>();
        services.AddTransient<IThreeLayerWorkflowService, ThreeLayerWorkflowService>();
        services.AddTransient<ITemplateVariableEngine, TemplateVariableEngine>();
        services.AddTransient<IGlobalExceptionHandler, GlobalExceptionHandler>();
        services.AddTransient<IContainerEngine, PodmanEngine>();
        services.AddTransient<IContainerEngine, DockerEngine>();
    }
}