using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Deck.Services.Tests;

public class EnvironmentConfigurationServiceTests : IDisposable
{
    private readonly Mock<ILogger<EnvironmentConfigurationService>> _loggerMock;
    private readonly EnvironmentConfigurationService _service;
    private readonly string _tempDir;

    public EnvironmentConfigurationServiceTests()
    {
        _loggerMock = new Mock<ILogger<EnvironmentConfigurationService>>();
        _service = new EnvironmentConfigurationService(_loggerMock.Object);
        _tempDir = Path.Combine(Path.GetTempPath(), $"deck-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region UpdateComposeEnvironmentAsync

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_FileNotFound_ShouldReturnFalse()
    {
        var result = await _service.UpdateComposeEnvironmentAsync("/nonexistent/compose.yaml", EnvironmentType.Development, "test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_Development_ShouldReplaceDevSuffix()
    {
        // Arrange - dotnet-deck 模板风格
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
              app-dev:
                image: dotnet:9.0
                container_name: ${PROJECT_NAME:-deck}-dev
                hostname: ${PROJECT_NAME:-deck}-dev
                command: app-dev bash
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Development, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        updated.Should().Contain("app-dev:");  // dev 环境，-dev 不变
        updated.Should().Contain("container_name: ${PROJECT_NAME:-myproject}");
        updated.Should().Contain("hostname: ${PROJECT_NAME:-myproject}");
        updated.Should().Contain("command: bash");
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_Test_ShouldReplaceDevToTest()
    {
        // Arrange
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
              app-dev:
                image: dotnet:9.0
                container_name: ${PROJECT_NAME:-deck}-dev
                hostname: ${PROJECT_NAME:-deck}-dev
                command: app-dev bash
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Test, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        updated.Should().Contain("app-test:");
        updated.Should().Contain("container_name: ${PROJECT_NAME:-myproject}");
        updated.Should().Contain("hostname: ${PROJECT_NAME:-myproject}");
        updated.Should().Contain("command: bash");
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_Production_ShouldReplaceDevToProd()
    {
        // Arrange
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
              app-dev:
                image: dotnet:9.0
                container_name: ${PROJECT_NAME:-deck}-dev
                hostname: ${PROJECT_NAME:-deck}-dev
                command: app-dev bash
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Production, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        updated.Should().Contain("app-prod:");
        updated.Should().Contain("container_name: ${PROJECT_NAME:-myproject}");
        updated.Should().Contain("hostname: ${PROJECT_NAME:-myproject}");
        updated.Should().Contain("command: bash");
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_AlreadyUpdated_ShouldNotDoubleReplace()
    {
        // Arrange - 文件已经是 test 后缀
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
              app-test:
                image: dotnet:9.0
                container_name: ${PROJECT_NAME:-deck}
                hostname: ${PROJECT_NAME:-deck}
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Test, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        // 不应出现 app-test-test（双重替换）
        updated.Should().NotContain("app-test-test:");
        updated.Should().Contain("app-test:");
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_MultipleServices_ShouldReplaceAll()
    {
        // Arrange - 多个服务
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
              web-dev:
                image: dotnet:9.0
              db-dev:
                image: postgres:15
              cache-dev:
                image: redis:7
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Test, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        updated.Should().Contain("web-test:");
        updated.Should().Contain("db-test:");
        updated.Should().Contain("cache-test:");
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_CommandWithServicePrefix_ShouldReplace()
    {
        // Arrange
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
              app-dev:
                image: dotnet:9.0
                command: app-dev bash
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Production, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        updated.Should().Contain("command: bash");
        updated.Should().NotContain("command: app-dev bash");
    }

    [Fact]
    public async Task UpdateComposeEnvironmentAsync_IndentedServices_ShouldPreserveIndentation()
    {
        // Arrange - 注意缩进
        var composeFile = Path.Combine(_tempDir, "compose.yaml");
        var originalContent = """
            services:
                app-dev:
                    image: dotnet:9.0
                another-dev:
                    image: redis:7
            """;
        await File.WriteAllTextAsync(composeFile, originalContent);

        // Act
        var result = await _service.UpdateComposeEnvironmentAsync(composeFile, EnvironmentType.Test, "myproject");

        // Assert
        result.Should().BeTrue();
        var updated = await File.ReadAllTextAsync(composeFile);
        updated.Should().Contain("    app-test:");
        updated.Should().Contain("    another-test:");
    }

    #endregion

    #region UpdateEnvFileEnvironmentAsync

    [Fact]
    public async Task UpdateEnvFileEnvironmentAsync_FileNotFound_ShouldReturnFalse()
    {
        var result = await _service.UpdateEnvFileEnvironmentAsync("/nonexistent/.env", EnvironmentType.Development);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEnvFileEnvironmentAsync_ShouldUpdateDotnetEnvironment()
    {
        // Arrange
        var envFile = Path.Combine(_tempDir, ".env");
        await File.WriteAllTextAsync(envFile, "DOTNET_ENVIRONMENT=Development\nDEV_PORT=5000\n");

        // Act
        var result = await _service.UpdateEnvFileEnvironmentAsync(envFile, EnvironmentType.Production);

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(envFile);
        content.Should().Contain("DOTNET_ENVIRONMENT=Production");
    }

    [Fact]
    public async Task UpdateEnvFileEnvironmentAsync_ShouldUpdateAspNetCoreEnvironment()
    {
        // Arrange
        var envFile = Path.Combine(_tempDir, ".env");
        await File.WriteAllTextAsync(envFile, "ASPNETCORE_ENVIRONMENT=Development\nHTTPS_PORT=5001\n");

        // Act
        var result = await _service.UpdateEnvFileEnvironmentAsync(envFile, EnvironmentType.Test);

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(envFile);
        content.Should().Contain("ASPNETCORE_ENVIRONMENT=Test");
    }

    [Fact]
    public async Task UpdateEnvFileEnvironmentAsync_ShouldAdjustPorts()
    {
        // Arrange
        var envFile = Path.Combine(_tempDir, ".env");
        await File.WriteAllTextAsync(envFile, "DEV_PORT=5000\nWEB_PORT=3000\n");

        // Act - Test environment has offset of 1000
        var result = await _service.UpdateEnvFileEnvironmentAsync(envFile, EnvironmentType.Test);

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(envFile);
        content.Should().Contain("DEV_PORT=6000");   // 5000 + 1000
        content.Should().Contain("WEB_PORT=4000");   // 3000 + 1000
    }

    [Fact]
    public async Task UpdateEnvFileEnvironmentAsync_ProductionPorts_ShouldAddOffset()
    {
        // Arrange
        var envFile = Path.Combine(_tempDir, ".env");
        await File.WriteAllTextAsync(envFile, "DEV_PORT=5000\nDEBUG_PORT=5050\n");

        // Act - Production environment has offset of 2000
        var result = await _service.UpdateEnvFileEnvironmentAsync(envFile, EnvironmentType.Production);

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(envFile);
        content.Should().Contain("DEV_PORT=7000");    // 5000 + 2000
        content.Should().Contain("DEBUG_PORT=7050");  // 5050 + 2000
    }

    [Fact]
    public async Task UpdateEnvFileEnvironmentAsync_ShouldUncommentEnvironmentVariables()
    {
        // Arrange
        var envFile = Path.Combine(_tempDir, ".env");
        await File.WriteAllTextAsync(envFile, "#DOTNET_ENVIRONMENT=Development\n#ASPNETCORE_ENVIRONMENT=Development\n");

        // Act
        var result = await _service.UpdateEnvFileEnvironmentAsync(envFile, EnvironmentType.Test);

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(envFile);
        content.Should().Contain("DOTNET_ENVIRONMENT=Test");
        content.Should().Contain("ASPNETCORE_ENVIRONMENT=Test");
    }

    #endregion

    #region CalculateEnvironmentPort

    [Theory]
    [InlineData(5000, EnvironmentType.Development, 5000)]
    [InlineData(5000, EnvironmentType.Test, 6000)]
    [InlineData(5000, EnvironmentType.Production, 7000)]
    [InlineData(8080, EnvironmentType.Test, 9080)]
    [InlineData(8080, EnvironmentType.Production, 10080)]
    public void CalculateEnvironmentPort_ShouldReturnCorrectPort(int basePort, EnvironmentType env, int expected)
    {
        var result = _service.CalculateEnvironmentPort(basePort, env);

        result.Should().Be(expected);
    }

    #endregion
}
