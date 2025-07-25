using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 容器引擎工厂，用于创建和获取合适的容器引擎实例
/// </summary>
public interface IContainerEngineFactory
{
    /// <summary>
    /// 获取指定类型的容器引擎
    /// </summary>
    IContainerEngine? GetEngine(ContainerEngineType type);
    
    /// <summary>
    /// 获取首选容器引擎（根据系统检测结果）
    /// </summary>
    Task<IContainerEngine?> GetPreferredEngineAsync();
}

/// <summary>
/// 容器引擎工厂实现
/// </summary>
public class ContainerEngineFactory : IContainerEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISystemDetectionService _systemDetectionService;
    private readonly ILogger<ContainerEngineFactory> _logger;
    private readonly IEnumerable<IContainerEngine> _engines;

    public ContainerEngineFactory(
        IServiceProvider serviceProvider,
        ISystemDetectionService systemDetectionService,
        ILogger<ContainerEngineFactory> logger,
        IEnumerable<IContainerEngine> engines)
    {
        _serviceProvider = serviceProvider;
        _systemDetectionService = systemDetectionService;
        _logger = logger;
        _engines = engines;
    }

    /// <summary>
    /// 获取指定类型的容器引擎
    /// </summary>
    public IContainerEngine? GetEngine(ContainerEngineType type)
    {
        return _engines.FirstOrDefault(e => e.Type == type);
    }

    /// <summary>
    /// 获取首选容器引擎（根据系统检测结果）
    /// </summary>
    public async Task<IContainerEngine?> GetPreferredEngineAsync()
    {
        try
        {
            var engineInfo = await _systemDetectionService.DetectContainerEngineAsync();
            
            if (!engineInfo.IsAvailable)
            {
                _logger.LogWarning("No available container engine detected");
                return null;
            }

            var engine = GetEngine(engineInfo.Type);
            if (engine == null)
            {
                _logger.LogWarning("Container engine implementation not found for type: {EngineType}", engineInfo.Type);
                return null;
            }

            _logger.LogInformation("Using container engine: {EngineType}", engineInfo.Type);
            return engine;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get preferred container engine");
            return null;
        }
    }
}