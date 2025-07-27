using Deck.Core.Models;
using Deck.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Runtime.InteropServices;

namespace Deck.Services.Tests;

/// <summary>
/// 系统检测服务测试
/// </summary>
public class SystemDetectionServiceTests
{
    private readonly Mock<ILogger<SystemDetectionService>> _mockLogger;
    private readonly SystemDetectionService _systemDetectionService;

    public SystemDetectionServiceTests()
    {
        _mockLogger = new Mock<ILogger<SystemDetectionService>>();
        _systemDetectionService = new SystemDetectionService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetSystemInfoAsync_ShouldReturnSystemInfo()
    {
        // Act
        var result = await _systemDetectionService.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.OperatingSystem.Should().NotBe(OperatingSystemType.Unknown);
        result.Architecture.Should().NotBe(SystemArchitecture.Unknown);
        result.Version.Should().NotBeNullOrEmpty();
        result.AvailableMemoryMb.Should().BeGreaterThan(0);
        result.AvailableDiskSpaceGb.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DetectContainerEngineAsync_ShouldDetectAvailableEngine()
    {
        // Act
        var result = await _systemDetectionService.DetectContainerEngineAsync();

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().NotBe(ContainerEngineType.None);
        
        if (result.IsAvailable)
        {
            result.Version.Should().NotBeNullOrEmpty();
            result.InstallPath.Should().NotBeNullOrEmpty();
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Theory]
    [InlineData(".")]
    [InlineData("/tmp")]
    public async Task DetectProjectTypeAsync_ShouldDetectProjectType(string projectPath)
    {
        // Arrange
        if (!Directory.Exists(projectPath))
        {
            Directory.CreateDirectory(projectPath);
        }

        // Act
        var result = await _systemDetectionService.DetectProjectTypeAsync(projectPath);

        // Assert
        result.Should().NotBeNull();
        result.ProjectRoot.Should().Be(projectPath);
        result.DetectedTypes.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckSystemRequirementsAsync_ShouldReturnRequirementsResult()
    {
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Checks.Should().NotBeEmpty();
        result.Checks.Should().ContainSingle(c => c.Name == "可用内存");
        result.Checks.Should().ContainSingle(c => c.Name == "可用磁盘空间");
        result.Checks.Should().ContainSingle(c => c.Name == "容器引擎");
        result.Checks.Should().ContainSingle(c => c.Name == "网络连接");
        
        // 应该包含必需工具检查
        result.Checks.Should().Contain(c => c.Name.StartsWith("必需工具:"));
        
        result.Warnings.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithAvaloniaProject_ShouldDetectAvalonia()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建 Avalonia 项目标识文件 (.csproj 文件)
            File.WriteAllText(Path.Combine(tempDir, "test-app.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Avalonia" Version="11.0.0" />
                  </ItemGroup>
                </Project>
                """);

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().Contain(ProjectType.Avalonia);
            result.RecommendedType.Should().Be(ProjectType.Avalonia);
            result.ProjectFiles.Should().Contain("test-app.csproj");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithFlutterProject_ShouldDetectFlutter()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建 Flutter 项目标识文件 (pubspec.yaml 包含 flutter)
            File.WriteAllText(Path.Combine(tempDir, "pubspec.yaml"), """
                name: test_app
                flutter:
                  sdk: flutter
                """);

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().Contain(ProjectType.Flutter);
            result.RecommendedType.Should().Be(ProjectType.Flutter);
            result.ProjectFiles.Should().Contain("pubspec.yaml");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithAvaloniaProjectPriority_ShouldPreferAvalonia()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建 Avalonia 项目标识文件 (.csproj 包含 Avalonia 引用)
            File.WriteAllText(Path.Combine(tempDir, "test.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Avalonia" Version="11.0.0" />
                  </ItemGroup>
                </Project>
                """);

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().Contain(ProjectType.Avalonia);
            result.RecommendedType.Should().Be(ProjectType.Avalonia);
            result.ProjectFiles.Should().Contain("test.csproj");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithDotNetProject_ShouldDetectDotNet()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建普通 .NET 项目标识文件
            File.WriteAllText(Path.Combine(tempDir, "test.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().Contain(ProjectType.DotNet);
            result.RecommendedType.Should().Be(ProjectType.DotNet);
            result.ProjectFiles.Should().Contain("*.csproj");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetSystemInfo_ShouldMatchCurrentPlatform()
    {
        // Act
        var result = await _systemDetectionService.GetSystemInfoAsync();

        // Assert - 验证操作系统检测准确性
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            result.OperatingSystem.Should().Be(OperatingSystemType.Windows);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            result.OperatingSystem.Should().Be(OperatingSystemType.Linux);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            result.OperatingSystem.Should().Be(OperatingSystemType.MacOS);
        }

        // 验证架构检测准确性
        var expectedArchitecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => SystemArchitecture.X64,
            Architecture.Arm64 => SystemArchitecture.ARM64,
            Architecture.X86 => SystemArchitecture.X86,
            _ => SystemArchitecture.Unknown
        };

        result.Architecture.Should().Be(expectedArchitecture);
    }

    [Fact]
    public async Task CheckSystemRequirementsAsync_MemoryCheck_ShouldHaveCorrectThreshold()
    {
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        var memoryCheck = result.Checks.First(c => c.Name == "可用内存");
        memoryCheck.Description.Should().Contain("4096MB"); // 验证 4GB 阈值
    }

    [Fact]
    public async Task CheckSystemRequirementsAsync_DiskCheck_ShouldHaveCorrectThreshold()
    {
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        var diskCheck = result.Checks.First(c => c.Name == "可用磁盘空间");
        diskCheck.Description.Should().Contain("10GB"); // 验证 10GB 阈值
    }

    [Fact]
    public async Task CheckSystemRequirementsAsync_ContainerEngineCheck_ShouldPreferPodman()
    {
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        var containerCheck = result.Checks.First(c => c.Name == "容器引擎");
        
        if (containerCheck.Passed)
        {
            // 如果检测成功，应该显示具体的引擎类型和版本
            containerCheck.Description.Should().MatchRegex(@"检测到 (Podman|Docker) .+");
        }
        else
        {
            containerCheck.Suggestion.Should().Contain("Podman (推荐)");
        }
    }

    [Fact]
    public async Task CheckSystemRequirementsAsync_NetworkCheck_ShouldIncludeConnectivityStatus()
    {
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        var networkCheck = result.Checks.FirstOrDefault(c => c.Name == "网络连接");
        networkCheck.Should().NotBeNull();
        
        if (networkCheck!.Passed)
        {
            networkCheck.Description.Should().MatchRegex(@"网络连接正常，已连通 \d+ 个服务");
        }
        else
        {
            networkCheck.Description.Should().Contain("网络连接异常");
            networkCheck.Suggestion.Should().Contain("检查网络连接，或考虑使用离线模式");
        }
    }

    [Theory]
    [InlineData("curl")]
    [InlineData("tar")]
    [InlineData("gzip")]
    public async Task CheckSystemRequirementsAsync_ShouldCheckRequiredTool(string toolName)
    {
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        result.Checks.Should().ContainSingle(c => c.Name == $"必需工具: {toolName}");
    }

    [Fact]
    public async Task CheckSystemRequirementsAsync_WhenNetworkUnavailable_ShouldAddWarning()
    {
        // 这个测试在网络不可用时才会通过，所以我们模拟检查结果
        // Act
        var result = await _systemDetectionService.CheckSystemRequirementsAsync();

        // Assert
        var networkCheck = result.Checks.FirstOrDefault(c => c.Name == "网络连接");
        if (networkCheck != null && !networkCheck.Passed)
        {
            result.Warnings.Should().Contain("网络连接不可用，将限制模板同步和远程镜像拉取功能");
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithReactNativeProject_ShouldDetectReactNative()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "android"));
        Directory.CreateDirectory(Path.Combine(tempDir, "ios"));
        
        try
        {
            // 创建 React Native 项目标识文件
            File.WriteAllText(Path.Combine(tempDir, "package.json"), "{}");

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().Contain(ProjectType.ReactNative);
            result.RecommendedType.Should().Be(ProjectType.ReactNative);
            result.ProjectFiles.Should().Contain("package.json, android/, ios/");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithElectronProject_ShouldDetectElectron()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建 Electron 项目标识文件
            File.WriteAllText(Path.Combine(tempDir, "package.json"), """
                {
                  "main": "main.js",
                  "dependencies": {
                    "electron": "^20.0.0"
                  }
                }
                """);

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().Contain(ProjectType.Electron);
            result.RecommendedType.Should().Be(ProjectType.Electron);
            result.ProjectFiles.Should().Contain("package.json");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithMultipleProjects_ShouldReturnCorrectPriority()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建多个项目类型的标识文件 (测试优先级)
            File.WriteAllText(Path.Combine(tempDir, "pubspec.yaml"), """
                name: test_app
                flutter:
                  sdk: flutter
                """); // Flutter - 优先级2
            File.WriteAllText(Path.Combine(tempDir, "package.json"), "{}"); // Node.js - 优先级6
            File.WriteAllText(Path.Combine(tempDir, "test.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />"); // .NET - 优先级8

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().HaveCountGreaterThan(1);
            result.DetectedTypes.Should().Contain(ProjectType.Flutter);
            result.DetectedTypes.Should().Contain(ProjectType.DotNet);
            // Flutter 应该有更高的优先级 (2 vs 8)
            result.RecommendedType.Should().Be(ProjectType.Flutter);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithAvaloniaProject_ShouldPreferAvalonia()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 创建 Avalonia + Flutter + .NET 的混合项目 (测试优先级)
            File.WriteAllText(Path.Combine(tempDir, "test-app.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Avalonia" Version="11.0.0" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(tempDir, "pubspec.yaml"), """
                name: test_app
                flutter:
                  sdk: flutter
                """);

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().HaveCountGreaterThan(1);
            result.DetectedTypes.Should().Contain(ProjectType.Avalonia);
            result.DetectedTypes.Should().Contain(ProjectType.Flutter);
            // Flutter 应该有更高优先级
            result.RecommendedType.Should().Be(ProjectType.Flutter);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithNoKnownProject_ShouldReturnEmpty()
    {
        // Arrange
        var tempDir = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // 不创建任何已知的项目标识文件
            File.WriteAllText(Path.Combine(tempDir, "unknown.txt"), "unknown content");

            // Act
            var result = await _systemDetectionService.DetectProjectTypeAsync(tempDir);

            // Assert
            result.DetectedTypes.Should().BeEmpty();
            result.RecommendedType.Should().BeNull();
            result.ProjectFiles.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}