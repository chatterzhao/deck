using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;

namespace Deck.Services.Tests;

public class EnhancedFileOperationsServiceTests : IDisposable
{
    private readonly IEnhancedFileOperationsService _service;
    private readonly ILogger<EnhancedFileOperationsService> _logger;
    private readonly IPortConflictService _portConflictService;
    private readonly string _testDirectory;

    public EnhancedFileOperationsServiceTests()
    {
        _logger = Substitute.For<ILogger<EnhancedFileOperationsService>>();
        _portConflictService = Substitute.For<IPortConflictService>();
        _service = new EnhancedFileOperationsService(_logger, _portConflictService);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"deck-test-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ProcessStandardPortsAsync_ShouldDetectAndAssignAvailablePorts()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var envFilePath = Path.Combine(_testDirectory, ".env");
        var initialContent = """
            # Development environment configuration
            DEV_PORT=5000
            DEBUG_PORT=9229
            WEB_PORT=8080
            PROJECT_NAME=test-project
            """;
        await File.WriteAllTextAsync(envFilePath, initialContent);

        var conflictResults = new List<PortCheckResult>
        {
            new() { Port = 5000, IsAvailable = false },
            new() { Port = 9229, IsAvailable = true },
            new() { Port = 8080, IsAvailable = true }
        };

        _portConflictService.CheckPortsAsync(Arg.Any<IEnumerable<int>>())
            .Returns(conflictResults);
        _portConflictService.FindAvailablePortAsync(5000, Arg.Any<int>(), Arg.Any<int>())
            .Returns(5001);

        // Act
        var result = await _service.ProcessStandardPortsAsync(envFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ModifiedPorts.Should().ContainKey("DEV_PORT");
        result.ModifiedPorts["DEV_PORT"].Should().Be(5001);

        var updatedContent = await File.ReadAllTextAsync(envFilePath);
        updatedContent.Should().Contain("DEV_PORT=5001");
        updatedContent.Should().Contain("DEBUG_PORT=9229");
        updatedContent.Should().Contain("WEB_PORT=8080");
    }

    [Fact]
    public async Task UpdateProjectNameAsync_ShouldGenerateUniqueProjectName()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var envFilePath = Path.Combine(_testDirectory, ".env");
        var initialContent = """
            DEV_PORT=5000
            PROJECT_NAME=old-name
            """;
        await File.WriteAllTextAsync(envFilePath, initialContent);

        var imageName = "test-avalonia";

        // Act
        var result = await _service.UpdateProjectNameAsync(envFilePath, imageName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.UpdatedProjectName.Should().Be(imageName);

        var updatedContent = await File.ReadAllTextAsync(envFilePath);
        updatedContent.Should().Contain($"PROJECT_NAME={imageName}");
        updatedContent.Should().NotContain("PROJECT_NAME=old-name");
    }

    [Fact]
    public async Task CopyWithHiddenFilesAsync_ShouldCopyAllFilesIncludingHidden()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDirectory, "source");
        var targetDir = Path.Combine(_testDirectory, "target");
        Directory.CreateDirectory(sourceDir);

        // 创建测试文件包括隐藏文件
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "compose.yaml"), "version: '3.8'");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "Dockerfile"), "FROM alpine");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, ".env"), "DEV_PORT=5000");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, ".gitignore"), "*.log");

        // Act
        var result = await _service.CopyWithHiddenFilesAsync(sourceDir, targetDir);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.CopiedFiles.Should().HaveCount(4);

        File.Exists(Path.Combine(targetDir, "compose.yaml")).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, "Dockerfile")).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, ".env")).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, ".gitignore")).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndBackupConfigAsync_ShouldCreateBackupBeforeModification()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var configFilePath = Path.Combine(_testDirectory, ".env");
        var originalContent = "DEV_PORT=5000\nPROJECT_NAME=test";
        await File.WriteAllTextAsync(configFilePath, originalContent);

        // Act
        var result = await _service.ValidateAndBackupConfigAsync(configFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.BackupFilePath.Should().NotBeNullOrEmpty();
        
        File.Exists(result.BackupFilePath).Should().BeTrue();
        var backupContent = await File.ReadAllTextAsync(result.BackupFilePath!);
        backupContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task RestoreConfigFromBackupAsync_ShouldRestoreOriginalContent()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var configFilePath = Path.Combine(_testDirectory, ".env");
        var originalContent = "DEV_PORT=5000\nPROJECT_NAME=test";
        await File.WriteAllTextAsync(configFilePath, originalContent);

        var backupResult = await _service.ValidateAndBackupConfigAsync(configFilePath);
        
        // 修改原文件
        await File.WriteAllTextAsync(configFilePath, "DEV_PORT=6000\nPROJECT_NAME=modified");

        // Act
        var restoreResult = await _service.RestoreConfigFromBackupAsync(backupResult.BackupFilePath!, configFilePath);

        // Assert
        restoreResult.Should().NotBeNull();
        restoreResult.IsSuccess.Should().BeTrue();

        var restoredContent = await File.ReadAllTextAsync(configFilePath);
        restoredContent.Should().Be(originalContent);
    }

    [Theory]
    [InlineData("DEV_PORT")]
    [InlineData("DEBUG_PORT")]
    [InlineData("WEB_PORT")]
    [InlineData("HTTPS_PORT")]
    [InlineData("ANDROID_DEBUG_PORT")]
    public async Task GetStandardPorts_ShouldReturnAllExpectedPortVariables(string expectedPortVar)
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var envFilePath = Path.Combine(_testDirectory, ".env");
        var envContent = """
            DEV_PORT=5000
            DEBUG_PORT=9229
            WEB_PORT=8080
            HTTPS_PORT=8443
            ANDROID_DEBUG_PORT=5037
            OTHER_VAR=value
            """;
        await File.WriteAllTextAsync(envFilePath, envContent);

        // Act
        var ports = await _service.GetStandardPortsAsync(envFilePath);

        // Assert
        ports.Should().ContainKey(expectedPortVar);
        ports[expectedPortVar].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessStandardPortsAsync_ShouldHandleMissingPortVariables()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var envFilePath = Path.Combine(_testDirectory, ".env");
        var initialContent = """
            # Only some ports defined
            DEV_PORT=5000
            PROJECT_NAME=test-project
            """;
        await File.WriteAllTextAsync(envFilePath, initialContent);

        _portConflictService.CheckPortsAsync(Arg.Any<IEnumerable<int>>())
            .Returns(new List<PortCheckResult>());

        // Act
        var result = await _service.ProcessStandardPortsAsync(envFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        var updatedContent = await File.ReadAllTextAsync(envFilePath);
        // 应该添加缺失的标准端口
        updatedContent.Should().Contain("DEBUG_PORT=9229");
        updatedContent.Should().Contain("WEB_PORT=8080");
        updatedContent.Should().Contain("HTTPS_PORT=8443");
        updatedContent.Should().Contain("ANDROID_DEBUG_PORT=5037");
    }

    [Fact]
    public async Task ValidateEnvFileFormat_ShouldDetectFormatErrors()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var envFilePath = Path.Combine(_testDirectory, ".env");
        var invalidContent = """
            DEV_PORT=5000
            INVALID_LINE_WITHOUT_EQUALS
            DEBUG_PORT=9229
            =VALUE_WITHOUT_KEY
            """;
        await File.WriteAllTextAsync(envFilePath, invalidContent);

        // Act
        var result = await _service.ValidateEnvFileFormatAsync(envFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
        result.Errors.Should().Contain(error => error.Contains("INVALID_LINE_WITHOUT_EQUALS"));
    }
}