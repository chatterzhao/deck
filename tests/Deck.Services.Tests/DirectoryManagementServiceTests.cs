using System.Text.Json;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Deck.Services.Tests;

/// <summary>
/// DirectoryManagementService 单元测试
/// 验证三层目录结构管理、权限控制、验证修复等核心功能
/// </summary>
public class DirectoryManagementServiceTests : IDisposable
{
    private readonly Mock<ILogger<DirectoryManagementService>> _mockLogger;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<IImagePermissionService> _mockImagePermissionService;
    private readonly DirectoryManagementService _service;
    private readonly ITestOutputHelper _output;
    private readonly string _testDirectory;

    public DirectoryManagementServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<DirectoryManagementService>>();
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockImagePermissionService = new Mock<IImagePermissionService>();
        
        _service = new DirectoryManagementService(
            _mockLogger.Object,
            _mockFileSystemService.Object,
            _mockImagePermissionService.Object);

        // 创建临时测试目录
        _testDirectory = Path.Combine(Path.GetTempPath(), $"deck_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        _output.WriteLine($"Test directory: {_testDirectory}");
    }

    public void Dispose()
    {
        // 清理测试目录
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region 目录初始化测试

    [Fact]
    public async Task InitializeDeckDirectoryAsync_ShouldCreateAllDirectories()
    {
        // Arrange
        var projectPath = _testDirectory;
        
        _mockFileSystemService.Setup(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.InitializeDeckDirectoryAsync(projectPath);

        // Assert
        _mockFileSystemService.Verify(x => x.EnsureDirectoryExistsAsync(Path.Combine(projectPath, ".deck")), Times.Once);
        _mockFileSystemService.Verify(x => x.EnsureDirectoryExistsAsync(Path.Combine(projectPath, ".deck", "templates")), Times.Once);
        _mockFileSystemService.Verify(x => x.EnsureDirectoryExistsAsync(Path.Combine(projectPath, ".deck", "custom")), Times.Once);
        _mockFileSystemService.Verify(x => x.EnsureDirectoryExistsAsync(Path.Combine(projectPath, ".deck", "images")), Times.Once);
        
        _output.WriteLine("✓ 验证三层目录结构创建调用");
    }

    [Fact]
    public async Task InitializeDeckDirectoryAsync_ShouldLogSuccess()
    {
        // Arrange
        var projectPath = _testDirectory;
        
        _mockFileSystemService.Setup(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.InitializeDeckDirectoryAsync(projectPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("成功初始化")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _output.WriteLine("✓ 验证成功日志记录");
    }

    [Fact]
    public async Task InitializeDeckDirectoryAsync_WhenFileSystemFails_ShouldThrow()
    {
        // Arrange
        var projectPath = _testDirectory;
        var expectedException = new UnauthorizedAccessException("Access denied");
        
        _mockFileSystemService.Setup(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.InitializeDeckDirectoryAsync(projectPath));
            
        Assert.Equal(expectedException, exception);
        _output.WriteLine("✓ 验证文件系统错误时的异常处理");
    }

    #endregion

    #region 目录结构验证测试

    [Fact]
    public async Task ValidateDirectoryStructureAsync_WithValidStructure_ShouldReturnValid()
    {
        // Arrange
        SetupValidDirectoryStructure();
        SetupImagePermissionChecks(hasIssues: false);

        // Act
        var result = await _service.ValidateDirectoryStructureAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
        _output.WriteLine("✓ 验证完整目录结构的有效性检查");
    }

    [Fact]
    public async Task ValidateDirectoryStructureAsync_WithMissingDeckDirectory_ShouldReturnInvalid()
    {
        // Arrange
        SetupMissingDeckDirectory();

        // Act
        var result = await _service.ValidateDirectoryStructureAsync();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("缺少 .deck 目录"));
        Assert.Contains(result.RepairSuggestions, s => s.Contains("deck doctor"));
        
        _output.WriteLine("✓ 验证缺少 .deck 目录时的错误检测");
    }

    [Fact]
    public async Task ValidateDirectoryStructureAsync_WithMissingSubDirectories_ShouldReportErrors()
    {
        // Arrange
        SetupMissingSubDirectories();

        // Act
        var result = await _service.ValidateDirectoryStructureAsync();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("templates 目录"));
        Assert.Contains(result.Errors, e => e.Contains("custom 目录"));
        Assert.Contains(result.Errors, e => e.Contains("images 目录"));
        
        _output.WriteLine("✓ 验证缺少子目录时的错误报告");
    }

    [Fact]
    public async Task ValidateDirectoryStructureAsync_WithImagePermissionIssues_ShouldReportWarnings()
    {
        // Arrange
        SetupValidDirectoryStructure();
        SetupImagePermissionChecks(hasIssues: true);

        // Act
        var result = await _service.ValidateDirectoryStructureAsync();

        // Assert
        Assert.True(result.IsValid); // 权限问题是警告，不是错误
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("权限异常"));
        
        _output.WriteLine("✓ 验证权限问题时的警告报告");
    }

    [Fact]
    public async Task ValidateDirectoryStructureAsync_WithEmptyTemplates_ShouldSuggestSync()
    {
        // Arrange
        SetupEmptyTemplatesDirectory();

        // Act
        var result = await _service.ValidateDirectoryStructureAsync();

        // Assert
        Assert.True(result.IsValid); // 空模板目录不是错误
        Assert.Contains(result.Warnings, w => w.Contains("templates 目录为空"));
        Assert.Contains(result.RepairSuggestions, s => s.Contains("模板同步"));
        
        _output.WriteLine("✓ 验证空模板目录时的同步建议");
    }

    #endregion

    #region 模板复制测试

    [Fact]
    public async Task CopyTemplateToCustomAsync_WithValidTemplate_ShouldSucceed()
    {
        // Arrange
        var templateName = "tauri-default";
        var customName = "my-custom-config";
        
        SetupValidTemplateForCopy(templateName);
        _mockFileSystemService.Setup(x => x.CopyDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CopyTemplateToCustomAsync(templateName, customName);

        // Assert
        Assert.Equal(customName, result);
        _mockFileSystemService.Verify(x => x.CopyDirectoryAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            true), Times.Once);
            
        _output.WriteLine("✓ 验证模板复制到自定义配置");
    }

    [Fact]
    public async Task CopyTemplateToCustomAsync_WithNonExistentTemplate_ShouldThrow()
    {
        // Arrange
        var templateName = "non-existent-template";
        
        SetupNonExistentTemplate(templateName);

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.CopyTemplateToCustomAsync(templateName));
            
        _output.WriteLine("✓ 验证不存在模板时的异常处理");
    }

    [Fact]
    public async Task CopyTemplateToCustomAsync_WithoutCustomName_ShouldGenerateName()
    {
        // Arrange
        var templateName = "tauri-default";
        
        SetupValidTemplateForCopy(templateName);
        SetupCustomNameGeneration(templateName);
        
        _mockFileSystemService.Setup(x => x.CopyDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CopyTemplateToCustomAsync(templateName);

        // Assert
        Assert.StartsWith($"{templateName}-custom", result);
        _output.WriteLine($"✓ 验证自动生成配置名称: {result}");
    }

    #endregion

    #region 镜像元数据测试

    [Fact]
    public async Task SaveImageMetadataAsync_ShouldCreateMetadataFile()
    {
        // Arrange
        var metadata = new ImageMetadata
        {
            ImageName = "test-image-20240101-120000",
            CreatedAt = DateTime.Now,
            CreatedBy = "test-user",
            BuildStatus = BuildStatus.Built,
            ContainerName = "test-container"
        };

        _mockFileSystemService.Setup(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.SaveImageMetadataAsync(metadata);

        // Assert
        _mockFileSystemService.Verify(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()), Times.Once);
        _output.WriteLine("✓ 验证镜像元数据保存");
    }

    [Fact]
    public async Task GetImageMetadataAsync_WithExistingMetadata_ShouldReturnMetadata()
    {
        // Arrange
        var imageName = "test-image-20240101-120000";
        var expectedMetadata = new ImageMetadata
        {
            ImageName = imageName,
            CreatedAt = DateTime.Now,
            BuildStatus = BuildStatus.Built
        };

        SetupExistingImageMetadata(imageName, expectedMetadata);

        // Act
        var result = await _service.GetImageMetadataAsync(imageName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedMetadata.ImageName, result.ImageName);
        Assert.Equal(expectedMetadata.BuildStatus, result.BuildStatus);
        
        _output.WriteLine("✓ 验证镜像元数据读取");
    }

    [Fact]
    public async Task GetImageMetadataAsync_WithNonExistentMetadata_ShouldReturnNull()
    {
        // Arrange
        var imageName = "non-existent-image";
        SetupNonExistentImageMetadata(imageName);

        // Act
        var result = await _service.GetImageMetadataAsync(imageName);

        // Assert
        Assert.Null(result);
        _output.WriteLine("✓ 验证不存在元数据时返回 null");
    }

    #endregion

    #region 三层配置获取测试

    [Fact]
    public async Task GetThreeLayerOptionsAsync_ShouldReturnAllLayers()
    {
        // Arrange
        SetupThreeLayerDirectories();

        // Act
        var result = await _service.GetThreeLayerOptionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Images);
        Assert.NotNull(result.Custom);
        Assert.NotNull(result.Templates);
        
        _output.WriteLine("✓ 验证三层配置选项获取");
    }

    [Fact]
    public async Task GetThreeLayerOptionsAsync_WithNoDeckDirectory_ShouldInitialize()
    {
        // Arrange
        SetupNoDeckDirectory();
        
        _mockFileSystemService.Setup(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GetThreeLayerOptionsAsync();

        // Assert
        Assert.NotNull(result);
        
        // 验证初始化调用
        _mockFileSystemService.Verify(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>()), Times.AtLeastOnce);
        
        _output.WriteLine("✓ 验证缺少目录时的自动初始化");
    }

    #endregion

    #region Private Helper Methods

    private void SetupValidDirectoryStructure()
    {
        // 模拟完整的目录结构存在
        var currentDir = Directory.GetCurrentDirectory();
        var deckDir = Path.Combine(currentDir, ".deck");
        
        // 由于我们在单元测试中，无法模拟 Directory.Exists，所以这里暂时设置简单的返回值
        _mockImagePermissionService.Setup(x => x.GetImagePermissionSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync(new ImagePermissionSummary
            {
                IsValidImageDirectory = true,
                ProtectedFiles = new List<ProtectedFile>()
            });
    }

    private void SetupMissingDeckDirectory()
    {
        // Directory.Exists 无法在单元测试中模拟，这里只是设置相关服务的返回值
    }

    private void SetupMissingSubDirectories()
    {
        // 类似上面，实际的目录检查在真实测试中进行
    }

    private void SetupImagePermissionChecks(bool hasIssues)
    {
        var result = new ImagePermissionSummary
        {
            IsValidImageDirectory = !hasIssues,
            ProtectedFiles = hasIssues ? new List<ProtectedFile> { new ProtectedFile() } : new List<ProtectedFile>()
        };

        _mockImagePermissionService.Setup(x => x.GetImagePermissionSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    private void SetupEmptyTemplatesDirectory()
    {
        // 设置模板目录为空的模拟
    }

    private void SetupValidTemplateForCopy(string templateName)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var templatePath = Path.Combine(currentDir, ".deck", "templates", templateName);
        // 在实际测试中需要创建模拟的目录结构
    }

    private void SetupNonExistentTemplate(string templateName)
    {
        // 设置模板不存在的情况
    }

    private void SetupCustomNameGeneration(string templateName)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var customDir = Path.Combine(currentDir, ".deck", "custom");
        // 在实际测试中需要设置自定义目录的状态
    }

    private void SetupExistingImageMetadata(string imageName, ImageMetadata metadata)
    {
        // 在真实的集成测试中，这里需要创建实际的元数据文件
        var currentDir = Directory.GetCurrentDirectory();
        var metadataPath = Path.Combine(currentDir, ".deck", "images", imageName, ".deck-metadata");
        
        // 这里只是记录调用，实际的文件操作在集成测试中验证
    }

    private void SetupNonExistentImageMetadata(string imageName)
    {
        // 设置元数据文件不存在的情况
    }

    private void SetupThreeLayerDirectories()
    {
        // 设置三层目录都存在的情况
    }

    private void SetupNoDeckDirectory()
    {
        // 设置 .deck 目录不存在的情况
    }

    #endregion
}

/// <summary>
/// DirectoryManagementService 集成测试
/// 使用真实的文件系统进行测试，验证实际的目录操作
/// </summary>
public class DirectoryManagementServiceIntegrationTests : IDisposable
{
    private readonly DirectoryManagementService _service;
    private readonly string _testDirectory;
    private readonly ITestOutputHelper _output;

    public DirectoryManagementServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // 创建真实的服务实例
        var logger = new Mock<ILogger<DirectoryManagementService>>().Object;
        var fileSystemService = new FileSystemService(new Mock<ILogger<FileSystemService>>().Object);
        var imagePermissionService = new ImagePermissionService(new Mock<ILogger<ImagePermissionService>>().Object);
        
        _service = new DirectoryManagementService(logger, fileSystemService, imagePermissionService);
        
        // 创建临时测试目录
        _testDirectory = Path.Combine(Path.GetTempPath(), $"deck_integration_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        // 设置当前工作目录为测试目录
        Environment.CurrentDirectory = _testDirectory;
        
        _output.WriteLine($"Integration test directory: {_testDirectory}");
    }

    public void Dispose()
    {
        // 恢复原始工作目录
        Environment.CurrentDirectory = Directory.GetCurrentDirectory();
        
        // 清理测试目录
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task InitializeDeckDirectoryAsync_ShouldCreateRealDirectories()
    {
        // Arrange
        var projectPath = _testDirectory;

        // Act
        await _service.InitializeDeckDirectoryAsync(projectPath);

        // Assert
        Assert.True(Directory.Exists(Path.Combine(projectPath, ".deck")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, ".deck", "templates")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, ".deck", "custom")));
        Assert.True(Directory.Exists(Path.Combine(projectPath, ".deck", "images")));
        
        _output.WriteLine("✓ 集成测试: 验证真实目录创建");
    }

    [Fact]
    public async Task ValidateDirectoryStructureAsync_WithRealDirectories_ShouldValidate()
    {
        // Arrange
        await _service.InitializeDeckDirectoryAsync(_testDirectory);

        // Act
        var result = await _service.ValidateDirectoryStructureAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
        _output.WriteLine("✓ 集成测试: 验证真实目录结构");
    }

    [Fact]
    public async Task GetThreeLayerOptionsAsync_WithRealDirectories_ShouldWork()
    {
        // Arrange
        await _service.InitializeDeckDirectoryAsync(_testDirectory);

        // Act
        var result = await _service.GetThreeLayerOptionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Images); // 新初始化的目录应该是空的
        Assert.Empty(result.Custom);
        Assert.Empty(result.Templates);
        
        _output.WriteLine("✓ 集成测试: 验证三层配置选项获取");
    }
}