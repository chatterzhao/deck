using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Deck.Services.Tests;

[Collection("ConfigurationServiceTests")]
[CollectionDefinition("ConfigurationServiceTests", DisableParallelization = true)]
public class ConfigurationServiceTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IConfigurationMerger> _mockConfigMerger;
    private readonly ConfigurationService _configurationService;
    private readonly string _testDirectory;
    private readonly string _originalDirectory;
    private static readonly object _directoryLock = new object();

    public ConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockConfigMerger = new Mock<IConfigurationMerger>();
        
        // 创建唯一的临时测试目录，使用线程安全的方式
        lock (_directoryLock)
        {
            try
            {
                // 先确保有一个有效的工作目录作为起始点
                _originalDirectory = EnsureValidWorkingDirectory();
                
                _testDirectory = Path.Combine(Path.GetTempPath(), 
                    $"deck-test-{Environment.ProcessId}-{Thread.CurrentThread.ManagedThreadId}-{Guid.NewGuid():N}");
                
                // 确保目录不存在，避免冲突
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, recursive: true);
                    Thread.Sleep(50); // 等待文件系统操作完成
                }
                
                Directory.CreateDirectory(_testDirectory);
                
                // 切换到测试目录
                Directory.SetCurrentDirectory(_testDirectory);
                
                // 验证目录切换成功
                var currentDir = Directory.GetCurrentDirectory();
                if (!Directory.Exists(currentDir))
                {
                    throw new InvalidOperationException($"测试目录设置失败，当前目录不存在: {currentDir}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"初始化测试环境失败: {ex.Message}", ex);
            }
        }
        
        _configurationService = new ConfigurationService(_mockLogger.Object, _mockConfigMerger.Object);
    }

    private static string EnsureValidWorkingDirectory()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            if (Directory.Exists(currentDir))
            {
                return currentDir;
            }
        }
        catch (Exception)
        {
            // 当前目录无效，继续到备用方案
        }

        // 如果当前目录无效，使用项目根目录或临时目录作为备用
        var fallbackDirectories = new[]
        {
            // 尝试使用应用程序基目录
            AppContext.BaseDirectory,
            // 使用临时目录
            Path.GetTempPath(),
            // 使用用户目录
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        foreach (var dir in fallbackDirectories)
        {
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Directory.SetCurrentDirectory(dir);
                return dir;
            }
        }

        throw new InvalidOperationException("无法找到有效的工作目录");
    }

    [Fact]
    public async Task GetConfigAsync_WhenConfigFileNotExists_ShouldCreateDefaultConfig()
    {
        // Arrange - 确保配置文件不存在
        Assert.False(_configurationService.ConfigExists());

        // Act
        var config = await _configurationService.GetConfigAsync();

        // Assert - 验证返回的配置
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
        // 验证测试环境
        var currentDir = Directory.GetCurrentDirectory();
        Assert.True(Directory.Exists(currentDir), $"当前工作目录不存在: {currentDir}");
        
        // 确保测试目录中没有预存的配置文件
        var expectedConfigPath = Path.Combine(currentDir, ".deck", "config.json");
        if (File.Exists(expectedConfigPath))
        {
            File.Delete(expectedConfigPath);
        }
        var configDir = Path.GetDirectoryName(expectedConfigPath);
        if (Directory.Exists(configDir))
        {
            Directory.Delete(configDir, true);
        }
        
        // 使用真实的配置合并器替代Mock来避免潜在的缓存问题
        var realConfigMerger = new Deck.Services.ConfigurationMerger();
        var serviceWithRealMerger = new ConfigurationService(_mockLogger.Object, realConfigMerger);

        // 验证配置文件不存在
        Assert.False(serviceWithRealMerger.ConfigExists(), "测试开始前不应该有配置文件存在");

        var originalConfig = await serviceWithRealMerger.CreateDefaultConfigAsync();
        originalConfig.RemoteTemplates.Repository = "https://custom.repo.com/templates.git";
        originalConfig.RemoteTemplates.Branch = "develop";
        originalConfig.RemoteTemplates.AutoUpdate = false;
        
        // 保存配置并验证
        await serviceWithRealMerger.SaveConfigAsync(originalConfig);
        
        // 验证配置文件确实被保存了 - 添加详细诊断
        var configPath = serviceWithRealMerger.GetConfigFilePath();
        var configExists = serviceWithRealMerger.ConfigExists();
        var fileExistsDirectly = File.Exists(configPath);
        
        if (!configExists)
        {
            var configDirectory = Path.GetDirectoryName(configPath);
            var workingDir = Directory.GetCurrentDirectory();
            var configDirExists = configDirectory != null && Directory.Exists(configDirectory);
            var workingDirExists = Directory.Exists(workingDir);
            var configDirFiles = configDirExists && configDirectory != null ? 
                string.Join(", ", Directory.GetFiles(configDirectory)) : "N/A";
            
            throw new InvalidOperationException($"配置文件保存失败:\n" +
                $"  配置文件路径: {configPath}\n" +
                $"  配置目录: {configDirectory}\n" +
                $"  配置目录存在: {configDirExists}\n" +
                $"  工作目录: {workingDir}\n" +
                $"  工作目录存在: {workingDirExists}\n" +
                $"  File.Exists直接检查: {fileExistsDirectly}\n" +
                $"  配置目录中的文件: {configDirFiles}");
        }
        
        // 验证保存的文件内容
        var savedContent = await File.ReadAllTextAsync(configPath);
        Assert.Contains("https://custom.repo.com/templates.git", savedContent);
        Assert.Contains("develop", savedContent);

        // Act - 重新加载配置
        var loadedConfig = await serviceWithRealMerger.GetConfigAsync();

        // Assert - 验证加载的配置与保存的配置一致
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
        lock (_directoryLock)
        {
            try
            {
                // 恢复原目录
                Directory.SetCurrentDirectory(_originalDirectory);
                
                // 健壮的清理测试目录
                CleanupTestDirectory();
            }
            catch (Exception ex)
            {
                // 记录清理失败，但不抛出异常
                System.Diagnostics.Debug.WriteLine($"测试清理失败: {ex.Message}");
            }
        }
    }

    private void CleanupTestDirectory()
    {
        if (!Directory.Exists(_testDirectory))
            return;

        const int maxRetries = 3;
        const int delayMs = 100;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // 设置目录及其内容为可删除
                SetDirectoryDeleteable(_testDirectory);
                
                Directory.Delete(_testDirectory, recursive: true);
                
                // 验证删除成功
                if (!Directory.Exists(_testDirectory))
                {
                    return;
                }
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
                continue;
            }
            catch (UnauthorizedAccessException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
                continue;
            }
        }
        
        // 如果无法删除，记录警告但不抛出异常
        System.Diagnostics.Debug.WriteLine($"无法删除测试目录: {_testDirectory}");
    }

    private static void SetDirectoryDeleteable(string directoryPath)
    {
        try
        {
            var directory = new DirectoryInfo(directoryPath);
            if (directory.Exists)
            {
                directory.Attributes = FileAttributes.Normal;
                
                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                }
                
                foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
                {
                    dir.Attributes = FileAttributes.Normal;
                }
            }
        }
        catch
        {
            // 忽略权限设置失败
        }
    }
}