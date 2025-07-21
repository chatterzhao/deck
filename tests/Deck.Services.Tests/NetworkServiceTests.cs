using System.Net;
using System.Net.NetworkInformation;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Deck.Services.Tests;

/// <summary>
/// 网络检测服务测试
/// </summary>
public class NetworkServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly INetworkService _networkService;

    public NetworkServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _networkService = new NetworkService(NullLogger<NetworkService>.Instance, _httpClient);
    }

    [Fact]
    public async Task CheckConnectivityAsync_WithSuccessfulConnections_ShouldReturnConnected()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.CheckConnectivityAsync(5000);

        // Assert
        result.Should().NotBeNull();
        result.IsConnected.Should().BeTrue();
        result.OverallStatus.Should().Be(ConnectivityStatus.Connected);
        result.ServiceResults.Should().NotBeEmpty();
        result.CheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.TotalElapsedMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckConnectivityAsync_WithTimeout_ShouldReturnFailed()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.RequestTimeout, TimeSpan.FromSeconds(10));

        // Act
        var result = await _networkService.CheckConnectivityAsync(1000);

        // Assert
        result.Should().NotBeNull();
        result.IsConnected.Should().BeFalse();
        result.OverallStatus.Should().Be(ConnectivityStatus.Failed);
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckServiceConnectivityAsync_GitHub_ShouldReturnResult()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.CheckServiceConnectivityAsync(NetworkServiceType.GitHub, 5000);

        // Assert
        result.Should().NotBeNull();
        result.ServiceType.Should().Be(NetworkServiceType.GitHub);
        result.ServiceName.Should().Be("GitHub");
        result.TestUrl.Should().Be("https://api.github.com");
        result.IsAvailable.Should().BeTrue();
        result.Status.Should().Be(ConnectivityStatus.Connected);
        result.ResponseTimeMs.Should().BeGreaterThan(0);
        result.CheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CheckServiceConnectivityAsync_DockerHub_ShouldReturnResult()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.Unauthorized); // Docker Registry often returns 401

        // Act
        var result = await _networkService.CheckServiceConnectivityAsync(NetworkServiceType.DockerHub, 5000);

        // Assert
        result.Should().NotBeNull();
        result.ServiceType.Should().Be(NetworkServiceType.DockerHub);
        result.ServiceName.Should().Be("Docker Hub");
        result.TestUrl.Should().Be("https://registry-1.docker.io/v2/");
        result.IsAvailable.Should().BeTrue(); // 401 is considered available for registry
        result.HttpStatusCode.Should().Be(401);
    }

    [Fact]
    public async Task CheckServiceConnectivityAsync_DnsResolution_ShouldPingAddress()
    {
        // Act - DNS resolution test uses ping, which might not work in test environment
        var result = await _networkService.CheckServiceConnectivityAsync(NetworkServiceType.DnsResolution, 3000);

        // Assert
        result.Should().NotBeNull();
        result.ServiceType.Should().Be(NetworkServiceType.DnsResolution);
        result.ServiceName.Should().Be("DNS解析");
        result.TestUrl.Should().Be("8.8.8.8");
        result.ResponseTimeMs.Should().BeGreaterThan(0);
        // Note: Result might be available or not depending on test environment
    }

    [Fact]
    public async Task CheckServiceConnectivityAsync_UnknownServiceType_ShouldThrowException()
    {
        // Arrange
        var unknownServiceType = (NetworkServiceType)999;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _networkService.CheckServiceConnectivityAsync(unknownServiceType));
    }

    [Fact]
    public async Task CheckMultipleServicesAsync_WithMultipleServices_ShouldReturnAllResults()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);
        var serviceTypes = new[]
        {
            NetworkServiceType.GitHub,
            NetworkServiceType.DockerHub,
            NetworkServiceType.HttpConnectivity
        };

        // Act
        var results = await _networkService.CheckMultipleServicesAsync(serviceTypes, 5000);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => serviceTypes.Contains(r.ServiceType));
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.ServiceName));
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.TestUrl));
    }

    [Fact]
    public async Task GetFallbackStrategyAsync_WithFailedGitHub_ShouldRecommendMirrorService()
    {
        // Arrange
        var connectivityResult = new NetworkConnectivityResult
        {
            IsConnected = true,
            OverallStatus = ConnectivityStatus.Slow
        };

        // Act
        var strategy = await _networkService.GetFallbackStrategyAsync(
            NetworkServiceType.GitHub, connectivityResult);

        // Assert
        strategy.Should().NotBeNull();
        strategy.StrategyType.Should().Be(FallbackStrategyType.UseMirrorService);
        strategy.Description.Should().Contain("GitHub");
        strategy.RecommendedActions.Should().NotBeEmpty();
        strategy.AlternativeServices.Should().NotBeEmpty();
        strategy.Severity.Should().BeOneOf(FallbackSeverity.Warning, FallbackSeverity.Error);
        strategy.EnableOfflineMode.Should().BeFalse(); // Network is connected
    }

    [Fact]
    public async Task GetFallbackStrategyAsync_WithNoNetwork_ShouldRecommendOfflineMode()
    {
        // Arrange
        var connectivityResult = new NetworkConnectivityResult
        {
            IsConnected = false,
            OverallStatus = ConnectivityStatus.Failed,
            ServiceResults = new List<ServiceConnectivityResult>
            {
                new() { ServiceType = NetworkServiceType.GitHub, IsAvailable = false },
                new() { ServiceType = NetworkServiceType.DockerHub, IsAvailable = false }
            }
        };

        // Act
        var strategy = await _networkService.GetFallbackStrategyAsync(
            NetworkServiceType.DockerHub, connectivityResult);

        // Assert
        strategy.Should().NotBeNull();
        strategy.StrategyType.Should().Be(FallbackStrategyType.EnableOfflineMode);
        strategy.Severity.Should().Be(FallbackSeverity.Critical);
        strategy.EnableOfflineMode.Should().BeTrue();
        strategy.Description.Should().Contain("离线模式");
        strategy.RecommendedActions.Should().NotBeEmpty();
        strategy.EstimatedRecoveryTime.Should().BeNull();
    }

    [Fact]
    public async Task DetectAndConfigureProxyAsync_ShouldReturnProxyConfiguration()
    {
        // Act
        var result = await _networkService.DetectAndConfigureProxyAsync();

        // Assert
        result.Should().NotBeNull();
        result.ConfigurationMessages.Should().NotBeEmpty();
        result.DetectedProxy.Should().NotBeNull();
        result.ProxyRecommendations.Should().NotBeNull();
        // IsConfigured depends on actual system proxy settings
    }

    [Fact]
    public async Task CheckRegistryConnectivityAsync_DockerHub_ShouldReturnResult()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.Unauthorized); // Registry API typical response

        // Act
        var result = await _networkService.CheckRegistryConnectivityAsync(
            ContainerRegistryType.DockerHub, 10000);

        // Assert
        result.Should().NotBeNull();
        result.RegistryType.Should().Be(ContainerRegistryType.DockerHub);
        result.RegistryName.Should().Be("Docker Hub");
        result.RegistryUrl.Should().Be("https://registry-1.docker.io/v2/");
        result.IsAvailable.Should().BeTrue(); // 401 is considered available for registry
        result.Status.Should().Be(ConnectivityStatus.Connected);
        result.ResponseTimeMs.Should().BeGreaterThan(0);
        result.SpeedRating.Should().BeOneOf(Enum.GetValues<SpeedRating>());
        result.CheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.SupportedFeatures.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckRegistryConnectivityAsync_AliyunRegistry_ShouldReturnResult()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.CheckRegistryConnectivityAsync(
            ContainerRegistryType.AliyunRegistry, 10000);

        // Assert
        result.Should().NotBeNull();
        result.RegistryType.Should().Be(ContainerRegistryType.AliyunRegistry);
        result.RegistryName.Should().Be("阿里云容器仓库");
        result.RegistryUrl.Should().Be("https://registry.cn-hangzhou.aliyuncs.com/v2/");
        result.IsAvailable.Should().BeTrue();
        result.Status.Should().Be(ConnectivityStatus.Connected);
        result.SupportedFeatures.Should().Contain(RegistryFeature.VulnerabilityScanning);
    }

    [Fact]
    public async Task CheckRegistryConnectivityAsync_UnknownRegistryType_ShouldThrowException()
    {
        // Arrange
        var unknownRegistryType = (ContainerRegistryType)999;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _networkService.CheckRegistryConnectivityAsync(unknownRegistryType));
    }

    [Fact]
    public async Task GetRecommendedRegistriesAsync_WithChinaRegion_ShouldPrioritizeChinaRegistries()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var recommendations = await _networkService.GetRecommendedRegistriesAsync("CN");

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().BeInDescendingOrder(r => r.RecommendationScore);
        
        // China region should prioritize local registries
        var topRecommendation = recommendations.First();
        topRecommendation.RegistryType.Should().BeOneOf(
            ContainerRegistryType.AliyunRegistry,
            ContainerRegistryType.TencentRegistry,
            ContainerRegistryType.UstcRegistry,
            ContainerRegistryType.TsinghuaRegistry);
    }

    [Fact]
    public async Task GetRecommendedRegistriesAsync_WithAutoDetectRegion_ShouldDetectAndRecommend()
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var recommendations = await _networkService.GetRecommendedRegistriesAsync();

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().BeInDescendingOrder(r => r.RecommendationScore);
        recommendations.Should().OnlyContain(r => !string.IsNullOrEmpty(r.RegistryName));
        recommendations.Should().OnlyContain(r => !string.IsNullOrEmpty(r.RegistryUrl));
        recommendations.Should().OnlyContain(r => r.Reasons.Count >= 0);
        recommendations.Should().OnlyContain(r => r.RecommendationScore >= 0 && r.RecommendationScore <= 100);
    }

    [Fact]
    public async Task ValidateNetworkSettingsAsync_ShouldReturnValidationResult()
    {
        // Act
        var result = await _networkService.ValidateNetworkSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.ValidationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ConfigurationChecks.Should().NotBeEmpty();
        result.PerformanceChecks.Should().NotBeEmpty();
        result.SecurityChecks.Should().NotBeEmpty();
        result.OverallScore.Should().BeInRange(0, 100);
        result.ImprovementSuggestions.Should().NotBeNull();
        // IsValid depends on actual network conditions
    }

    [Fact]
    public async Task EnableOfflineModeAsync_ShouldEnableOfflineMode()
    {
        // Arrange
        const string reason = "网络连接失败，启用离线模式";

        // Act
        var result = await _networkService.EnableOfflineModeAsync(reason);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsOfflineModeEnabled.Should().BeTrue();
        result.Reason.Should().Be(reason);
        result.OperationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Limitations.Should().NotBeEmpty();
        result.AvailableFeatures.Should().NotBeEmpty();
        result.StatusMessage.Should().Contain("离线模式已启用");
    }

    [Fact]
    public async Task DisableOfflineModeAsync_ShouldDisableOfflineMode()
    {
        // Arrange - First enable offline mode
        await _networkService.EnableOfflineModeAsync("测试");
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.DisableOfflineModeAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsOfflineModeEnabled.Should().BeFalse();
        result.OperationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.StatusMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetNetworkStatusAsync_ShouldReturnCurrentStatus()
    {
        // Act
        var result = await _networkService.GetNetworkStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.StatusTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.NetworkInterfaces.Should().NotBeNull();
        result.Statistics.Should().NotBeNull();
        result.DnsServers.Should().NotBeNull();
        result.NetworkType.Should().NotBeNullOrEmpty();
        result.CurrentProxy.Should().NotBeNull();
        // Network availability depends on actual system state
    }

    [Fact]
    public async Task TestNetworkSpeedAsync_WithDefaultUrl_ShouldReturnSpeedTest()
    {
        // Arrange
        SetupHttpClientMockForSpeedTest();

        // Act
        var result = await _networkService.TestNetworkSpeedAsync(timeout: 10000);

        // Assert
        result.Should().NotBeNull();
        result.TestTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.TestUrl.Should().NotBeNullOrEmpty();
        result.TestDurationSeconds.Should().BeGreaterThan(0);
        result.QualityRating.Should().BeOneOf(Enum.GetValues<NetworkQualityRating>());
        // Success and speed metrics depend on actual network conditions and mock setup
    }

    [Fact]
    public async Task TestNetworkSpeedAsync_WithCustomUrl_ShouldUseCustomUrl()
    {
        // Arrange
        const string customUrl = "https://httpbin.org/bytes/1024";
        SetupHttpClientMockForSpeedTest();

        // Act
        var result = await _networkService.TestNetworkSpeedAsync(customUrl, 10000);

        // Assert
        result.Should().NotBeNull();
        result.TestUrl.Should().Be(customUrl);
    }

    [Theory]
    [InlineData(NetworkServiceType.GitHub)]
    [InlineData(NetworkServiceType.DockerHub)]
    [InlineData(NetworkServiceType.QuayIo)]
    [InlineData(NetworkServiceType.AliyunRegistry)]
    [InlineData(NetworkServiceType.HttpConnectivity)]
    public async Task CheckServiceConnectivityAsync_AllSupportedServices_ShouldReturnResults(
        NetworkServiceType serviceType)
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.CheckServiceConnectivityAsync(serviceType, 5000);

        // Assert
        result.Should().NotBeNull();
        result.ServiceType.Should().Be(serviceType);
        result.ServiceName.Should().NotBeNullOrEmpty();
        result.TestUrl.Should().NotBeNullOrEmpty();
        result.ResponseTimeMs.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(ContainerRegistryType.DockerHub)]
    [InlineData(ContainerRegistryType.QuayIo)]
    [InlineData(ContainerRegistryType.AliyunRegistry)]
    [InlineData(ContainerRegistryType.TencentRegistry)]
    [InlineData(ContainerRegistryType.UstcRegistry)]
    public async Task CheckRegistryConnectivityAsync_AllSupportedRegistries_ShouldReturnResults(
        ContainerRegistryType registryType)
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.Unauthorized); // Common registry response

        // Act
        var result = await _networkService.CheckRegistryConnectivityAsync(registryType, 10000);

        // Assert
        result.Should().NotBeNull();
        result.RegistryType.Should().Be(registryType);
        result.RegistryName.Should().NotBeNullOrEmpty();
        result.RegistryUrl.Should().NotBeNullOrEmpty();
        result.ResponseTimeMs.Should().BeGreaterThan(0);
        result.SupportedFeatures.Should().NotBeNull();
    }

    [Fact]
    public void NetworkServiceEndpoints_ServiceUrls_ShouldContainAllServices()
    {
        // Assert
        NetworkServiceEndpoints.ServiceUrls.Should().ContainKeys(
            NetworkServiceType.GitHub,
            NetworkServiceType.DockerHub,
            NetworkServiceType.QuayIo,
            NetworkServiceType.AliyunRegistry,
            NetworkServiceType.HttpConnectivity,
            NetworkServiceType.DnsResolution);

        NetworkServiceEndpoints.ServiceUrls.Values.Should().OnlyContain(url => 
            !string.IsNullOrEmpty(url) && (url.StartsWith("http") || url.Contains(".")));
    }

    [Fact]
    public void NetworkServiceEndpoints_ServiceNames_ShouldContainAllServices()
    {
        // Assert
        NetworkServiceEndpoints.ServiceNames.Should().ContainKeys(
            NetworkServiceType.GitHub,
            NetworkServiceType.DockerHub,
            NetworkServiceType.QuayIo,
            NetworkServiceType.AliyunRegistry);

        NetworkServiceEndpoints.ServiceNames.Values.Should().OnlyContain(name => 
            !string.IsNullOrEmpty(name));
    }

    [Fact]
    public void NetworkServiceEndpoints_RegistryUrls_ShouldContainAllRegistries()
    {
        // Assert
        NetworkServiceEndpoints.RegistryUrls.Should().ContainKeys(
            ContainerRegistryType.DockerHub,
            ContainerRegistryType.QuayIo,
            ContainerRegistryType.AliyunRegistry,
            ContainerRegistryType.TencentRegistry);

        NetworkServiceEndpoints.RegistryUrls.Values.Should().OnlyContain(url => 
            !string.IsNullOrEmpty(url) && url.StartsWith("https://"));
    }

    [Fact]
    public void NetworkServiceEndpoints_RegistryNames_ShouldContainAllRegistries()
    {
        // Assert
        NetworkServiceEndpoints.RegistryNames.Should().ContainKeys(
            ContainerRegistryType.DockerHub,
            ContainerRegistryType.QuayIo,
            ContainerRegistryType.AliyunRegistry,
            ContainerRegistryType.Custom);

        NetworkServiceEndpoints.RegistryNames.Values.Should().OnlyContain(name => 
            !string.IsNullOrEmpty(name));
    }

    private void SetupHttpClientMock(HttpStatusCode statusCode, TimeSpan? delay = null)
    {
        var response = new HttpResponseMessage(statusCode);
        response.Content = new StringContent("Mock response content");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async () =>
            {
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value);
                }
                return response;
            });
    }

    private void SetupHttpClientMockForSpeedTest()
    {
        // Create a response with some content for speed testing
        var content = new byte[1024 * 1024]; // 1MB of data
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(content);
        response.Content.Headers.ContentLength = content.Length;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}