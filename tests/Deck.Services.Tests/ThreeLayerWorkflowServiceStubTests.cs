using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Deck.Services.Tests;

public class ThreeLayerWorkflowServiceStubTests
{
    private readonly Mock<ILogger<ThreeLayerWorkflowServiceStub>> _loggerMock;
    private readonly Mock<IDirectoryManagementService> _directoryMock;
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<IInteractiveSelectionService> _interactiveMock;
    private readonly ThreeLayerWorkflowServiceStub _service;

    public ThreeLayerWorkflowServiceStubTests()
    {
        _loggerMock = new Mock<ILogger<ThreeLayerWorkflowServiceStub>>();
        _directoryMock = new Mock<IDirectoryManagementService>();
        _fileSystemMock = new Mock<IFileSystemService>();
        _interactiveMock = new Mock<IInteractiveSelectionService>();
        
        _service = new ThreeLayerWorkflowServiceStub(
            _loggerMock.Object,
            _directoryMock.Object,
            _fileSystemMock.Object,
            _interactiveMock.Object
        );
    }

    [Fact]
    public async Task ExecuteTemplateWorkflow_WithEditableFlow_ShouldCreateCustomConfigAndExit()
    {
        // Arrange
        var templateName = "tauri-default";
        var envType = "auto-detect";
        var expectedCustomName = $"{templateName}-001";
        
        _interactiveMock.Setup(x => x.ShowWorkflowSelectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowType.CreateEditableConfig);
            
        _directoryMock.Setup(x => x.GenerateUniqueCustomName(templateName))
            .Returns(expectedCustomName);
            
        _directoryMock.Setup(x => x.CreateCustomFromTemplateAsync(templateName, expectedCustomName, envType))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExecuteTemplateWorkflowAsync(templateName, envType);

        // Assert
        result.Should().NotBeNull();
        result.WorkflowType.Should().Be(WorkflowType.CreateEditableConfig);
        result.CustomConfigName.Should().Be(expectedCustomName);
        result.IsComplete.Should().BeFalse(); // 用户需要编辑后再运行
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTemplateWorkflow_WithDirectBuildFlow_ShouldCompleteFullPipeline()
    {
        // Arrange
        var templateName = "flutter-default";
        var envType = "development";
        var customName = $"{templateName}-temp-202410221500";
        var imageName = $"{customName}-build";
        
        _interactiveMock.Setup(x => x.ShowWorkflowSelectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowType.DirectBuildAndStart);
            
        _directoryMock.Setup(x => x.GenerateTimestampedName(It.IsAny<string>()))
            .Returns(customName);
            
        _directoryMock.Setup(x => x.GenerateImageName(customName))
            .Returns(imageName);
            
        _directoryMock.Setup(x => x.CreateCustomFromTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _directoryMock.Setup(x => x.GetCustomConfigPath(It.IsAny<string>()))
            .Returns("/path/to/custom");

        _directoryMock.Setup(x => x.CreateImageFromCustomAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/path/to/image");

        // Act
        var result = await _service.ExecuteTemplateWorkflowAsync(templateName, envType);

        // Assert
        result.Should().NotBeNull();
        result.WorkflowType.Should().Be(WorkflowType.DirectBuildAndStart);
        result.IsComplete.Should().BeTrue();
        result.Success.Should().BeTrue();
        result.ImageName.Should().Be(imageName);
    }

    [Fact]
    public async Task ExecuteCustomConfigWorkflow_ShouldBuildNewImageFromConfig()
    {
        // Arrange
        var configName = "my-tauri-app";
        var imageName = $"{configName}-20241022-1500";
        var customDir = $"/path/to/custom/{configName}";
        
        _directoryMock.Setup(x => x.GetCustomConfigPath(configName))
            .Returns(customDir);
            
        _fileSystemMock.Setup(x => x.DirectoryExists(customDir))
            .Returns(true);
            
        _directoryMock.Setup(x => x.GenerateTimestampedImageName(configName))
            .Returns(imageName);

        _directoryMock.Setup(x => x.CreateImageFromCustomAsync(imageName, customDir))
            .ReturnsAsync("/path/to/image");

        // 模拟配置验证成功
        _fileSystemMock.Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.ExecuteCustomConfigWorkflowAsync(configName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ImageName.Should().Be(imageName);
        result.ContainerName.Should().Be($"deck_{imageName}");
    }

    [Fact]
    public async Task ExecuteImagesWorkflow_ShouldReturnStubResult()
    {
        // Arrange
        var imageName = "my-app-20241022";

        // Act
        var result = await _service.ExecuteImagesWorkflowAsync(imageName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Action.Should().Be(ContainerAction.BuildAndStart);
        result.ImageName.Should().Be(imageName);
        result.ContainerName.Should().Be($"deck_{imageName}");
    }

    [Fact]
    public async Task ValidateConfigurationState_WithCompleteConfig_ShouldReturnValid()
    {
        // Arrange
        var configPath = "/path/to/config";
        
        _fileSystemMock.Setup(x => x.DirectoryExists(configPath))
            .Returns(true);
        _fileSystemMock.Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.ValidateConfigurationStateAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(ConfigValidationStatus.Complete);
        result.MissingFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateConfigurationState_WithMissingFiles_ShouldReturnInvalid()
    {
        // Arrange
        var configPath = "/path/to/incomplete-config";
        
        _fileSystemMock.Setup(x => x.DirectoryExists(configPath))
            .Returns(true);
            
        // .env 存在，但其他文件不存在
        _fileSystemMock.Setup(x => x.FileExists(It.Is<string>(p => p.EndsWith(".env"))))
            .Returns(true);
        _fileSystemMock.Setup(x => x.FileExists(It.Is<string>(p => p.EndsWith("compose.yaml"))))
            .Returns(false);
        _fileSystemMock.Setup(x => x.FileExists(It.Is<string>(p => p.EndsWith("Dockerfile"))))
            .Returns(false);

        // Act
        var result = await _service.ValidateConfigurationStateAsync(configPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(ConfigValidationStatus.Incomplete);
        result.MissingFiles.Should().Contain("compose.yaml");
        result.MissingFiles.Should().Contain("Dockerfile");
    }

    [Fact]
    public async Task GenerateConfigurationChain_ShouldCreateChain()
    {
        // Arrange
        var templateName = "template-1";
        var customName = "custom-1";
        var imageName = "image-1";

        // Act
        var result = await _service.GenerateConfigurationChainAsync(templateName, customName, imageName);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Should().Be($"📋 Templates: {templateName} → ");
        result[1].Should().Be($"🔧 Custom: {customName} → ");
        result[2].Should().Be($"📦 Images: {imageName}");
    }

    [Fact]
    public async Task UpdateImageMetadata_ShouldWriteMetadataFile()
    {
        // Arrange
        var imageDir = "/path/to/image";
        var metadata = new ImageMetadata
        {
            ImageName = "test-image",
            CreatedAt = new DateTime(2024, 10, 22, 15, 0, 0, DateTimeKind.Utc),
            CreatedBy = "testuser",
            SourceConfig = "/path/to/source",
            BuildStatus = BuildStatus.Prepared
        };

        // Act
        await _service.UpdateImageMetadataAsync(imageDir, metadata);

        // Assert
        _fileSystemMock.Verify(x => x.WriteTextFileAsync(
            Path.Combine(imageDir, ".deck-metadata"),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task ReadImageMetadata_WithExistingFile_ShouldReturnMetadata()
    {
        // Arrange
        var imageDir = "/path/to/image";
        var metadataContent = """
            IMAGE_NAME=test-image
            CREATED_AT=2024-10-22T15:00:00Z
            CREATED_BY=testuser
            SOURCE_CONFIG=/path/to/source
            BUILD_STATUS=Prepared
            LAST_STARTED=2024-10-22T16:00:00Z
            """;
        
        _fileSystemMock.Setup(x => x.FileExists(Path.Combine(imageDir, ".deck-metadata")))
            .Returns(true);
        _fileSystemMock.Setup(x => x.ReadTextFileAsync(Path.Combine(imageDir, ".deck-metadata")))
            .ReturnsAsync(metadataContent);

        // Act
        var result = await _service.ReadImageMetadataAsync(imageDir);

        // Assert
        result.Should().NotBeNull();
        result!.ImageName.Should().Be("test-image");
        result.CreatedBy.Should().Be("testuser");
        result.BuildStatus.Should().Be(BuildStatus.Prepared);
    }
}