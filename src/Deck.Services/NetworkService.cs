using System.Diagnostics;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;

namespace Deck.Services;

/// <summary>
/// 网络服务 - 专注于模板同步的网络处理
/// 不再进行通用网络测试，只处理实际需要的场景
/// </summary>
public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;
    private readonly HttpClient _httpClient;

    public NetworkService(ILogger<NetworkService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// 测试模板仓库连接性 - 仅在实际同步模板时使用
    /// </summary>
    public async Task<bool> TestTemplateRepositoryAsync(string repositoryUrl, int timeout = 10000)
    {
        _logger.LogInformation("测试模板仓库连接性: {RepositoryUrl}", repositoryUrl);
        
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            using var request = new HttpRequestMessage(HttpMethod.Head, repositoryUrl);
            request.Headers.Add("User-Agent", "Deck-Template-Sync/1.0");
            
            using var response = await _httpClient.SendAsync(request, cts.Token);
            var isAvailable = response.IsSuccessStatusCode;
            
            _logger.LogInformation("模板仓库连接性测试结果: {IsAvailable}", isAvailable);
            return isAvailable;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("模板仓库连接超时: {RepositoryUrl}", repositoryUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "模板仓库连接失败: {RepositoryUrl}", repositoryUrl);
            return false;
        }
    }

    /// <summary>
    /// 废弃：不再进行通用网络连通性检测
    /// </summary>
    [Obsolete("不再进行通用网络测试，只在模板同步时检测仓库连接性")]
    public async Task<NetworkConnectivityResult> CheckConnectivityAsync(int timeout = 5000)
    {
        await Task.CompletedTask;
        _logger.LogWarning("CheckConnectivityAsync 已废弃，不再进行通用网络测试");
        
        return new NetworkConnectivityResult
        {
            CheckTime = DateTime.UtcNow,
            IsConnected = true,
            OverallStatus = ConnectivityStatus.Connected,
            ServiceResults = new List<ServiceConnectivityResult>(),
            NetworkType = "Unknown"
        };
    }

    #region 废弃方法 - 仅保留接口兼容性，内部返回默认值

    public async Task<ServiceConnectivityResult> CheckServiceConnectivityAsync(NetworkServiceType serviceType, int timeout = 5000)
    {
        await Task.CompletedTask;
        _logger.LogWarning("CheckServiceConnectivityAsync 已废弃，建议使用 TestTemplateRepositoryAsync");
        
        return new ServiceConnectivityResult
        {
            ServiceType = serviceType,
            IsAvailable = true,
            Status = ConnectivityStatus.Connected,
            ResponseTimeMs = 0
        };
    }

    public async Task<List<ServiceConnectivityResult>> CheckMultipleServicesAsync(IEnumerable<NetworkServiceType> serviceTypes, int timeout = 5000)
    {
        await Task.CompletedTask;
        _logger.LogWarning("CheckMultipleServicesAsync 已废弃，建议使用 TestTemplateRepositoryAsync");
        
        return serviceTypes.Select(serviceType => new ServiceConnectivityResult
        {
            ServiceType = serviceType,
            IsAvailable = true,
            Status = ConnectivityStatus.Connected,
            ResponseTimeMs = 0
        }).ToList();
    }

    public async Task<NetworkFallbackStrategy> GetFallbackStrategyAsync(NetworkServiceType failedService, NetworkConnectivityResult connectivityResult)
    {
        await Task.CompletedTask;
        _logger.LogWarning("GetFallbackStrategyAsync 已废弃");
        
        return new NetworkFallbackStrategy
        {
            StrategyType = FallbackStrategyType.EnableOfflineMode,
            Description = "网络不可用时的回退策略",
            RecommendedActions = new List<FallbackAction>
            {
                new FallbackAction { ActionType = ActionType.EnableOfflineMode, Description = "使用本地缓存" }
            }
        };
    }

    public async Task<ProxyConfigurationResult> DetectAndConfigureProxyAsync()
    {
        await Task.CompletedTask;
        _logger.LogWarning("DetectAndConfigureProxyAsync 已废弃");
        
        return new ProxyConfigurationResult
        {
            IsConfigured = false,
            DetectedProxy = null,
            CurrentProxy = null
        };
    }

    public async Task<RegistryConnectivityResult> CheckRegistryConnectivityAsync(ContainerRegistryType registryType, int timeout = 10000)
    {
        await Task.CompletedTask;
        _logger.LogWarning("CheckRegistryConnectivityAsync 已废弃");
        
        return new RegistryConnectivityResult
        {
            RegistryType = registryType,
            IsAvailable = true,
            ResponseTimeMs = 0
        };
    }

    public async Task<List<ContainerRegistryRecommendation>> GetRecommendedRegistriesAsync(string? region = null)
    {
        await Task.CompletedTask;
        _logger.LogWarning("GetRecommendedRegistriesAsync 已废弃");
        
        return new List<ContainerRegistryRecommendation>();
    }

    public async Task<NetworkValidationResult> ValidateNetworkSettingsAsync()
    {
        await Task.CompletedTask;
        _logger.LogWarning("ValidateNetworkSettingsAsync 已废弃");
        
        return new NetworkValidationResult
        {
            IsValid = true,
            ConfigurationChecks = new List<NetworkConfigCheck>()
        };
    }

    public async Task<OfflineModeResult> EnableOfflineModeAsync(string reason)
    {
        await Task.CompletedTask;
        _logger.LogWarning("EnableOfflineModeAsync 已废弃");
        
        return new OfflineModeResult
        {
            IsOfflineModeEnabled = false,
            StatusMessage = "离线模式功能已废弃"
        };
    }

    public async Task<OfflineModeResult> DisableOfflineModeAsync()
    {
        await Task.CompletedTask;
        _logger.LogWarning("DisableOfflineModeAsync 已废弃");
        
        return new OfflineModeResult
        {
            IsOfflineModeEnabled = false,
            StatusMessage = "离线模式功能已废弃"
        };
    }

    public async Task<NetworkStatusResult> GetNetworkStatusAsync()
    {
        await Task.CompletedTask;
        
        return new NetworkStatusResult
        {
            IsNetworkAvailable = true,
            IsInternetAvailable = true,
            IsOfflineMode = false
        };
    }

    public async Task<NetworkSpeedResult> TestNetworkSpeedAsync(string? testUrl = null, int timeout = 30000)
    {
        await Task.CompletedTask;
        _logger.LogWarning("TestNetworkSpeedAsync 已废弃");
        
        return new NetworkSpeedResult
        {
            DownloadSpeedMbps = 0,
            UploadSpeedMbps = 0,
            LatencyMs = 0
        };
    }

    #endregion
}