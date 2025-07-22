using Deck.Console.Commands;
using Deck.Core.Interfaces;
using Deck.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Deck.Console.Tests.Commands;

/// <summary>
/// Doctor命令单元测试
/// </summary>
public class DoctorCommandTests
{
    private readonly Mock<IConsoleDisplay> _mockConsoleDisplay;
    private readonly Mock<ISystemDetectionService> _mockSystemDetectionService;
    private readonly Mock<INetworkService> _mockNetworkService;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IDirectoryManagementService> _mockDirectoryManagementService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DoctorCommand _doctorCommand;

    public DoctorCommandTests()
    {
        _mockConsoleDisplay = new Mock<IConsoleDisplay>();
        _mockSystemDetectionService = new Mock<ISystemDetectionService>();
        _mockNetworkService = new Mock<INetworkService>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockDirectoryManagementService = new Mock<IDirectoryManagementService>();
        _mockLogger = new Mock<ILogger>();

        _mockLoggingService
            .Setup(x => x.GetLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _doctorCommand = new DoctorCommand(
            _mockConsoleDisplay.Object,
            _mockSystemDetectionService.Object,
            _mockNetworkService.Object,
            _mockLoggingService.Object,
            _mockDirectoryManagementService.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_AllChecksPass_ReturnsTrue()
    {
        // Arrange
        var systemInfo = new SystemInfo
        {
            OperatingSystem = OperatingSystemType.MacOS,
            Architecture = SystemArchitecture.ARM64,
            Version = "macOS 14.0",
            AvailableMemoryMb = 8192,
            AvailableDiskSpaceGb = 50,
            IsWsl = false
        };

        var containerEngine = new ContainerEngineInfo
        {
            Type = ContainerEngineType.Podman,
            IsAvailable = true,
            Version = "5.5.2"
        };

        var projectInfo = new ProjectTypeInfo
        {
            DetectedTypes = new List<ProjectType> { ProjectType.DotNet },
            RecommendedType = ProjectType.DotNet,
            ProjectRoot = "/test/path",
            ProjectFiles = new List<string> { "*.csproj" }
        };

        var systemRequirements = new SystemRequirementsResult
        {
            MeetsRequirements = true,
            Checks = new List<RequirementCheck>
            {
                new RequirementCheck
                {
                    Name = "测试检查",
                    Passed = true,
                    Description = "测试通过"
                }
            },
            Warnings = new List<string>()
        };

        var networkConnectivity = new NetworkConnectivityResult
        {
            IsConnected = true
        };

        var registryResult = new RegistryConnectivityResult
        {
            IsAvailable = true
        };

        var serviceResults = new List<ServiceConnectivityResult>
        {
            new ServiceConnectivityResult
            {
                IsAvailable = true,
                ServiceType = NetworkServiceType.GitHub
            }
        };

        _mockSystemDetectionService
            .Setup(x => x.GetSystemInfoAsync())
            .ReturnsAsync(systemInfo);

        _mockSystemDetectionService
            .Setup(x => x.DetectContainerEngineAsync())
            .ReturnsAsync(containerEngine);

        _mockSystemDetectionService
            .Setup(x => x.DetectProjectTypeAsync(It.IsAny<string>()))
            .ReturnsAsync(projectInfo);

        _mockSystemDetectionService
            .Setup(x => x.CheckSystemRequirementsAsync())
            .ReturnsAsync(systemRequirements);

        _mockNetworkService
            .Setup(x => x.CheckConnectivityAsync(It.IsAny<int>()))
            .ReturnsAsync(networkConnectivity);

        _mockNetworkService
            .Setup(x => x.CheckRegistryConnectivityAsync(It.IsAny<ContainerRegistryType>(), It.IsAny<int>()))
            .ReturnsAsync(registryResult);

        _mockNetworkService
            .Setup(x => x.CheckMultipleServicesAsync(It.IsAny<IEnumerable<NetworkServiceType>>(), It.IsAny<int>()))
            .ReturnsAsync(serviceResults);

        // Act
        var result = await _doctorCommand.ExecuteAsync();

        // Assert
        Assert.True(result);

        // Verify display methods were called
        _mockConsoleDisplay.Verify(x => x.ShowInfo(It.IsAny<string>()), Times.AtLeast(1));
        _mockConsoleDisplay.Verify(x => x.ShowTitle(It.IsAny<string>()), Times.AtLeast(4));
        _mockConsoleDisplay.Verify(x => x.ShowSuccess(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SystemRequirementsFail_ReturnsFalse()
    {
        // Arrange
        var systemInfo = new SystemInfo
        {
            OperatingSystem = OperatingSystemType.Linux,
            Architecture = SystemArchitecture.X64,
            Version = "Ubuntu 20.04",
            AvailableMemoryMb = 2048, // 低于要求的4GB
            AvailableDiskSpaceGb = 5,  // 低于要求的10GB
            IsWsl = false
        };

        var containerEngine = new ContainerEngineInfo
        {
            Type = ContainerEngineType.None,
            IsAvailable = false,
            ErrorMessage = "未找到容器引擎"
        };

        var projectInfo = new ProjectTypeInfo
        {
            DetectedTypes = new List<ProjectType>(),
            RecommendedType = null,
            ProjectRoot = "/test/path",
            ProjectFiles = new List<string>()
        };

        var systemRequirements = new SystemRequirementsResult
        {
            MeetsRequirements = false,  // 不满足系统要求
            Checks = new List<RequirementCheck>
            {
                new RequirementCheck
                {
                    Name = "内存检查",
                    Passed = false,
                    Description = "内存不足",
                    Suggestion = "建议升级内存至8GB"
                }
            },
            Warnings = new List<string> { "系统资源不足" }
        };

        var networkConnectivity = new NetworkConnectivityResult
        {
            IsConnected = false  // 网络不可用
        };

        var registryResult = new RegistryConnectivityResult
        {
            IsAvailable = false
        };

        var serviceResults = new List<ServiceConnectivityResult>
        {
            new ServiceConnectivityResult
            {
                IsAvailable = false,
                ServiceType = NetworkServiceType.GitHub
            }
        };

        _mockSystemDetectionService
            .Setup(x => x.GetSystemInfoAsync())
            .ReturnsAsync(systemInfo);

        _mockSystemDetectionService
            .Setup(x => x.DetectContainerEngineAsync())
            .ReturnsAsync(containerEngine);

        _mockSystemDetectionService
            .Setup(x => x.DetectProjectTypeAsync(It.IsAny<string>()))
            .ReturnsAsync(projectInfo);

        _mockSystemDetectionService
            .Setup(x => x.CheckSystemRequirementsAsync())
            .ReturnsAsync(systemRequirements);

        _mockNetworkService
            .Setup(x => x.CheckConnectivityAsync(It.IsAny<int>()))
            .ReturnsAsync(networkConnectivity);

        _mockNetworkService
            .Setup(x => x.CheckRegistryConnectivityAsync(It.IsAny<ContainerRegistryType>(), It.IsAny<int>()))
            .ReturnsAsync(registryResult);

        _mockNetworkService
            .Setup(x => x.CheckMultipleServicesAsync(It.IsAny<IEnumerable<NetworkServiceType>>(), It.IsAny<int>()))
            .ReturnsAsync(serviceResults);

        // Act
        var result = await _doctorCommand.ExecuteAsync();

        // Assert
        Assert.False(result);

        // Verify warning was shown
        _mockConsoleDisplay.Verify(x => x.ShowWarning(It.IsAny<string>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        _mockSystemDetectionService
            .Setup(x => x.GetSystemInfoAsync())
            .ThrowsAsync(new Exception("系统检测失败"));

        // Act
        var result = await _doctorCommand.ExecuteAsync();

        // Assert
        Assert.False(result);

        // Verify error was displayed
        _mockConsoleDisplay.Verify(x => x.ShowError(It.IsAny<string>()), Times.Once);
    }
}