using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Deck.Services.Tests;

/// <summary>
/// ThreeLayerWorkflowService 完整实现的单元测试
/// 重点测试本次修改：BuildAndStartContainerAsync 委托给 IStartCommandService
/// </summary>
public class ThreeLayerWorkflowServiceTests
{
    private readonly Mock<ILogger<ThreeLayerWorkflowService>> _loggerMock;
    private readonly Mock<IDirectoryManagementService> _directoryMock;
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<IInteractiveSelectionService> _interactiveMock;
    private readonly Mock<IContainerService> _containerMock;
    private readonly Mock<IConfigurationService> _configurationMock;
    private readonly Mock<IStartCommandService> _startCommandMock;
    private readonly ThreeLayerWorkflowService _service;

    public ThreeLayerWorkflowServiceTests()
    {
        _loggerMock = new Mock<ILogger<ThreeLayerWorkflowService>>();
        _directoryMock = new Mock<IDirectoryManagementService>();
        _fileSystemMock = new Mock<IFileSystemService>();
        _interactiveMock = new Mock<IInteractiveSelectionService>();
        _containerMock = new Mock<IContainerService>();
        _configurationMock = new Mock<IConfigurationService>();
        _startCommandMock = new Mock<IStartCommandService>();

        _service = new ThreeLayerWorkflowService(
            _loggerMock.Object,
            _directoryMock.Object,
            _fileSystemMock.Object,
            _interactiveMock.Object,
            _containerMock.Object,
            _configurationMock.Object,
            _startCommandMock.Object
        );
    }

    #region BuildAndStartContainerAsync - 委托逻辑

    [Fact]
    public async Task BuildAndStartContainer_ShouldDelegateToStartCommandService()
    {
        // Arrange - 通过 ExecuteImagesWorkflow 触发 BuildAndStartContainerAsync
        var imageName = "my-app-dev";
        var imageDir = "/path/to/images/my-app-dev";

        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns(imageDir);
        _fileSystemMock.Setup(f => f.DirectoryExists(imageDir)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        // 容器不存在
        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.NotExists });

        // StartCommandService 返回成功
        var expectedResult = StartCommandResult.Success(imageName, $"deck-{imageName}");
        _startCommandMock.Setup(s => s.StartFromImageAsync(imageName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Action.Should().Be(ContainerAction.BuildAndStart);

        // 验证委托调用
        _startCommandMock.Verify(s => s.StartFromImageAsync(imageName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildAndStartContainer_WhenStartCommandFails_ShouldReturnFailure()
    {
        // Arrange
        var imageName = "my-app-test";
        var imageDir = "/path/to/images/my-app-test";

        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns(imageDir);
        _fileSystemMock.Setup(f => f.DirectoryExists(imageDir)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.NotExists });

        // StartCommandService 返回失败
        var failedResult = StartCommandResult.Failure("compose up failed");
        _startCommandMock.Setup(s => s.StartFromImageAsync(imageName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("容器启动失败"));
    }

    [Fact]
    public async Task BuildAndStartContainer_MissingComposeFile_ShouldReturnError()
    {
        // Arrange
        var imageName = "my-app-dev";
        var imageDir = "/path/to/images/my-app-dev";

        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns(imageDir);
        _fileSystemMock.Setup(f => f.DirectoryExists(imageDir)).Returns(true);

        // compose.yaml 不存在
        _fileSystemMock.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("compose.yaml"))))
            .Returns(false);

        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.NotExists });

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("缺少 compose.yaml"));

        // 不应调用 StartCommandService
        _startCommandMock.Verify(s => s.StartFromImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuildAndStartContainer_ShouldUseContainerNameFromStartResult()
    {
        // Arrange
        var imageName = "my-app-prod";
        var imageDir = "/path/to/images/my-app-prod";
        var expectedContainerName = "deck-my-app-prod-final";

        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns(imageDir);
        _fileSystemMock.Setup(f => f.DirectoryExists(imageDir)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.NotExists });

        var startResult = StartCommandResult.Success(imageName, expectedContainerName);
        _startCommandMock.Setup(s => s.StartFromImageAsync(imageName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(startResult);

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Success.Should().BeTrue();
        result.ContainerName.Should().Be(expectedContainerName);
    }

    [Fact]
    public async Task BuildAndStartContainer_WhenStartCommandThrows_ShouldHandleException()
    {
        // Arrange
        var imageName = "my-app-dev";
        var imageDir = "/path/to/images/my-app-dev";

        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns(imageDir);
        _fileSystemMock.Setup(f => f.DirectoryExists(imageDir)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.NotExists });

        _startCommandMock.Setup(s => s.StartFromImageAsync(imageName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Docker not available"));

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Success.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("构建失败"));
    }

    #endregion

    #region ImagesWorkflow - 容器已存在的场景

    [Fact]
    public async Task ExecuteImagesWorkflow_ContainerRunning_ShouldEnterContainer()
    {
        // Arrange
        var imageName = "running-app-dev";
        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns("/path/to/images/running-app-dev");
        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.Running });

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Success.Should().BeTrue();
        result.Action.Should().Be(ContainerAction.Enter);
        // 不应触发构建
        _startCommandMock.Verify(s => s.StartFromImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteImagesWorkflow_ContainerStopped_ShouldRestart()
    {
        // Arrange
        var imageName = "stopped-app-dev";
        _directoryMock.Setup(d => d.GetImageDirectory(imageName)).Returns("/path/to/images/stopped-app-dev");
        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        _containerMock.Setup(c => c.DetectContainerStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContainerStatusResult { Status = ContainerStatus.Stopped });

        _containerMock.Setup(c => c.StartContainerAsync(It.IsAny<string>(), It.IsAny<StartOptions>()))
            .ReturnsAsync(new StartContainerResult { Success = true });

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Success.Should().BeTrue();
        result.Action.Should().Be(ContainerAction.Restart);
        // 不应触发构建
        _startCommandMock.Verify(s => s.StartFromImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ValidateConfigurationState

    [Fact]
    public async Task ValidateConfigurationState_DirectoryNotFound_ShouldReturnNotFound()
    {
        var result = await _service.ValidateConfigurationStateAsync("/nonexistent/path");

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(ConfigValidationStatus.NotFound);
    }

    [Fact]
    public async Task ValidateConfigurationState_Complete_ShouldReturnComplete()
    {
        var configPath = "/path/to/complete";

        _fileSystemMock.Setup(f => f.DirectoryExists(configPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var result = await _service.ValidateConfigurationStateAsync(configPath);

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(ConfigValidationStatus.Complete);
        result.MissingFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateConfigurationState_MissingFiles_ShouldReturnIncomplete()
    {
        var configPath = "/path/to/incomplete";

        _fileSystemMock.Setup(f => f.DirectoryExists(configPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".env")))).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("compose.yaml")))).Returns(false);
        _fileSystemMock.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Dockerfile")))).Returns(false);

        var result = await _service.ValidateConfigurationStateAsync(configPath);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(ConfigValidationStatus.Incomplete);
        result.MissingFiles.Should().HaveCount(2);
        result.MissingFiles.Should().Contain("compose.yaml");
        result.MissingFiles.Should().Contain("Dockerfile");
    }

    #endregion

    #region GenerateConfigurationChain

    [Theory]
    [InlineData("tpl", "cfg", "img", 3)]
    [InlineData("tpl", null, null, 1)]
    [InlineData(null, "cfg", "img", 2)]
    [InlineData(null, null, null, 0)]
    public async Task GenerateConfigurationChain_ShouldReturnCorrectChain(string? tpl, string? cfg, string? img, int expectedCount)
    {
        var result = await _service.GenerateConfigurationChainAsync(tpl, cfg, img);

        result.Should().HaveCount(expectedCount);
    }

    #endregion

    #region Metadata

    [Fact]
    public async Task UpdateImageMetadata_ShouldWriteFile()
    {
        var imageDir = "/path/to/image";
        var metadata = new ImageMetadata
        {
            ImageName = "test-image",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tester",
            SourceConfig = "/path/to/source",
            BuildStatus = BuildStatus.Built
        };

        await _service.UpdateImageMetadataAsync(imageDir, metadata);

        _fileSystemMock.Verify(f => f.WriteTextFileAsync(
            Path.Combine(imageDir, ".deck-metadata"),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task ReadImageMetadata_FileNotFound_ShouldReturnNull()
    {
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await _service.ReadImageMetadataAsync("/path/to/image");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadImageMetadata_ValidFile_ShouldParseCorrectly()
    {
        var imageDir = "/path/to/image";
        var content = """
            IMAGE_NAME=test-image
            CREATED_AT=2024-10-22T15:00:00Z
            CREATED_BY=tester
            SOURCE_CONFIG=/path/to/source
            BUILD_STATUS=Built
            LAST_STARTED=2024-10-22T16:00:00Z
            """;

        _fileSystemMock.Setup(f => f.FileExists(Path.Combine(imageDir, ".deck-metadata"))).Returns(true);
        _fileSystemMock.Setup(f => f.ReadTextFileAsync(Path.Combine(imageDir, ".deck-metadata")))
            .ReturnsAsync(content);

        var result = await _service.ReadImageMetadataAsync(imageDir);

        result.Should().NotBeNull();
        result!.ImageName.Should().Be("test-image");
        result.CreatedBy.Should().Be("tester");
        result.BuildStatus.Should().Be(BuildStatus.Built);
        result.LastStarted.Should().NotBeNull();
    }

    #endregion
}
