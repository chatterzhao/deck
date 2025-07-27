using Deck.Console.Commands;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Deck.Console.Tests.Commands;

/// <summary>
/// ImagesCommand单元测试
/// </summary>
public class ImagesCommandTests
{
    private readonly Mock<IConsoleDisplay> _mockConsoleDisplay;
    private readonly Mock<IImagesUnifiedService> _mockImagesUnifiedService;
    private readonly Mock<IInteractiveSelectionService> _mockInteractiveSelection;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<ILogger<ImagesCommand>> _mockLogger;
    private readonly ImagesCommand _imagesCommand;

    public ImagesCommandTests()
    {
        _mockConsoleDisplay = new Mock<IConsoleDisplay>();
        _mockImagesUnifiedService = new Mock<IImagesUnifiedService>();
        _mockInteractiveSelection = new Mock<IInteractiveSelectionService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockLogger = new Mock<ILogger<ImagesCommand>>();

        _mockLoggingService
            .Setup(x => x.GetLogger<ImagesCommand>())
            .Returns(_mockLogger.Object);

        _imagesCommand = new ImagesCommand(
            _mockConsoleDisplay.Object,
            _mockImagesUnifiedService.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object);
    }

    #region ExecuteListAsync Tests

    [Fact]
    public async Task ExecuteListAsync_ShouldReturnTrue_WhenResourcesExist()
    {
        // Arrange
        var resourceList = CreateTestResourceList();
        _mockImagesUnifiedService
            .Setup(x => x.GetUnifiedResourceListAsync(null))
            .ReturnsAsync(resourceList);

        // Act
        var result = await _imagesCommand.ExecuteListAsync();

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowStatusMessage("📋 正在加载三层统一镜像列表..."), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo("🏗️  Deck 三层统一镜像管理"), Times.Once);
        _mockImagesUnifiedService.Verify(x => x.GetUnifiedResourceListAsync(null), Times.Once);
    }

    [Fact]
    public async Task ExecuteListAsync_ShouldShowWarning_WhenNoResourcesExist()
    {
        // Arrange
        var emptyResourceList = new UnifiedResourceList
        {
            Images = new List<UnifiedResource>(),
            Custom = new List<UnifiedResource>(),
            Templates = new List<UnifiedResource>()
        };

        _mockImagesUnifiedService
            .Setup(x => x.GetUnifiedResourceListAsync(null))
            .ReturnsAsync(emptyResourceList);

        // Act
        var result = await _imagesCommand.ExecuteListAsync();

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowWarning("未找到任何镜像资源"), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo("使用 'deck start <env-type>' 创建第一个镜像"), Times.Once);
    }

    [Fact]
    public async Task ExecuteListAsync_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        _mockImagesUnifiedService
            .Setup(x => x.GetUnifiedResourceListAsync(null))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _imagesCommand.ExecuteListAsync();

        // Assert
        result.Should().BeFalse();
        _mockConsoleDisplay.Verify(x => x.ShowError("列表显示失败: Service error"), Times.Once);
    }

    #endregion

    #region ExecuteCleanAsync Tests

    [Fact]
    public async Task ExecuteCleanAsync_ShouldReturnTrue_WhenCleaningSucceeds()
    {
        // Arrange
        const int keepCount = 3;
        var cleaningOptions = CreateTestCleaningOptions();
        var cleaningResult = new CleaningResult
        {
            IsSuccess = true,
            CleanedCount = 2,
            CleanedResources = new List<string> { "image1", "image2" }
        };

        _mockImagesUnifiedService
            .Setup(x => x.GetCleaningOptionsAsync())
            .ReturnsAsync(cleaningOptions);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(
                It.IsAny<string>(),
                It.IsAny<List<SelectableItem<CleaningOption>>>(),
                true))
            .ReturnsAsync(new SelectableItem<CleaningOption> { Item = cleaningOptions[0] });

        _mockImagesUnifiedService
            .Setup(x => x.ExecuteCleaningAsync(
                It.IsAny<CleaningOption>(),
                It.IsAny<Func<string, Task<bool>>?>()))
            .ReturnsAsync(cleaningResult);

        // Act
        var result = await _imagesCommand.ExecuteCleanAsync(keepCount);

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowStatusMessage($"🧹 正在分析镜像清理策略 (保留: {keepCount} 个)..."), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowSuccess("清理完成: 删除了 2 个资源"), Times.Once);
    }

    [Fact]
    public async Task ExecuteCleanAsync_ShouldReturnTrue_WhenNoCleaningOptionsExist()
    {
        // Arrange
        _mockImagesUnifiedService
            .Setup(x => x.GetCleaningOptionsAsync())
            .ReturnsAsync(new List<CleaningOption>());

        // Act
        var result = await _imagesCommand.ExecuteCleanAsync();

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowInfo("没有需要清理的资源"), Times.Once);
    }

    [Fact]
    public async Task ExecuteCleanAsync_ShouldReturnTrue_WhenUserCancelsSelection()
    {
        // Arrange
        var cleaningOptions = CreateTestCleaningOptions();
        
        _mockImagesUnifiedService
            .Setup(x => x.GetCleaningOptionsAsync())
            .ReturnsAsync(cleaningOptions);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(
                It.IsAny<string>(),
                It.IsAny<List<SelectableItem<CleaningOption>>>(),
                true))
            .ReturnsAsync((SelectableItem<CleaningOption>?)null);

        // Act
        var result = await _imagesCommand.ExecuteCleanAsync();

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowInfo("已取消清理操作"), Times.Once);
    }

    [Fact]
    public async Task ExecuteCleanAsync_ShouldReturnFalse_WhenCleaningFails()
    {
        // Arrange
        var cleaningOptions = CreateTestCleaningOptions();
        var cleaningResult = new CleaningResult
        {
            IsSuccess = false,
            ErrorMessage = "Cleaning failed"
        };

        _mockImagesUnifiedService
            .Setup(x => x.GetCleaningOptionsAsync())
            .ReturnsAsync(cleaningOptions);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(
                It.IsAny<string>(),
                It.IsAny<List<SelectableItem<CleaningOption>>>(),
                true))
            .ReturnsAsync(new SelectableItem<CleaningOption> { Item = cleaningOptions[0] });

        _mockImagesUnifiedService
            .Setup(x => x.ExecuteCleaningAsync(
                It.IsAny<CleaningOption>(),
                It.IsAny<Func<string, Task<bool>>?>()))
            .ReturnsAsync(cleaningResult);

        // Act
        var result = await _imagesCommand.ExecuteCleanAsync();

        // Assert
        result.Should().BeFalse();
        _mockConsoleDisplay.Verify(x => x.ShowError("清理失败: Cleaning failed"), Times.Once);
    }

    #endregion

    #region ExecuteInfoAsync Tests

    [Fact]
    public async Task ExecuteInfoAsync_ShouldReturnTrue_WhenImageNameProvided()
    {
        // Arrange
        const string imageName = "test-image-20241215-1430";
        var resourceDetail = CreateTestResourceDetail();

        _mockImagesUnifiedService
            .Setup(x => x.GetResourceDetailAsync(UnifiedResourceType.Images, imageName))
            .ReturnsAsync(resourceDetail);

        // Act
        var result = await _imagesCommand.ExecuteInfoAsync(imageName);

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowStatusMessage($"ℹ️  正在获取镜像详细信息: {imageName}..."), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo($"📦 镜像详细信息: {imageName}"), Times.Once);
    }

    [Fact]
    public async Task ExecuteInfoAsync_ShouldReturnFalse_WhenImageNotFound()
    {
        // Arrange
        const string imageName = "non-existent-image";

        _mockImagesUnifiedService
            .Setup(x => x.GetResourceDetailAsync(UnifiedResourceType.Images, imageName))
            .ReturnsAsync((UnifiedResourceDetail?)null);

        // Act
        var result = await _imagesCommand.ExecuteInfoAsync(imageName);

        // Assert
        result.Should().BeFalse();
        _mockConsoleDisplay.Verify(x => x.ShowError($"未找到镜像: {imageName}"), Times.Once);
    }

    [Fact]
    public async Task ExecuteInfoAsync_ShouldUseInteractiveSelection_WhenImageNameNotProvided()
    {
        // Arrange
        const string selectedImage = "selected-image";
        var resourceList = CreateTestResourceList();
        var resourceDetail = CreateTestResourceDetail();

        _mockImagesUnifiedService
            .Setup(x => x.GetUnifiedResourceListAsync(null))
            .ReturnsAsync(resourceList);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(
                "选择镜像",
                It.IsAny<List<SelectableItem<string>>>(),
                true))
            .ReturnsAsync(new SelectableItem<string> { Item = selectedImage });

        _mockImagesUnifiedService
            .Setup(x => x.GetResourceDetailAsync(UnifiedResourceType.Images, selectedImage))
            .ReturnsAsync(resourceDetail);

        // Act
        var result = await _imagesCommand.ExecuteInfoAsync();

        // Assert
        result.Should().BeTrue();
        _mockImagesUnifiedService.Verify(x => x.GetUnifiedResourceListAsync(null), Times.Once);
        _mockInteractiveSelection.Verify(x => x.ShowSingleSelectionAsync(
            "选择镜像",
            It.IsAny<List<SelectableItem<string>>>(),
            true), Times.Once);
    }

    [Fact]
    public async Task ExecuteInfoAsync_ShouldReturnTrue_WhenUserCancelsInteractiveSelection()
    {
        // Arrange
        var resourceList = CreateTestResourceList();

        _mockImagesUnifiedService
            .Setup(x => x.GetUnifiedResourceListAsync(null))
            .ReturnsAsync(resourceList);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(
                "选择镜像",
                It.IsAny<List<SelectableItem<string>>>(),
                true))
            .ReturnsAsync((SelectableItem<string>?)null);

        // Act
        var result = await _imagesCommand.ExecuteInfoAsync();

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowInfo("已取消操作"), Times.Once);
    }

    #endregion

    #region ExecuteHelpAsync Tests

    [Fact]
    public async Task ExecuteHelpAsync_ShouldReturnTrue_WhenExecutedSuccessfully()
    {
        // Act
        var result = await _imagesCommand.ExecuteHelpAsync();

        // Assert
        result.Should().BeTrue();
        _mockConsoleDisplay.Verify(x => x.ShowInfo("🛡️  Deck 三层统一管理 - Images目录权限说明"), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo("📋 三层架构说明:"), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo("🔐 Images目录权限规则:"), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo("🔄 推荐工作流程:"), Times.Once);
        _mockConsoleDisplay.Verify(x => x.ShowInfo("💡 相关命令:"), Times.Once);
    }

    [Fact]
    public async Task ExecuteHelpAsync_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        _mockConsoleDisplay
            .Setup(x => x.ShowInfo(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Display error"));

        // Act
        var result = await _imagesCommand.ExecuteHelpAsync();

        // Assert
        result.Should().BeFalse();
        _mockConsoleDisplay.Verify(x => x.ShowError("帮助显示失败: Display error"), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static UnifiedResourceList CreateTestResourceList()
    {
        return new UnifiedResourceList
        {
            Images = new List<UnifiedResource>
            {
                new()
                {
                    Name = "test-app-20241215-1430",
                    Type = UnifiedResourceType.Images,
                    Status = ResourceStatus.Ready,
                    DisplayLabel = "启动镜像: test-app-20241215-1430 (2天前)",
                    RelativeTime = "2天前",
                    IsAvailable = true
                },
                new()
                {
                    Name = "another-app-20241214-1000",
                    Type = UnifiedResourceType.Images,
                    Status = ResourceStatus.Stopped,
                    DisplayLabel = "启动镜像: another-app-20241214-1000 (3天前)",
                    RelativeTime = "3天前",
                    IsAvailable = true
                }
            },
            Custom = new List<UnifiedResource>
            {
                new()
                {
                    Name = "my-custom-config",
                    Type = UnifiedResourceType.Custom,
                    Status = ResourceStatus.Ready,
                    DisplayLabel = "从配置构建: my-custom-config",
                    IsAvailable = true
                }
            },
            Templates = new List<UnifiedResource>
            {
                new()
                {
                    Name = "avalonia-default",
                    Type = UnifiedResourceType.Templates,
                    Status = ResourceStatus.Builtin,
                    DisplayLabel = "从模板创建: avalonia-default",
                    IsAvailable = true
                },
                new()
                {
                    Name = "flutter-default",
                    Type = UnifiedResourceType.Templates,
                    Status = ResourceStatus.Builtin,
                    DisplayLabel = "从模板创建: flutter-default",
                    IsAvailable = true
                }
            }
        };
    }

    private static List<CleaningOption> CreateTestCleaningOptions()
    {
        return new List<CleaningOption>
        {
            new()
            {
                Id = "images_keep_latest",
                DisplayName = "清理旧镜像（保留最新3个）",
                Description = "删除每个前缀下的旧镜像，每个前缀保留最新的3个镜像",
                ResourceType = UnifiedResourceType.Images,
                Strategy = CleaningStrategy.KeepLatestN,
                WarningLevel = ConfirmationLevel.Medium,
                EstimatedCount = 2,
                Parameters = new Dictionary<string, object> { ["keepCount"] = 3 }
            }
        };
    }

    private static UnifiedResourceDetail CreateTestResourceDetail()
    {
        return new UnifiedResourceDetail
        {
            Resource = new UnifiedResource
            {
                Name = "test-image-20241215-1430",
                Type = UnifiedResourceType.Images,
                Status = ResourceStatus.Ready,
                DisplayLabel = "启动镜像: test-image-20241215-1430 (2天前)",
                RelativeTime = "2天前",
                IsAvailable = true
            },
            ConfigurationStatus = new ConfigurationStatus
            {
                HasDockerfile = true,
                HasComposeYaml = true,
                HasEnvFile = true,
                MissingFiles = new List<string>()
            },
            FileSystemInfo = new ResourceFileSystemInfo
            {
                DirectoryPath = ".deck/images/test-image-20241215-1430",
                DirectorySize = "15.2 MB",
                CreatedAt = DateTime.Now.AddDays(-2),
                ModifiedAt = DateTime.Now.AddHours(-1)
            }
        };
    }

    #endregion
}