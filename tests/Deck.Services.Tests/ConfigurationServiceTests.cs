using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Deck.Services.Tests;

public class ConfigurationServiceTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IConfigurationMerger> _mockConfigMerger;
    private readonly ConfigurationService _configurationService;
    private readonly string _testDirectory;
    private readonly string _originalDirectory;

    public ConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockConfigMerger = new Mock<IConfigurationMerger>();
        _configurationService = new ConfigurationService(_mockLogger.Object, _mockConfigMerger.Object);
        
        // 创建临时测试目录
        _testDirectory = Path.Combine(Path.GetTempPath(), $"deck-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        // 切换到测试目录
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);
    }

    [Fact]
    public async Task GetConfigAsync_WhenConfigFileNotExists_ShouldCreateDefaultConfig()
    {
        // Act
        var config = await _configurationService.GetConfigAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://gitee.com/zhaoquan/deck.git", config.RemoteTemplates.Repository);
        Assert.Equal("main", config.RemoteTemplates.Branch);
        Assert.True(config.RemoteTemplates.AutoUpdate);
        
        // 验证配置文件已创建
        Assert.True(_configurationService.ConfigExists());
    }

    [Fact]
    public async Task CreateDefaultConfigAsync_ShouldReturnValidConfiguration()
    {
        // Act
        var config = await _configurationService.CreateDefaultConfigAsync();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.RemoteTemplates);
        Assert.Equal("https://gitee.com/zhaoquan/deck.git", config.RemoteTemplates.Repository);
        Assert.Equal("main", config.RemoteTemplates.Branch);
        Assert.Equal("24h", config.RemoteTemplates.CacheTtl);
        Assert.True(config.RemoteTemplates.AutoUpdate);
    }

    [Fact]
    public async Task ValidateConfigAsync_WithValidConfig_ShouldReturnValid()
    {
        // Arrange
        var config = await _configurationService.CreateDefaultConfigAsync();

        // Act
        var result = await _configurationService.ValidateConfigAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GetConfigAsync_AfterSaving_ShouldLoadCorrectly()
    {
        // Arrange - 先设置合并器的模拟行为
        _mockConfigMerger.Setup(m => m.Merge(It.IsAny<DeckConfig>(), It.IsAny<DeckConfig>()))
            .Returns((DeckConfig baseConfig, DeckConfig overrideConfig) => overrideConfig ?? baseConfig);

        var originalConfig = await _configurationService.CreateDefaultConfigAsync();
        originalConfig.RemoteTemplates.Repository = "https://custom.repo.com/templates.git";
        originalConfig.RemoteTemplates.Branch = "develop";
        originalConfig.RemoteTemplates.AutoUpdate = false;
        
        await _configurationService.SaveConfigAsync(originalConfig);

        // Act
        var loadedConfig = await _configurationService.GetConfigAsync();

        // Assert
        Assert.Equal("https://custom.repo.com/templates.git", loadedConfig.RemoteTemplates.Repository);
        Assert.Equal("develop", loadedConfig.RemoteTemplates.Branch);
        Assert.False(loadedConfig.RemoteTemplates.AutoUpdate);
    }

    [Fact]
    public void GetConfigFilePath_ShouldReturnCorrectPath()
    {
        // Act
        var configPath = _configurationService.GetConfigFilePath();

        // Assert - 检查路径结构是否正确，避免符号链接路径差异问题
        Assert.EndsWith(Path.Combine(".deck", "config.json"), configPath);
        Assert.True(Path.IsPathRooted(configPath));
    }

    [Fact]
    public void ConfigExists_WhenFileNotExists_ShouldReturnFalse()
    {
        // Act & Assert
        Assert.False(_configurationService.ConfigExists());
    }

    [Fact]
    public async Task ConfigExists_WhenFileExists_ShouldReturnTrue()
    {
        // Arrange
        var config = await _configurationService.CreateDefaultConfigAsync();
        await _configurationService.SaveConfigAsync(config);

        // Act & Assert
        Assert.True(_configurationService.ConfigExists());
    }

    public void Dispose()
    {
        // 恢复原目录
        Directory.SetCurrentDirectory(_originalDirectory);
        
        // 清理测试目录
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}