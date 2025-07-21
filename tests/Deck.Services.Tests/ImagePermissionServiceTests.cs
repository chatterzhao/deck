using Deck.Core.Interfaces;
using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deck.Services.Tests;

/// <summary>
/// 镜像权限管理服务测试
/// </summary>
public class ImagePermissionServiceTests : IDisposable
{
    private readonly IImagePermissionService _imagePermissionService;
    private readonly string _testImagePath;
    private readonly string _testDirectory;

    public ImagePermissionServiceTests()
    {
        _imagePermissionService = new ImagePermissionService(NullLogger<ImagePermissionService>.Instance);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"deck-test-{Guid.NewGuid():N}");
        _testImagePath = Path.Combine(_testDirectory, "testapp-20240315-1430");
        
        // 创建测试目录结构
        Directory.CreateDirectory(_testImagePath);
        CreateTestFiles();
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_ReadProtectedFile_ShouldAllow()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, "compose.yaml", FileOperation.Read);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Allowed);
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("读取受保护文件是允许的");
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_WriteProtectedFile_ShouldDeny()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, "compose.yaml", FileOperation.Write);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Denied);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("受保护的配置快照");
        result.Alternatives.Should().NotBeEmpty();
        result.Alternatives.Should().Contain(alt => alt.Contains("Custom/"));
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_DeleteProtectedFile_ShouldDeny()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, "Dockerfile", FileOperation.Delete);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Denied);
        result.IsAllowed.Should().BeFalse();
        result.Suggestions.Should().Contain(s => s.Contains("构建时配置的快照"));
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_ReadEnvFile_ShouldAllow()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, ".env", FileOperation.Read);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Allowed);
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_WriteEnvFile_ShouldWarn()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, ".env", FileOperation.Write);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Warning);
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("仅限运行时变量");
        result.Suggestions.Should().Contain(s => s.Contains("DEV_PORT"));
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_DeleteEnvFile_ShouldDeny()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, ".env", FileOperation.Delete);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Denied);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("环境变量丢失");
    }

    [Fact]
    public async Task ValidateFilePermissionAsync_RegularFile_ShouldAllow()
    {
        // Act
        var result = await _imagePermissionService.ValidateFilePermissionAsync(
            _testImagePath, "custom-script.sh", FileOperation.Write);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Allowed);
        result.IsAllowed.Should().BeTrue();
        result.Suggestions.Should().Contain(s => s.Contains("版本控制"));
    }

    [Fact]
    public async Task ValidateDirectoryOperationAsync_ReadOperation_ShouldAllow()
    {
        // Act
        var result = await _imagePermissionService.ValidateDirectoryOperationAsync(
            _testImagePath, DirectoryOperation.Read);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Allowed);
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Contain("读取操作不受限制");
    }

    [Fact]
    public async Task ValidateDirectoryOperationAsync_DeleteOperation_ShouldDeny()
    {
        // Act
        var result = await _imagePermissionService.ValidateDirectoryOperationAsync(
            _testImagePath, DirectoryOperation.Delete);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Denied);
        result.IsAllowed.Should().BeFalse();
        result.Impact.Should().NotBeNull();
        result.Impact!.Level.Should().Be(ImpactLevel.Critical);
        result.Impact.AffectedComponents.Should().Contain("镜像管理");
    }

    [Fact]
    public async Task ValidateDirectoryOperationAsync_RenameOperation_ShouldDeny()
    {
        // Act
        var result = await _imagePermissionService.ValidateDirectoryOperationAsync(
            _testImagePath, DirectoryOperation.Rename);

        // Assert
        result.Should().NotBeNull();
        result.Permission.Should().Be(PermissionLevel.Denied);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Contain("对应关系");
        result.Alternatives.Should().Contain(alt => alt.Contains("Custom/"));
    }

    [Fact]
    public async Task ValidateEnvFileChangesAsync_RuntimeVariables_ShouldAllow()
    {
        // Arrange
        var envChanges = new Dictionary<string, string?>
        {
            { "DEV_PORT", "3001" },
            { "DEBUG_PORT", "9230" },
            { "PROJECT_NAME", "myapp-updated" }
        };

        // Act
        var result = await _imagePermissionService.ValidateEnvFileChangesAsync(_testImagePath, envChanges);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.AllowedChanges.Should().HaveCount(3);
        result.DeniedChanges.Should().BeEmpty();
        result.ValidationDetails.Should().HaveCount(3);
        result.ValidationDetails.Should().OnlyContain(v => v.Permission == PermissionLevel.Allowed);
    }

    [Fact]
    public async Task ValidateEnvFileChangesAsync_BuildTimeVariables_ShouldDeny()
    {
        // Arrange
        var envChanges = new Dictionary<string, string?>
        {
            { "NODE_VERSION", "18" },
            { "BASE_IMAGE", "ubuntu:22.04" },
            { "BUILD_ARGS", "--verbose" }
        };

        // Act
        var result = await _imagePermissionService.ValidateEnvFileChangesAsync(_testImagePath, envChanges);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.AllowedChanges.Should().BeEmpty();
        result.DeniedChanges.Should().HaveCount(3);
        result.ValidationDetails.Should().OnlyContain(v => v.VariableType == EnvVariableType.BuildTime);
        result.ValidationDetails.Should().OnlyContain(v => v.Suggestion != null && v.Suggestion.Contains("Custom/"));
    }

    [Fact]
    public async Task ValidateEnvFileChangesAsync_MixedVariables_ShouldPartiallyAllow()
    {
        // Arrange
        var envChanges = new Dictionary<string, string?>
        {
            { "DEV_PORT", "3001" },         // Runtime - should allow
            { "NODE_VERSION", "18" },       // Build-time - should deny
            { "CONTAINER_NAME", "myapp" }   // Runtime - should allow
        };

        // Act
        var result = await _imagePermissionService.ValidateEnvFileChangesAsync(_testImagePath, envChanges);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.AllowedChanges.Should().HaveCount(2);
        result.DeniedChanges.Should().HaveCount(1);
        result.DeniedChanges.Should().ContainKey("NODE_VERSION");
        result.AllowedChanges.Should().ContainKey("DEV_PORT");
        result.AllowedChanges.Should().ContainKey("CONTAINER_NAME");
    }

    [Fact]
    public async Task GetImagePermissionSummaryAsync_ValidImageDirectory_ShouldReturnSummary()
    {
        // Act
        var summary = await _imagePermissionService.GetImagePermissionSummaryAsync(_testImagePath);

        // Assert
        summary.Should().NotBeNull();
        summary.ImagePath.Should().Be(_testImagePath);
        summary.ImageName.Should().Be("testapp-20240315-1430");
        summary.IsValidImageDirectory.Should().BeTrue();
        summary.ProtectedFiles.Should().NotBeEmpty();
        summary.ProtectedFiles.Should().Contain(f => f.FilePath == "compose.yaml");
        summary.ProtectedFiles.Should().Contain(f => f.FilePath == "Dockerfile");
        summary.ModifiableFiles.Should().NotBeEmpty();
        summary.RuntimeVariables.Should().Contain("DEV_PORT");
        summary.BestPractices.Should().NotBeEmpty();
        summary.PolicyDescription.Should().Contain("三层架构");
    }

    [Fact]
    public async Task ValidateImageDirectoryNameAsync_ValidName_ShouldPass()
    {
        // Arrange
        var validName = "myapp-20240315-1430";

        // Act
        var result = await _imagePermissionService.ValidateImageDirectoryNameAsync(validName);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
        result.ParsedInfo.Should().NotBeNull();
        result.ParsedInfo!.Prefix.Should().Be("myapp");
        result.ParsedInfo.TimeStamp.Should().Be("20240315-1430");
        result.ParsedInfo.IsStandardFormat.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateImageDirectoryNameAsync_InvalidName_ShouldFail()
    {
        // Arrange
        var invalidName = "myapp-invalid-format";

        // Act
        var result = await _imagePermissionService.ValidateImageDirectoryNameAsync(invalidName);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
        result.SuggestedName.Should().NotBeNullOrEmpty();
        result.SuggestedName.Should().StartWith("myapp-");
        result.FormatDescription.Should().Contain("prefix-YYYYMMDD-HHMM");
    }

    [Theory]
    [InlineData("compose.yaml")]
    [InlineData("compose.yml")]
    [InlineData("docker-compose.yaml")]
    [InlineData("Dockerfile")]
    [InlineData("Dockerfile.dev")]
    [InlineData("metadata.json")]
    [InlineData(".dockerignore")]
    public async Task IsProtectedConfigFileAsync_ProtectedFiles_ShouldReturnTrue(string fileName)
    {
        // Act
        var result = await _imagePermissionService.IsProtectedConfigFileAsync(fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(".env")]
    [InlineData("custom-script.sh")]
    [InlineData("README.md")]
    [InlineData("package.json")]
    public async Task IsProtectedConfigFileAsync_NonProtectedFiles_ShouldReturnFalse(string fileName)
    {
        // Act
        var result = await _imagePermissionService.IsProtectedConfigFileAsync(fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRuntimeModifiableVariablesAsync_ShouldReturnExpectedVariables()
    {
        // Act
        var variables = await _imagePermissionService.GetRuntimeModifiableVariablesAsync();

        // Assert
        variables.Should().NotBeEmpty();
        variables.Should().Contain("DEV_PORT");
        variables.Should().Contain("DEBUG_PORT");
        variables.Should().Contain("PROJECT_NAME");
        variables.Should().Contain("WORKSPACE_PATH");
        variables.Should().Contain("CONTAINER_NAME");
        variables.Should().Contain("NETWORK_NAME");
        variables.Should().Contain("VOLUME_PREFIX");
    }

    [Fact]
    public async Task GetPermissionGuidanceAsync_ProtectedFileModification_ShouldProvideGuidance()
    {
        // Arrange
        var violation = new PermissionViolation
        {
            Type = ViolationType.ProtectedFileModification,
            Path = "compose.yaml",
            Operation = "write",
            Description = "尝试修改受保护的配置文件",
            Severity = ViolationSeverity.Error
        };

        // Act
        var guidance = await _imagePermissionService.GetPermissionGuidanceAsync(violation);

        // Assert
        guidance.Should().NotBeNull();
        guidance.Violation.Should().Be(violation);
        guidance.DetailedExplanation.Should().NotBeNullOrEmpty();
        guidance.DesignRationale.Should().Contain("三层架构");
        guidance.FixSteps.Should().NotBeEmpty();
        guidance.Alternatives.Should().Contain(alt => alt.Contains("Custom/"));
        guidance.Examples.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPermissionGuidanceAsync_DirectoryRename_ShouldProvideGuidance()
    {
        // Arrange
        var violation = new PermissionViolation
        {
            Type = ViolationType.DirectoryRename,
            Path = _testImagePath,
            Operation = "rename",
            Description = "尝试重命名镜像目录",
            Severity = ViolationSeverity.Critical
        };

        // Act
        var guidance = await _imagePermissionService.GetPermissionGuidanceAsync(violation);

        // Assert
        guidance.Should().NotBeNull();
        guidance.DetailedExplanation.Should().Contain("映射关系");
        guidance.DesignRationale.Should().Contain("命名约定");
        guidance.FixSteps.Should().Contain(step => step.Contains("不要直接重命名"));
        guidance.Alternatives.Should().Contain(alt => alt.Contains("deck build"));
    }

    [Theory]
    [InlineData("myapp")]
    [InlineData("myapp-2024")]
    [InlineData("myapp-invalid-format")]
    [InlineData("")]
    [InlineData("myapp-20240315")]
    [InlineData("myapp-2024-03-15-1430")]
    public async Task ValidateImageDirectoryNameAsync_VariousInvalidFormats_ShouldFail(string invalidName)
    {
        // Act
        var result = await _imagePermissionService.ValidateImageDirectoryNameAsync(invalidName);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void FilePermissionResult_Properties_ShouldInitializeCorrectly()
    {
        // Act
        var result = new FilePermissionResult
        {
            FilePath = "test.txt",
            Operation = FileOperation.Write,
            Permission = PermissionLevel.Allowed
        };

        // Assert
        result.FilePath.Should().Be("test.txt");
        result.Operation.Should().Be(FileOperation.Write);
        result.Permission.Should().Be(PermissionLevel.Allowed);
        result.IsAllowed.Should().BeTrue();
        result.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DirectoryPermissionResult_Properties_ShouldInitializeCorrectly()
    {
        // Act
        var result = new DirectoryPermissionResult
        {
            DirectoryPath = "/path/to/dir",
            Operation = DirectoryOperation.Create,
            Permission = PermissionLevel.Warning
        };

        // Assert
        result.DirectoryPath.Should().Be("/path/to/dir");
        result.Operation.Should().Be(DirectoryOperation.Create);
        result.Permission.Should().Be(PermissionLevel.Warning);
        result.IsAllowed.Should().BeTrue();  // Warning still allows
    }

    [Fact]
    public void EnvPermissionResult_IsValid_ShouldReflectDeniedChanges()
    {
        // Act
        var resultWithDenied = new EnvPermissionResult();
        resultWithDenied.DeniedChanges["TEST"] = "value";

        var resultWithoutDenied = new EnvPermissionResult();
        resultWithoutDenied.AllowedChanges["TEST"] = "value";

        // Assert
        resultWithDenied.IsValid.Should().BeFalse();
        resultWithoutDenied.IsValid.Should().BeTrue();
    }

    private void CreateTestFiles()
    {
        // 创建受保护的配置文件
        File.WriteAllText(Path.Combine(_testImagePath, "compose.yaml"), "version: '3.8'\nservices:\n  app:\n    build: .");
        File.WriteAllText(Path.Combine(_testImagePath, "Dockerfile"), "FROM node:18\nWORKDIR /app");
        File.WriteAllText(Path.Combine(_testImagePath, "metadata.json"), "{}");

        // 创建环境变量文件
        File.WriteAllText(Path.Combine(_testImagePath, ".env"), "DEV_PORT=3000\nNODE_VERSION=18");

        // 创建普通文件
        File.WriteAllText(Path.Combine(_testImagePath, "README.md"), "# Test App");
        File.WriteAllText(Path.Combine(_testImagePath, "custom-script.sh"), "#!/bin/bash\necho 'test'");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }
}