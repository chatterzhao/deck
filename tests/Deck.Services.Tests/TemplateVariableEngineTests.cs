using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Deck.Core.Interfaces;
using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Deck.Services.Tests;

public class TemplateVariableEngineTests : IDisposable
{
    private readonly ITemplateVariableEngine _templateVariableEngine;
    private readonly ILogger<TemplateVariableEngine> _logger;
    private readonly string _testDirectory;

    public TemplateVariableEngineTests()
    {
        _logger = Substitute.For<ILogger<TemplateVariableEngine>>();
        _templateVariableEngine = new TemplateVariableEngine(_logger);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"deck-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void ReplaceVariables_WithEmptyContent_ShouldReturnEmptyContent()
    {
        // Arrange
        var content = "";
        var variables = new Dictionary<string, string> { { "VAR", "value" } };

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceVariables_WithNullVariables_ShouldReturnOriginalContent()
    {
        // Arrange
        var content = "Hello ${NAME}";
        IDictionary<string, string>? variables = null;

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void ReplaceVariables_WithEmptyVariables_ShouldReturnOriginalContent()
    {
        // Arrange
        var content = "Hello ${NAME}";
        var variables = new Dictionary<string, string>();

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void ReplaceVariables_WithDollarFormat_ShouldReplaceVariables()
    {
        // Arrange
        var content = "Hello ${NAME}, you are ${AGE} years old";
        var variables = new Dictionary<string, string>
        {
            { "NAME", "Alice" },
            { "AGE", "25" }
        };

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().Be("Hello Alice, you are 25 years old");
    }

    [Fact]
    public void ReplaceVariables_WithDoubleBraceFormat_ShouldReplaceVariables()
    {
        // Arrange
        var content = "Hello {{NAME}}, you are {{AGE}} years old";
        var variables = new Dictionary<string, string>
        {
            { "NAME", "Bob" },
            { "AGE", "30" }
        };

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().Be("Hello Bob, you are 30 years old");
    }

    [Fact]
    public void ReplaceVariables_WithMixedFormats_ShouldReplaceAllVariables()
    {
        // Arrange
        var content = "Hello ${NAME}, also known as {{NICKNAME}}";
        var variables = new Dictionary<string, string>
        {
            { "NAME", "Charlie" },
            { "NICKNAME", "Chuck" }
        };

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().Be("Hello Charlie, also known as Chuck");
    }

    [Fact]
    public void ReplaceVariables_WithMissingVariables_ShouldKeepOriginalPlaceholders()
    {
        // Arrange
        var content = "Hello ${NAME}, you are ${AGE} years old";
        var variables = new Dictionary<string, string>
        {
            { "NAME", "Alice" }
            // AGE is missing
        };

        // Act
        var result = _templateVariableEngine.ReplaceVariables(content, variables);

        // Assert
        result.Should().Be("Hello Alice, you are ${AGE} years old");
    }

    [Fact]
    public async Task ReplaceVariablesInFileAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "non-existent.txt");
        var variables = new Dictionary<string, string> { { "VAR", "value" } };

        // Act
        var result = await _templateVariableEngine.ReplaceVariablesInFileAsync(filePath, variables);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("文件不存在");
    }

    [Fact]
    public async Task ReplaceVariablesInFileAsync_WithValidFile_ShouldReplaceVariables()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var originalContent = "Hello ${NAME}!";
        var variables = new Dictionary<string, string> { { "NAME", "World" } };
        
        await File.WriteAllTextAsync(filePath, originalContent);

        // Act
        var result = await _templateVariableEngine.ReplaceVariablesInFileAsync(filePath, variables);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Changed.Should().BeTrue();
        
        var fileContent = await File.ReadAllTextAsync(filePath);
        fileContent.Should().Be("Hello World!");
    }

    [Fact]
    public async Task ReplaceVariablesInFileAsync_WithNoVariables_ShouldNotChangeFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var originalContent = "Hello World!";
        var variables = new Dictionary<string, string>();
        
        await File.WriteAllTextAsync(filePath, originalContent);

        // Act
        var result = await _templateVariableEngine.ReplaceVariablesInFileAsync(filePath, variables);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Changed.Should().BeFalse();
        
        var fileContent = await File.ReadAllTextAsync(filePath);
        fileContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task ReplaceVariablesInDirectoryAsync_WithNonExistentDirectory_ShouldReturnFailure()
    {
        // Arrange
        var directoryPath = Path.Combine(_testDirectory, "non-existent");
        var variables = new Dictionary<string, string> { { "VAR", "value" } };

        // Act
        var result = await _templateVariableEngine.ReplaceVariablesInDirectoryAsync(directoryPath, variables);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("目录不存在");
    }

    [Fact]
    public async Task ReplaceVariablesInDirectoryAsync_WithValidDirectory_ShouldReplaceVariablesInAllFiles()
    {
        // Arrange
        var directoryPath = Path.Combine(_testDirectory, "test-dir");
        Directory.CreateDirectory(directoryPath);
        
        var file1Path = Path.Combine(directoryPath, "file1.txt");
        var file2Path = Path.Combine(directoryPath, "file2.txt");
        var subDirPath = Path.Combine(directoryPath, "subdir");
        Directory.CreateDirectory(subDirPath);
        var file3Path = Path.Combine(subDirPath, "file3.txt");
        
        await File.WriteAllTextAsync(file1Path, "Hello ${NAME}!");
        await File.WriteAllTextAsync(file2Path, "Hi {{NAME}}!");
        await File.WriteAllTextAsync(file3Path, "Greetings ${NAME} and {{NAME}}!");
        
        var variables = new Dictionary<string, string> { { "NAME", "World" } };

        // Act
        var result = await _templateVariableEngine.ReplaceVariablesInDirectoryAsync(directoryPath, variables);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.FileResults.Should().HaveCount(3);
        
        var content1 = await File.ReadAllTextAsync(file1Path);
        var content2 = await File.ReadAllTextAsync(file2Path);
        var content3 = await File.ReadAllTextAsync(file3Path);
        
        content1.Should().Be("Hello World!");
        content2.Should().Be("Hi World!");
        content3.Should().Be("Greetings World and World!");
    }
}