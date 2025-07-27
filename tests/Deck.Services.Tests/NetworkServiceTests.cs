using System.Net;
using Deck.Core.Interfaces;
using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Deck.Services.Tests;

/// <summary>
/// 网络服务测试 - 只测试模板同步的网络功能
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
    public async Task TestTemplateRepositoryAsync_WithValidRepository_ShouldReturnTrue()
    {
        // Arrange
        var repositoryUrl = "https://gitee.com/test/deck.git";
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.TestTemplateRepositoryAsync(repositoryUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestTemplateRepositoryAsync_WithInvalidRepository_ShouldReturnFalse()
    {
        // Arrange
        var repositoryUrl = "https://invalid-repo.com/test.git";
        SetupHttpClientMock(HttpStatusCode.NotFound);

        // Act
        var result = await _networkService.TestTemplateRepositoryAsync(repositoryUrl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestTemplateRepositoryAsync_WithTimeout_ShouldReturnFalse()
    {
        // Arrange
        var repositoryUrl = "https://slow-repo.com/test.git";
        SetupHttpClientMock(HttpStatusCode.OK, TimeSpan.FromSeconds(2)); // 2秒延迟

        // Act
        var result = await _networkService.TestTemplateRepositoryAsync(repositoryUrl, 500); // 500ms超时

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestTemplateRepositoryAsync_WithNetworkError_ShouldReturnFalse()
    {
        // Arrange
        var repositoryUrl = "https://error-repo.com/test.git";
        SetupHttpClientMockWithException(new HttpRequestException("Network error"));

        // Act
        var result = await _networkService.TestTemplateRepositoryAsync(repositoryUrl);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("https://gitee.com/test/deck.git")]
    [InlineData("https://github.com/test/deck.git")]
    [InlineData("https://gitlab.com/test/deck.git")]
    public async Task TestTemplateRepositoryAsync_WithDifferentGitProviders_ShouldWork(string repositoryUrl)
    {
        // Arrange
        SetupHttpClientMock(HttpStatusCode.OK);

        // Act
        var result = await _networkService.TestTemplateRepositoryAsync(repositoryUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestTemplateRepositoryAsync_WithCustomTimeout_ShouldRespectTimeout()
    {
        // Arrange
        var repositoryUrl = "https://test-repo.com/test.git";
        SetupHttpClientMock(HttpStatusCode.OK, TimeSpan.FromMilliseconds(200));

        // Act
        var result = await _networkService.TestTemplateRepositoryAsync(repositoryUrl, 100); // 100ms超时

        // Assert
        result.Should().BeFalse(); // 应该超时
    }

    // 废弃方法的测试 - 确保向后兼容性
    [Fact]
    public async Task CheckConnectivityAsync_ObsoleteMethod_ShouldReturnDefaultResult()
    {
        // Act
        #pragma warning disable CS0618 // Type or member is obsolete
        var result = await _networkService.CheckConnectivityAsync(5000);
        #pragma warning restore CS0618

        // Assert - 废弃方法应该返回默认的成功结果
        result.Should().NotBeNull();
        result.IsConnected.Should().BeTrue();
        result.OverallStatus.Should().Be(Deck.Core.Models.ConnectivityStatus.Connected);
    }

    private void SetupHttpClientMock(HttpStatusCode statusCode, TimeSpan? delay = null)
    {
        var response = new HttpResponseMessage(statusCode);
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value, cancellationToken);
                }
                return response;
            });
    }

    private void SetupHttpClientMockWithException(Exception exception)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}