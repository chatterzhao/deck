using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Deck.Services.Tests;

public class ConfigurationServiceTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly ConfigurationService _configurationService;
    private readonly string _testDirectory;
    private readonly string _originalDirectory;

    public ConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _configurationService = new ConfigurationService(_mockLogger.Object);
        
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
        Assert.Equal("https://github.com/chatterzhao/deck-templates.git", config.Templates.Repository.Url);
        Assert.Equal("podman", config.Container.Engine);
        Assert.True(config.UI.ShowPodmanCommands);
        
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
        
        // 验证模板配置
        Assert.NotNull(config.Templates);
        Assert.Equal("https://github.com/chatterzhao/deck-templates.git", config.Templates.Repository.Url);
        Assert.Equal("main", config.Templates.Repository.Branch);
        Assert.True(config.Templates.AutoUpdate);
        
        // 验证容器配置
        Assert.NotNull(config.Container);
        Assert.Equal("podman", config.Container.Engine);
        Assert.Equal("docker", config.Container.FallbackEngine);
        
        // 验证UI配置
        Assert.NotNull(config.UI);
        Assert.Equal("zh-CN", config.UI.Language);
        Assert.True(config.UI.ShowPodmanCommands);
        
        // 验证项目配置
        Assert.NotNull(config.Project);
        Assert.False(string.IsNullOrEmpty(config.Project.Name));
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
    public async Task ValidateConfigAsync_WithInvalidRepositoryUrl_ShouldReturnInvalid()
    {
        // Arrange
        var config = await _configurationService.CreateDefaultConfigAsync();
        config.Templates.Repository.Url = "invalid-url";

        // Act
        var result = await _configurationService.ValidateConfigAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("无效的模板仓库URL"));
    }

    [Fact]
    public async Task ValidateConfigAsync_WithInvalidEngine_ShouldReturnInvalid()
    {
        // Arrange
        var config = await _configurationService.CreateDefaultConfigAsync();
        config.Container.Engine = "invalid-engine";

        // Act
        var result = await _configurationService.ValidateConfigAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("不支持的容器引擎"));
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldCreateConfigFile()
    {
        // Arrange
        var config = await _configurationService.CreateDefaultConfigAsync();

        // Act
        await _configurationService.SaveConfigAsync(config);

        // Assert
        var configPath = _configurationService.GetConfigFilePath();
        Assert.True(File.Exists(configPath));
        
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("templates:", content);
        Assert.Contains("repository:", content);
        Assert.Contains("url:", content);
        Assert.Contains("github.com/chatterzhao/deck-templates.git", content);
    }

    [Fact]
    public async Task GetConfigAsync_AfterSaving_ShouldLoadCorrectly()
    {
        // Arrange
        var originalConfig = await _configurationService.CreateDefaultConfigAsync();
        originalConfig.Templates.Repository.Url = "https://custom.repo.com/templates.git";
        originalConfig.Container.Engine = "docker";
        
        await _configurationService.SaveConfigAsync(originalConfig);

        // Act
        var loadedConfig = await _configurationService.GetConfigAsync();

        // Assert
        Assert.Equal("https://custom.repo.com/templates.git", loadedConfig.Templates.Repository.Url);
        Assert.Equal("docker", loadedConfig.Container.Engine);
    }

    [Fact]
    public void GetConfigFilePath_ShouldReturnCorrectPath()
    {
        // Act
        var configPath = _configurationService.GetConfigFilePath();

        // Assert - 检查路径结构是否正确，避免符号链接路径差异问题
        Assert.EndsWith(Path.Combine(".deck", "config.yaml"), configPath);
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