using Deck.Core.Models;

namespace Deck.Core.Interfaces;

/// <summary>
/// 网络服务接口 - 专注于模板同步的网络处理
/// 不再进行通用网络测试，只处理实际需要的场景
/// </summary>
public interface INetworkService
{
    /// <summary>
    /// 测试模板仓库连接性 - 仅在实际同步模板时使用
    /// </summary>
    /// <param name="repositoryUrl">仓库地址</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    Task<bool> TestTemplateRepositoryAsync(string repositoryUrl, int timeout = 10000);

    /// <summary>
    /// 废弃：不再进行通用网络连通性检测
    /// </summary>
    [Obsolete("不再进行通用网络测试，只在模板同步时检测仓库连接性")]
    Task<NetworkConnectivityResult> CheckConnectivityAsync(int timeout = 5000);

    /// <summary>
    /// 检测特定服务的网络连通性
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    Task<ServiceConnectivityResult> CheckServiceConnectivityAsync(NetworkServiceType serviceType, int timeout = 5000);

    /// <summary>
    /// 批量检测多个服务的连通性
    /// </summary>
    /// <param name="serviceTypes">服务类型列表</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    Task<List<ServiceConnectivityResult>> CheckMultipleServicesAsync(IEnumerable<NetworkServiceType> serviceTypes, int timeout = 5000);

    /// <summary>
    /// 获取网络异常的Fallback策略
    /// </summary>
    /// <param name="failedService">失败的服务</param>
    /// <param name="connectivityResult">连通性检测结果</param>
    Task<NetworkFallbackStrategy> GetFallbackStrategyAsync(NetworkServiceType failedService, NetworkConnectivityResult connectivityResult);

    /// <summary>
    /// 检测并配置代理设置
    /// </summary>
    Task<ProxyConfigurationResult> DetectAndConfigureProxyAsync();

    /// <summary>
    /// 检测容器镜像仓库可用性
    /// </summary>
    /// <param name="registryType">仓库类型</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    Task<RegistryConnectivityResult> CheckRegistryConnectivityAsync(ContainerRegistryType registryType, int timeout = 10000);

    /// <summary>
    /// 获取推荐的镜像仓库
    /// </summary>
    /// <param name="region">地理区域</param>
    Task<List<ContainerRegistryRecommendation>> GetRecommendedRegistriesAsync(string? region = null);

    /// <summary>
    /// 验证网络设置
    /// </summary>
    Task<NetworkValidationResult> ValidateNetworkSettingsAsync();

    /// <summary>
    /// 启用离线模式
    /// </summary>
    /// <param name="reason">启用原因</param>
    Task<OfflineModeResult> EnableOfflineModeAsync(string reason);

    /// <summary>
    /// 禁用离线模式
    /// </summary>
    Task<OfflineModeResult> DisableOfflineModeAsync();

    /// <summary>
    /// 获取当前网络状态
    /// </summary>
    Task<NetworkStatusResult> GetNetworkStatusAsync();

    /// <summary>
    /// 测试网络速度
    /// </summary>
    /// <param name="testUrl">测试URL</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    Task<NetworkSpeedResult> TestNetworkSpeedAsync(string? testUrl = null, int timeout = 30000);
}