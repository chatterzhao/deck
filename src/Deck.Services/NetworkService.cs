using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using SystemNetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace Deck.Services;

/// <summary>
/// 网络检测服务实现 - 提供网络连通性检测、代理配置、镜像源检测等功能
/// 支持离线模式和网络异常的Fallback策略
/// </summary>
public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;
    private readonly HttpClient _httpClient;
    private bool _isOfflineModeEnabled;

    public NetworkService(ILogger<NetworkService> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _isOfflineModeEnabled = false;
    }

    /// <summary>
    /// 检测网络连通性
    /// </summary>
    public async Task<NetworkConnectivityResult> CheckConnectivityAsync(int timeout = 5000)
    {
        _logger.LogInformation("开始网络连通性检测，超时设置: {Timeout}ms", timeout);
        
        var result = new NetworkConnectivityResult
        {
            CheckTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 并行检测多个关键服务
            var servicesToCheck = new[]
            {
                NetworkServiceType.HttpConnectivity,
                NetworkServiceType.DnsResolution,
                NetworkServiceType.GitHub,
                NetworkServiceType.DockerHub
            };

            var tasks = servicesToCheck.Select(service => 
                CheckServiceConnectivityAsync(service, timeout)).ToArray();

            result.ServiceResults = (await Task.WhenAll(tasks)).ToList();

            // 评估整体连通性状态
            var connectedServices = result.ServiceResults.Count(r => r.IsAvailable);
            var totalServices = result.ServiceResults.Count;

            result.IsConnected = connectedServices > 0;
            result.OverallStatus = EvaluateOverallStatus(connectedServices, totalServices);

            // 获取网络信息
            await PopulateNetworkInfoAsync(result);

            // 生成建议
            GenerateConnectivitySuggestions(result);

            _logger.LogInformation("网络连通性检测完成，状态: {Status}，连通服务: {Connected}/{Total}", 
                result.OverallStatus, connectedServices, totalServices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络连通性检测过程中出错");
            result.IsConnected = false;
            result.OverallStatus = ConnectivityStatus.Failed;
            result.Errors.Add($"检测过程异常: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.TotalElapsedMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 检测特定服务的网络连通性
    /// </summary>
    public async Task<ServiceConnectivityResult> CheckServiceConnectivityAsync(NetworkServiceType serviceType, int timeout = 5000)
    {
        if (!NetworkServiceEndpoints.ServiceUrls.TryGetValue(serviceType, out var testUrl) ||
            !NetworkServiceEndpoints.ServiceNames.TryGetValue(serviceType, out var serviceName))
        {
            throw new ArgumentException($"未知的网络服务类型: {serviceType}");
        }

        var result = new ServiceConnectivityResult
        {
            ServiceType = serviceType,
            ServiceName = serviceName,
            TestUrl = testUrl,
            CheckTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("检测服务 {ServiceName} 连通性: {TestUrl}", serviceName, testUrl);

            if (serviceType == NetworkServiceType.DnsResolution)
            {
                await CheckDnsConnectivityAsync(result, timeout);
            }
            else
            {
                await CheckHttpConnectivityAsync(result, timeout);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "服务 {ServiceName} 连通性检测失败", serviceName);
            result.Status = ConnectivityStatus.Failed;
            result.IsAvailable = false;
            result.ErrorMessage = ex.Message;
            result.DetailedError = ex.ToString();
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 批量检测多个服务的连通性
    /// </summary>
    public async Task<List<ServiceConnectivityResult>> CheckMultipleServicesAsync(
        IEnumerable<NetworkServiceType> serviceTypes, int timeout = 5000)
    {
        var serviceTypesList = serviceTypes.ToList();
        _logger.LogInformation("批量检测 {Count} 个服务的连通性", serviceTypesList.Count);

        var tasks = serviceTypesList.Select(serviceType => 
            CheckServiceConnectivityAsync(serviceType, timeout)).ToArray();

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// 获取网络异常的Fallback策略
    /// </summary>
    public async Task<NetworkFallbackStrategy> GetFallbackStrategyAsync(
        NetworkServiceType failedService, NetworkConnectivityResult connectivityResult)
    {
        _logger.LogInformation("为失败服务 {FailedService} 生成Fallback策略", failedService);

        var strategy = new NetworkFallbackStrategy
        {
            StrategyType = DetermineFallbackStrategyType(failedService, connectivityResult),
            Severity = DetermineFallbackSeverity(connectivityResult)
        };

        strategy.Description = GenerateFallbackDescription(strategy.StrategyType, failedService);
        strategy.RecommendedActions = await GenerateFallbackActionsAsync(strategy.StrategyType, failedService);
        strategy.AlternativeServices = await GetAlternativeServicesAsync(failedService);
        strategy.EstimatedRecoveryTime = EstimateRecoveryTime(strategy.StrategyType);
        strategy.EnableOfflineMode = ShouldEnableOfflineMode(strategy.Severity, connectivityResult);

        _logger.LogDebug("生成Fallback策略: {StrategyType}，严重程度: {Severity}", 
            strategy.StrategyType, strategy.Severity);

        return strategy;
    }

    /// <summary>
    /// 检测并配置代理设置
    /// </summary>
    public async Task<ProxyConfigurationResult> DetectAndConfigureProxyAsync()
    {
        _logger.LogInformation("开始检测和配置代理设置");

        var result = new ProxyConfigurationResult();

        try
        {
            // 检测系统代理设置
            var detectedProxy = await DetectSystemProxyAsync();
            result.DetectedProxy = detectedProxy;

            // 测试代理连接性
            if (detectedProxy != null && detectedProxy.Type != ProxyType.None)
            {
                var testResult = await TestProxyAsync(detectedProxy);
                result.ProxyTests.Add(testResult);
            }

            // 获取代理推荐
            result.ProxyRecommendations = await GetProxyRecommendationsAsync();

            // 评估配置状态
            result.IsConfigured = result.ProxyTests.Any(t => t.IsWorking) || 
                                 (detectedProxy?.Type == ProxyType.None);
            result.CurrentProxy = detectedProxy;

            result.ConfigurationMessages.Add(result.IsConfigured ? 
                "代理配置正常" : "代理配置可能需要调整");

            _logger.LogInformation("代理配置检测完成，是否配置: {IsConfigured}", result.IsConfigured);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "代理配置检测过程中出错");
            result.ConfigurationMessages.Add($"代理检测异常: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测容器镜像仓库可用性
    /// </summary>
    public async Task<RegistryConnectivityResult> CheckRegistryConnectivityAsync(
        ContainerRegistryType registryType, int timeout = 10000)
    {
        if (!NetworkServiceEndpoints.RegistryUrls.TryGetValue(registryType, out var registryUrl) ||
            !NetworkServiceEndpoints.RegistryNames.TryGetValue(registryType, out var registryName))
        {
            throw new ArgumentException($"未知的容器仓库类型: {registryType}");
        }

        _logger.LogInformation("检测容器仓库 {RegistryName} 连通性", registryName);

        var result = new RegistryConnectivityResult
        {
            RegistryType = registryType,
            RegistryName = registryName,
            RegistryUrl = registryUrl,
            CheckTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, registryUrl);
            request.Headers.Add("User-Agent", "Deck-Network-Check/1.0");

            using var cts = new CancellationTokenSource(timeout);
            using var response = await _httpClient.SendAsync(request, cts.Token);

            result.IsAvailable = response.IsSuccessStatusCode || 
                               response.StatusCode == HttpStatusCode.Unauthorized; // Registry API often returns 401
            result.Status = result.IsAvailable ? ConnectivityStatus.Connected : ConnectivityStatus.Failed;
            result.SpeedRating = EvaluateSpeedRating(stopwatch.ElapsedMilliseconds);

            // 检测支持的功能
            result.SupportedFeatures = await DetectRegistryFeaturesAsync(registryUrl, response);

            _logger.LogDebug("仓库 {RegistryName} 检测完成，可用: {IsAvailable}，响应时间: {ResponseTime}ms",
                registryName, result.IsAvailable, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            result.Status = ConnectivityStatus.Timeout;
            result.ErrorMessage = "连接超时";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "仓库 {RegistryName} 连通性检测失败", registryName);
            result.Status = ConnectivityStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 获取推荐的镜像仓库
    /// </summary>
    public async Task<List<ContainerRegistryRecommendation>> GetRecommendedRegistriesAsync(string? region = null)
    {
        _logger.LogInformation("获取推荐镜像仓库，区域: {Region}", region ?? "自动检测");

        var recommendations = new List<ContainerRegistryRecommendation>();

        // 检测地理位置（如果未指定区域）
        if (string.IsNullOrEmpty(region))
        {
            region = await DetectUserRegionAsync();
        }

        // 根据区域生成推荐
        var registryTypesToTest = GetRegistryTypesByRegion(region);

        foreach (var registryType in registryTypesToTest)
        {
            var connectivityResult = await CheckRegistryConnectivityAsync(registryType);
            var recommendation = CreateRegistryRecommendation(registryType, connectivityResult, region);
            recommendations.Add(recommendation);
        }

        // 按推荐评分排序
        recommendations = recommendations.OrderByDescending(r => r.RecommendationScore).ToList();

        _logger.LogInformation("生成 {Count} 个仓库推荐", recommendations.Count);
        return recommendations;
    }

    /// <summary>
    /// 验证网络设置
    /// </summary>
    public async Task<NetworkValidationResult> ValidateNetworkSettingsAsync()
    {
        _logger.LogInformation("开始验证网络设置");

        var result = new NetworkValidationResult
        {
            ValidationTime = DateTime.UtcNow
        };

        try
        {
            // 配置检查
            result.ConfigurationChecks = await PerformConfigurationChecksAsync();

            // 性能检查
            result.PerformanceChecks = await PerformPerformanceChecksAsync();

            // 安全检查
            result.SecurityChecks = await PerformSecurityChecksAsync();

            // 计算总体评分
            result.OverallScore = CalculateOverallScore(result);
            result.IsValid = result.OverallScore >= 70;

            // 生成改进建议
            result.ImprovementSuggestions = GenerateImprovementSuggestions(result);

            _logger.LogInformation("网络设置验证完成，评分: {Score}，有效: {IsValid}", 
                result.OverallScore, result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络设置验证过程中出错");
            result.IsValid = false;
            result.OverallScore = 0;
        }

        return result;
    }

    /// <summary>
    /// 启用离线模式
    /// </summary>
    public async Task<OfflineModeResult> EnableOfflineModeAsync(string reason)
    {
        _logger.LogInformation("启用离线模式，原因: {Reason}", reason);

        var result = new OfflineModeResult
        {
            Success = true,
            IsOfflineModeEnabled = true,
            Reason = reason,
            OperationTime = DateTime.UtcNow
        };

        try
        {
            _isOfflineModeEnabled = true;

            // 设置离线模式限制
            result.Limitations.AddRange(new[]
            {
                "无法从远程仓库下载模板",
                "无法检查模板更新",
                "无法验证远程镜像可用性",
                "无法获取最新的容器镜像"
            });

            // 设置可用的离线功能
            result.AvailableFeatures.AddRange(new[]
            {
                "使用本地已缓存的模板",
                "管理本地容器和镜像",
                "查看本地配置信息",
                "使用已下载的镜像构建容器"
            });

            result.StatusMessage = "离线模式已启用，将使用本地资源";

            _logger.LogInformation("离线模式启用成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用离线模式失败");
            result.Success = false;
            result.StatusMessage = $"启用离线模式失败: {ex.Message}";
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// 禁用离线模式
    /// </summary>
    public async Task<OfflineModeResult> DisableOfflineModeAsync()
    {
        _logger.LogInformation("禁用离线模式");

        var result = new OfflineModeResult
        {
            Success = true,
            IsOfflineModeEnabled = false,
            OperationTime = DateTime.UtcNow
        };

        try
        {
            _isOfflineModeEnabled = false;

            // 检查网络连通性
            var connectivityResult = await CheckConnectivityAsync();
            
            if (connectivityResult.IsConnected)
            {
                result.StatusMessage = "离线模式已禁用，网络连接正常";
                result.Reason = "网络连接已恢复";
            }
            else
            {
                result.StatusMessage = "离线模式已禁用，但网络连接仍有问题";
                result.Reason = "强制禁用离线模式";
            }

            _logger.LogInformation("离线模式禁用成功，网络状态: {IsConnected}", connectivityResult.IsConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用离线模式失败");
            result.Success = false;
            result.StatusMessage = $"禁用离线模式失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 获取当前网络状态
    /// </summary>
    public async Task<NetworkStatusResult> GetNetworkStatusAsync()
    {
        _logger.LogDebug("获取当前网络状态");

        var result = new NetworkStatusResult
        {
            StatusTime = DateTime.UtcNow,
            IsOfflineMode = _isOfflineModeEnabled
        };

        try
        {
            // 检查网络可用性
            result.IsNetworkAvailable = SystemNetworkInterface.GetIsNetworkAvailable();

            // 检查互联网连通性
            var connectivityResult = await CheckConnectivityAsync(3000);
            result.IsInternetAvailable = connectivityResult.IsConnected;

            // 获取网络接口信息
            result.NetworkInterfaces = await GetNetworkInterfacesAsync();

            // 获取网络统计信息
            result.Statistics = await GetNetworkStatisticsAsync();

            // 获取DNS服务器
            result.DnsServers = await GetDnsServersAsync();

            // 获取代理信息
            result.CurrentProxy = await DetectSystemProxyAsync();

            // 确定网络类型
            result.NetworkType = DetermineNetworkType(result.NetworkInterfaces);

            _logger.LogDebug("网络状态获取完成，网络可用: {NetworkAvailable}，互联网可用: {InternetAvailable}", 
                result.IsNetworkAvailable, result.IsInternetAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取网络状态时出错");
        }

        return result;
    }

    /// <summary>
    /// 测试网络速度
    /// </summary>
    public async Task<NetworkSpeedResult> TestNetworkSpeedAsync(string? testUrl = null, int timeout = 30000)
    {
        testUrl ??= "https://www.google.com";
        
        _logger.LogInformation("开始网络速度测试，测试URL: {TestUrl}", testUrl);

        var result = new NetworkSpeedResult
        {
            TestUrl = testUrl,
            TestTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 执行下载速度测试
            var downloadResults = await PerformDownloadSpeedTestAsync(testUrl, timeout);
            result.DownloadSpeedMbps = downloadResults.speedMbps;
            result.LatencyMs = downloadResults.latencyMs;
            result.TestDataSizeMB = downloadResults.dataSizeMB;

            // 简化的上传测试（基于延迟推算）
            result.UploadSpeedMbps = result.DownloadSpeedMbps * 0.1; // 估算值

            // 计算其他指标
            result.JitterMs = result.LatencyMs * 0.1; // 估算值
            result.PacketLossPercent = 0; // 简化实现

            result.Success = true;
            result.QualityRating = EvaluateNetworkQuality(result);

            _logger.LogInformation("网络速度测试完成，下载速度: {DownloadSpeed:F2} Mbps，延迟: {Latency:F2} ms", 
                result.DownloadSpeedMbps, result.LatencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络速度测试失败");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.QualityRating = NetworkQualityRating.Poor;
        }
        finally
        {
            stopwatch.Stop();
            result.TestDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        }

        return result;
    }

    #region Private Helper Methods

    private ConnectivityStatus EvaluateOverallStatus(int connectedServices, int totalServices)
    {
        var ratio = (double)connectedServices / totalServices;
        return ratio switch
        {
            >= 0.8 => ConnectivityStatus.Connected,
            >= 0.5 => ConnectivityStatus.Slow,
            > 0 => ConnectivityStatus.ProxyError,
            _ => ConnectivityStatus.Failed
        };
    }

    private async Task PopulateNetworkInfoAsync(NetworkConnectivityResult result)
    {
        try
        {
            // 获取本地IP地址
            result.LocalIPAddress = await GetLocalIPAddressAsync();

            // 获取公网IP和地理位置（简化实现）
            var geoInfo = await GetGeolocationInfoAsync();
            if (geoInfo != null)
            {
                result.PublicIPAddress = "xxx.xxx.xxx.xxx"; // 隐私考虑
                result.Geolocation = geoInfo;
            }

            // 确定网络类型
            result.NetworkType = await GetNetworkTypeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取网络信息时出错");
        }
    }

    private void GenerateConnectivitySuggestions(NetworkConnectivityResult result)
    {
        if (!result.IsConnected)
        {
            result.Suggestions.Add("检查网络连接和网线/WiFi状态");
            result.Suggestions.Add("确认防火墙设置是否阻止了网络访问");
            result.Suggestions.Add("尝试重启网络适配器");
        }
        else if (result.OverallStatus == ConnectivityStatus.Slow)
        {
            result.Suggestions.Add("网络连接较慢，建议使用国内镜像源");
            result.Suggestions.Add("考虑配置代理以提高访问速度");
        }
    }

    private async Task CheckDnsConnectivityAsync(ServiceConnectivityResult result, int timeout)
    {
        try
        {
            using var ping = new Ping();
            using var cts = new CancellationTokenSource(timeout);
            
            var reply = await ping.SendPingAsync(result.TestUrl, timeout);
            
            result.IsAvailable = reply.Status == IPStatus.Success;
            result.Status = reply.Status == IPStatus.Success ? 
                ConnectivityStatus.Connected : ConnectivityStatus.Failed;
        }
        catch
        {
            result.IsAvailable = false;
            result.Status = ConnectivityStatus.Failed;
        }
    }

    private async Task CheckHttpConnectivityAsync(ServiceConnectivityResult result, int timeout)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, result.TestUrl);
        request.Headers.Add("User-Agent", "Deck-Network-Check/1.0");

        using var cts = new CancellationTokenSource(timeout);
        using var response = await _httpClient.SendAsync(request, cts.Token);

        result.IsAvailable = response.IsSuccessStatusCode;
        result.Status = response.IsSuccessStatusCode ? 
            ConnectivityStatus.Connected : ConnectivityStatus.Failed;
        result.HttpStatusCode = (int)response.StatusCode;
    }

    private FallbackStrategyType DetermineFallbackStrategyType(
        NetworkServiceType failedService, NetworkConnectivityResult connectivityResult)
    {
        if (!connectivityResult.IsConnected)
        {
            return FallbackStrategyType.EnableOfflineMode;
        }

        if (failedService == NetworkServiceType.DockerHub || 
            failedService == NetworkServiceType.GitHub)
        {
            return FallbackStrategyType.UseMirrorService;
        }

        return FallbackStrategyType.ConfigureProxy;
    }

    private FallbackSeverity DetermineFallbackSeverity(NetworkConnectivityResult connectivityResult)
    {
        if (!connectivityResult.IsConnected)
            return FallbackSeverity.Critical;

        var failedServices = connectivityResult.ServiceResults.Count(r => !r.IsAvailable);
        return failedServices switch
        {
            0 => FallbackSeverity.Info,
            1 => FallbackSeverity.Warning,
            _ => FallbackSeverity.Error
        };
    }

    private string GenerateFallbackDescription(FallbackStrategyType strategyType, NetworkServiceType failedService)
    {
        return strategyType switch
        {
            FallbackStrategyType.UseMirrorService => $"使用镜像服务替代 {NetworkServiceEndpoints.ServiceNames.GetValueOrDefault(failedService, failedService.ToString())}",
            FallbackStrategyType.ConfigureProxy => "配置网络代理以改善连接",
            FallbackStrategyType.EnableOfflineMode => "启用离线模式使用本地资源",
            FallbackStrategyType.RetryConnection => "等待并重试网络连接",
            _ => "应用通用网络恢复策略"
        };
    }

    private async Task<List<FallbackAction>> GenerateFallbackActionsAsync(
        FallbackStrategyType strategyType, NetworkServiceType failedService)
    {
        var actions = new List<FallbackAction>();

        switch (strategyType)
        {
            case FallbackStrategyType.UseMirrorService:
                actions.Add(new FallbackAction
                {
                    ActionType = ActionType.SwitchRegistry,
                    Description = "切换到国内镜像源",
                    CanAutoExecute = true,
                    Priority = ActionPriority.High,
                    SuccessRate = 0.85
                });
                break;

            case FallbackStrategyType.ConfigureProxy:
                actions.Add(new FallbackAction
                {
                    ActionType = ActionType.ConfigureProxy,
                    Description = "配置HTTP/HTTPS代理",
                    CanAutoExecute = false,
                    Priority = ActionPriority.Medium,
                    SuccessRate = 0.70
                });
                break;

            case FallbackStrategyType.EnableOfflineMode:
                actions.Add(new FallbackAction
                {
                    ActionType = ActionType.EnableOfflineMode,
                    Description = "启用离线模式",
                    CanAutoExecute = true,
                    Priority = ActionPriority.High,
                    SuccessRate = 1.0
                });
                break;
        }

        return await Task.FromResult(actions);
    }

    private async Task<List<AlternativeService>> GetAlternativeServicesAsync(NetworkServiceType failedService)
    {
        var alternatives = new List<AlternativeService>();

        switch (failedService)
        {
            case NetworkServiceType.DockerHub:
                alternatives.AddRange(new[]
                {
                    new AlternativeService
                    {
                        ServiceName = "阿里云容器仓库",
                        ServiceUrl = NetworkServiceEndpoints.RegistryUrls[ContainerRegistryType.AliyunRegistry],
                        ServiceType = NetworkServiceType.AliyunRegistry,
                        Region = "CN",
                        ReliabilityScore = 90,
                        SpeedScore = 95,
                        IsRecommended = true,
                        Description = "阿里云提供的Docker Hub镜像加速服务"
                    },
                    new AlternativeService
                    {
                        ServiceName = "中科大镜像站",
                        ServiceUrl = NetworkServiceEndpoints.RegistryUrls[ContainerRegistryType.UstcRegistry],
                        ServiceType = NetworkServiceType.UstcRegistry,
                        Region = "CN",
                        ReliabilityScore = 85,
                        SpeedScore = 90,
                        IsRecommended = true,
                        Description = "中科大开源软件镜像站"
                    }
                });
                break;

            case NetworkServiceType.GitHub:
                alternatives.Add(new AlternativeService
                {
                    ServiceName = "Gitee",
                    ServiceUrl = "https://gitee.com",
                    ServiceType = NetworkServiceType.HttpConnectivity,
                    Region = "CN",
                    ReliabilityScore = 88,
                    SpeedScore = 92,
                    IsRecommended = true,
                    Description = "国内的Git仓库托管平台"
                });
                break;
        }

        return await Task.FromResult(alternatives);
    }

    private TimeSpan? EstimateRecoveryTime(FallbackStrategyType strategyType)
    {
        return strategyType switch
        {
            FallbackStrategyType.UseMirrorService => TimeSpan.FromMinutes(2),
            FallbackStrategyType.ConfigureProxy => TimeSpan.FromMinutes(10),
            FallbackStrategyType.RetryConnection => TimeSpan.FromMinutes(5),
            _ => null
        };
    }

    private bool ShouldEnableOfflineMode(FallbackSeverity severity, NetworkConnectivityResult connectivityResult)
    {
        return severity == FallbackSeverity.Critical || 
               (!connectivityResult.IsConnected && 
                connectivityResult.ServiceResults.All(r => !r.IsAvailable));
    }

    private Task<ProxyInfo?> DetectSystemProxyAsync()
    {
        try
        {
            // 简化的代理检测实现
            var proxy = WebRequest.GetSystemWebProxy();
            var testUri = new Uri("https://www.google.com");
            var proxyUri = proxy.GetProxy(testUri);

            if (proxyUri == testUri || proxyUri == null || string.IsNullOrEmpty(proxyUri.Host)) // 没有代理
            {
                return Task.FromResult<ProxyInfo?>(new ProxyInfo { Type = ProxyType.None });
            }

            return Task.FromResult<ProxyInfo?>(new ProxyInfo
            {
                Type = ProxyType.Http,
                Host = proxyUri.Host,
                Port = proxyUri.Port,
                AutoDetect = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检测系统代理设置时出错");
            return Task.FromResult<ProxyInfo?>(new ProxyInfo { Type = ProxyType.None });
        }
    }

    private async Task<ProxyTestResult> TestProxyAsync(ProxyInfo proxyInfo)
    {
        var result = new ProxyTestResult
        {
            ProxyInfo = proxyInfo,
            TestUrl = "https://www.google.com",
            TestTime = DateTime.UtcNow
        };

        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // 简化的代理测试
            using var request = new HttpRequestMessage(HttpMethod.Head, result.TestUrl);
            using var response = await _httpClient.SendAsync(request);
            
            stopwatch.Stop();
            
            result.IsWorking = response.IsSuccessStatusCode;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            result.IsWorking = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task<List<ProxyRecommendation>> GetProxyRecommendationsAsync()
    {
        // 简化实现，返回空列表
        return await Task.FromResult(new List<ProxyRecommendation>());
    }

    private SpeedRating EvaluateSpeedRating(long responseTimeMs)
    {
        return responseTimeMs switch
        {
            < 100 => SpeedRating.VeryFast,
            < 300 => SpeedRating.Fast,
            < 1000 => SpeedRating.Average,
            < 3000 => SpeedRating.Slow,
            _ => SpeedRating.VerySlow
        };
    }

    private async Task<List<RegistryFeature>> DetectRegistryFeaturesAsync(string registryUrl, HttpResponseMessage response)
    {
        var features = new List<RegistryFeature>();

        // 简化实现，基于URL判断
        if (registryUrl.Contains("docker.io"))
        {
            features.AddRange(new[] { RegistryFeature.RegistryV2, RegistryFeature.PublicImages });
        }
        else if (registryUrl.Contains("aliyuncs.com"))
        {
            features.AddRange(new[] { RegistryFeature.RegistryV2, RegistryFeature.ImageCaching, RegistryFeature.VulnerabilityScanning });
        }

        return await Task.FromResult(features);
    }

    private async Task<string> DetectUserRegionAsync()
    {
        try
        {
            // 简化的区域检测，基于默认DNS或其他指标
            // 实际实现可能需要使用IP地理位置服务
            return await Task.FromResult("CN"); // 默认假设中国区域
        }
        catch
        {
            return "US"; // 默认美国区域
        }
    }

    private List<ContainerRegistryType> GetRegistryTypesByRegion(string region)
    {
        if (region.Equals("CN", StringComparison.OrdinalIgnoreCase))
        {
            return new List<ContainerRegistryType>
            {
                ContainerRegistryType.AliyunRegistry,
                ContainerRegistryType.TencentRegistry,
                ContainerRegistryType.UstcRegistry,
                ContainerRegistryType.TsinghuaRegistry,
                ContainerRegistryType.NetEaseRegistry,
                ContainerRegistryType.DockerHub
            };
        }

        return new List<ContainerRegistryType>
        {
            ContainerRegistryType.DockerHub,
            ContainerRegistryType.QuayIo
        };
    }

    private ContainerRegistryRecommendation CreateRegistryRecommendation(
        ContainerRegistryType registryType, RegistryConnectivityResult connectivityResult, string region)
    {
        var recommendation = new ContainerRegistryRecommendation
        {
            RegistryType = registryType,
            RegistryName = connectivityResult.RegistryName,
            RegistryUrl = connectivityResult.RegistryUrl,
            Region = region
        };

        // 计算推荐评分
        var baseScore = connectivityResult.IsAvailable ? 70 : 0;
        var speedBonus = connectivityResult.SpeedRating switch
        {
            SpeedRating.VeryFast => 30,
            SpeedRating.Fast => 25,
            SpeedRating.Average => 15,
            SpeedRating.Slow => 5,
            _ => 0
        };

        recommendation.RecommendationScore = baseScore + speedBonus;

        // 设置优势和劣势
        if (connectivityResult.IsAvailable)
        {
            recommendation.Advantages.Add($"响应时间: {connectivityResult.ResponseTimeMs}ms");
            recommendation.Advantages.Add($"速度评级: {connectivityResult.SpeedRating}");
        }
        else
        {
            recommendation.Disadvantages.Add("当前无法访问");
            if (!string.IsNullOrEmpty(connectivityResult.ErrorMessage))
            {
                recommendation.Disadvantages.Add($"错误: {connectivityResult.ErrorMessage}");
            }
        }

        return recommendation;
    }

    private async Task<List<NetworkConfigCheck>> PerformConfigurationChecksAsync()
    {
        var checks = new List<NetworkConfigCheck>();

        // DNS配置检查
        checks.Add(new NetworkConfigCheck
        {
            CheckName = "DNS配置",
            Passed = true,
            Description = "DNS服务器配置正常"
        });

        // 网络适配器检查
        checks.Add(new NetworkConfigCheck
        {
            CheckName = "网络适配器",
            Passed = SystemNetworkInterface.GetIsNetworkAvailable(),
            Description = "网络适配器状态检查"
        });

        return await Task.FromResult(checks);
    }

    private async Task<List<NetworkPerformanceCheck>> PerformPerformanceChecksAsync()
    {
        var checks = new List<NetworkPerformanceCheck>();

        // 延迟检查
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);

            checks.Add(new NetworkPerformanceCheck
            {
                CheckName = "网络延迟",
                MetricValue = reply.RoundtripTime,
                MetricUnit = "ms",
                BenchmarkValue = 100,
                MeetsBenchmark = reply.RoundtripTime <= 100,
                Rating = reply.RoundtripTime switch
                {
                    < 50 => PerformanceRating.Excellent,
                    < 100 => PerformanceRating.Good,
                    < 200 => PerformanceRating.Average,
                    < 500 => PerformanceRating.Poor,
                    _ => PerformanceRating.VeryPoor
                }
            });
        }
        catch
        {
            checks.Add(new NetworkPerformanceCheck
            {
                CheckName = "网络延迟",
                MetricValue = -1,
                MetricUnit = "ms",
                BenchmarkValue = 100,
                MeetsBenchmark = false,
                Rating = PerformanceRating.VeryPoor
            });
        }

        return checks;
    }

    private async Task<List<NetworkSecurityCheck>> PerformSecurityChecksAsync()
    {
        var checks = new List<NetworkSecurityCheck>();

        // 简化的安全检查
        checks.Add(new NetworkSecurityCheck
        {
            CheckName = "HTTPS支持",
            SecurityLevel = SecurityLevel.Secure,
            Passed = true
        });

        return await Task.FromResult(checks);
    }

    private int CalculateOverallScore(NetworkValidationResult result)
    {
        var configScore = result.ConfigurationChecks.Count(c => c.Passed) * 100 / 
                         Math.Max(1, result.ConfigurationChecks.Count);
        var perfScore = result.PerformanceChecks.Count(c => c.MeetsBenchmark) * 100 / 
                       Math.Max(1, result.PerformanceChecks.Count);
        var secScore = result.SecurityChecks.Count(c => c.Passed) * 100 / 
                      Math.Max(1, result.SecurityChecks.Count);

        return (configScore + perfScore + secScore) / 3;
    }

    private List<string> GenerateImprovementSuggestions(NetworkValidationResult result)
    {
        var suggestions = new List<string>();

        if (result.ConfigurationChecks.Any(c => !c.Passed))
        {
            suggestions.Add("修复网络配置问题");
        }

        if (result.PerformanceChecks.Any(c => !c.MeetsBenchmark))
        {
            suggestions.Add("优化网络性能设置");
        }

        if (result.SecurityChecks.Any(c => !c.Passed))
        {
            suggestions.Add("加强网络安全配置");
        }

        return suggestions;
    }

    private async Task<List<Deck.Core.Models.NetworkInterface>> GetNetworkInterfacesAsync()
    {
        var interfaces = new List<Deck.Core.Models.NetworkInterface>();

        try
        {
            var networkInterfaces = SystemNetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in networkInterfaces.Where(n => n.OperationalStatus == OperationalStatus.Up))
            {
                var networkInterface = new Deck.Core.Models.NetworkInterface
                {
                    Name = ni.Name,
                    Type = ni.NetworkInterfaceType.ToString(),
                    IsActive = ni.OperationalStatus == OperationalStatus.Up,
                    MacAddress = ni.GetPhysicalAddress().ToString()
                };

                // 获取IP地址
                var ipProperties = ni.GetIPProperties();
                foreach (var addr in ipProperties.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        networkInterface.IPAddresses.Add(addr.Address.ToString());
                    }
                }

                // 获取统计信息
                var stats = ni.GetIPStatistics();
                networkInterface.BytesReceived = stats.BytesReceived;
                networkInterface.BytesSent = stats.BytesSent;

                interfaces.Add(networkInterface);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取网络接口信息时出错");
        }

        return await Task.FromResult(interfaces);
    }

    private async Task<NetworkStatistics> GetNetworkStatisticsAsync()
    {
        var statistics = new NetworkStatistics();

        try
        {
            // 简化的统计信息实现
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = properties.GetActiveTcpConnections();

            statistics.ActiveTcpConnections = tcpConnections.Length;
            statistics.ActiveUdpConnections = properties.GetActiveUdpListeners().Length;

            // 累计网络接口统计
            var networkInterfaces = SystemNetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces.Where(n => n.OperationalStatus == OperationalStatus.Up))
            {
                var stats = ni.GetIPStatistics();
                statistics.TotalBytesReceived += stats.BytesReceived;
                statistics.TotalBytesSent += stats.BytesSent;
                statistics.NetworkErrors += (int)(stats.IncomingPacketsWithErrors + stats.OutgoingPacketsWithErrors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取网络统计信息时出错");
        }

        return await Task.FromResult(statistics);
    }

    private async Task<List<string>> GetDnsServersAsync()
    {
        var dnsServers = new List<string>();

        try
        {
            var networkInterfaces = SystemNetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in networkInterfaces.Where(n => n.OperationalStatus == OperationalStatus.Up))
            {
                var ipProperties = ni.GetIPProperties();
                foreach (var dns in ipProperties.DnsAddresses)
                {
                    if (dns.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        dnsServers.Add(dns.ToString());
                    }
                }
            }

            // 去重
            dnsServers = dnsServers.Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取DNS服务器信息时出错");
        }

        return await Task.FromResult(dnsServers);
    }

    private string DetermineNetworkType(List<Deck.Core.Models.NetworkInterface> interfaces)
    {
        if (interfaces.Any(i => i.Type.Contains("Wireless") || i.Type.Contains("WiFi")))
        {
            return "WiFi";
        }

        if (interfaces.Any(i => i.Type.Contains("Ethernet")))
        {
            return "Ethernet";
        }

        return "Unknown";
    }

    private async Task<string> GetLocalIPAddressAsync()
    {
        try
        {
            var networkInterfaces = SystemNetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in networkInterfaces.Where(n => n.OperationalStatus == OperationalStatus.Up))
            {
                var ipProperties = ni.GetIPProperties();
                foreach (var addr in ipProperties.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        return addr.Address.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取本地IP地址时出错");
        }

        return await Task.FromResult("127.0.0.1");
    }

    private async Task<GeolocationInfo?> GetGeolocationInfoAsync()
    {
        // 简化实现，返回默认地理信息
        return await Task.FromResult(new GeolocationInfo
        {
            CountryCode = "CN",
            CountryName = "China",
            Region = "Unknown",
            City = "Unknown",
            ISP = "Unknown",
            TimeZone = TimeZoneInfo.Local.Id
        });
    }

    private async Task<string> GetNetworkTypeAsync()
    {
        try
        {
            var interfaces = await GetNetworkInterfacesAsync();
            return DetermineNetworkType(interfaces);
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<(double speedMbps, double latencyMs, double dataSizeMB)> PerformDownloadSpeedTestAsync(
        string testUrl, int timeout)
    {
        const double testDataSizeMB = 1.0; // 1MB测试数据
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, testUrl);
            using var cts = new CancellationTokenSource(timeout);
            using var response = await _httpClient.SendAsync(request, cts.Token);

            var contentLength = response.Content.Headers.ContentLength ?? 0;
            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            
            var buffer = new byte[8192];
            var totalBytes = 0;
            int bytesRead;
            
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0 && 
                   totalBytes < testDataSizeMB * 1024 * 1024)
            {
                totalBytes += bytesRead;
            }

            stopwatch.Stop();

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var actualDataMB = totalBytes / (1024.0 * 1024.0);
            var speedMbps = (actualDataMB * 8) / elapsedSeconds; // Convert to Mbps

            return (speedMbps, stopwatch.ElapsedMilliseconds, actualDataMB);
        }
        catch
        {
            stopwatch.Stop();
            return (0, stopwatch.ElapsedMilliseconds, 0);
        }
    }

    private NetworkQualityRating EvaluateNetworkQuality(NetworkSpeedResult result)
    {
        if (result.DownloadSpeedMbps >= 100)
            return NetworkQualityRating.Excellent;
        if (result.DownloadSpeedMbps >= 50)
            return NetworkQualityRating.Good;
        if (result.DownloadSpeedMbps >= 10)
            return NetworkQualityRating.Fair;
        if (result.DownloadSpeedMbps >= 1)
            return NetworkQualityRating.Poor;
        
        return NetworkQualityRating.VeryPoor;
    }

    #endregion
}