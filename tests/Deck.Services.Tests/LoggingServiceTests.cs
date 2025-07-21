using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Deck.Services.Tests;

/// <summary>
/// 日志服务测试
/// </summary>
public class LoggingServiceTests : IDisposable
{
    private readonly LoggingService _loggingService;

    public LoggingServiceTests()
    {
        _loggingService = (LoggingService)LoggingService.CreateDefault();
    }

    [Fact]
    public void CreateDefault_ShouldReturnValidLoggingService()
    {
        // Act & Assert
        _loggingService.Should().NotBeNull();
        
        var config = _loggingService.GetCurrentConfiguration();
        config.Should().NotBeNull();
        config.DefaultLevel.Should().Be(LogLevel.Information);
        config.EnableConsoleColors.Should().BeTrue();
        config.EnableTimestamps.Should().BeTrue();
    }

    [Fact]
    public void GetLogger_Generic_ShouldReturnTypedLogger()
    {
        // Act
        var logger = _loggingService.GetLogger<LoggingServiceTests>();
        
        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<ILogger<LoggingServiceTests>>();
    }

    [Fact]
    public void GetLogger_String_ShouldReturnNamedLogger()
    {
        // Act
        var logger = _loggingService.GetLogger("TestCategory");
        
        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void CreateVerbose_ShouldReturnVerboseLoggingService()
    {
        // Arrange & Act
        using var verboseService = (LoggingService)LoggingService.CreateVerbose();
        
        // Assert
        var config = verboseService.GetCurrentConfiguration();
        config.DefaultLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void SetLogLevel_ShouldUpdateConfiguration()
    {
        // Arrange
        var categoryName = "TestCategory";
        var logLevel = LogLevel.Warning;
        
        // Act
        _loggingService.SetLogLevel(categoryName, logLevel);
        
        // Assert
        var config = _loggingService.GetCurrentConfiguration();
        config.CategoryLevels.Should().ContainKey(categoryName);
        config.CategoryLevels[categoryName].Should().Be(logLevel);
    }

    [Fact]
    public void EnableConsoleColors_ShouldUpdateConfiguration()
    {
        // Act
        _loggingService.EnableConsoleColors(false);
        
        // Assert
        var config = _loggingService.GetCurrentConfiguration();
        config.EnableConsoleColors.Should().BeFalse();
    }

    public void Dispose()
    {
        _loggingService?.Dispose();
    }
}