using Deck.Core.Models;
using Deck.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deck.Services.Tests;

/// <summary>
/// 配置验证服务测试
/// </summary>
public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator;

    public ConfigurationValidatorTests()
    {
        _validator = new ConfigurationValidator(NullLogger<ConfigurationValidator>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_WithValidConfig_ShouldReturnValid()
    {
        // Arrange
        var config = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/chatterzhao/deck-templates.git",
                Branch = "main",
                CacheTtl = "24h",
                AutoUpdate = true
            }
        };

        // Act
        var result = await _validator.ValidateAsync(config);

        // Assert
        result.Should().NotBeNull();
        // 注意：网络连接可能失败，所以不严格要求 IsValid = true
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRemoteTemplatesAsync_WithEmptyRepository_ShouldReturnInvalid()
    {
        // Arrange
        var config = new RemoteTemplatesConfig
        {
            Repository = "",
            Branch = "main"
        };

        // Act
        var result = await _validator.ValidateRemoteTemplatesAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("模板仓库URL不能为空"));
    }

    [Fact]
    public async Task ValidateRemoteTemplatesAsync_WithInvalidUrl_ShouldReturnInvalid()
    {
        // Arrange
        var config = new RemoteTemplatesConfig
        {
            Repository = "not-a-valid-url",
            Branch = "main"
        };

        // Act
        var result = await _validator.ValidateRemoteTemplatesAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("无效的模板仓库URL格式"));
    }

    [Fact]
    public async Task ValidateRemoteTemplatesAsync_WithInvalidCacheTtl_ShouldReturnWarning()
    {
        // Arrange
        var config = new RemoteTemplatesConfig
        {
            Repository = "https://github.com/chatterzhao/deck-templates.git",
            Branch = "main",
            CacheTtl = "invalid-ttl"
        };

        // Act
        var result = await _validator.ValidateRemoteTemplatesAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // 只是警告，不是错误
        result.Warnings.Should().Contain(w => w.Contains("缓存TTL格式可能无效"));
    }

    [Fact]
    public async Task ValidateRemoteTemplatesAsync_WithBranchContainingSpaces_ShouldReturnInvalid()
    {
        // Arrange
        var config = new RemoteTemplatesConfig
        {
            Repository = "https://github.com/chatterzhao/deck-templates.git",
            Branch = "main branch", // 包含空格
            CacheTtl = "24h"
        };

        // Act
        var result = await _validator.ValidateRemoteTemplatesAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("分支名称不能包含空格"));
    }

    [Fact]
    public async Task GetRepairSuggestionsAsync_WithEmptyRepository_ShouldReturnFix()
    {
        // Arrange
        var config = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "",
                Branch = "main"
            }
        };

        // Act
        var fixes = await _validator.GetRepairSuggestionsAsync(config);

        // Assert
        fixes.Should().NotBeEmpty();
        fixes.Should().Contain(f => f.Issue.Contains("模板仓库URL为空") && f.CanAutoFix);
    }

    [Fact]
    public async Task GetRepairSuggestionsAsync_WithEmptyBranch_ShouldReturnFix()
    {
        // Arrange
        var config = new DeckConfig
        {
            RemoteTemplates = new RemoteTemplatesConfig
            {
                Repository = "https://github.com/chatterzhao/deck-templates.git",
                Branch = ""
            }
        };

        // Act
        var fixes = await _validator.GetRepairSuggestionsAsync(config);

        // Assert
        fixes.Should().NotBeEmpty();
        fixes.Should().Contain(f => f.Issue.Contains("模板仓库分支为空") && f.CanAutoFix);
    }

    [Theory]
    [InlineData("24h", true)]
    [InlineData("30m", true)]  
    [InlineData("7d", true)]
    [InlineData("invalid", false)]
    [InlineData("24", false)]
    [InlineData("h24", false)]
    public async Task ValidateRemoteTemplatesAsync_CacheTtlFormats_ShouldValidateCorrectly(string cacheTtl, bool shouldBeValid)
    {
        // Arrange
        var config = new RemoteTemplatesConfig
        {
            Repository = "https://github.com/chatterzhao/deck-templates.git",
            Branch = "main",
            CacheTtl = cacheTtl
        };

        // Act
        var result = await _validator.ValidateRemoteTemplatesAsync(config);

        // Assert
        if (shouldBeValid)
        {
            result.Warnings.Should().NotContain(w => w.Contains("缓存TTL格式可能无效"));
        }
        else
        {
            result.Warnings.Should().Contain(w => w.Contains("缓存TTL格式可能无效"));
        }
    }
}