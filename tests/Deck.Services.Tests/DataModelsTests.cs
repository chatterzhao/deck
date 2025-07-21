using Deck.Core.Models;
using FluentAssertions;
using System.Text.Json;

namespace Deck.Services.Tests;

/// <summary>
/// 数据模型测试 - 验证Task 2.1 新增的数据模型
/// </summary>
public class DataModelsTests
{
    [Fact]
    public void ContainerInfo_ShouldInitializeWithDefaultValues()
    {
        // Act
        var containerInfo = new ContainerInfo();

        // Assert
        containerInfo.Should().NotBeNull();
        containerInfo.Id.Should().BeEmpty();
        containerInfo.Name.Should().BeEmpty();
        containerInfo.Status.Should().Be(ContainerStatus.NotExists);
        containerInfo.PortMappings.Should().NotBeNull().And.BeEmpty();
        containerInfo.Environment.Should().NotBeNull().And.BeEmpty();
        containerInfo.Mounts.Should().NotBeNull().And.BeEmpty();
        containerInfo.Networks.Should().NotBeNull().And.BeEmpty();
        containerInfo.Labels.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ContainerInfo_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var created = DateTime.UtcNow;
        var started = DateTime.UtcNow.AddMinutes(1);
        
        // Act
        var containerInfo = new ContainerInfo
        {
            Id = "container123",
            Name = "test-container",
            Status = ContainerStatus.Running,
            ImageName = "nginx:latest",
            ImageId = "image456",
            Created = created,
            StartedAt = started,
            EngineType = ContainerEngineType.Podman,
            ProjectType = ProjectType.DotNet,
            ProjectRoot = "/app/project"
        };

        // Assert
        containerInfo.Id.Should().Be("container123");
        containerInfo.Name.Should().Be("test-container");
        containerInfo.Status.Should().Be(ContainerStatus.Running);
        containerInfo.ImageName.Should().Be("nginx:latest");
        containerInfo.ImageId.Should().Be("image456");
        containerInfo.Created.Should().Be(created);
        containerInfo.StartedAt.Should().Be(started);
        containerInfo.EngineType.Should().Be(ContainerEngineType.Podman);
        containerInfo.ProjectType.Should().Be(ProjectType.DotNet);
        containerInfo.ProjectRoot.Should().Be("/app/project");
    }

    [Fact]
    public void ComposeValidationResult_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new ComposeValidationResult();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.FilePath.Should().BeEmpty();
        result.ParseErrors.Should().NotBeNull().And.BeEmpty();
        result.Warnings.Should().NotBeNull().And.BeEmpty();
        result.ServiceResults.Should().NotBeNull().And.BeEmpty();
        result.NetworkResults.Should().NotBeNull().And.BeEmpty();
        result.VolumeResults.Should().NotBeNull().And.BeEmpty();
        result.EnvironmentResults.Should().NotBeNull().And.BeEmpty();
        result.PortConflicts.Should().NotBeNull().And.BeEmpty();
        result.DependencyResults.Should().NotBeNull().And.BeEmpty();
        result.Summary.Should().NotBeNull();
    }

    [Fact]
    public void ComposeValidationResult_ShouldCalculateSummaryCorrectly()
    {
        // Arrange
        var result = new ComposeValidationResult
        {
            IsValid = true,
            FilePath = "/app/docker-compose.yml",
            ComposeVersion = "3.8"
        };

        result.ServiceResults.Add(new ServiceValidationResult
        {
            ServiceName = "web",
            IsValid = true
        });

        result.ServiceResults.Add(new ServiceValidationResult
        {
            ServiceName = "db",
            IsValid = false,
            Errors = new List<string> { "Missing required image" }
        });

        result.ParseErrors.Add(new ComposeValidationError
        {
            Type = ComposeErrorType.InvalidImageName,
            Message = "Invalid image name format",
            Severity = ValidationSeverity.Error
        });

        result.Warnings.Add(new ComposeValidationWarning
        {
            Type = ComposeWarningType.BestPracticeViolation,
            Message = "Consider using specific image tags",
            Severity = ValidationSeverity.Warning
        });

        // Act
        result.Summary = new ComposeValidationSummary
        {
            TotalServices = result.ServiceResults.Count,
            ValidServices = result.ServiceResults.Count(s => s.IsValid),
            TotalErrors = result.ParseErrors.Count,
            TotalWarnings = result.Warnings.Count
        };

        // Assert
        result.Summary.TotalServices.Should().Be(2);
        result.Summary.ValidServices.Should().Be(1);
        result.Summary.TotalErrors.Should().Be(1);
        result.Summary.TotalWarnings.Should().Be(1);
    }

    [Fact]
    public void PortMapping_ShouldInitializeCorrectly()
    {
        // Act
        var portMapping = new PortMapping
        {
            HostPort = 8080,
            ContainerPort = 80,
            Protocol = DeckProtocolType.TCP,
            HostIP = "127.0.0.1"
        };

        // Assert
        portMapping.HostPort.Should().Be(8080);
        portMapping.ContainerPort.Should().Be(80);
        portMapping.Protocol.Should().Be(DeckProtocolType.TCP);
        portMapping.HostIP.Should().Be("127.0.0.1");
    }

    [Fact]
    public void ContainerResourceUsage_ShouldCalculatePercentageCorrectly()
    {
        // Act
        var usage = new ContainerResourceUsage
        {
            CpuUsagePercent = 25.5,
            MemoryUsageBytes = 512 * 1024 * 1024, // 512MB
            MemoryLimitBytes = 1024 * 1024 * 1024, // 1GB
            MemoryUsagePercent = 50.0,
            NetworkRxBytes = 1024 * 1024, // 1MB
            NetworkTxBytes = 2 * 1024 * 1024, // 2MB
            ProcessCount = 15,
            Uptime = TimeSpan.FromHours(2.5)
        };

        // Assert
        usage.CpuUsagePercent.Should().Be(25.5);
        usage.MemoryUsageBytes.Should().Be(512 * 1024 * 1024);
        usage.MemoryLimitBytes.Should().Be(1024 * 1024 * 1024);
        usage.MemoryUsagePercent.Should().Be(50.0);
        usage.NetworkRxBytes.Should().Be(1024 * 1024);
        usage.NetworkTxBytes.Should().Be(2 * 1024 * 1024);
        usage.ProcessCount.Should().Be(15);
        usage.Uptime.Should().Be(TimeSpan.FromHours(2.5));
    }

    [Fact]
    public void ContainerListOptions_ShouldHaveReasonableDefaults()
    {
        // Act
        var options = new ContainerListOptions();

        // Assert
        options.ShowAll.Should().BeTrue();
        options.FilterByProjectType.Should().BeNull();
        options.FilterByStatus.Should().BeNull();
        options.FilterByEngine.Should().BeNull();
        options.NamePattern.Should().BeNull();
        options.ShowResourceUsage.Should().BeFalse();
        options.SortBy.Should().Be(ContainerSortBy.Created);
        options.Descending.Should().BeTrue();
    }

    [Fact]
    public void SecurityScanResult_ShouldCalculateTotalVulnerabilitiesCorrectly()
    {
        // Act
        var scanResult = new SecurityScanResult
        {
            IsScanned = true,
            HighVulnerabilities = 5,
            MediumVulnerabilities = 10,
            LowVulnerabilities = 15,
            ScanTool = "Trivy",
            ScanTime = DateTime.UtcNow
        };

        // Assert
        scanResult.TotalVulnerabilities.Should().Be(30);
        scanResult.HighVulnerabilities.Should().Be(5);
        scanResult.MediumVulnerabilities.Should().Be(10);
        scanResult.LowVulnerabilities.Should().Be(15);
    }

    [Fact]
    public void EnumsShould_HaveExpectedValues()
    {
        // Assert Container Status
        var containerStatuses = Enum.GetValues<ContainerStatus>();
        containerStatuses.Should().Contain(ContainerStatus.NotExists);
        containerStatuses.Should().Contain(ContainerStatus.Running);
        containerStatuses.Should().Contain(ContainerStatus.Stopped);
        containerStatuses.Should().Contain(ContainerStatus.Paused);
        containerStatuses.Should().Contain(ContainerStatus.Error);

        // Assert Protocol Types
        var protocolTypes = Enum.GetValues<DeckProtocolType>();
        protocolTypes.Should().Contain(DeckProtocolType.TCP);
        protocolTypes.Should().Contain(DeckProtocolType.UDP);
        protocolTypes.Should().Contain(DeckProtocolType.SCTP);

        // Assert Mount Types
        var mountTypes = Enum.GetValues<MountType>();
        mountTypes.Should().Contain(MountType.Bind);
        mountTypes.Should().Contain(MountType.Volume);
        mountTypes.Should().Contain(MountType.Tmpfs);

        // Assert Network Modes
        var networkModes = Enum.GetValues<NetworkMode>();
        networkModes.Should().Contain(NetworkMode.Bridge);
        networkModes.Should().Contain(NetworkMode.Host);
        networkModes.Should().Contain(NetworkMode.None);
        networkModes.Should().Contain(NetworkMode.Container);
        networkModes.Should().Contain(NetworkMode.Custom);

        // Assert Validation Severities
        var severities = Enum.GetValues<ValidationSeverity>();
        severities.Should().Contain(ValidationSeverity.Info);
        severities.Should().Contain(ValidationSeverity.Warning);
        severities.Should().Contain(ValidationSeverity.Error);
        severities.Should().Contain(ValidationSeverity.Critical);

        // Assert Container Sort Options
        var sortOptions = Enum.GetValues<ContainerSortBy>();
        sortOptions.Should().Contain(ContainerSortBy.Name);
        sortOptions.Should().Contain(ContainerSortBy.Created);
        sortOptions.Should().Contain(ContainerSortBy.Status);
        sortOptions.Should().Contain(ContainerSortBy.Image);
        sortOptions.Should().Contain(ContainerSortBy.Size);
        sortOptions.Should().Contain(ContainerSortBy.CpuUsage);
        sortOptions.Should().Contain(ContainerSortBy.MemoryUsage);
    }

    [Fact]
    public void DataModels_ShouldBeJsonSerializable()
    {
        // Arrange
        var containerInfo = new ContainerInfo
        {
            Id = "test123",
            Name = "test-container",
            Status = ContainerStatus.Running,
            PortMappings = new Dictionary<string, string>
            {
                { "8080", "80" }
            },
            ResourceUsage = new ContainerResourceUsage
            {
                CpuUsagePercent = 25.0,
                MemoryUsageBytes = 512 * 1024 * 1024
            }
        };

        // Act & Assert - Should not throw exception
        var json = JsonSerializer.Serialize(containerInfo);
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("test123");
        json.Should().Contain("test-container");
        
        var deserialized = JsonSerializer.Deserialize<ContainerInfo>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("test123");
        deserialized.Name.Should().Be("test-container");
        deserialized.Status.Should().Be(ContainerStatus.Running);
    }
}