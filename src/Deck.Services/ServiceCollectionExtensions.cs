using Deck.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

public static class ServiceCollectionExtensions
{
    public static void AddDeckServices(this IServiceCollection services)
    {
        // 添加HttpClient服务
        services.AddHttpClient();
        
        // 添加核心服务
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IConfigurationMerger, ConfigurationMerger>();
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<IConsoleDisplay, ConsoleDisplayService>();
        services.AddSingleton<IConsoleUIService, ConsoleUIService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IEnhancedFileOperationsService, EnhancedFileOperationsService>();
        services.AddSingleton<IEnvironmentConfigurationService, EnvironmentConfigurationService>();
        services.AddSingleton<IRemoteTemplatesService, RemoteTemplatesService>();
        services.AddSingleton<IInteractiveSelectionService, InteractiveSelectionService>();
        services.AddSingleton<IAdvancedInteractiveSelectionService, AdvancedInteractiveSelectionService>();
        services.AddSingleton<IStartCommandService, StartCommandServiceSimple>();
        services.AddSingleton<IThreeLayerWorkflowService, ThreeLayerWorkflowService>();
        services.AddSingleton<ITemplateVariableEngine, TemplateVariableEngine>();
        services.AddSingleton<IPortConflictService, PortConflictService>();
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IImagePermissionService, ImagePermissionService>();
        services.AddSingleton<IImagesUnifiedService, ImagesUnifiedServiceSimple>();
        services.AddSingleton<ICleaningService, CleaningService>();
        services.AddSingleton<IGlobalExceptionHandler, GlobalExceptionHandler>();
        services.AddSingleton<IDirectoryManagementService, DirectoryManagementService>();
        services.AddSingleton<ISystemDetectionService, SystemDetectionService>();
        services.AddSingleton<IContainerService, ContainerService>();
        
        // 添加新的容器引擎管理服务
        services.AddSingleton<IContainerEngineManagementService, ContainerEngineManagementService>();
        
        // 工厂模式注册
        services.AddSingleton<IContainerEngineFactory, ContainerEngineFactory>();
    }
}