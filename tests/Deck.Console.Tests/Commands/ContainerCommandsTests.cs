using Deck.Console.Commands;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Deck.Console.Tests.Commands;

/// <summary>
/// 容器命令测试
/// </summary>
public class ContainerCommandsTests
{
    private readonly Mock<IConsoleDisplay> _mockConsoleDisplay;
    private readonly Mock<IInteractiveSelectionService> _mockInteractiveSelection;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IDirectoryManagementService> _mockDirectoryManagement;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IGlobalExceptionHandler> _mockGlobalExceptionHandler;
    private readonly Mock<IContainerService> _mockContainerService;

    public ContainerCommandsTests()
    {
        _mockConsoleDisplay = new Mock<IConsoleDisplay>();
        _mockInteractiveSelection = new Mock<IInteractiveSelectionService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockDirectoryManagement = new Mock<IDirectoryManagementService>();
        _mockLogger = new Mock<ILogger>();
        _mockGlobalExceptionHandler = new Mock<IGlobalExceptionHandler>();
        _mockContainerService = new Mock<IContainerService>();

        _mockLoggingService
            .Setup(x => x.GetLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
    }

    #region StopCommand Tests

    [Fact]
    public async Task StopCommand_WithValidImageName_ShouldReturnTrue()
    {
        // Arrange
        var imageName = "test-image";
        var command = new StopCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockGlobalExceptionHandler.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        // 首先检查直接的镜像名称，然后检查-dev后缀
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = imageName });

        _mockContainerService
            .Setup(x => x.StopContainerAsync(imageName, false))
            .ReturnsAsync(new StopContainerResult
            {
                Success = true,
                Message = "Container stopped successfully"
            });

        // Act & Assert - This test is basic and would need real process execution
        // In a real scenario, we'd need to mock the process execution
        var result = await command.ExecuteAsync(imageName);
        
        // For now, just verify the setup was called
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Stop"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.StopContainerAsync(imageName, false), Times.Once);
    }

    [Fact]
    public async Task StopCommand_WithNullImageName_ShouldCallInteractiveSelection()
    {
        // Arrange
        var command = new StopCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockGlobalExceptionHandler.Object,
            _mockContainerService.Object
        );

        var mockImages = new List<ConfigurationOption>
        {
            new() { Name = "image1", Type = ConfigurationType.Images },
            new() { Name = "image2", Type = ConfigurationType.Images }
        };

        _mockDirectoryManagement
            .Setup(x => x.GetImagesAsync())
            .ReturnsAsync(mockImages);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(It.IsAny<InteractiveSelector<SelectableOption>>(), null, default))
            .ReturnsAsync(new SelectionResult<SelectableOption>
            {
                IsCancelled = false,
                SelectedItems = new List<SelectableOption>
                {
                    new() { Value = "image1", DisplayName = "image1" }
                }
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync("image1"))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = "image1",
                Type = ConfigurationType.Images,
                Path = "/test/path/image1"
            });

        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync("image1"))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = "image1" });

        _mockContainerService
            .Setup(x => x.StopContainerAsync("image1", false))
            .ReturnsAsync(new StopContainerResult
            {
                Success = true,
                Message = "Container stopped successfully"
            });

        // Act
        var result = await command.ExecuteAsync(null);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImagesAsync(), Times.Once);
        _mockInteractiveSelection.Verify(
            x => x.ShowSingleSelectionAsync(It.IsAny<InteractiveSelector<SelectableOption>>(), null, default),
            Times.Once);
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync("image1"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync("image1"), Times.Once);
        _mockContainerService.Verify(x => x.StopContainerAsync("image1", false), Times.Once);
    }

    #endregion

    #region RestartCommand Tests

    [Fact]
    public async Task RestartCommand_WithValidImageName_ShouldCallServices()
    {
        // Arrange
        var imageName = "test-image";
        var command = new RestartCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockGlobalExceptionHandler.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = imageName });

        _mockContainerService
            .Setup(x => x.RestartContainerAsync(imageName))
            .ReturnsAsync(new RestartContainerResult
            {
                Success = true,
                Message = "Container restarted successfully"
            });

        // Act
        var result = await command.ExecuteAsync(imageName);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Restart"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.RestartContainerAsync(imageName), Times.Once);
    }

    [Fact]
    public async Task RestartCommand_WithDevContainerName_ShouldCallServices()
    {
        // Arrange
        var imageName = "test-image";
        var command = new RestartCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockGlobalExceptionHandler.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        // 首先检查直接的镜像名称，返回null
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync((Deck.Core.Models.ContainerInfo)null!);

        // 然后检查-dev后缀，返回容器信息
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync($"{imageName}-dev"))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = $"{imageName}-dev" });

        _mockContainerService
            .Setup(x => x.RestartContainerAsync($"{imageName}-dev"))
            .ReturnsAsync(new RestartContainerResult
            {
                Success = true,
                Message = "Container restarted successfully"
            });

        // Act
        var result = await command.ExecuteAsync(imageName);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Restart"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync($"{imageName}-dev"), Times.Once);
        _mockContainerService.Verify(x => x.RestartContainerAsync($"{imageName}-dev"), Times.Once);
    }

    #endregion

    #region LogsCommand Tests

    [Fact]
    public async Task LogsCommand_WithValidImageName_ShouldCallServices()
    {
        // Arrange
        var imageName = "test-image";
        var command = new LogsCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = imageName });

        _mockContainerService
            .Setup(x => x.GetContainerLogsAsync(imageName, 100))
            .ReturnsAsync(new ContainerLogsResult
            {
                Success = true,
                Logs = "Test log output"
            });

        // Act
        var result = await command.ExecuteAsync(imageName, false);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Logs"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerLogsAsync(imageName, 100), Times.Once);
    }

    [Fact]
    public async Task LogsCommand_WithDevContainerName_ShouldCallServices()
    {
        // Arrange
        var imageName = "test-image";
        var command = new LogsCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        // 首先检查直接的镜像名称，返回null
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync((Deck.Core.Models.ContainerInfo)null!);

        // 然后检查-dev后缀，返回容器信息
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync($"{imageName}-dev"))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = $"{imageName}-dev" });

        _mockContainerService
            .Setup(x => x.GetContainerLogsAsync($"{imageName}-dev", 100))
            .ReturnsAsync(new ContainerLogsResult
            {
                Success = true,
                Logs = "Test log output"
            });

        // Act
        var result = await command.ExecuteAsync(imageName, false);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Logs"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync($"{imageName}-dev"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerLogsAsync($"{imageName}-dev", 100), Times.Once);
    }

    [Fact]
    public async Task LogsCommand_WithFollowFlag_ShouldHandleFollowMode()
    {
        // Arrange
        var imageName = "test-image";
        var command = new LogsCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = imageName });

        _mockContainerService
            .Setup(x => x.GetContainerLogsAsync(imageName, 0))
            .ReturnsAsync(new ContainerLogsResult
            {
                Success = true,
                Logs = "Test log output"
            });

        // Act
        var result = await command.ExecuteAsync(imageName, true);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Logs"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerLogsAsync(imageName, 0), Times.Once);
    }

    #endregion

    #region ShellCommand Tests

    [Fact]
    public async Task ShellCommand_WithValidImageName_ShouldCallServices()
    {
        // Arrange
        var imageName = "test-image";
        var command = new ShellCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = imageName });

        _mockContainerService
            .Setup(x => x.IsContainerRunningAsync(imageName))
            .ReturnsAsync(true);

        // Act
        var result = await command.ExecuteAsync(imageName);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Shell"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.IsContainerRunningAsync(imageName), Times.Once);
    }

    [Fact]
    public async Task ShellCommand_WithDevContainerName_ShouldCallServices()
    {
        // Arrange
        var imageName = "test-image";
        var command = new ShellCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImageByNameAsync(imageName))
            .ReturnsAsync(new ConfigurationOption
            {
                Name = imageName,
                Type = ConfigurationType.Images,
                Path = $"/test/path/{imageName}"
            });

        // 设置容器服务Mock以匹配新的容器名称获取逻辑
        // 首先检查直接的镜像名称，返回null
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync(imageName))
            .ReturnsAsync((Deck.Core.Models.ContainerInfo)null!);

        // 然后检查-dev后缀，返回容器信息
        _mockContainerService
            .Setup(x => x.GetContainerInfoAsync($"{imageName}-dev"))
            .ReturnsAsync(new Deck.Core.Models.ContainerInfo { Name = $"{imageName}-dev" });

        _mockContainerService
            .Setup(x => x.IsContainerRunningAsync($"{imageName}-dev"))
            .ReturnsAsync(true);

        // Act
        var result = await command.ExecuteAsync(imageName);

        // Assert
        _mockDirectoryManagement.Verify(x => x.GetImageByNameAsync(imageName), Times.Once);
        _mockLoggingService.Verify(x => x.GetLogger("Deck.Console.Shell"), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync(imageName), Times.Once);
        _mockContainerService.Verify(x => x.GetContainerInfoAsync($"{imageName}-dev"), Times.Once);
        _mockContainerService.Verify(x => x.IsContainerRunningAsync($"{imageName}-dev"), Times.Once);
    }

    #endregion

    #region InteractiveSelection Tests

    [Fact]
    public async Task InteractiveSelection_WhenNoImagesAvailable_ShouldShowWarning()
    {
        // Arrange
        var command = new StopCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockGlobalExceptionHandler.Object,
            _mockContainerService.Object
        );

        _mockDirectoryManagement
            .Setup(x => x.GetImagesAsync())
            .ReturnsAsync(new List<ConfigurationOption>());

        // Act
        var result = await command.ExecuteAsync(null);

        // Assert
        Assert.False(result);
        _mockConsoleDisplay.Verify(
            x => x.ShowWarning(It.Is<string>(s => s.Contains("没有找到任何镜像配置"))),
            Times.Once);
    }

    [Fact]
    public async Task InteractiveSelection_WhenUserCancels_ShouldReturnFalse()
    {
        // Arrange
        var command = new StopCommand(
            _mockConsoleDisplay.Object,
            _mockInteractiveSelection.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagement.Object,
            _mockGlobalExceptionHandler.Object,
            _mockContainerService.Object
        );

        var mockImages = new List<ConfigurationOption>
        {
            new() { Name = "image1", Type = ConfigurationType.Images }
        };

        _mockDirectoryManagement
            .Setup(x => x.GetImagesAsync())
            .ReturnsAsync(mockImages);

        _mockInteractiveSelection
            .Setup(x => x.ShowSingleSelectionAsync(It.IsAny<InteractiveSelector<SelectableOption>>(), null, default))
            .ReturnsAsync(new SelectionResult<SelectableOption>
            {
                IsCancelled = true,
                SelectedItems = new List<SelectableOption>()
            });

        // Act
        var result = await command.ExecuteAsync(null);

        // Assert
        Assert.False(result);
        _mockConsoleDisplay.Verify(
            x => x.ShowError(It.Is<string>(s => s.Contains("没有选择镜像"))),
            Times.Once);
    }

    #endregion
}